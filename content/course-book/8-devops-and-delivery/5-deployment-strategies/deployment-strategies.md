+++
title = "Deployment Strategies"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 50
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/8-devops-and-delivery/5-deployment-strategies.html)

[Se presentationen på svenska](/presentations/course-book/8-devops-and-delivery/5-deployment-strategies-swe.html)

---

The moment a new build replaces the running version is the riskiest moment in the life of a service. Tests have passed, the artifact is signed, and the pipeline is ready to ship — but the new code has never carried real traffic. A bug that the test suite missed will surface here, in front of users, with the previous version already gone. Different deployment strategies exist precisely to mitigate this risk in different ways: some make the cutover instant and reversible, some bleed the change in slowly while watching metrics, some keep a human in the loop. The choice shapes how a release feels to the team and to users — and which failures stay invisible.

## Why a strategy is needed

A **deployment strategy** is a method for rolling out code changes to production (e.g., all-at-once, blue-green, canary, rolling); the choice of strategy affects risk, rollback speed, and user impact. The naive approach — stop the old version, start the new one — works for a hobby project. It fails for a production service for two related reasons. The first is downtime: every user hitting the service during the swap sees an error. The second is blast radius: if the new build is broken, every user sees the breakage at once, and rollback means another stop-start cycle that compounds the outage.

Strategies differ along three axes. The first is **cutover duration** — a blue-green flip lasts seconds, while a rolling deployment can take many minutes. The second is **blast radius** when the new version is broken: a canary exposes 1% of users, a rolling deployment exposes a growing fraction, and an all-at-once swap exposes 100%. The third is **reversal speed** — blue-green keeps the old environment idle, so rollback is one traffic flip, whereas rolling deployment requires reversing the rollout step by step.

These axes do not move independently. A faster cutover usually means a larger blast radius. A safer rollout usually means more infrastructure to maintain. The deployment strategy is the team's pre-committed trade among these costs: which of them is acceptable in exchange for which protections.

## The manual gate

The simplest production-grade strategy is also the oldest. The pipeline builds, tests, and packages the code, then stops. A human reviews the result and clicks **Approve** to deploy. This is **continuous delivery** without **continuous deployment** — the artifact is always shippable, but a person decides when it ships.

A **manual gate** is a point in a deployment process where a human must explicitly approve before proceeding; it adds a safety checkpoint but increases lead time if the approver is not available. Manual gates show up in three common forms:

- **Approve to deploy** — the pipeline reaches a `deploy` job that requires a reviewer's click before running.
- **Approve to promote** — the build deploys to staging automatically, and a click promotes the same artifact to production.
- **Click to flip traffic** — the new version is fully deployed but receives no traffic until a human routes it there. The companion exercise's first variant uses this form: the GitHub Actions pipeline pushes a new image, but a developer must open the Azure Portal and click **Create new revision** before users see the change.

The trade-off is bluntly mechanical. Manual gates catch the failure modes a human would notice — a release on a Friday afternoon, a deploy during a customer demo, a build that has the right number but suspicious test output. They do not catch failures that only show up under traffic, because no traffic is hitting the new version yet. They also tax lead time: if the approver is asleep, on vacation, or in a meeting, the change waits. Many teams keep a manual gate for production while letting staging deploy automatically — a useful middle ground that captures the safety value without paying the lead-time cost on every deploy.

## Rolling deployment

A **rolling deployment** is a strategy where instances of the old version are progressively replaced with the new version, a few at a time; the application remains available throughout, but the rollout takes time and rollback is more complex than blue-green. The mechanics are straightforward when the workload is stateless and runs on multiple replicas. The orchestrator (Kubernetes, Container Apps, an autoscaling group) takes one or two replicas out of the load-balancer pool, replaces them with the new version, waits for them to pass a readiness probe, and puts them back. It then repeats for the next batch until all replicas run the new version.

Two settings shape a rolling deployment. **`maxUnavailable`** is how many replicas can be missing from the load balancer at once — too low, and the rollout stalls if a new replica is slow to start; too high, and an in-progress rollout under heavy load drops requests. **`maxSurge`** is how many extra replicas can run during the rollout — a higher surge keeps capacity stable but costs more during the rollout window.

Rolling deployments suit stateless services that can run mixed versions side by side for several minutes. The application remains available throughout, and capacity is preserved within the surge budget. The trade-off is the half-state in the middle: while the rollout is running, requests land on either version depending on which replica the load balancer picks. If the new version introduces a backwards-incompatible change to a shared resource — a database schema, a cache key format, a session cookie — the mixed-version window will surface bugs that a same-version deployment would not. Rollback is also slow: reversing a rolling deployment means rolling back batch by batch, while users continue to hit the broken version until the reversal completes.

## Blue-green deployment

**Blue-green deployment** is a strategy where two identical production environments (blue and green) are maintained; the new version is deployed to the inactive environment, tested, and then traffic is switched over, allowing instant rollback to the previous version if problems occur. At any moment, one environment serves all production traffic and the other sits idle. To deploy, the team installs the new build on the idle environment, runs smoke tests against it through an internal hostname, and then flips a single switch — usually a DNS record, a load-balancer rule, or a traffic-split setting — to point users at the new environment.

The defining property is the cutover. It is atomic: a request is either served by the old version or the new one, never half-and-half. There is no mixed-version window, so backwards-incompatible changes are easier to handle than under rolling deployment. Rollback is equally atomic: flipping the switch back routes all traffic to the still-running old environment in seconds. Until the team is confident in the new release, the old environment is kept warm and ready.

The cost is duplication. Blue-green requires roughly twice the infrastructure of a single environment, at least during the deploy window. For stateless workloads on elastic platforms, this cost is short-lived and small. For stateful workloads, the picture is harder — both environments cannot own the database at once, so blue-green is usually paired with backwards-compatible schema migrations applied before the cutover, with the database itself remaining shared.

## Canary deployment

**Canary deployment** is a strategy where a new version is deployed to a small percentage of production traffic first (the "canary"); if metrics look good, the rollout continues; if issues arise, traffic reverts to the stable version, limiting user impact. The new version runs alongside the stable version, but the load balancer routes only 1–5% of requests to it. The team watches metrics — error rate, latency, business KPIs — for a fixed window. If the canary stays healthy, the percentage ramps up: 1% → 10% → 50% → 100%. If a metric breaches a threshold, the load balancer routes traffic away from the canary and the deployment is rolled back.

A canary catches failures that only surface under real production traffic — load-dependent bugs, edge cases in real user data, third-party-API timeouts, latency regressions invisible at low scale. It catches them on a small slice of users, not on everyone. The blast radius at 1% traffic is roughly 1% of what an all-at-once deploy would have produced.

The price is operational sophistication. A canary needs traffic-splitting infrastructure, a metric-driven rollout controller, and clear go/no-go thresholds defined before the deploy. It also requires that the stable and canary versions can run in parallel without sharing state in incompatible ways. Teams that adopt canaries usually combine them with **automated promotion** — a controller (Argo Rollouts, Flagger, a custom controller in Container Apps) advances the percentage when metrics stay green and rolls back when they go red, without a human clicking through each step.

## Feature flags

A **feature flag** is a configuration switch in an application that enables or disables a feature at runtime without redeploying code; flags allow features to be deployed to production but hidden from users until ready, and enable quick rollback if issues arise. Feature flags are not a deployment strategy in the same sense as blue-green or canary — they sit at a different layer. Where deployment strategies decide which version of the code is running, feature flags decide which features inside that version are reachable.

This separation is the key idea. **Deploying** code means installing a new build; **releasing** a feature means making it visible to users. Feature flags decouple the two. A team can deploy a half-finished feature behind an off-by-default flag, run it in production with no user impact for weeks, and then flip the flag for internal users, then for 1% of customers, then for everyone — long after the deployment that introduced the code has shipped.

Flag-based rollouts complement deployment strategies. A canary controls the version mix at the infrastructure layer; a feature flag controls the feature mix at the application layer. The two combine well: a build deployed via canary can include several flagged features, each released independently of the deployment that delivered them. Rollback also gets safer — turning off a flag is faster than redeploying, and avoids the mixed-version window of a rolling rollback.

## Choosing a strategy

The strategies are not mutually exclusive, but each one fits a different risk profile.

| Strategy | Cutover | Blast radius if broken | Rollback speed | Infrastructure cost | Best fit |
|----------|---------|------------------------|----------------|---------------------|----------|
| Manual gate | Whatever the underlying deploy is | Same as underlying | Same as underlying | None extra | Low-frequency releases; humans add the missing signal |
| Rolling | Several minutes | Grows with the rollout | Slow (reverse the rollout) | None extra | Stateless services with many replicas; backwards-compatible changes |
| Blue-green | Seconds | 100% during cutover, but cutover is short | Instant (flip back) | Roughly 2× during the window | Cutover-sensitive services; backwards-incompatible changes with prior data migration |
| Canary | Long (hours) | 1–5% during canary phase | Fast (route traffic away) | Slight surge + observability | Services with rich metrics; rollouts that need real-traffic validation |
| Feature flag | Instant | Whatever the flag scope is | Instant (flip the flag) | A flag service or library | Decoupling release from deploy; gradual feature exposure independent of deploy cadence |

The choice depends on how much the team trusts the test suite, how rich the production metrics are, how much extra infrastructure is acceptable, and how compatible successive versions are with each other. Teams that release rarely tolerate a manual gate; teams that release dozens of times per day need automated canaries to keep up. Teams running stateless container workloads default to rolling or blue-green; teams running customer-facing experiments lean on feature flags.

## A worked example with Container Apps

The companion exercise [CI/CD to Azure Container Apps](/exercises/3-deployment/9-cicd-to-container-apps/) builds the manual-gate strategy explicitly. The pipeline ends with `docker push` to a registry, but the Container App keeps serving the previous **revision** — the immutable version unit in Container Apps. A developer opens the Portal, clicks **Create new revision**, picks the new image tag, and only then does the new code reach users. The gap between "image is built" and "users see it" is something a developer should feel at least once.

A pipeline that automates the same flow with a smoke-tested cutover replaces the manual click with two CLI calls. The first creates a new revision in single-revision mode, which Container Apps treats as a one-step blue-green flip:

```bash
az containerapp update \
  --name myapp \
  --resource-group rg-prod \
  --image mycr.azurecr.io/myapp:${{ github.sha }} \
  --revision-suffix "${{ github.run_number }}"

FQDN=$(az containerapp show \
  --name myapp \
  --resource-group rg-prod \
  --query properties.configuration.ingress.fqdn -o tsv)

curl --fail --silent --max-time 10 "https://${FQDN}/health" \
  || { echo "Smoke test failed"; exit 1; }
```

The first command pushes a new revision with a deterministic suffix and, in single-revision mode, retires the previous revision once the new one is healthy. The second probes the public FQDN with a `--fail` flag that turns any non-2xx response into a non-zero exit code — the pipeline's failure signal. If the smoke test fails, the workflow exits non-zero and a follow-up step or manual intervention can roll the revision back.

For a true canary, Container Apps can run in multi-revision mode with a `traffic` block that splits requests across revisions by weight. The pipeline pushes a new revision at 5% weight, observes for a window, then issues a second update that ramps the weight to 100% and retires the old revision. The same primitive — a revision plus a traffic weight — supports both the manual-gate and the canary forms of the same release.

## Summary

A deployment strategy is the team's plan for the riskiest moment in a service's life. The manual gate keeps a human in the loop and trades lead time for an extra signal. Rolling deployments preserve availability on stateless services but expose a mixed-version window. Blue-green deployments make the cutover atomic and rollback instant at the price of duplicate infrastructure. Canary deployments validate a release against real traffic on a small slice of users before extending to all of them. Feature flags decouple deploy from release entirely and operate at the application layer rather than the infrastructure layer. Each strategy matches a different risk profile, and most production systems combine several — a canary deploy of a build that contains several flagged features, with a manual gate guarding the production environment as a whole.
