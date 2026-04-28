+++
title = "Deployment Strategies"
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

## Deployment Strategies
Part VIII — DevOps and Delivery

---

## Why a strategy is needed
- A deploy is the **riskiest moment** — the new code has never carried real traffic
- Strategies differ on **cutover length**, **blast radius**, and **rollback speed**
- Faster cutover usually means **larger blast radius**
- Safer rollout usually means **more infrastructure** to maintain

---

## The manual gate
- A **manual gate** stops the pipeline until a human clicks **Approve**
- Continuous **delivery** without continuous **deployment**
- Catches "wrong moment" failures — Friday deploy, customer demo, suspicious build
- Trade-off: **lead time** when the approver is unavailable

---

## Rolling deployment
- Replace replicas **one batch at a time** behind the load balancer
- Application stays available throughout the rollout
- `maxUnavailable` and `maxSurge` shape capacity during the window
- Trade-off: a **mixed-version window** can surface incompatibility bugs

---

## Blue-green deployment
- Two identical environments — one **active**, one **idle**
- Deploy to idle, smoke-test, then **flip traffic atomically**
- Rollback is one switch back — measured in seconds
- Trade-off: roughly **2× infrastructure** during the deploy window

---

## Canary deployment
- Route **1–5%** of traffic to the new version first
- Watch error rate, latency, and KPIs against thresholds
- Ramp **1% → 10% → 50% → 100%** if metrics stay green; revert if not
- Needs traffic-splitting + metric-driven promotion

---

## Feature flags
- A **feature flag** turns a feature on or off at runtime, no redeploy
- Decouples **deploy** (install code) from **release** (expose to users)
- Combines with any deployment strategy — flag inside a canary build
- Rollback via flag flip is faster than redeploy

---

## Choosing a strategy
- **Manual gate** — low-frequency releases; humans add the missing signal
- **Rolling** — stateless services; backwards-compatible changes
- **Blue-green** — cutover-sensitive; incompatible changes with prior migration
- **Canary** — rich metrics; need real-traffic validation before full exposure

---

## Worked example — Container Apps
- The exercise's manual gate: push image, then click **Create new revision**
- Automated form: `az containerapp update` + `curl --fail https://$FQDN/health`
- A failing smoke test exits the workflow non-zero
- Multi-revision mode + `traffic` weights gives a real **canary** primitive

---

## Questions?
