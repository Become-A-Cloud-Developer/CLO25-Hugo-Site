+++
title = "Continuous Integration vs Continuous Deployment"
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

## Continuous Integration vs Continuous Deployment
Part VIII — DevOps and Delivery

---

## CI/CD is three practices, not one
- **Continuous integration** — daily merges to a shared trunk
- **Continuous delivery** — every green commit is releasable, gated by a human
- **Continuous deployment** — every green commit ships, no human gate
- Treating them as one blob hides which step a team should adopt next

---

## The pain that motivated CI
- Two-week feature branches drift hard from the trunk
- Renames, dependency upgrades, and refactors collide at merge
- Integration risk compounds **non-linearly** with branch age
- Three small daily merges beat one merge of three days of work

---

## What CI actually requires
- Every commit triggers build and test, unconditionally
- Every commit reaches a shared trunk within hours
- A red build blocks **everyone** until it is green again
- Discipline around the red build is what makes CI a practice

---

## Trunk-based development
- Short-lived branches: hours to two days, never weeks
- Vertical slicing: ship a migration first, then the endpoint, then the UI
- Feature flags hide unfinished work behind a runtime switch
- The trunk is always production-shaped

---

## The pull-request gate
- The trunk's quality bar moves to merge time
- Build green, tests green, lint clean, one peer review
- Different from a deployment gate — a PR gate protects the trunk itself
- Fast pipeline (~minutes) is a precondition, not a luxury

---

## The spectrum
- **CI** — trunk is always buildable; deploy is a separate question
- **Continuous delivery** — green commit could ship within minutes, human chooses when
- **Continuous deployment** — green commit ships automatically, no button to click
- Each step strictly assumes the previous one is already in place

---

## Worked example: feature branch vs flag-protected trunk
- Team A: 9-day branch → painful merge, two integration bugs, 14 days to prod
- Team B: 5 small PRs over 4 days, each green and shipped behind a flag
- Day 4: flag flipped 5% canary → 100% by end of day
- Same feature, less integration risk, deploy decoupled from release

---

## Choosing CD vs continuous deployment
- Regulated regime → human approval is mandated; pick **delivery**
- Rollback faster than diagnosis → pick **deployment**
- No canary or flags yet → not ready for **deployment**
- Decision is operational maturity, not technical capability

---

## Where this connects
- Ex 3.9.1 — CI + manual deploy (click "Create new revision")
- Ex 3.9.2 — Continuous deployment with a smoke-test gate
- Ex 3.9.3 — Same delivery shape, OIDC federation replaces the stored secret
- Cross-link: `/exercises/3-deployment/9-cicd-to-container-apps/`

---

## Questions?
