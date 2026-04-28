+++
title = "Inner Loop vs Outer Loop"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 30
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/10-collaboration-and-process/3-inner-loop-vs-outer-loop.html)

[Se presentationen på svenska](/presentations/course-book/10-collaboration-and-process/3-inner-loop-vs-outer-loop-swe.html)

---

A developer's productivity is dominated by feedback latency. Every time a code change is made, some interval of time passes before the developer learns whether that change is correct, useful, or broken. That interval—measured from keystroke to verifiable answer—governs how many ideas can be tested in an hour, how willing a developer is to refactor, and how confidently a team can ship. Two distinct cycles produce that feedback at very different speeds, and writing software well requires understanding both. The inner loop runs locally and answers narrow questions in seconds; the outer loop runs on shared infrastructure and answers integration questions in minutes. Optimising the inner loop is the highest-leverage productivity investment a developer can make, and several tools — hot reload, fast unit tests, in-memory data stores, and dev containers — exist specifically to shorten it.

## The two loops of software development

Every change a developer makes travels through two feedback cycles. The first runs entirely on the developer's own machine and produces an answer in seconds. The second runs on shared infrastructure, involves the rest of the team, and produces an answer in minutes. Both cycles are necessary, but they answer different questions and operate on different timescales.

### The inner loop

The **inner loop** is the fast, local development cycle where a developer edits code, runs it (via `dotnet run` or similar), and verifies changes instantly without waiting for CI/CD; it emphasizes rapid feedback and iteration. Concretely, this is the edit-build-run-test cycle that happens on the developer's laptop, ideally completing in seconds to single-digit minutes.

The inner loop answers narrow, immediate questions. *Does this method compile?* *Does this test still pass after the refactor?* *Does the endpoint return the JSON shape the front-end expects?* These are the questions a developer asks dozens or hundreds of times in a productive day. Each iteration is short, cheap, and private—nothing leaves the local machine until the developer is satisfied.

A typical inner-loop iteration looks like this. The developer edits a file in the editor. The build runs, either invoked manually with `dotnet build` or triggered automatically by a watcher. The application starts, or a unit test runner executes the relevant test. The developer reads the result—a passing test, a stack trace, a rendered page—and edits again. Most of this happens without the developer thinking about the cycle at all. That invisibility is the goal.

### The outer loop

The **outer loop** is the slower, automated validation cycle triggered by pushing code (e.g., to GitHub), where CI/CD pipelines build, test, and deploy changes; it provides confidence that changes work across all environments before reaching production. This is the commit-push-CI-deploy cycle that happens on shared infrastructure, typically completing in minutes rather than seconds.

The outer loop answers broader, integrative questions. *Does the code build on a clean machine?* *Do the integration tests pass against a real database?* *Does the container image still start when deployed to a staging environment?* These questions require shared resources—a build agent identical to the production host, a real Postgres instance, the deployment target—and they require coordination with the rest of the team's work on the same branch.

The outer loop runs through a [pipeline](/course-book/8-devops-and-delivery/3-pipelines-as-code/), defined as code in the repository, triggered by `git push` or by opening a [pull request](/course-book/10-collaboration-and-process/2-branching-pull-requests-and-code-review/). It is the team's shared safety net: the place where work converges, where assumptions about "it builds on my machine" are tested against the team's reproducible environment, and where the merge to `main` is gated.

### Why the distinction matters

The two loops are not redundant. Each catches problems the other cannot. The inner loop catches typos, broken unit tests, and wrong-shape API responses while the developer is still holding the change in working memory. The outer loop catches environment drift, missing dependencies, and integration failures that only appear when the code meets a system the developer cannot fully reproduce locally. A well-run team uses both, and uses each for the questions it is good at answering.

The mistake worth avoiding is treating the outer loop as a substitute for the inner loop. When the inner loop is broken or slow, developers compensate by pushing half-finished work to CI and using the pipeline as their build machine. The pipeline becomes a bottleneck, builds queue up, and a fifteen-second mistake takes ten minutes to discover. This pattern is recoverable but expensive—and it almost always indicates that the inner loop deserves attention.

## Why optimizing the inner loop matters

A developer runs the inner loop hundreds of times a day. A team of six developers runs it tens of thousands of times a week. Every second saved per iteration compounds across that volume into hours of recovered focus, more refactors attempted, and more hypotheses tested.

The math is unforgiving in the other direction as well. A thirty-second build feels fine. A two-minute build feels like an interruption. A five-minute build is long enough that the developer will switch tabs, lose the thread of the change, and return distracted. The cost is not the five minutes themselves—it is the broken concentration that costs another ten minutes of re-orientation. Across a week, a slow inner loop quietly erases an entire workday per developer.

Speed also changes what a developer is willing to attempt. With a sub-second test feedback cycle, a developer will write the test first, watch it fail, write the implementation, and watch it pass—the discipline of test-driven development is sustainable. With a forty-second test feedback cycle, that same developer will write the implementation first, then the test, then run both at the end of the change, hoping for the best. The slow cycle quietly degrades engineering practice without anyone deciding to lower the bar.

The inner loop is also where exploratory work happens. A developer trying three approaches to a tricky problem needs to throw away two of them. If each attempt costs a minute to verify, three attempts cost three minutes—nothing. If each attempt costs eight minutes, three attempts cost half an hour, and the developer will commit to the first approach that works rather than the best one. Inner-loop speed is, indirectly, code-quality investment.

## Tools that shorten the inner loop

Several tools and patterns specifically target the inner loop. They are not interchangeable; each addresses a different source of latency.

### Hot reload

**Hot reload** is a development feature that automatically rebuilds and restarts the running application when source code changes, eliminating the manual `stop → edit → run` cycle and accelerating the inner feedback loop. In .NET, this is exposed through `dotnet watch`, which monitors the source tree and applies changes to a running application without a full restart where possible.

The mechanism collapses three steps—save, stop, run again—into one. The developer saves; the watcher detects the change; the application reloads with the new code; the next browser refresh shows the updated behavior. For UI work, where the developer is iterating on visible output, hot reload changes the felt experience of programming. The browser window becomes a live mirror of the source file.

Figure 1: Starting a .NET application in watch mode

```bash
dotnet watch run
```

Hot reload has limits. Some changes—altered method signatures, new dependency injection registrations, migrations—require a full restart, and the watcher will fall back to a rebuild rather than patch in place. The developer experience is not magic; it is a careful trade-off between patch-and-continue and rebuild-and-restart, with the watcher choosing the cheapest option that produces correct behavior.

### Fast unit tests

Unit tests that run in milliseconds are a different kind of inner-loop tool. They give the developer a verifiable answer ("the function does what I claim it does") without involving the application as a whole. A test suite of a thousand fast unit tests can run in under five seconds. The developer can run the entire suite on save and treat any failure as an immediate, local signal.

Fast unit tests stay fast by avoiding I/O. They do not touch the disk, the network, or a real database. They exercise pure functions and isolated classes, with collaborators replaced by test doubles. The cost of this discipline is that unit tests cannot validate integration—they cannot prove the code works against a real Postgres instance, only that the code works against an in-memory fake. That is a trade-off the team accepts: integration is the outer loop's job; correctness of individual units is the inner loop's job.

### In-memory databases for tests

For tests that need data-access logic but cannot afford the latency of a real database, in-memory database providers offer a middle ground. Entity Framework Core's `InMemoryDatabase` provider, or SQLite running in `:memory:` mode, gives the test code something that responds to LINQ queries and tracks entities without a network round-trip.

The in-memory option is fast enough to run on every save, and it catches a class of bugs—wrong joins, missing `Include` calls, wrong query shape—that pure unit tests cannot. The trade-off is that the in-memory provider is not the production database. SQL features like JSON columns, full-text search, or vendor-specific functions behave differently or not at all. In-memory tests are a useful intermediate signal, not a substitute for an outer-loop test against the real database engine.

### Dev containers

A **dev container** is a containerized development environment (e.g., via Docker and VS Code Dev Containers extension) that ensures consistent tooling, dependencies, and configuration across team members' machines, reducing "works on my machine" problems.

The problem dev containers solve is environmental drift. One developer has .NET 8 installed; another has .NET 9. One has Node 18; another has Node 20. The Postgres versions differ. The local certificates differ. Each developer's machine is a slightly different production target, and bugs that depend on those differences cost hours to diagnose. A dev container declares the entire toolchain—runtimes, compilers, databases, formatters, linters—in a single configuration file checked into the repository.

Figure 2: Minimal `.devcontainer/devcontainer.json`

```json
{
  "name": "API dev",
  "image": "mcr.microsoft.com/devcontainers/dotnet:8.0",
  "features": {
    "ghcr.io/devcontainers/features/docker-in-docker:2": {}
  },
  "postCreateCommand": "dotnet restore"
}
```

When a new developer clones the repository, the IDE detects the dev container configuration, builds (or pulls) the image, and opens the editor inside that container. The toolchain on the host machine is irrelevant. The dev container is the toolchain. Onboarding shrinks from "spend a day installing things and asking Slack" to "open the repo, click Reopen in Container, write code."

Dev containers also bring inner-loop tooling to any IDE that supports the protocol. The container exposes a consistent runtime; the IDE—VS Code, JetBrains Rider, others—connects to it. The same dev container works on Windows, macOS, and Linux because the container itself is Linux, and the host's role is reduced to running Docker.

The trade-off is the indirection layer. File watchers cross a virtual filesystem boundary; ports are forwarded; debugging crosses a container edge. On a fast machine with good Docker integration, this is invisible. On a slow machine, or with a heavy image, it adds noticeable latency to the inner loop and undoes some of the speed it was meant to protect. The dev container is a tool, not a default; whether to adopt it is a judgement about consistency-versus-speed for the specific team.

## The boundary conversation

A live tension runs through every test suite: deciding which checks belong in the inner loop and which earn their slot in the outer loop is a continuous engineering judgement.

The inner loop is constrained by speed. Anything that runs on every save must complete in seconds. The outer loop is constrained by infrastructure cost and by how long the team will tolerate a yellow check mark on a pull request. Anything that takes minutes belongs in the outer loop—but the outer loop is a worse place to learn about a problem, because the developer has already moved on.

The default heuristic is to push every check toward the inner loop until something forces it outward. A check earns its outer-loop slot when one of three conditions holds: it depends on infrastructure the developer cannot run locally (a real cloud database, a third-party API), it takes long enough that running it on save would break flow (a full integration suite), or it validates a property that is meaningful only in a production-like environment (deployment health, smoke tests against a staging URL).

| Check type | Loop | Why |
|------------|------|-----|
| Compile, type-check | Inner | Sub-second; runs on save via the IDE |
| Unit tests (no I/O) | Inner | Sub-second; high-volume signal |
| In-memory or SQLite-backed data tests | Inner | Fast enough; catches query-shape bugs |
| Format and lint checks | Inner (and outer as a gate) | Inner for speed, outer to enforce |
| Integration tests against a real database | Outer | Requires shared infra; too slow for save |
| End-to-end browser tests | Outer | Slow, flaky, requires a running app |
| Build of production container image | Outer | Reproducibility matters more than speed |
| Deploy and smoke-test | Outer | Requires a deployment target |

The conversation is rarely settled once. As the codebase grows, slow tests creep into the inner loop and developers stop running them locally. Fast tests are sometimes promoted from outer to inner once a faster alternative (an in-memory provider, a contract test) becomes available. The team's shared judgement about where each check belongs is itself a piece of engineering practice that needs maintenance.

For hands-on practice with the local development cycle that the inner loop describes, see the exercises at [/exercises/15-code-collaboration/](/exercises/15-code-collaboration/), where the local edit-build-run cycle around `dotnet run` is the foundation of the [commit](/course-book/10-collaboration-and-process/1-version-control-with-git/) and review workflow.

## Summary

A developer's day is governed by feedback latency, and that latency comes in two distinct cycles. The inner loop is the fast, local edit-build-run-test cycle that runs on the developer's own machine in seconds; it answers narrow questions about correctness while the change is still in working memory. The outer loop is the slower commit-push-CI-deploy cycle that runs on shared infrastructure in minutes; it answers integration questions that require a reproducible environment and the team's converged work. Optimizing the inner loop is high-leverage because every saved second compounds across thousands of iterations a week, and because slow cycles quietly degrade engineering practice. Hot reload, fast unit tests, in-memory database providers, and dev containers each shorten the inner loop along a different axis—live patching, no-I/O testing, lightweight data-layer testing, and reproducible toolchains. The boundary between what belongs in the inner loop and what earns its slot in the outer loop is a continuous engineering judgement, not a fixed rule, and a healthy team revisits it as the codebase and the tools evolve.
