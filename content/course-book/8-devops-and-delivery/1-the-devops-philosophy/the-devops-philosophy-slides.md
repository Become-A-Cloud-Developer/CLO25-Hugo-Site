+++
title = "The DevOps Philosophy"
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

## The DevOps Philosophy
Part VIII — DevOps and Delivery

---

## The legacy split
- Developers wrote code; **operations** ran it
- Quarterly releases, weekend cutovers, blame after failures
- Two teams optimized for **opposing goals**: change vs. uptime
- Cost was hidden in **batch size** and **handoff queues**

---

## What DevOps changed
- **Shared ownership**: build it, run it, page on it
- **Blameless post-mortems** focus on missing guardrails, not people
- **Automation as default**: anything done twice is scripted
- Culture comes first; tooling and metrics follow

---

## The four DORA metrics
- **Lead time** — commit to production
- **Deployment frequency** — how often code reaches users
- **Mean time to recovery (MTTR)** — incident to restored service
- **Change-failure rate** — percentage of deploys causing incidents
- Two measure throughput; two measure stability

---

## Throughput vs. stability
- Throughput without stability = chaos pipeline
- Stability without throughput = frozen release cycle
- Elite teams score high on **all four** simultaneously
- Frequent practice of rollback drives MTTR down

---

## The value stream
- Sequence of steps from idea to running code
- Most time is **waiting**, not working
- Waste hides in large batches and manual handoffs
- Map it, then automate the worst queue first

---

## Worked example: a two-year arc
- Before: quarterly deploys, 8 h MTTR, 30% change-failure
- After: daily deploys, 12 min MTTR, 5% change-failure
- Cultural shift: shared on-call + blameless review
- Tool-chain shift: GitHub Actions + Container Apps + smoke tests

---

## What Part VIII covers
- CI vs. CD, pipelines as code, gates, deployment strategies
- Secrets, OIDC federation, Azure Container Apps as target
- Each chapter answers: which DORA metric does this move?

---

## Questions?
