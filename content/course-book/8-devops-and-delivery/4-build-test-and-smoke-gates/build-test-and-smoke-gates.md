+++
title = "Build, Test, and Smoke Gates"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 40
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/8-devops-and-delivery/4-build-test-and-smoke-gates.html)

[Se presentationen på svenska](/presentations/course-book/8-devops-and-delivery/4-build-test-and-smoke-gates-swe.html)

---

A pipeline that compiles code and pushes an image is a build script with a fancy trigger. What turns automation into continuous integration is the set of checks that decide whether the result is safe to ship. Each check is a stage in the pipeline, and each stage either passes the artifact along or stops the run. This chapter develops the three gates that earn their keep on every CI/CD pipeline a CLO student will write — the build stage, the test layers, and the smoke test against the deployed application — and explains how their ordering controls cost, signal, and feedback time.

## From build script to continuous integration

Continuous integration ([CI](/course-book/8-devops-and-delivery/2-ci-vs-cd/)) only delivers value when each commit is verified against a battery of automated checks. Without those checks, the pipeline is a publishing mechanism: it takes whatever the developer pushed and produces an artifact, regardless of whether the artifact works. The verification layer is what gives CI its name. A change is only "integrated" once the pipeline has demonstrated, mechanically and reproducibly, that it still compiles, still passes its tests, and still runs once deployed.

The vocabulary for these checks is the **gate (CI)** — a stage or condition that must be satisfied before the pipeline proceeds to the next stage. Gates prevent broken or untested code from reaching later stages. The gate is not the test itself but the structural decision that the pipeline halts when the test fails. A test suite that prints failures but lets the pipeline continue is not a gate; the same suite wired so the pipeline exits non-zero on any failure is.

Three gates appear in almost every pipeline that ships container workloads to a managed platform: the build gate, the test gate, and the smoke gate. They differ in cost, in the kind of failure they catch, and in how close to production they execute.

## The build stage

A **build stage** is the first phase of a CI pipeline that compiles source code into executable binaries or artifacts; it fails fast if the code does not compile, preventing broken code from proceeding to tests or deployment. For a .NET application this stage runs `dotnet restore` and `dotnet build`. For a TypeScript application it runs `npm ci` and `tsc`. For a Java application it runs `mvn compile`. The exact commands differ by ecosystem; the role does not.

The build stage answers the cheapest question the pipeline can ask: "Does this source code form a valid program?" A missing semicolon, a misspelled package name, a renamed method that broke a caller — all of these surface here, before a single test runs. The cost is small, usually under a minute on a hosted runner, and the signal is binary. If the build fails, no later stage can produce useful information.

Linting often rides along with the build stage. A linter (`dotnet format --verify-no-changes`, `eslint`, `ruff`) checks style and common error patterns without executing the code. Including lint in the build gate keeps stylistic regressions out of the trunk and catches a class of bugs (unused variables, unreachable branches) that compilers may not flag. The runtime is small enough that bundling lint with build pays for itself.

The artifact emitted by the build stage — compiled binaries, a published directory, a container image — is what later stages consume. In a containerized pipeline the build stage frequently produces a Docker image and uploads it to a registry; subsequent stages then pull that image rather than recompiling. This pattern is described in [pipelines as code](/course-book/8-devops-and-delivery/3-pipelines-as-code/), where the artifact is the unit of work passed between jobs.

## Test layers

A passing build proves the code is valid. It proves nothing about whether the code is correct. Tests close that gap, and they do so in layers, each catching a different class of defect at a different cost.

### Unit tests

A **unit test** is an automated test that verifies a single function or method in isolation; it runs quickly and is the first line of defense against bugs, catching logical errors before code integration. A unit test for a discount calculator constructs the calculator, calls `Apply(orderTotal, couponCode)`, and asserts the returned total. Nothing else runs — no database, no HTTP server, no message queue. Dependencies that the unit collaborates with are replaced by test doubles (mocks, fakes, stubs).

The defining property of a unit test is speed. A well-structured suite of several hundred unit tests should complete in seconds. That speed comes from isolation: with no external dependencies to start or wait for, the test exercises only the logic under examination. Unit tests catch the kinds of mistakes that live inside a method — incorrect branching, off-by-one errors, mishandled null inputs, wrong arithmetic. They cannot catch mistakes that emerge from how units combine.

### Integration tests

An **integration test** is an automated test that verifies how multiple units (functions, services, databases) interact together; it runs slower than unit tests because it sets up more dependencies, but it catches bugs that unit tests miss. An integration test for the same discount calculator might start a real PostgreSQL container, load the coupon table from a fixture, and call the calculator through the same service entry point a controller would use. The point is to exercise the wiring: the SQL the data layer emits, the JSON the service deserializes, the configuration the application binds.

These tests find the failures that mock-based unit tests cannot reach. A SQL query that returns the wrong column. A JSON contract that drifted between client and server. A migration that added a non-nullable column without a default. None of these surface in unit tests because the unit-test boundary deliberately excludes the systems where the bug lives. They surface as soon as a real version of the dependency runs end-to-end.

The cost is the runtime. Spinning up a database container, applying schema, seeding data, and running queries adds seconds per test. A suite of fifty integration tests can take several minutes. That cost is why integration tests sit later in the pipeline — they should not run if the build is broken or unit tests are red, because their failure would be redundant with cheaper signals.

### Smoke tests

A **smoke test** is a lightweight, high-level verification that the deployed application is alive and responding; it typically makes an HTTP request to a public endpoint and checks for a successful response, providing a quick confidence check that deployment succeeded. The name comes from electronics: power up the device and check that no smoke comes out. The test does not verify functionality in depth — that is what unit and integration tests are for. It verifies that the deployment landed at all.

A smoke test runs against the deployed artifact, not against locally built code. It targets the production URL (or a staging URL, depending on the deployment target) and confirms the live process is reachable, the routing is correct, the configuration loaded, and the dependencies the application needs at startup are wired. A passing smoke test does not promise the application is bug-free; a failing smoke test promises the deployment is broken.

## Why each layer earns its keep

The three layers — unit, integration, smoke — are not redundant. Each catches failures the others miss, and each runs at a different cost. The economics is what makes the layered approach pay off.

| Layer | Speed | Catches | Cost per failure caught |
|-------|-------|---------|-------------------------|
| Unit | Seconds | Logical bugs in a single function | Lowest |
| Integration | Minutes | Contract drift between components | Medium |
| Smoke | Seconds | Deployment, configuration, networking | Lowest at deploy time |

A unit test cannot detect that the production database is unreachable. A smoke test cannot pinpoint which method computed the wrong total. Treating tests as a single mass and asking "do we have enough tests?" is the wrong question. The right question is whether each layer is present in proportion to the failures it is meant to catch.

The pipeline's contribution is to enforce ordering. A failing unit test should stop the pipeline before integration tests run. A failing integration test should stop the pipeline before the deployment job runs. A failing smoke test should stop the pipeline before traffic shifts to the new revision. Each gate trades a small additional runtime for protection against a larger downstream cost.

## Gate ordering and fail fast

A pipeline that runs every check in parallel maximizes feedback speed but burns runner minutes on jobs whose result is no longer interesting. A pipeline that runs every check sequentially is cheap but slow. The compromise is to order gates by cost, cheapest first.

The principle is **fail fast**: a broken build should stop the pipeline before it spends twenty minutes building containers, twenty minutes running integration tests, and twenty minutes deploying to a staging environment. The earlier a failure surfaces, the cheaper it is to fix and the less downstream work the pipeline wastes.

The standard ordering for a containerized pipeline:

1. **Build** — compile and lint. Fast, deterministic, binary signal.
2. **Unit test** — isolated tests. Fast, executes against the freshly built binaries.
3. **Integration test** — tests with real dependencies. Slower, but still pre-deployment.
4. **Image build and push** — produce the deployment artifact. Only runs if all earlier gates passed.
5. **Deploy** — update the running service.
6. **Smoke test** — verify the deployed service responds. Final gate before declaring success.

Each gate is a `needs:` dependency on the previous one in GitHub Actions, or a `dependsOn:` clause in Azure Pipelines. The pipeline halts at the first failing stage; nothing downstream runs. The runtime saved is real — a broken build that would have triggered ten more jobs now triggers one.

## Test-result publishing

A test that fails inside a pipeline is invisible unless the pipeline surfaces the result. The job log will record a non-zero exit code, but reading raw logs to find which test failed and why is a poor developer experience. A **test-result publisher** is a tool or action in a CI pipeline that captures, formats, and displays test results (pass/fail counts, coverage, failures) in a human-readable report; it provides visibility into test quality across builds.

For .NET tests, `dotnet test --logger "trx"` emits a structured TRX file. A GitHub Actions step like `dorny/test-reporter@v1` consumes the TRX and renders an inline summary on the pull request — pass and fail counts, the names of failing tests, the assertion diffs. The PR reviewer sees the test outcome without expanding the log. For TypeScript tests, JUnit-format XML serves the same role; for Python, a JUnit reporter from pytest.

Test-result publishing matters because the gate is only as useful as its visibility. A failing test that buries its diagnostic in a 5000-line log slows the response. A failing test whose name and stack trace appear on the PR page enables an immediate fix. The publisher does not change what the test verifies; it changes how quickly the developer learns what went wrong.

## The smoke test as deployment gate

The build and test stages happen before deployment. The smoke test happens after. That timing gives it a different role: it is the final check that decides whether the deployment is declared successful, and it is the only check that runs against the running production (or staging) environment.

A worked example, drawn from the pipeline built in [Exercise 3.9 — CI/CD to Azure Container Apps](/exercises/3-deployment/9-cicd-to-container-apps/), shows the pattern. The pipeline first runs `dotnet test` in a job named `test`. If any test fails, the job exits non-zero and the dependent deploy job never runs. If all tests pass, a deploy job updates the Container App revision. A final smoke job then verifies the live endpoint:

```yaml
test:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'
    - run: dotnet test --logger "trx" --results-directory TestResults
    - uses: dorny/test-reporter@v1
      if: always()
      with:
        name: dotnet tests
        path: TestResults/*.trx
        reporter: dotnet-trx

deploy:
  needs: test
  runs-on: ubuntu-latest
  steps:
    - uses: azure/login@v2
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}
    - run: az containerapp update --name $APP --resource-group $RG --image $IMAGE

smoke:
  needs: deploy
  runs-on: ubuntu-latest
  steps:
    - run: |
        curl --fail --max-time 10 \
          "https://${CONTAINER_APP_FQDN}/healthz"
```

The smoke job is short — a single `curl --fail` against the health endpoint. The `--fail` flag tells `curl` to exit non-zero on any HTTP status outside 2xx, which propagates as a job failure. That non-zero exit fails the workflow run, marks the commit red on GitHub, and signals the team that the deployed revision is unhealthy. A more thorough smoke check inspects the response body for a version cue (a build SHA, a release tag) to confirm the new revision actually serves traffic, not the previous one.

The reason a smoke test belongs in the pipeline rather than in a monitoring tool is timing. Monitoring detects unhealthy services minutes after they degrade. The smoke gate detects them seconds after the deployment finishes, when the change that caused the regression is still on the developer's screen. The smoke gate is also the gate that decides whether the pipeline reports success — without it, the workflow would turn green the moment `az containerapp update` returned, regardless of whether the new revision could actually serve a request.

## Summary

The gates that separate a pipeline from a build script are the build stage, the test layers, and the smoke test. The build stage answers the cheapest question and emits the artifact later stages consume. Unit tests verify logic in isolation and run in seconds; integration tests verify how units combine and run in minutes; smoke tests verify the deployed artifact responds and run against the live URL. Ordering gates by cost — cheapest first — surfaces failures early and saves runner time, the principle known as fail fast. A test-result publisher renders the gate's outcome where developers can act on it. The smoke test, run as the final stage against the deployed application, is the gate that decides whether the workflow declares success or red-flags the release.
