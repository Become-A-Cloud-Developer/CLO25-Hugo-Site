# CloudSoft-Careers

Reference project for the ACD course's **Uploads and Deep Probes** chapter under `content/exercises/6-storage-and-resilience/1-uploads-and-deep-probes/`.

## Purpose

A small ASP.NET Core MVC web app called `CloudCiCareers.Web` that runs an anonymous recruitment portal. The home page lists 3–4 hard-coded job postings (Cloud Engineer, Backend Developer, DevOps Specialist, Site Reliability Engineer); applicants upload a PDF CV against a posting; recruiters browse the resulting applications, edit status and notes, download the CV, or delete the record. The visible cue students can see propagating across the three exercises is the home page itself plus the recruiter listing — the same `Application` domain (Id, JobId, ApplicantName, ApplicantEmail, CvBlobName, SubmittedAt, Status, Notes) survives end-to-end, but the persistence backend is transformed underneath: in-memory + local-file in Ex 1, Azure Cosmos DB + Azure Blob Storage (managed-identity authenticated) in Ex 2, and finally observable through deep health probes in Ex 3.

- **Exercise 1** — Scaffold the MVC app (job listing + apply form + recruiter pages), validate uploads via PDF magic-bytes (`%PDF-`), persist via fragile `InMemoryApplicationStore` and `LocalFileBlobService` (writing to `./uploads/`), deploy via the OIDC pipeline pattern from the deployment chapter, layer Application Insights via the `secretref:` pattern from the logging chapter.
- **Exercise 2** — Replace fragile persistence with Azure Cosmos DB (serverless) and Azure Blob Storage. Both authenticated via the Container App's system-assigned managed identity using `DefaultAzureCredential` — no connection strings. Includes a Concept Deep Dive on Cosmos DB's data-plane RBAC (the `00000000-0000-0000-0000-000000000002` Cosmos DB Built-in Data Contributor role definition ID, which is a Cosmos data-plane role, separate from the regular Azure RBAC plane).
- **Exercise 3** — Add deep health probes. `/health/live` (liveness, no dependencies), `/health/ready` (readiness, gated on Cosmos + Blob via tagged checks), `/health` (diagnostic JSON detail). Custom `CosmosHealthCheck` and `BlobHealthCheck` classes. Wire Container Apps liveness + readiness probes via `az containerapp update --probe-type ...`. Final cleanup substep tears down `rg-careers-week7` AND `az ad app delete github-cloudci-careers-oidc`, mirroring the Week 6 chapter pattern.

## Layout

```text
reference/CloudSoft-Careers/
├── src/CloudCiCareers.Web/                     # The MVC app (final post-Ex 7.3 state)
│   ├── CloudCiCareers.Web.csproj
│   ├── Program.cs
│   ├── Controllers/HomeController.cs           # Job listing
│   ├── Controllers/ApplicationsController.cs   # Apply form + recruiter listing/detail/CV download/delete
│   ├── Controllers/HealthController.cs         # /health/live, /health/ready, /health JSON detail
│   ├── HealthChecks/CosmosHealthCheck.cs       # Tagged "ready", probes Cosmos
│   ├── HealthChecks/BlobHealthCheck.cs         # Tagged "ready", probes Blob
│   ├── Models/Application.cs                   # domain entity (Id, JobId, ApplicantName, ApplicantEmail, CvBlobName, SubmittedAt, Status, Notes)
│   ├── Models/Job.cs                           # hard-coded job posting
│   ├── Models/ApplicationStatus.cs             # enum: Submitted, Reviewing, Rejected, Hired
│   ├── Services/IApplicationStore.cs
│   ├── Services/CosmosApplicationStore.cs      # Cosmos-backed (managed identity)
│   ├── Services/IBlobService.cs
│   ├── Services/AzureBlobService.cs            # Azure Blob Storage-backed (managed identity)
│   ├── Views/Home/Index.cshtml                 # Job listing
│   ├── Views/Applications/Apply.cshtml         # Upload form
│   ├── Views/Applications/Index.cshtml         # Recruiter listing
│   ├── Views/Applications/Details.cshtml       # Recruiter detail (status edit, notes, CV download, delete)
│   ├── Views/Shared/_Layout.cshtml
│   ├── Dockerfile                              # multi-stage build (.NET 10 SDK → ASP.NET runtime, port 8080)
│   ├── .dockerignore
│   └── appsettings.Development.json            # dev-only Cosmos:Endpoint placeholder + Logging
├── .github/workflows/ci.yml                    # Final OIDC-authenticated pipeline (smoke test scoped to /health/live)
├── scripts/
│   ├── validate.mjs                            # Playwright capture of home page + apply form + recruiter listing
│   └── package.json
├── docs/
│   ├── EXERCISE-VALIDATION-REPORT.md           # Live-execution validation record
│   ├── screenshots/                            # week-7-home.png, week-7-apply.png, week-7-recruiter-list.png, week-7-detail.png
│   └── validation/                             # week-7-curl-matrix.txt, week-7-health-output.txt, week-7-app-insights-output.txt, week-7-probes.txt
├── CLAUDE.md
└── README.md
```

The `InMemoryApplicationStore.cs` and `LocalFileBlobService.cs` files from Exercise 1 are **not** present in the final state — Exercise 2 replaces both with the Cosmos-backed and Blob-backed implementations, since the cleaner story for the chapter's arc is *replace* rather than *layer*.

## Running locally

```bash
cd src/CloudCiCareers.Web
dotnet run
```

The app prints the port at startup (typically `http://localhost:5XXX`). Visit `/` to see the job listing, click **Apply** on any posting, upload a PDF, then visit `/applications` to see the recruiter view.

The persistence layer auto-detects environment: when the `Cosmos:Endpoint` configuration value is **unset** (the local-dev default), `Program.cs` falls back to `InMemoryApplicationStore` + `LocalFileBlobService` (the latter writes uploaded CVs to `./uploads/<guid>.pdf`). When `Cosmos:Endpoint` **is** set (e.g. via `dotnet user-secrets` for cloud-emulator testing or in the deployed Container App's env vars), the app uses `CosmosApplicationStore` + `AzureBlobService`, both authenticated via `DefaultAzureCredential` — which works locally only if the Azure CLI is signed in to a tenant that has been granted the Cosmos data-plane role and the Blob `Storage Blob Data Contributor` role on the live resources.

```bash
# Submit an application against the seeded "Cloud Engineer" job (id=1)
curl -i -X POST http://localhost:5XXX/jobs/1/apply \
  -F "ApplicantName=Alice" \
  -F "ApplicantEmail=alice@example.com" \
  -F "Cv=@./sample.pdf;type=application/pdf"

# List submitted applications
curl http://localhost:5XXX/applications
```

Magic-bytes validation rejects any upload whose first five bytes aren't `%PDF-` regardless of the declared content-type or filename — try uploading a renamed `.exe` or a `.txt` to see the `400 Bad Request` path.

## Building the container locally

```bash
cd src/CloudCiCareers.Web
docker build -t cloudci-careers:local .
docker run --rm -p 8080:8080 cloudci-careers:local
```

The local container has no `Cosmos__Endpoint` env var, so it falls back to `InMemoryApplicationStore` + `LocalFileBlobService` — uploaded CVs land in the container's ephemeral `/app/uploads/` and are lost when the container exits, which is the deliberately fragile state Ex 1 finishes in. The deployed Container App injects `Cosmos__Endpoint`, `Cosmos__Database`, `Cosmos__Container`, and `Storage__BlobEndpoint` as plain env vars (no secrets — the data-plane authorisation flows through the managed identity), so the production path uses the durable backends.

## Exercise progression

Each exercise corresponds to one or more commits in the live GitHub repository (`larsappel/cloudci-careers`). The state in this directory represents the **final** state after all three exercises are complete: Cosmos-backed persistence, Blob-backed CV storage, deep health probes wired to Container Apps liveness + readiness, and the deployment torn down.

## Live deployment

See `docs/EXERCISE-VALIDATION-REPORT.md` for the live URL, resource names, GitHub Actions run links, and manual verification steps.

## Validation

Smoke-check the live deployment with the included Playwright script:

```bash
cd scripts
npm install
FQDN=<container-app-fqdn> node validate.mjs
```

The script loads `https://$FQDN/`, asserts HTTP 200 and that all four job postings render, takes a full-page screenshot (`docs/screenshots/week-7-home.png`), walks the apply form for one job (`docs/screenshots/week-7-apply.png`), then loads `/applications` and screenshots the recruiter listing (`docs/screenshots/week-7-recruiter-list.png`) and one detail page (`docs/screenshots/week-7-detail.png`). The curl matrix, health-probe transcript, App Insights query transcript, and probe-config JSON in `docs/validation/` are produced by the chapter-development run; re-run those probes manually against any future re-deployment.
