+++
title = "Pipelines as Code"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 30
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/8-devops-and-delivery/3-pipelines-as-code.html)

[Se presentationen på svenska](/presentations/course-book/8-devops-and-delivery/3-pipelines-as-code-swe.html)

---

A team that ships code every day cannot afford a release process that lives only in someone's head or in a sequence of clicks inside a portal. Build steps configured through a portal UI cannot be code-reviewed, cannot be diffed, and cannot be reproduced when the portal redesigns its menus. The remedy is to express the entire build-and-release process as a file that lives in the repository alongside the application code. This chapter introduces the vocabulary and mechanics of that approach in [GitHub Actions](https://docs.github.com/en/actions), the platform used by the companion exercise.

## Why the pipeline belongs in the repository

A portal-driven release process accumulates configuration in a place that is invisible to most of the team. A developer who joins the project six months later cannot read the production deployment logic by browsing the source tree — they have to know which portal to open, which project to inspect, and which tabs hide the relevant settings. When a build breaks, there is no commit history to bisect. When the release machinery itself needs to change, no pull request gates the change against review. And when the portal's vendor renames a tab or removes a feature, the team's institutional knowledge silently rots.

The phrase **pipeline as code** describes the alternative: the pipeline definition is stored in the same repository as the application, in a plain-text file, and is applied by reading that file. The same review process that gates application changes now gates release changes. A revert of a bad pipeline change is a `git revert` away. A new team member reads the pipeline by reading the file. The pipeline becomes a first-class artifact of the project rather than a configuration that exists in a separate, lossy medium.

This shift carries an obvious cultural implication. Pipeline edits are now visible to everyone with read access to the repository, and breaking the pipeline now leaves a fingerprint in the commit log. That visibility is the point. It also nudges teams toward the [continuous integration (CI)](/course-book/8-devops-and-delivery/2-ci-vs-cd/) practice the previous chapter introduced — small, frequent merges to the trunk become realistic only when the integration check is automated, defined in code, and reliably triggered on every push.

## The GitHub Actions vocabulary

Different platforms (GitLab CI, Azure Pipelines, CircleCI, Jenkins) all converge on roughly the same conceptual model, but the specific names differ. The remainder of this chapter uses GitHub Actions terminology because that is what the companion exercise uses, and because GitHub Actions is the most common starting point for new projects.

A **pipeline** is a sequence of automated stages (build, test, deploy) that takes source code and produces a running application; each stage depends on the previous one succeeding, and failure at any stage halts the pipeline. The word "pipeline" is the platform-neutral term used in the [DevOps](/course-book/8-devops-and-delivery/1-the-devops-philosophy/) literature.

A **workflow** (in GitHub Actions) is an automated process defined in a YAML file (`.github/workflows/*.yml`) that triggers on events (push, pull request, schedule) and orchestrates jobs to build, test, and deploy code. In GitHub Actions terminology, the workflow is the file; the pipeline is the abstract concept the file expresses. A repository can hold many workflow files, and each one is independent — a `ci.yml` for build-and-test, a `release.yml` for production deploys, a `nightly.yml` for scheduled long-running tasks.

A **job** (in GitHub Actions) is a set of steps that run on the same runner; jobs can run in parallel or sequentially, and a workflow often contains multiple jobs (e.g., build job, test job, deploy job). Jobs are the unit of parallelism: a workflow with three independent jobs runs them concurrently on three separate runners by default. A job that needs the output of another job declares the dependency with `needs:`, and the platform schedules them in the right order.

A **step** (in GitHub Actions) is a single command or action within a job; it runs sequentially in the order defined, and failure in a step can halt the job unless explicitly ignored. Steps within a job share the same filesystem and environment, so files written in one step are visible to the next. Steps across different jobs do not share a filesystem — each job starts on a fresh runner, which is why artifacts (covered below) exist.

A **runner** is a machine (GitHub-hosted or self-hosted) that executes the steps of a GitHub Actions job; GitHub-hosted runners are provided by GitHub (Ubuntu, Windows, macOS), while self-hosted runners are managed by the organization. A runner is provisioned for the duration of a single job and discarded afterward. The fresh-machine guarantee is part of why the pipeline is reproducible: there is no state from a previous run to pollute the next one. The trade-off is that anything the job needs (the source code, dependencies, build tools) must be installed by the steps themselves.

An **action** (in GitHub) is a reusable unit of code published to the GitHub Marketplace; actions encapsulate common tasks (checkout code, set up a runtime, deploy) and are called by name in workflow steps (e.g., `actions/checkout@v4`). An action is the GitHub-specific equivalent of a shell function — a named, parameterised block that someone else has written and that the workflow can invoke without copying its implementation. Most workflow steps are either a shell command (`run:`) or an action invocation (`uses:`).

The vocabulary fits together hierarchically: a workflow contains jobs, a job runs on a runner and contains steps, and a step is either a shell command or an action call.

## Triggers: when a workflow runs

A workflow runs only when an event triggers it. The `on:` field at the top of the workflow file lists the events the workflow listens for. Four triggers cover almost every common case:

- **`push`** — fires whenever a commit lands on a specified branch. The default CI trigger; every push to `main` rebuilds and re-tests.
- **`pull_request`** — fires when a pull request is opened, updated, or reopened. The pull-request gate runs the build against the proposed merge before any human review begins.
- **`workflow_dispatch`** — adds a manual "Run workflow" button in the GitHub UI. Useful for production deploys that should not happen automatically, or for re-running a flaky job without pushing a fake commit.
- **`schedule`** — fires on a cron expression (`0 2 * * *` for 02:00 every day). Useful for nightly integration tests, dependency-update checks, or housekeeping tasks.

A single workflow can listen for several triggers at once, and triggers can be filtered by branch, by path, or by label, so the same file can express "build on every PR, but deploy only on push to main."

## Code review for the pipeline itself

Once the pipeline is a file in the repository, every change to it flows through the same pull-request review the team already uses for application code. A reviewer reading a diff of `ci.yml` sees exactly what is changing: a new step being added, a runner image being upgraded, a deploy command being modified. The reviewer can ask the same questions they would ask of an application change — is this safe, is this tested, does this match our conventions — and can block the merge until the answers are satisfactory.

This matters most when a pipeline change is risky. Adding a step that writes to production, rotating a credential reference, or changing the order in which gates run — these are the changes that break releases at 02:00. Subjecting them to review is the cheapest insurance the practice provides. It also creates a paper trail: the commit history of `.github/workflows/` is the audit log of every release-process change the team has made.

## Artifacts: passing files between jobs

A **workflow run** is a single execution of a workflow, triggered by a single event. Each job in the run starts on a fresh runner with an empty filesystem, which means a job cannot read files produced by an earlier job by reading the disk directly — the disk does not carry over. The mechanism for moving files between jobs is the **artifact**.

An **artifact** (in CI) is a file or collection of files (compiled binaries, [Docker images](/course-book/7-containers/2-images-and-layers/), test reports) produced by a pipeline job that can be stored, passed to later jobs, or published for download. A `build` job uploads its compiled output as an artifact; a downstream `test` job downloads the same artifact and runs against it. The artifact is also retained on the workflow run page in the GitHub UI for a configurable period (90 days by default), which makes it easy to download a build that shipped six weeks ago for forensic inspection.

Artifacts and Docker images serve overlapping but distinct purposes. An artifact is a workflow-internal mechanism — a way to move files between jobs of the same run. A Docker image is an external mechanism — a portable, content-addressed package pushed to a registry, intended to be pulled by other systems (a Container App, another pipeline, a developer's laptop). A pipeline that builds a container often does both: it uploads the build log and test report as workflow artifacts, and it pushes the production image to a registry as the actual deliverable.

## Worked example: a minimal CI workflow

The companion exercise [CI/CD to Container Apps](/exercises/3-deployment/9-cicd-to-container-apps/) walks through three increasingly capable pipelines. The first pipeline is the smallest one that does useful work: it builds a .NET app, runs its tests, and produces a Docker image. Figure 1 shows the file in full.

Figure 1: `.github/workflows/ci.yml` — minimal build-test-image pipeline

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet build --configuration Release

  test:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test --configuration Release

  docker-build:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKERHUB_USERNAME }}
          password: ${{ secrets.DOCKERHUB_TOKEN }}
      - uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: myorg/myapp:${{ github.sha }}
```

The `on:` block declares two triggers. The `jobs:` block defines three jobs that run in sequence: `build` produces compiled binaries, `test` runs the test suite, and `docker-build` packages the application into a container image and pushes it to Docker Hub. The `needs:` keyword chains the jobs — `test` will not start until `build` finishes successfully, and `docker-build` will not start until `test` passes. Each job runs on a fresh `ubuntu-latest` runner, which is why every job repeats `actions/checkout@v4` and `actions/setup-dotnet@v4`: the previous job's runner no longer exists. The `${{ secrets.DOCKERHUB_TOKEN }}` reference reads a value injected from the repository's secret store, never visible in the file or the logs.

This pipeline is small enough to read end-to-end in under a minute, which is the property that matters most. A reviewer can see at a glance what builds, what tests, and what ships. The next chapter ([Build, Test, and Smoke Gates](/course-book/8-devops-and-delivery/4-build-test-and-smoke-gates/)) layers gates and result publishing on top of this skeleton; the chapter on [secrets and OIDC](/course-book/8-devops-and-delivery/6-pipeline-secrets-and-oidc/) replaces the long-lived `DOCKERHUB_TOKEN` with a short-lived federated credential.

## The trade-off: YAML readability degrades

The pipeline-as-code practice has one durable downside: the file format. YAML is forgiving about whitespace until it isn't, expression substitution syntax (`${{ ... }}`) accumulates quickly, and a workflow that started at 30 lines often grows past 300 as the team adds matrix builds, conditional steps, and deploy logic. At that size the file stops being scannable.

The discipline that keeps the practice working is the same discipline that keeps application code working: refactor early. GitHub Actions provides three mechanisms for keeping workflows small. **Reusable workflows** (`workflow_call`) let one workflow call another, the way a function call factors out shared logic. **Composite actions** package a sequence of steps into a single named action that lives in `.github/actions/` inside the repository. **Matrix strategies** collapse "the same job for each of these versions" from a copy-paste of N jobs into one job definition with a list of variants.

A pipeline that is hard to read is a pipeline that hides bugs. The cost of a poorly factored workflow is paid every time someone tries to add a step, debug a failure, or onboard a new engineer. Treat the workflow file with the same refactoring instincts the team applies to the application code, and the practice scales. Treat it as throwaway scripting, and the file eventually becomes the same opaque artifact the portal-driven approach was meant to escape.

## Summary

A pipeline expressed as code lives in the repository, is reviewed like any other code change, and reproduces deterministically on a fresh runner each time it executes. GitHub Actions structures the work as workflows containing jobs, jobs containing steps, and steps invoking either shell commands or pre-published actions. Triggers (`push`, `pull_request`, `workflow_dispatch`, `schedule`) determine when a workflow runs, and artifacts move files between jobs that otherwise share no filesystem. The companion CI/CD exercise builds an increasingly capable pipeline on this foundation, starting with a minimal build-test-image workflow and ending with passwordless deployment to Azure Container Apps. The single durable cost of the practice is YAML drift: workflows grow, and the team that does not refactor them ends up with the same opaque release machinery the practice was meant to avoid.
