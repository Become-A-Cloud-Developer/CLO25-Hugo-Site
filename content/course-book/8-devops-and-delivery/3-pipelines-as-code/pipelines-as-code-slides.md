+++
title = "Pipelines as Code"
program = "CLO"
cohort = "25"
courses = ["ACD"]
type = "slide"
date = 2026-04-28
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls = true
progress = true
history = true
center = true
+++

## Pipelines as Code
Part VIII — DevOps and Delivery

---

## Why the pipeline belongs in the repo
- Clicks in a portal cannot be **code-reviewed** or **diffed**
- A new team member cannot read the release process by reading the source
- Portal redesigns silently break institutional knowledge
- A YAML file in the repo is reviewed, reverted, and audited like any other code

---

## The GitHub Actions vocabulary
- **Workflow** — a YAML file in `.github/workflows/` that triggers on events
- **Job** — a set of steps that run together on one runner
- **Step** — a single shell command or action invocation
- **Runner** — the machine (Ubuntu, Windows, macOS) that executes the job
- **Action** — a reusable, named unit of code called from a step

---

## The hierarchy fits together
- A **workflow** contains one or more **jobs**
- A **job** runs on a **runner** and contains **steps**
- A **step** is either a shell `run:` or an `uses:` action call
- Jobs run in parallel by default; `needs:` chains them sequentially
- Each job starts on a fresh runner — no state from previous runs

---

## Triggers: when a workflow runs
- **`push`** — fires on every commit to a specified branch
- **`pull_request`** — fires when a PR is opened or updated; the gate
- **`workflow_dispatch`** — adds a manual "Run workflow" button
- **`schedule`** — fires on a cron expression (nightly tests, housekeeping)
- One workflow can listen for several triggers at once

---

## Code review for the pipeline itself
- Pipeline edits flow through the same PR review as application changes
- Risky changes (new deploy step, rotated credentials) are gated by a reviewer
- The commit history of `.github/workflows/` becomes the release-process audit log
- A bad pipeline change is one `git revert` away

---

## Artifacts: passing files between jobs
- An **artifact** is a file or set of files produced by a job
- Each job runs on a fresh runner — disk does not carry over
- Upload in the build job, download in the test job
- Stored on the workflow run page (90-day default retention) for forensic download
- Artifacts are workflow-internal; Docker images are external deliverables

---

## Worked example: minimal CI workflow
- `on: [push, pull_request]` — fires on commits to `main` and PRs
- Three jobs: `build`, `test`, `docker-build`
- `needs:` chains them; failure halts the chain
- Each job re-checks out the source — fresh runners share no state
- `${{ secrets.DOCKERHUB_TOKEN }}` is injected, never visible in logs

---

## The trade-off: YAML drift
- Workflows grow past readability fast — 30 lines become 300
- **Reusable workflows** (`workflow_call`) factor out shared logic
- **Composite actions** package step sequences into a named action
- **Matrix strategies** collapse "same job for each version" into one definition
- Refactor the workflow with the same instincts as application code

---

## Practice
- Exercise: [/exercises/3-deployment/9-cicd-to-container-apps/](/exercises/3-deployment/9-cicd-to-container-apps/)
- Three pipelines, each layering one concept on the previous
- Start with build + push, end with passwordless OIDC deploy
- Read every workflow file end-to-end before running it

---

## Questions?
