+++
title = "Build, Test, and Smoke Gates"
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

## Build, Test, and Smoke Gates
Part VIII — DevOps and Delivery

---

## A pipeline without gates is a build script
- Continuous integration only delivers value when each commit is **verified**
- Automation that ships whatever was pushed is not CI
- A change is "integrated" once the pipeline mechanically proves it still works
- Three gates earn their keep on every container pipeline: **build, test, smoke**

---

## The build stage
- A **build stage** compiles source code into executable artifacts
- Cheapest question the pipeline can ask: "Does this form a valid program?"
- Lint rides along — style and easy bug patterns caught for almost no cost
- Output is the artifact later stages consume (binary, image, published dir)
- **Fails fast** before any test or deploy runs

---

## Unit tests
- A **unit test** verifies a single function or method in **isolation**
- Test doubles replace databases, HTTP servers, and message queues
- Defining property is **speed** — hundreds of tests in seconds
- Catches logical bugs inside a method: bad branching, off-by-one, null handling
- Cannot catch failures that emerge from how units combine

---

## Integration tests
- An **integration test** exercises how multiple units interact end-to-end
- Real PostgreSQL container, real HTTP layer, real configuration binding
- Catches contract drift unit tests can't see — wrong SQL columns, JSON mismatches, broken migrations
- Cost is runtime — minutes per suite, not seconds
- Runs after unit tests so a cheap failure does not waste expensive cycles

---

## Smoke tests
- A **smoke test** verifies the **deployed** application is alive and responding
- Targets the live FQDN — runs against the artifact, not local code
- Lightweight — typically `curl /healthz` and check the status code
- Does not promise correctness; promises the deployment landed
- The **deployment gate** — final check before declaring success

---

## Gate ordering — fail fast
- Order gates by cost: cheapest first, slowest last
- Build → unit → integration → image build → deploy → smoke
- Each stage `needs:` the previous; pipeline halts on first failure
- A broken build should not spend 20 minutes building containers
- Saves runner minutes and shortens feedback loop

---

## Test-result publishing
- A **test-result publisher** renders pass/fail counts and failures inline on the PR
- `dotnet test --logger trx` + `dorny/test-reporter@v1` for .NET
- JUnit XML for TypeScript / Python
- Failed tests visible without expanding 5000-line job logs
- Gate is only as useful as its visibility

---

## Worked example — `dotnet test` then smoke
- Job 1: `dotnet test` with TRX logger; publishes results
- Job 2: `needs: test`, deploys with `az containerapp update`
- Job 3: `needs: deploy`, runs `curl --fail https://${FQDN}/healthz`
- `--fail` exits non-zero on any non-2xx response → workflow fails red
- Bonus check: assert the response body shows the new build SHA
- See [/exercises/3-deployment/9-cicd-to-container-apps/](/exercises/3-deployment/9-cicd-to-container-apps/)

---

## Questions?
