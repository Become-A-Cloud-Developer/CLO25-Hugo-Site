+++
title = "The DevOps Philosophy"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 10
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/8-devops-and-delivery/1-the-devops-philosophy.html)

[Se presentationen på svenska](/presentations/course-book/8-devops-and-delivery/1-the-devops-philosophy-swe.html)

---

Software organizations historically split the work of building applications and the work of running them between two separate groups. Developers wrote features and threw releases over a wall to operations staff, who installed, monitored, and patched them. The handoff itself became the most expensive step in delivery: each release required coordination meetings, change tickets, and late-night cutover windows, and when something broke the two groups blamed each other instead of fixing the system. This chapter introduces the philosophy that replaced that arrangement, the metrics that tell an organization whether it has succeeded, and the rest of Part VIII previews how pipelines, gates, and deployment strategies put the philosophy into practice.

## The legacy split and its handoff cost

For most of the 1990s and 2000s, a typical enterprise release looked the same. A development team finished a quarterly release, packaged the binaries, and produced a deployment runbook. An operations team scheduled a maintenance window, often on a weekend, and worked through the runbook step by step. If the deployment failed, operations rolled back to the previous version and filed a ticket. Developers learned about the failure on Monday morning, debugged what they could from log fragments, and queued a fix for the next quarterly release.

The cost of this arrangement was hidden in two places. The first was the size of each release. Because deployments were rare and risky, every release accumulated months of changes. A failure in production could trace back to any one of dozens of merged feature branches, and isolating the cause took days. The second was the incentive structure. Operations staff were measured on uptime, so they resisted change. Developers were measured on feature velocity, so they pushed for change. The two groups optimized for opposing goals, and the system as a whole optimized for neither.

This pattern produced predictable failure modes. Production incidents took hours or days to resolve because the people who understood the code were not the people with access to the servers. Releases were postponed because operations could not absorb the risk on top of normal maintenance load. Hotfixes required heroics — pulling engineers out of meetings, opening emergency change tickets, and skipping the test suites that the regular pipeline would have run. None of these failures came from individual incompetence; they came from a structure that placed a wall between writing code and running it.

## What DevOps changed

**DevOps** is a cultural and technical movement that bridges development and operations teams; it emphasizes automation, measurement, and sharing to shorten the feedback loop between writing code and running it in production, enabling faster, safer releases. The label became common after 2009, when conference talks and books started naming the pattern that high-performing internet companies had quietly adopted. The cultural shift came first, the tooling second, and the metrics last.

The cultural shift centred on shared ownership. A team that builds a service also runs it: it is on call for production incidents, it sees the operational metrics every day, and it has both the authority and the responsibility to change the system when something hurts. Shared ownership eliminates the handoff because there is nothing to hand off — the same people sign the commit and watch the deploy.

A second cultural change is the **blameless post-mortem**. When an incident happens, the investigation focuses on what the system allowed to go wrong, not on which engineer typed the wrong command. The goal is to find the missing guardrail — the pre-flight check, the automated rollback, the alarm that should have fired earlier — and add it to the system. Blame discourages reporting, and unreported incidents cannot be learned from, so a culture that punishes individual mistakes ends up with worse outages, not fewer.

A third cultural change is **automation as the default**. Manual steps in a release are treated as defects to be fixed, not as features to be preserved. Anything done twice is scripted; anything scripted is committed; anything committed runs in a pipeline. The point is not to remove humans from the loop but to remove humans from the boring, error-prone steps so that they can spend attention on the steps where judgment actually matters.

These cultural changes are what make the tooling work. A pipeline without shared ownership becomes a wall in YAML form, and a metric without a blameless culture becomes a stick to beat individuals with. The order matters: culture, then tooling, then measurement.

## The four DORA metrics

The DevOps Research and Assessment program, run by Nicole Forsgren, Jez Humble, and Gene Kim and now part of Google Cloud, measured thousands of teams over multiple years and found that four metrics together separate high-performing organizations from low-performing ones. Two metrics measure throughput — how fast value moves to production — and two measure stability — how reliably it lands.

**Lead time** is the elapsed time from when a developer commits a code change to when that change is live in production; it measures how long the organization takes to deliver value and is a key metric for DevOps maturity. Lead time captures everything between `git push` and a user seeing the change: code review, build, test, packaging, deployment, gate approvals. A team with hours of lead time can fix a critical bug the same morning it is reported. A team with weeks of lead time cannot, no matter how skilled its engineers are.

**Deployment frequency** is how often code changes reach production (e.g., once per week, multiple times per day); high deployment frequency correlates with lower lead time and lower change-failure rate, indicating a mature CI/CD capability. Deployment frequency is the easier metric to game, because a team can technically deploy daily by deploying nothing of substance, but in practice the metric is honest because nobody runs an empty pipeline. Frequent deployments force every supporting practice — fast tests, automated rollback, small batches, feature flags — to work, because nothing else can survive that cadence.

**Mean time to recovery (MTTR)** is the average time required to restore service after a production incident; organizations that automate deployment and testing can revert or fix problems faster, reducing MTTR and limiting business impact. MTTR matters because deployment frequency and lead time both push more change into production, and more change means more incidents. The DORA research found, counter-intuitively, that the same teams that deploy most often also recover fastest — not despite the change rate but because of it. Teams that practise deploying every day have practised rolling back every day; teams that deploy once a quarter have not rolled back since the previous quarter.

**Change-failure rate** is the percentage of deployments that result in a production incident or rollback; a low change-failure rate indicates reliable releases and reflects the maturity of testing, code review, and deployment automation practices. Change-failure rate disciplines the throughput metrics: a team can drive deployment frequency to the moon by skipping tests, but the change-failure rate will rise to meet it. The four metrics are read together, never in isolation.

The DORA reports group teams into bands — "low," "medium," "high," and "elite" — and the gap between the bands is wide. Elite teams measure lead time in hours and MTTR in minutes; low-performing teams measure both in months. The metrics are not vanity numbers; they predict business outcomes, including time-to-market and the team's own retention rate, because a team trapped in a slow release cycle is also a team that loses its best engineers.

## The value stream and where waste hides

A **value stream** is the sequence of steps (from idea to running code) that an organization must complete to deliver value to users; DevOps practices aim to shorten and optimize this stream by removing bottlenecks and automating manual work. Mapping the value stream is a diagnostic exercise: list every step from a feature idea entering a backlog to the resulting code running for users, mark how long each step typically takes, and mark how much of that time is the step itself versus waiting for the next step to start.

Waiting time dominates the typical value stream. A code change might compile in 30 seconds, but wait 18 hours for a code reviewer to be available. A merged pull request might pass tests in 4 minutes, but wait two days for a release manager to schedule a deployment window. A failed deployment might be fixable in 10 minutes, but the rollback procedure requires a change ticket that takes 2 hours to approve. Each of these waits is invisible in any single engineer's day but ruinous when summed.

Two patterns expose waste reliably. First, **batch size**: large batches hide problems and slow feedback. A release that bundles 200 commits is harder to test, harder to diagnose, and harder to roll back than 200 releases of one commit each, even though the total work is the same. Second, **manual handoffs**: every place where one human waits for another human creates queue time. The fix is automation that closes the handoff, not heroics that work through it.

Optimizing the value stream is what the rest of Part VIII is about. The next chapters cover continuous integration and deployment as the practices that compress the build-test-deploy waiting times, pipelines as code as the artefact that captures the optimization in version control, and deployment strategies as the techniques that reduce the risk per release so that the gates between commit and production can be removed without raising change-failure rate.

## Worked example: a team's two-year arc

Consider a small team running an e-commerce checkout service. At the start of the period, the team's metrics looked like this:

- **Deployment frequency**: once per quarter, in a Saturday-night maintenance window.
- **Lead time**: 11 weeks from commit to production.
- **MTTR**: 8 hours, because incidents required pulling the on-call developer out of bed, getting them VPN access, and walking them through the operations runbook.
- **Change-failure rate**: 30 percent — roughly one in three releases caused a customer-visible incident in the first 24 hours.

Two years later, the same team measured:

- **Deployment frequency**: 4–6 times per day, on demand, no maintenance window.
- **Lead time**: 90 minutes from commit to production.
- **MTTR**: 12 minutes, because the on-call engineer can roll back to the previous container revision from a phone.
- **Change-failure rate**: 5 percent — and the failures that do happen are caught by the smoke test before they reach customers.

What changed culturally and in the tool chain is worth listing side by side, because neither half works alone. Culturally, the team adopted shared ownership of the production service, ran blameless post-mortems after every incident, and shifted on-call from "the operations team" to "whichever developer is on rotation." Tool-chain-wise, the team built a GitHub Actions pipeline that ran tests on every pull request, packaged the application as a container, and deployed it to Azure Container Apps via a smoke-tested workflow. A failed smoke test rolled back automatically by directing all ingress traffic to the previous revision.

The cultural changes made the tools usable. Without shared ownership, no developer would have agreed to be on call for the pipeline they wrote. Without blameless post-mortems, the team would have stopped deploying on Friday afternoons after the first incident, and lead time would have crept back up. The tool changes made the culture sustainable. Without an automated rollback, the on-call engineer would have dreaded every page; without smoke tests, every deploy would have required the on-call engineer to watch for an hour.

The exercise [/exercises/3-deployment/9-cicd-to-container-apps/](/exercises/3-deployment/9-cicd-to-container-apps/) walks through the same arc in miniature: the first sub-exercise has a pipeline that builds and pushes an image but leaves deployment manual, the second closes the gap with an automated, smoke-tested deploy, and the third replaces the long-lived secret with OIDC federation. Each step takes one cell of the worked-example table from the "before" to the "after" column.

## How the rest of Part VIII operationalizes the philosophy

The remaining chapters in this Part are not separate topics. Each one operationalizes a part of the philosophy.

The chapter on [continuous integration and continuous deployment](/course-book/8-devops-and-delivery/2-ci-vs-cd/) defines the two practices that drive lead time and deployment frequency down. [Pipelines as code](/course-book/8-devops-and-delivery/3-pipelines-as-code/) is where the practices live in version control, so that the pipeline itself can be reviewed, tested, and rolled back. [Build, test, and smoke gates](/course-book/8-devops-and-delivery/4-build-test-and-smoke-gates/) are the mechanisms that hold change-failure rate down even as deployment frequency rises. [Deployment strategies](/course-book/8-devops-and-delivery/5-deployment-strategies/) — blue-green, canary, rolling — are the techniques that reduce the blast radius of any single release, which is what allows MTTR to stay low even when releases accelerate. [Pipeline secrets and OIDC federation](/course-book/8-devops-and-delivery/6-pipeline-secrets-and-oidc/) are the security primitives that make pipelines safe to run with real production credentials. [Azure Container Apps](/course-book/8-devops-and-delivery/7-azure-container-apps/) is the deployment target this Part uses because it makes revisions, ingress, and rollback first-class concepts that the pipeline can drive directly.

A reader who has internalized this chapter should expect each later chapter to answer one specific question: which of the four DORA metrics the practice moves, and at what cost.

## Summary

DevOps replaces the historical wall between development and operations with shared ownership, blameless post-mortems, and automation as the default. The four DORA metrics — lead time, deployment frequency, mean time to recovery, and change-failure rate — measure whether the philosophy has actually taken hold, with the first two measuring throughput and the second two measuring stability. The value stream is the diagnostic tool that exposes where waste hides, and most of the waste is queue time between manual handoffs rather than the work itself. The rest of Part VIII operationalizes the philosophy through CI, CD, pipelines as code, gates, deployment strategies, secrets management, and a managed deployment target. The arc from a team that deploys once a quarter to one that deploys several times a day is achievable, but only when culture, tooling, and measurement move together.
