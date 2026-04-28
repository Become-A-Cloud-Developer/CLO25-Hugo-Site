+++
title = "Continuous Integration vs Continuous Deployment"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 20
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/8-devops-and-delivery/2-ci-vs-cd.html)

[Se presentationen på svenska](/presentations/course-book/8-devops-and-delivery/2-ci-vs-cd-swe.html)

---

The acronym "CI/CD" hides three distinct practices that arrived at different points in software history and solve different problems. Continuous integration addresses the pain of merging long-lived branches. Continuous delivery addresses the pain of release-day rituals. Continuous deployment goes further still and removes the human approval entirely. Treating them as one undifferentiated blob makes it impossible to reason about what a team should adopt next, because each step assumes the previous one is in place.

This chapter separates the three. It also names the branching model and the human checkpoint that have to be present for any of them to work in practice — without those, the pipeline becomes theatre rather than engineering.

## The pain that motivated continuous integration

Consider a team where each developer works on a feature branch for two or three weeks before merging back to `main`. By the time the merge happens, the trunk has moved on. Files have been renamed, function signatures have changed, dependencies have been upgraded. The merge surfaces dozens of conflicts at once, and the integration takes a full day of fragile manual work. Worse, the branches do not just collide on text — they collide on assumptions. Two developers may have independently written the same helper function, or relied on database fields that the other one removed.

This pattern was the norm in the late 1990s, and it produced a predictable failure mode: integration risk grew non-linearly with branch age. A one-week branch was annoying to merge. A four-week branch was an event. A six-month branch became a separate product that no longer fit the trunk at all.

**Continuous integration (CI)** is a practice where developers integrate code changes into a shared repository frequently (multiple times per day); each integration is automatically built, tested, and verified to catch integration errors early. The word "continuous" is doing real work here — it is not a synonym for "automated." A team that runs builds on a nightly cron is not doing CI. A team that integrates each developer's work hourly, automatically, and rejects integrations that break the build is doing CI.

The motivating insight is that integration pain compounds. Three small daily merges are dramatically easier than one merge holding three days of divergent work, even though the line counts are identical. CI forces the small daily merges and pays down the integration cost continuously instead of letting it accumulate.

## What CI actually requires

CI is often described as "running tests on every push," but that description omits the parts that make it work. Three properties have to hold at the same time:

1. **Every commit triggers a build and test run.** Not just the commits a developer chooses to test — every commit, including the small ones, including the ones that "just" change a comment. The pipeline runs unconditionally. This is what makes CI a property of the repository rather than a habit of the developer.
2. **Every commit eventually reaches a shared trunk.** A pipeline that runs on a feature branch for six weeks before the branch merges has not actually integrated anything. The integration happens at merge time, and a passing CI on a stale branch tells you very little about whether the merge will succeed.
3. **A broken build blocks everyone.** If `main` is red, no further work can merge until it is green again. The team treats a broken build as the highest-priority interruption — not because of moralism, but because every additional commit on top of a broken trunk makes the eventual fix harder.

Property three is what most teams get wrong. It is comfortable to merge "just one more thing" while the build is broken, on the grounds that it is unrelated. The cost of doing so is that the next failure is now ambiguous — it could be the original break, the new commit, or an interaction. Discipline around the red build is the difference between CI as a practice and CI as a job that happens to run.

## The spectrum: integration, delivery, deployment

The three practices form a spectrum, each one strictly building on the previous. Skipping a step does not work.

### Continuous integration

The trunk is always buildable. Tests pass on `main`. Any developer can pull `main` at any time and have a working starting point. The pipeline runs on every commit. Whether the artifact gets deployed anywhere is a separate question — at this stage, deployment can still be a quarterly ritual handled by an operations team.

### Continuous delivery

**Continuous delivery** is a practice where code changes are automatically built, tested, and packaged so they are always in a releasable state; deployment to production is automated but gated by a manual approval step. The artifact is ready for production at all times. The pipeline could deploy it within minutes, but a human chooses when. The gate exists for non-technical reasons: a marketing launch window, a compliance review, a customer communication that must precede the change.

The crucial property is that the readiness is real. A team practicing continuous delivery should be able to point at any green commit on `main` and say "this could be in production by lunch." If the path from green build to production involves a two-week QA cycle, the team is not yet doing continuous delivery — they are doing CI plus a heavy release process.

### Continuous deployment

**Continuous deployment** is a practice where code changes that pass automated tests are automatically deployed to production without manual gates; every commit to the main branch can reach users. The human gate is gone. A green merge to `main` reaches production within minutes, automatically, without anyone clicking a button.

This requires a higher bar of automated quality assurance than continuous delivery, because no human will catch the problem the tests miss. It also requires deployment strategies that limit the blast radius of a bad change — feature flags, canary releases, fast rollback paths. Continuous deployment without those safety mechanisms is reckless rather than mature.

The progression matters. A team that adopts continuous deployment without first establishing continuous integration is automating the delivery of unreliable software at high frequency, which is worse than the slow manual process they replaced.

## Trunk-based development

A branching model that fits CI must allow daily integration. The model that does so is **trunk-based development** — a branching strategy where developers work on short-lived feature branches that are frequently merged to the main branch (the "trunk"); this enables frequent integration and reduces merge conflicts and slow feedback loops.

"Short-lived" usually means hours to two days. A branch that lives a week is already drifting. The expectation is that a developer cuts a branch from `main`, makes a focused change, opens a pull request, gets it merged, and deletes the branch — all within the same working day if possible.

Two patterns make this practical for changes that are too large to ship in a single small commit:

- **Vertical slicing.** A feature is decomposed into independently mergeable slices — a database migration first, then a backend endpoint that uses it, then a frontend that calls the endpoint. Each slice merges separately, and the trunk is never inconsistent.
- **Feature flags.** A larger change is merged in pieces hidden behind a runtime configuration switch. The code is in `main` and going through CI on every commit, but users do not see it until the flag is enabled. This separates the act of deploying from the act of releasing — two things that the older release-day model conflated.

The contrast is **GitFlow** — a model with long-lived `develop`, `release`, and `hotfix` branches. GitFlow encodes the assumption that releases are events. Trunk-based development encodes the opposite assumption: that releases are continuous and the trunk is always production-shaped.

## The pull-request gate

Continuous integration on the trunk does not mean the trunk has no quality bar. The bar moves from "release day" to the moment of merge. The mechanism is the **pull-request gate** — an automated check (build, linting, tests) that must pass before a code review can approve and merge a pull request; it prevents broken or low-quality code from reaching the trunk.

The pull-request gate is the human checkpoint in an otherwise automated flow. It runs on every PR and typically includes:

- A successful build (compilation, type checks).
- All unit tests passing.
- Static analysis (linting, security scans) reporting clean.
- At least one peer reviewer's approval.

The gate is not the same as a deployment gate. A deployment gate decides whether tested code reaches production; a PR gate decides whether code reaches the trunk at all. Conflating them is a common mistake — teams that put their main quality controls in a "pre-prod" gate after the merge end up with a red trunk, because the merge has already happened by the time anyone notices.

A well-tuned PR gate finishes in minutes. If it takes 45 minutes, developers will start batching up several days of work into a single PR to avoid waiting for the pipeline three times — which destroys the property that made CI work in the first place. Pipeline speed is therefore not a vanity metric; it is a precondition for trunk-based development.

## A worked example

Two teams ship the same feature: adding a "favourite" button to a product list.

**Team A — long-lived feature branch.** A developer cuts `feature/favourites` from `main`. The branch lives for nine days. Over that period, three other developers merge unrelated changes to `main` — a refactor of the data access layer, an upgrade of the ORM, and a new authentication helper. By the time the favourites branch is ready to merge, the data access calls in the branch use the old API, the ORM upgrade has changed how queries are written, and the new authentication helper would have replaced 40 lines of boilerplate that the branch still contains. The merge takes most of a day. Two integration bugs surface in QA the following week. The change reaches production 14 days after the original commit.

**Team B — flag-protected trunk-based.** A developer cuts `feat/favourites-table` from `main` for an hour, adds the database migration, opens a PR, gets it reviewed and merged the same morning. The next morning, a second branch adds the backend endpoint behind a feature flag `favourites_enabled=false`. It merges by lunch. Over the next three days, three more small PRs add the API client, the UI component, and the wiring — each of them merged to `main` on the day it was opened, each gated by the same PR pipeline, each going to production immediately because the team practices continuous deployment. None of them are visible to users because the flag is off. On day four, the flag is flipped to `true` for 5% of users via a canary, then expanded to 100% by the end of the day. The change reaches production four days after the original commit, and the production switch was a configuration change, not a deployment.

The two flows ship the same feature. The second one carries less integration risk, has shorter feedback cycles, and decouples the deployment from the release — which is what makes it possible to roll back the favourites button instantly without redeploying anything.

## Choosing where on the spectrum to land

The choice between continuous delivery and continuous deployment is not primarily a technical one. The technical bar is similar — automated tests, deployable artifacts, monitored production. The deciding factors are usually external:

| Factor | Favours continuous delivery | Favours continuous deployment |
|--------|------------------------------|--------------------------------|
| Regulatory regime | Audit trails require human approval per change | No mandated approval step |
| Customer communication | Releases must be announced or coordinated | Users tolerate frequent silent updates |
| Test confidence | Test suite cannot fully replace human judgement | Test coverage is high and trustworthy |
| Rollback speed | Rollbacks are slow or destructive | Rollback is automated and seconds-quick |
| Blast radius control | No feature-flag or canary infrastructure | Mature deployment strategies in place |

A team that ships an embedded firmware update to medical devices will not adopt continuous deployment regardless of how good their tests are — the rollback cost is too high and the regulatory environment forbids it. A team that ships a consumer web feature where the worst case is "the favourites button is broken for an hour" can adopt continuous deployment safely, provided the rollback path is automated.

A useful rule: do not adopt continuous deployment until rollback is faster than diagnosis. If a bad change takes three minutes to roll back and 30 minutes to diagnose, automated deployment is fine — the worst case is a three-minute outage. If rollback takes 30 minutes and diagnosis takes three minutes, a manual gate provides real value, because a human can catch the problem before it ships.

## Where this connects to the exercises

The exercises in [`/exercises/3-deployment/9-cicd-to-container-apps/`](/exercises/3-deployment/9-cicd-to-container-apps/) walk through this spectrum on a working pipeline. The first exercise stops at CI plus a manual deployment step (the developer clicks "Create new revision" in the Azure Portal — a stand-in for a manual approval gate). The second exercise extends it into continuous deployment: a green build automatically updates the Container App revision and a smoke test confirms the new version is responding before the pipeline reports success. The third exercise replaces the stored credential with OIDC federation but keeps the same delivery shape — the same continuous deployment, with a stronger trust model.

The mining ground for this chapter is therefore not theoretical. The same code travels from "build and push" to "build, push, deploy, verify" across three iterations of the same pipeline, and the differences between them are exactly the differences between CI, continuous delivery, and continuous deployment.

## Summary

Continuous integration is the practice of integrating code into a shared trunk multiple times a day, with every commit automatically built and tested and broken builds treated as blockers for the whole team. Continuous delivery extends CI by keeping every green commit in a releasable state, gated by a human approval. Continuous deployment removes the human gate and ships every green commit to production automatically. Trunk-based development is the branching model that makes any of this possible, and the pull-request gate is the human checkpoint that protects the trunk from regressions before they merge. The choice between continuous delivery and continuous deployment depends on regulatory context, rollback speed, and the maturity of the deployment safety net — not on technical capability alone. The shared origin of all three practices is the recognition that integration risk and release risk both compound non-linearly, and that paying them down continuously is cheaper than paying them down in lumps.
