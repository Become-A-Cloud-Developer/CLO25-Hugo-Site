# Part VIII — Glossary

Terminology contract for the seven chapters of Part VIII — DevOps and Delivery.

## Terms owned by this Part

### DevOps
- **Owner chapter**: `1-the-devops-philosophy`
- **Canonical definition**: **DevOps** is a cultural and technical movement that bridges development and operations teams; it emphasizes automation, measurement, and sharing to shorten the feedback loop between writing code and running it in production, enabling faster, safer releases.
- **Used by chapters**: 1 (owner), 2, 3, 4, 5, 6, 7

### Lead time
- **Owner chapter**: `1-the-devops-philosophy`
- **Canonical definition**: **Lead time** is the elapsed time from when a developer commits a code change to when that change is live in production; it measures how long the organization takes to deliver value and is a key metric for DevOps maturity.
- **Used by chapters**: 1 (owner)

### Mean time to recovery (MTTR)
- **Owner chapter**: `1-the-devops-philosophy`
- **Canonical definition**: **Mean time to recovery (MTTR)** is the average time required to restore service after a production incident; organizations that automate deployment and testing can revert or fix problems faster, reducing MTTR and limiting business impact.
- **Used by chapters**: 1 (owner), 5

### Deployment frequency
- **Owner chapter**: `1-the-devops-philosophy`
- **Canonical definition**: **Deployment frequency** is how often code changes reach production (e.g., once per week, multiple times per day); high deployment frequency correlates with lower lead time and lower change-failure rate, indicating a mature CI/CD capability.
- **Used by chapters**: 1 (owner)

### Change-failure rate
- **Owner chapter**: `1-the-devops-philosophy`
- **Canonical definition**: **Change-failure rate** is the percentage of deployments that result in a production incident or rollback; a low change-failure rate indicates reliable releases and reflects the maturity of testing, code review, and deployment automation practices.
- **Used by chapters**: 1 (owner)

### Value stream
- **Owner chapter**: `1-the-devops-philosophy`
- **Canonical definition**: A **value stream** is the sequence of steps (from idea to running code) that an organization must complete to deliver value to users; DevOps practices aim to shorten and optimize this stream by removing bottlenecks and automating manual work.
- **Used by chapters**: 1 (owner)

### Continuous integration (CI)
- **Owner chapter**: `2-ci-vs-cd`
- **Canonical definition**: **Continuous integration (CI)** is a practice where developers integrate code changes into a shared repository frequently (multiple times per day); each integration is automatically built, tested, and verified to catch integration errors early.
- **Used by chapters**: 2 (owner), 3, 4, 6, 7

### Continuous delivery
- **Owner chapter**: `2-ci-vs-cd`
- **Canonical definition**: **Continuous delivery** is a practice where code changes are automatically built, tested, and packaged so they are always in a releasable state; deployment to production is automated but gated by a manual approval step.
- **Used by chapters**: 2 (owner), 5

### Continuous deployment
- **Owner chapter**: `2-ci-vs-cd`
- **Canonical definition**: **Continuous deployment** is a practice where code changes that pass automated tests are automatically deployed to production without manual gates; every commit to the main branch can reach users.
- **Used by chapters**: 2 (owner), 5

### Trunk-based development
- **Owner chapter**: `2-ci-vs-cd`
- **Canonical definition**: **Trunk-based development** is a branching strategy where developers work on short-lived feature branches that are frequently merged to the main branch (the "trunk"); this enables frequent integration and reduces merge conflicts and slow feedback loops.
- **Used by chapters**: 2 (owner)

### Pull-request gate
- **Owner chapter**: `2-ci-vs-cd`
- **Canonical definition**: A **pull-request gate** is an automated check (build, linting, tests) that must pass before a code review can approve and merge a pull request; it prevents broken or low-quality code from reaching the trunk.
- **Used by chapters**: 2 (owner)

### Pipeline
- **Owner chapter**: `3-pipelines-as-code`
- **Canonical definition**: A **pipeline** is a sequence of automated stages (build, test, deploy) that takes source code and produces a running application; each stage depends on the previous one succeeding, and failure at any stage halts the pipeline.
- **Used by chapters**: 3 (owner), 4, 5, 6, 7

### Workflow (GitHub Actions)
- **Owner chapter**: `3-pipelines-as-code`
- **Canonical definition**: A **workflow** (in GitHub Actions) is an automated process defined in a YAML file (`.github/workflows/*.yml`) that triggers on events (push, pull request, schedule) and orchestrates jobs to build, test, and deploy code.
- **Used by chapters**: 3 (owner)

### Job
- **Owner chapter**: `3-pipelines-as-code`
- **Canonical definition**: A **job** (in GitHub Actions) is a set of steps that run on the same runner; jobs can run in parallel or sequentially, and a workflow often contains multiple jobs (e.g., build job, test job, deploy job).
- **Used by chapters**: 3 (owner)

### Step
- **Owner chapter**: `3-pipelines-as-code`
- **Canonical definition**: A **step** (in GitHub Actions) is a single command or action within a job; it runs sequentially in the order defined, and failure in a step can halt the job unless explicitly ignored.
- **Used by chapters**: 3 (owner)

### Runner
- **Owner chapter**: `3-pipelines-as-code`
- **Canonical definition**: A **runner** is a machine (GitHub-hosted or self-hosted) that executes the steps of a GitHub Actions job; GitHub-hosted runners are provided by GitHub (Ubuntu, Windows, macOS), while self-hosted runners are managed by the organization.
- **Used by chapters**: 3 (owner)

### Action (GitHub)
- **Owner chapter**: `3-pipelines-as-code`
- **Canonical definition**: An **action** (in GitHub) is a reusable unit of code published to the GitHub Marketplace; actions encapsulate common tasks (checkout code, set up a runtime, deploy) and are called by name in workflow steps (e.g., `actions/checkout@v4`).
- **Used by chapters**: 3 (owner)

### Artifact (CI)
- **Owner chapter**: `3-pipelines-as-code`
- **Canonical definition**: An **artifact** (in CI) is a file or collection of files (compiled binaries, Docker images, test reports) produced by a pipeline job that can be stored, passed to later jobs, or published for download.
- **Used by chapters**: 3 (owner), 4

### Build stage
- **Owner chapter**: `4-build-test-and-smoke-gates`
- **Canonical definition**: A **build stage** is the first phase of a CI pipeline that compiles source code into executable binaries or artifacts; it fails fast if the code does not compile, preventing broken code from proceeding to tests or deployment.
- **Used by chapters**: 4 (owner), 3

### Unit test
- **Owner chapter**: `4-build-test-and-smoke-gates`
- **Canonical definition**: A **unit test** is an automated test that verifies a single function or method in isolation; it runs quickly and is the first line of defense against bugs, catching logical errors before code integration.
- **Used by chapters**: 4 (owner)

### Integration test
- **Owner chapter**: `4-build-test-and-smoke-gates`
- **Canonical definition**: An **integration test** is an automated test that verifies how multiple units (functions, services, databases) interact together; it runs slower than unit tests because it sets up more dependencies, but it catches bugs that unit tests miss.
- **Used by chapters**: 4 (owner)

### Smoke test
- **Owner chapter**: `4-build-test-and-smoke-gates`
- **Canonical definition**: A **smoke test** is a lightweight, high-level verification that the deployed application is alive and responding; it typically makes an HTTP request to a public endpoint and checks for a successful response, providing a quick confidence check that deployment succeeded.
- **Used by chapters**: 4 (owner), 5, 7

### Gate (CI)
- **Owner chapter**: `4-build-test-and-smoke-gates`
- **Canonical definition**: A **gate** (in CI) is a stage or condition that must be satisfied before the pipeline proceeds to the next stage; gates prevent broken or untested code from reaching later stages (e.g., "all tests must pass before deploy").
- **Used by chapters**: 4 (owner)

### Test-result publisher
- **Owner chapter**: `4-build-test-and-smoke-gates`
- **Canonical definition**: A **test-result publisher** is a tool or action in a CI pipeline that captures, formats, and displays test results (pass/fail counts, coverage, failures) in a human-readable report; it provides visibility into test quality across builds.
- **Used by chapters**: 4 (owner)

### Deployment strategy
- **Owner chapter**: `5-deployment-strategies`
- **Canonical definition**: A **deployment strategy** is a method for rolling out code changes to production (e.g., all-at-once, blue-green, canary, rolling); the choice of strategy affects risk, rollback speed, and user impact.
- **Used by chapters**: 5 (owner)

### Manual gate
- **Owner chapter**: `5-deployment-strategies`
- **Canonical definition**: A **manual gate** is a point in a deployment process where a human must explicitly approve before proceeding; it adds a safety checkpoint but increases lead time if the approver is not available.
- **Used by chapters**: 5 (owner)

### Blue-green deployment
- **Owner chapter**: `5-deployment-strategies`
- **Canonical definition**: **Blue-green deployment** is a strategy where two identical production environments (blue and green) are maintained; the new version is deployed to the inactive environment, tested, and then traffic is switched over, allowing instant rollback to the previous version if problems occur.
- **Used by chapters**: 5 (owner)

### Canary deployment
- **Owner chapter**: `5-deployment-strategies`
- **Canonical definition**: **Canary deployment** is a strategy where a new version is deployed to a small percentage of production traffic first (the "canary"); if metrics look good, the rollout continues; if issues arise, traffic reverts to the stable version, limiting user impact.
- **Used by chapters**: 5 (owner)

### Rolling deployment
- **Owner chapter**: `5-deployment-strategies`
- **Canonical definition**: **Rolling deployment** is a strategy where instances of the old version are progressively replaced with the new version, a few at a time; the application remains available throughout, but the rollout takes time and rollback is more complex than blue-green.
- **Used by chapters**: 5 (owner)

### Feature flag
- **Owner chapter**: `5-deployment-strategies`
- **Canonical definition**: A **feature flag** is a configuration switch in an application that enables or disables a feature at runtime without redeploying code; flags allow features to be deployed to production but hidden from users until ready, and enable quick rollback if issues arise.
- **Used by chapters**: 5 (owner)

### GitHub secret
- **Owner chapter**: `6-pipeline-secrets-and-oidc`
- **Canonical definition**: A **GitHub secret** is a confidential value (API key, access token, password) stored encrypted in a GitHub repository; secrets are injected into workflows as environment variables and are masked in logs to prevent accidental exposure.
- **Used by chapters**: 6 (owner)

### Service principal
- **Owner chapter**: `6-pipeline-secrets-and-oidc`
- **Canonical definition**: A **service principal** is an Azure-managed identity that represents a non-human principal (a CI pipeline, a scheduled job) and can be granted Azure RBAC roles; it enables automation without storing human passwords in code.
- **Used by chapters**: 6 (owner), 7

### Federated credential
- **Owner chapter**: `6-pipeline-secrets-and-oidc`
- **Canonical definition**: A **federated credential** is a trust relationship between a service principal in Azure and an external identity provider (e.g., GitHub); it allows the external provider to mint short-lived tokens that Azure validates, eliminating the need for a stored password.
- **Used by chapters**: 6 (owner)

### OIDC federation (workload)
- **Owner chapter**: `6-pipeline-secrets-and-oidc`
- **Canonical definition**: **OIDC federation** (workload) is the practice of using OpenID Connect tokens issued by a CI/CD provider (GitHub Actions, GitLab CI) to authenticate to cloud providers (Azure, AWS, GCP); it replaces long-lived stored credentials with short-lived, automatically rotated tokens.
- **Used by chapters**: 6 (owner)

### Short-lived token (pipeline)
- **Owner chapter**: `6-pipeline-secrets-and-oidc`
- **Canonical definition**: A **short-lived token** (pipeline) is a bearer token issued by a CI/CD provider (e.g., GitHub) for a specific workflow run; it expires after a short duration (minutes to hours) and is automatically rotated on each run, reducing the blast radius of a compromise.
- **Used by chapters**: 6 (owner)

### Azure Container Apps
- **Owner chapter**: `7-azure-container-apps`
- **Canonical definition**: **Azure Container Apps** is a managed, serverless container service that runs Docker images without requiring container orchestration expertise; it handles scaling, networking, and revision management, making it ideal for deploying containerized applications without operational burden.
- **Used by chapters**: 7 (owner)

### Revision (ACA)
- **Owner chapter**: `7-azure-container-apps`
- **Canonical definition**: A **revision** (in Azure Container Apps) is an immutable version of a Container App; when you deploy a new image or change configuration, a new revision is created automatically, and traffic can be split between revisions for canary or blue-green deployments.
- **Used by chapters**: 7 (owner)

### Ingress
- **Owner chapter**: `7-azure-container-apps`
- **Canonical definition**: **Ingress** (in Azure Container Apps) is the network configuration that exposes a Container App to incoming traffic; it defines the external hostname (FQDN), port, transport protocol (HTTP, gRPC), and optionally internal-only or public visibility.
- **Used by chapters**: 7 (owner)

### Scale rule
- **Owner chapter**: `7-azure-container-apps`
- **Canonical definition**: A **scale rule** (in Azure Container Apps) is a declarative policy that automatically adjusts the number of container replicas based on a metric (CPU, memory, HTTP requests per second, custom metrics); it enables horizontal autoscaling without manual intervention.
- **Used by chapters**: 7 (owner)

### Container App environment
- **Owner chapter**: `7-azure-container-apps`
- **Canonical definition**: A **Container App environment** is the shared infrastructure and network namespace in which one or more Container Apps run; it provides a virtual network boundary, shared logging, and a common internal DNS domain for service-to-service communication.
- **Used by chapters**: 7 (owner)

## Terms borrowed from earlier Parts

### Container
- **Defined in**: Part VII — Containers / `1-containers-vs-vms`
- **Reference link**: `/course-book/7-containers/1-containers-vs-vms/`

### Image
- **Defined in**: Part VII — Containers / `2-images-and-layers`
- **Reference link**: `/course-book/7-containers/2-images-and-layers/`

### Container registry
- **Defined in**: Part VII — Containers / `6-container-registries`
- **Reference link**: `/course-book/7-containers/6-container-registries/`

### Image tag
- **Defined in**: Part VII — Containers / `6-container-registries`
- **Reference link**: `/course-book/7-containers/6-container-registries/`

### Bearer token
- **Defined in**: Part V — Identity & Security / `5-` (JWT and tokens)
- **Reference link**: `/course-book/5-identity-and-security/` (adjust with actual chapter path)

### JWT
- **Defined in**: Part V — Identity & Security / `5-` (JWT and tokens)
- **Reference link**: `/course-book/5-identity-and-security/` (adjust with actual chapter path)

### Managed identity
- **Defined in**: Part V — Identity & Security / `8-` (placeholder for specific chapter)
- **Reference link**: `/course-book/5-identity-and-security/` (adjust with actual chapter path)

### ASP.NET Core
- **Defined in**: Part III — Application Development / `2-the-dotnet-platform`
- **Reference link**: `/course-book/3-application-development/2-the-dotnet-platform/`

