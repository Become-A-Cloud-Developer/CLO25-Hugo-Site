# Week 7 prompt — Recruitment Uploads and Deep Health Probes

Paste the section below into a fresh `/develop-exercise` invocation when ready
to run Week 7 end-to-end. The prompt is self-contained: every Phase A
decision is pre-aligned, so the skill should skip directly to Phase B
execution.

---

Develop the entire chapter for ACD Course Week 7 (v.21, **May 18–22, 2026**): "Blob Storage och hälsokontroller" — Blob Storage and Health Checks. The studieguide schedule:

| Day | Where | Content |
|-----|-------|---------|
| Wed | Campus, full day | File upload, PDF validation, `IBlobService` |
| Thu | Campus, full day | Azure Storage Account, health checks, Google OAuth |
| Fri | Online, half day | Demo: file upload and health checks in production |

## Treat this prompt as the alignment

This prompt fully specifies every Phase A decision. **Do NOT enter plan mode and do NOT ask clarifying questions.** Treat the brief below as the approved chapter plan and proceed straight to Phase B execution as defined in `.claude/skills/develop-exercise/PHASES.md`.

If you hit a *genuinely ambiguous* decision during execution (a name collision, a permissions failure you cannot self-recover from, an Azure SKU not available in the region), pause and ask the user. Otherwise run end-to-end without interactive approval. The default `CLAUDE.md` rule "ask before committing" is **explicitly overridden** for this run by the commit authorisation in the *Commit & push* section below.

**Google OAuth is explicitly out of scope for this chapter.** The studieguide mentions it under Thursday's content, but it was already postponed once in Week 4 (commit `8b3cab1`, "Ex 4.4: mark as draft (Google OIDC postponed)") and the per-week-pace doesn't have room for it alongside Cosmos + Blob + deep-probe content. Defer it again. The portal is anonymous — no login flow of any kind.

## Pre-aligned scope

| Decision | Value |
|----------|-------|
| Number of exercises | 3 |
| Parent section | `content/exercises/6-storage-and-resilience/` (**new** parent section — create `_index.md` with title "Storage and Resilience" and `weight = 6`) |
| Subsection directory | `content/exercises/6-storage-and-resilience/1-uploads-and-deep-probes/` |
| Subsection title | `1. File Uploads and Deep Health Probes` |
| Frontmatter (all files) | `program = "CLO"`, `cohort = "25"`, `courses = ["ACD"]`, `weight` matches file order |
| Reference project | **New** `reference/CloudSoft-Careers/` (sibling to `CloudSoft-Pipeline`, `CloudSoft-Api`) — follow `develop-exercise/REFERENCE-PROJECT.md` |
| Inner project | `CloudCiCareers.Web` (new .NET 10 MVC project, `dotnet new mvc --framework net10.0`) |
| Domain | Recruitment portal — applicants apply for jobs by uploading a PDF CV; recruiters list/edit/delete applications. **Anonymous** — no login |
| Cloud target | Azure — fresh `rg-careers-week7` (northeurope), new ACR, new Container App, new Storage Account, new CosmosDB account (serverless), new App Insights, new Entra OIDC app |
| GitHub repo | `larsappel/cloudci-careers` (new) |
| External accounts | Same as Weeks 4–6 — GitHub `larsappel`, Azure subscription Lars Appel. No new third-party accounts |
| Validation method | Playwright captures of the Apply flow + curl matrix against `/health/live`, `/health/ready`, `/health` (JSON detail) |
| Cleanup scope | Final substep tears down `rg-careers-week7` AND `az ad app delete` for the new OIDC app — mirror the Week 6 Ex 6.3 Step 13 pattern |

## Pre-flight: confirm Week 6 teardown is complete

Before provisioning anything, run:

```bash
az group exists -n rg-api-week6
az ad app list --display-name github-cloudci-api-oidc -o tsv
```

If either still returns truthy values, run the Week 6 cleanup commands first:

```bash
az group delete -n rg-api-week6 --yes --no-wait
az ad app delete --id cbe3d715-650e-44d6-9802-381870be8c63
```

Then proceed. The Week 7 chapter creates *fresh* resources; do not extend Week 6's resource group.

## Existing state — what carries over from previous chapters

These are *durable artifacts* the student already has. Do not re-teach them. Reference them as prerequisites in Ex 7.1.

- **CI/CD pipeline pattern** — students built one in Week 4 (Docker → ACR → Container App via OIDC) and re-applied it in Week 6 against `larsappel/cloudci-api`. Ex 7.1's deploy step abbreviates the setup using the same exact commands but for the new repo and resource group, with cross-references back to the deployment chapter for explanations.
- **Observability pattern** — students wired `ILogger<T>`, structured message templates, and Application Insights in Week 5 and reused the pattern in Week 6. Ex 7.1 inherits all of it: register App Insights the same way, watch request telemetry flow.
- **`secretref:` env var pattern** — students injected the App Insights connection string this way in Weeks 5 and 6. Ex 7.1 reuses it for App Insights once more. Ex 7.2 deliberately moves AWAY from secretref for Cosmos and Blob authentication — managed identity replaces shared-secret connection strings entirely. The contrast is part of the lesson.
- **Three-tier MVC shape, repository pattern, Cosmos client** — students saw these in BCD's `10-webapp-development/3-data-layer/` exercises. Ex 7.1 establishes the shape quickly without dwelling on theory; Ex 7.2 introduces Cosmos with a focus on managed-identity data-plane RBAC, which is *new* even for students who've used Cosmos before.
- **Per-memory: reference projects are teacher-only.** Do not direct student-facing exercises to read or peek at any `reference/` path or scripts. The `reference/CloudSoft-Recruitment/` project shapes what `CloudCiCareers.Web` looks like internally, but the exercises must establish the project's structure on their own without pointing at it.

## Three-exercise arc

The whole chapter builds one MVC application called `CloudCiCareers.Web` — a small recruitment portal where:

- The home page lists hard-coded job postings (3–4 seeded entries: "Cloud Engineer", "Backend Developer", "DevOps Specialist", "Site Reliability Engineer").
- An applicant clicks a job, fills in name + email, attaches a PDF CV, submits.
- The recruiter visits `/applications` and sees the list of submitted applications, can click into a detail page to update Status (Submitted → Under Review → Rejected → Hired) and edit Notes, can download the CV, can delete the application.
- All anonymous; the portal does not enforce auth.

The same `Application` domain (Id, JobId, ApplicantName, ApplicantEmail, CvBlobName, SubmittedAt, Status, Notes) survives all three exercises; what changes is the *persistence backend* (in-memory + local files → Cosmos + Azure Blob) and the *operational surface* (no probes → deep probes wired to Container Apps).

### Exercise 1 — MVC scaffold, Apply form, PDF magic-bytes, deploy

Scaffold a fresh `CloudCiCareers.Web` MVC project (`dotnet new mvc -o CloudCiCareers.Web --framework net10.0`). Add:

- An `Application` entity (in `Models/Application.cs`) and a `Job` record (in `Models/Job.cs`).
- `IApplicationStore` interface + `InMemoryApplicationStore` (singleton) — same pattern as Week 6's `IQuoteStore`. CRUD methods: `GetAll()`, `GetById(string id)`, `Create(Application)`, `UpdateStatus(string id, ApplicationStatus status, string? notes)`, `Delete(string id)`.
- A hard-coded `IJobCatalog` with the 3–4 jobs seeded.
- `IBlobService` interface + `LocalFileBlobService` implementation that writes uploads to `./uploads/` (gitignored). Methods: `UploadAsync(string name, Stream content, CancellationToken)`, `OpenReadAsync(string name, CancellationToken)`, `DeleteAsync(string name, CancellationToken)`.
- A static helper `PdfValidation.IsPdf(Stream)` that reads the first four bytes and checks for `0x25 0x50 0x44 0x46` (`%PDF`). Reset the stream position after the check.
- `JobsController`: `Index()` lists jobs, `Apply(int id)` shows the apply form, `[HttpPost] Apply(int id, ApplyForm form, IFormFile cv)` validates magic bytes, generates `cvBlobName = $"{Guid.NewGuid()}.pdf"`, saves via `IBlobService`, persists the `Application`, redirects to `/applications/{newId}` with a TempData "thanks" message. Uses antiforgery token.
- `ApplicationsController`: `Index()` recruiter listing, `Details(string id)` detail view, `[HttpPost] UpdateStatus(string id, ApplicationStatus newStatus, string? notes)`, `[HttpPost] Delete(string id)`, `Cv(string id)` proxies the CV bytes via `IBlobService.OpenReadAsync` with `Content-Type: application/pdf` and `Content-Disposition: inline`.
- Razor views: `Views/Jobs/Index.cshtml` (job grid), `Views/Jobs/Apply.cshtml` (form with `enctype="multipart/form-data"` and `[ValidateAntiForgeryToken]`), `Views/Applications/Index.cshtml` (table with status badges), `Views/Applications/Details.cshtml` (form to edit status/notes + Delete button + CV download link).

Set up the Container Apps + ACR + OIDC pipeline as a single Step ("Set up the pipeline as you did in the deployment chapter — same commands, new names"), with the new resource names baked in. Do not re-explain OIDC; cross-reference back to that chapter.

Add Application Insights as in Week 5 — students have already learned this. App Insights component name: `cloudci-careers-insights`. Workspace-based mode against the auto-provisioned Log Analytics workspace inside `rg-careers-week7`. Connection string injected as a Container Apps secret + `secretref:` env var.

End-state at end of Ex 7.1:

- Live MVC app at `https://ca-careers-week7.<env>.northeurope.azurecontainerapps.io/`.
- Home page lists 3–4 jobs.
- An applicant can apply with a name, email, and a valid PDF; an invalid file (renamed `.pdf` of arbitrary content) is rejected with a validation error.
- The recruiter listing page shows the application, the detail page allows status edits and CV download.
- App Insights `cloudci-careers-insights` receiving request telemetry.
- **The persistence is intentionally fragile** — every Container App revision rollover wipes the in-memory store and the `./uploads/` directory. Mention this fragility explicitly at the end of Ex 7.1 as the motivation for Ex 7.2.

### Exercise 2 — CosmosDB + Azure Blob via managed identity

Replace the in-memory store with a `CosmosApplicationStore` and the local-file blob with an `AzureBlobService`. Both authenticate via the Container App's system-assigned managed identity — **no connection strings**, no secretref shortcuts. This is the contrast moment with Weeks 5/6.

**Provisioning:**

```bash
# Storage Account
az storage account create \
  -n stcareers<rand> \
  -g rg-careers-week7 \
  -l northeurope \
  --sku Standard_LRS \
  --allow-blob-public-access false

az storage container create \
  --account-name stcareers<rand> \
  --name cvs \
  --auth-mode login

# CosmosDB serverless
az cosmosdb create \
  -n cosmos-careers-<rand> \
  -g rg-careers-week7 \
  --capabilities EnableServerless \
  --default-consistency-level Session

az cosmosdb sql database create \
  -a cosmos-careers-<rand> -g rg-careers-week7 -n careers

az cosmosdb sql container create \
  -a cosmos-careers-<rand> -g rg-careers-week7 \
  -d careers -n applications \
  --partition-key-path /id
```

**Managed-identity RBAC** — the wrinkle worth a Concept Deep Dive:

```bash
PRINCIPAL_ID=$(az containerapp show -n ca-careers-week7 -g rg-careers-week7 \
  --query identity.principalId -o tsv)

# Storage uses regular Azure RBAC
az role assignment create \
  --assignee "$PRINCIPAL_ID" \
  --role "Storage Blob Data Contributor" \
  --scope "$(az storage account show -n stcareers<rand> --query id -o tsv)"

# CosmosDB has its OWN data-plane RBAC, separate from Azure RBAC
az cosmosdb sql role assignment create \
  --account-name cosmos-careers-<rand> \
  -g rg-careers-week7 \
  --scope "/" \
  --principal-id "$PRINCIPAL_ID" \
  --role-definition-id 00000000-0000-0000-0000-000000000002
```

The Concept Deep Dive in Ex 7.2 covers exactly this: **`Cosmos DB Built-in Data Contributor` is NOT a regular Azure role.** The role definition ID `00000000-0000-0000-0000-000000000002` is a Cosmos data-plane role. Many students see this as a confusing Azure-RBAC paper-cut — surface it explicitly, document why the wrinkle exists (separation between control-plane operations like "rename the database" and data-plane operations like "read documents"), and explain how to discover the role ID via `az cosmosdb sql role definition list`.

**Code changes:**

- Pin packages: `Microsoft.Azure.Cosmos` 3.x, `Azure.Storage.Blobs` 12.x, `Azure.Identity` for `DefaultAzureCredential`. The Container App acquires tokens via the system-assigned managed identity automatically — `DefaultAzureCredential` Just Works in Container Apps.
- `CosmosApplicationStore` uses `CosmosClient(accountEndpoint, new DefaultAzureCredential())`. Container is `applications`, partition key on `/id`. Translation between the `Application` C# record and Cosmos JSON is straightforward — `Microsoft.Azure.Cosmos` uses `System.Text.Json` natively.
- `AzureBlobService` uses `BlobServiceClient(serviceUri, new DefaultAzureCredential())`. Container `cvs`, blob name = `cvBlobName` from Ex 7.1.
- Configuration:
  - `Cosmos:Endpoint` = `https://cosmos-careers-<rand>.documents.azure.com:443/`
  - `Cosmos:Database` = `careers`
  - `Cosmos:Container` = `applications`
  - `Storage:BlobEndpoint` = `https://stcareers<rand>.blob.core.windows.net`
  - `Storage:Container` = `cvs`
- Container App env vars (all non-secret because managed identity is the auth):
  - `Cosmos__Endpoint`, `Cosmos__Database`, `Cosmos__Container`
  - `Storage__BlobEndpoint`, `Storage__Container`
- `Program.cs` registers `CosmosApplicationStore` and `AzureBlobService` as the production implementations; `LocalFileBlobService` and `InMemoryApplicationStore` remain in the assembly for development. A simple feature switch (read `Cosmos:Endpoint` — if set, register the cloud impls; otherwise fall back to the local ones) keeps `dotnet run` from `localhost` working without any Azure resources. **Document this as a deliberate convention, not a feature-flag library**: the lesson is the registration logic, not Microsoft.FeatureManagement.

**Concept Deep Dives in Ex 7.2:**

- Why managed identity beats connection strings: rotation, attribution, blast radius. Compare against Week 6's `secretref:` for the API key — both work, the managed-identity approach moves the trust boundary from "whoever holds this string" to "this specific compute identity in this subscription."
- Cosmos data-plane RBAC and the `00000000-0000-0000-0000-000000000002` role ID. Why it exists separately from Azure RBAC.
- Why Cosmos serverless: predictable per-request cost (cents per chapter), no RU/s tuning, cleanly torn down.
- Container Apps revisions and managed-identity propagation: when you assign a role, the running revision needs a few seconds for the assignment to propagate. Common Mistakes should call this out — first request after role assignment may 403; retry succeeds.

**Visible cue at end of Ex 7.2:**

- Apply for a job, deploy a new revision, the application persists (vs Ex 7.1 where it would have vanished).
- `az cosmosdb sql query --account-name cosmos-careers-<rand> --database-name careers --container-name applications --query-text "SELECT * FROM c"` shows the document in Cosmos.
- `az storage blob list --account-name stcareers<rand> --container-name cvs --auth-mode login -o table` shows the uploaded PDF.

### Exercise 3 — Deep health probes and cleanup

Add `AddHealthChecks()` and three endpoints with distinct semantics:

| Path | Predicate | Purpose | Container Apps probe |
|------|-----------|---------|---------------------|
| `/health/live` | `_ => false` (no checks) | Process is alive — only that ASP.NET Core is responsive. Independent of Cosmos/Blob. | **Liveness** — restart if 503 |
| `/health/ready` | `c => c.Tags.Contains("ready")` | App + dependencies. Cosmos + Blob probes tagged `"ready"`. | **Readiness** — pull from rotation if 503 |
| `/health` | (all checks) + JSON `ResponseWriter` | Diagnostic detail — per-check status + duration + exception. For humans, dashboards, alerting. | (not wired) |

Custom checks:

- `CosmosHealthCheck` — calls `container.ReadContainerAsync()` with a 5-second timeout. Tagged `"ready"`.
- `BlobHealthCheck` — calls `containerClient.ExistsAsync()` with a 5-second timeout. Tagged `"ready"`.

Roll the JSON `ResponseWriter` by hand (10 lines using `System.Text.Json`); don't take a dependency on `AspNetCore.HealthChecks.UI.Client` — mention it in Going Deeper. Output shape:

```json
{
  "status": "Healthy",
  "checks": [
    {"name": "self", "status": "Healthy", "duration_ms": 0},
    {"name": "cosmos", "status": "Healthy", "duration_ms": 12},
    {"name": "blob", "status": "Healthy", "duration_ms": 8}
  ]
}
```

Wire Container Apps probes via `az containerapp update` with `--probe-type Liveness ...` etc.:

```bash
az containerapp update \
  -n ca-careers-week7 -g rg-careers-week7 \
  --probe-type Liveness \
  --probe-path /health/live \
  --probe-port 8080 \
  --probe-period-seconds 10 \
  --probe-timeout-seconds 3 \
  --probe-failure-threshold 3
# Repeat for Readiness with --probe-path /health/ready
```

**Concept Deep Dives in Ex 7.3:**

- **Why liveness must NOT depend on external services.** If Cosmos is down, killing the container does not fix Cosmos — it just creates a restart loop and makes the outage worse. Liveness is "is *this process* alive," not "is everything fine." This is the load-bearing distinction the studieguide's reflection question (`Vad är syftet med hälsokontroller i en produktionsmiljö?`) is gesturing at.
- **Readiness as traffic gate.** When Cosmos is down, the app can't serve requests meaningfully — readiness should fail and Container Apps pulls the replica from rotation. Once Cosmos comes back, readiness recovers and traffic returns. No restarts; no thundering herd against the recovering dependency.
- **Why a third diagnostic endpoint.** `/health` exposes the per-check breakdown for humans and dashboards. Don't wire it as a probe — Container Apps shouldn't react to "blob check is degraded but cosmos is fine." That's a *human* signal, not a *machine* signal.
- **Health-check timeouts.** Without a timeout, a hung Cosmos call freezes the readiness endpoint, which makes Container Apps think the app is failing. 5 seconds per check; tune based on dependency SLAs.
- **Operational simulation.** Show how to break readiness deliberately by removing the Cosmos role assignment, watching `/health/ready` flip to 503, watching Container Apps stop sending traffic — then re-grant the role and watch recovery. (Optional, but it's the kind of operational drill that makes the chapter memorable.)

End the chapter and the live cloud lab with the **cleanup substep**, modeled on Week 6 Ex 6.3 Step 13:

```bash
APP_ID=$(az ad app list --display-name github-cloudci-careers-oidc --query "[0].appId" -o tsv)
az group delete -n rg-careers-week7 --yes --no-wait
az ad app delete --id "$APP_ID"
```

Plus the verification commands and the explanatory paragraph about the tenant-vs-subscription split. The optional `gh secret delete` block (`AZURE_CLIENT_ID`/`AZURE_TENANT_ID`/`AZURE_SUBSCRIPTION_ID`/`ACR_NAME`) follows the same pattern as Week 6.

End-state at end of Ex 7.3:

- `curl https://<fqdn>/health/live` → `200` always (process is alive).
- `curl https://<fqdn>/health/ready` → `200` when Cosmos and Blob are reachable; `503` when either is down.
- `curl -s https://<fqdn>/health | jq '.'` → JSON breakdown with per-check status and duration.
- Container Apps probes wired: liveness on `/health/live`, readiness on `/health/ready`.
- After cleanup: `az group exists -n rg-careers-week7` → `false`; `az ad app list --display-name github-cloudci-careers-oidc` → empty.

## Validation (Phase 4)

After all three exercises succeed end-to-end against the live Container App (and *before* the cleanup substep is executed):

1. **Apply-flow Playwright capture.** Extend a new `reference/CloudSoft-Careers/scripts/validate.mjs` (copy the shape from `reference/CloudSoft-Api/scripts/validate.mjs`). Walk:
   - Load home, screenshot the job listing → `docs/screenshots/week-7-jobs.png`.
   - Click a job, screenshot the apply form → `docs/screenshots/week-7-apply-form.png`.
   - Fill the form with a small valid PDF (you can generate a 4-byte file starting with `%PDF` for the magic-bytes check; a real one is nicer if available), submit, screenshot the redirect page → `docs/screenshots/week-7-application-thanks.png`.
   - Visit `/applications`, screenshot the listing → `docs/screenshots/week-7-listing.png`.
   - Click into the detail page, screenshot the form → `docs/screenshots/week-7-detail.png`.
   - (Optional) Submit a non-PDF (txt renamed `.pdf`), screenshot the validation error → `docs/screenshots/week-7-validation-error.png`.

2. **Health-probe curl matrix.** Save to `reference/CloudSoft-Careers/docs/validation/week-7-health-output.txt`:
   - `curl -is https://<fqdn>/health/live` (expect `HTTP/2 200`, body `Healthy`).
   - `curl -is https://<fqdn>/health/ready` (expect `HTTP/2 200`).
   - `curl -s https://<fqdn>/health | jq '.'` (full JSON; redact endpoint hostnames if present).
   - **Failure simulation** (optional but recommended): temporarily remove the Cosmos data-plane role, hit `/health/ready` (expect `503` + JSON showing cosmos check failed), restore the role, hit again (`200`). Capture both transcripts.

3. **App Insights telemetry transcript.** Same shape as Weeks 5/6:

   ```kusto
   requests | where timestamp > ago(20m) | summarize count() by name, resultCode | order by count_ desc
   dependencies | where timestamp > ago(20m) | summarize count() by type, target | order by count_ desc
   exceptions | where timestamp > ago(20m) | summarize count() by type | order by count_ desc
   ```

   The dependencies query is new — `Microsoft.Azure.Cosmos` and `Azure.Storage.Blobs` both auto-instrument as App Insights dependencies. Save to `reference/CloudSoft-Careers/docs/validation/week-7-app-insights-output.txt`.

4. **Container Apps probe configuration verification.**

   ```bash
   az containerapp show -g rg-careers-week7 -n ca-careers-week7 \
     --query 'properties.template.containers[0].probes' -o json
   ```

   Expected: a Liveness probe on `/health/live` and a Readiness probe on `/health/ready`. Capture as `reference/CloudSoft-Careers/docs/validation/week-7-probes.txt`.

5. **Application Map screenshot deferred.** Same story as Weeks 5/6 — Portal blades require interactive Microsoft SSO. Document as a deviation; the App Insights query transcripts (which you should still produce) prove dependencies and requests are flowing.

## Reports (Phase 5)

Create `reference/CloudSoft-Careers/docs/EXERCISE-VALIDATION-REPORT.md` from scratch, following the template in `develop-exercise/REFERENCE-PROJECT.md`. Sections (mirror Week 6's populated report):

- Overview (one paragraph, dated).
- Resources Provisioned (table of new Azure + Entra resources for `rg-careers-week7`, including the Cosmos account, Storage Account, container, and the data-plane role assignment).
- Live URL.
- New Endpoints (`/`, `/jobs/{id}/apply`, `/applications`, `/applications/{id}`, `/applications/{id}/cv`, `/health/live`, `/health/ready`, `/health`).
- GitHub Repository (URL + final secret list).
- Workflow Run History (one row per stage that validates a different exercise).
- Build SHA Progression (visible cue per exercise — the application-listing page gains entries that survive deploy in Ex 7.2 and the health endpoints get wired in Ex 7.3).
- Validation Artifacts (Playwright screenshots, health probe transcripts, KQL transcripts, probe-config JSON).
- Manual Verification Steps (copy-paste curl + browser commands).
- Deviations from Exercise Text.
- Status.

Update `reference/CloudSoft-Careers/CLAUDE.md` and `reference/CloudSoft-Careers/README.md` to describe the new project and link to the three exercise files. Per memory, the README and CLAUDE.md are teacher-only; the Hugo content does not link to them.

Write a chat-output summary at the end with: live URL, what's new, sample browser + curl commands, manual verification steps, and outstanding decisions (cloud teardown, push to remote, secret revocation).

## Commit & push authorisation

Commit the new exercises + reference project as a **single git commit** on `main` of the Hugo site. Suggested message style matches Weeks 4–6:

```
Add Recruitment Uploads and Deep Health Probes chapter (Course Week 7)
```

Body should follow the same pattern as commits `046cc0a`, `dfca405`, and Week 6's commit: bullet list of what landed, references to phase outcomes, the trailing `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>` footer.

**Do NOT `git push`.** Leave the commit local. The user will review and push manually.

## Operating notes

- Use `Agent` calls for Phase 1 (parallel authoring) and Phase 2 (cross-review). Drive Phases 3–5 directly as the leader (no subagents — too much shared state).
- Use `TaskCreate`/`TaskUpdate` to track per-phase progress.
- The user's GitHub PAT for the repo is already authenticated via `gh auth status`. The Azure CLI is signed in (`az account show`).
- Follow `.claude/skills/create-exercise/{TEMPLATE,GUIDE,EXAMPLE}.md` for exercise-markdown rules.
- After the run, verify `hugo --gc --minify` builds cleanly.
- Set up the new GitHub repo with `gh repo create larsappel/cloudci-careers --public --source=<live-dir> --push` before the first push.
- The new Entra OIDC app should be named `github-cloudci-careers-oidc` (parallel to `github-cloudci-oidc` and `github-cloudci-api-oidc`); federated subject `repo:larsappel/cloudci-careers:ref:refs/heads/main`.
- The new Application Insights component should be named `cloudci-careers-insights` and use workspace-based mode against the auto-provisioned Log Analytics workspace inside `rg-careers-week7`.
- **Pre-bake these Week 6 deviation fixes into the exercise text from the start.** They are known issues; do not let students or future agents re-discover them:
  - Ex 7.1 Step 1: `dotnet new mvc -o CloudCiCareers.Web --framework net10.0`. The framework pin is the same one Week 6 needed.
  - Ex 7.1 (App Insights step): pin `Microsoft.ApplicationInsights.AspNetCore` to `2.22.0` — the 3.x line throws on missing connection string and breaks `dotnet run` against localhost. Same pin Week 5/6 used.
  - Ex 7.1 (workflow): smoke test on `/health/live` — this works from Ex 7.3 onwards but Ex 7.1's deploy doesn't have `/health/live` yet. Use `/` (the home page, anonymous) as the smoke target during Ex 7.1, then update to `/health/live` in Ex 7.3 when the endpoints land.
  - The MVC scaffold doesn't include `Microsoft.AspNetCore.OpenApi` (that was an API-template thing) — no removal needed.
- **CosmosDB free-tier check.** Each Azure subscription gets one free-tier Cosmos account. If the user's free tier is taken (verify with `az cosmosdb list --query "[?enableFreeTier].name" -o tsv`), the `--capabilities EnableServerless` flag is the right choice anyway — it sidesteps the free-tier question. Mention serverless as a deliberate cost-management choice in the Concept Deep Dive.
- Pipe the `IFormFile` upload stream directly to `IBlobService` — do not buffer to disk. Azure Blob accepts streams natively. Mention this in a Common Mistake.
- **PDF magic bytes**: bytes `0x25 0x50 0x44 0x46` (`%PDF`). Read first four bytes of the upload, compare, reset stream position before saving. Cite the spec (RFC 8118 / ISO 32000) in passing — don't dwell.
- **PDFs are not safe just because they're valid PDFs.** A magic-bytes check confirms the file *is* a PDF; it does not confirm the PDF is safe. Antivirus, sandboxed parsers, content stripping are separate layers. Make this distinction explicit in a Concept Deep Dive — the studieguide's reflection question (`Varför validerar man filinnehåll (magic bytes) och inte bara filändelsen?`) is asking exactly this.
- **Antiforgery** on the apply form. `<form method="post" enctype="multipart/form-data">` with `@Html.AntiForgeryToken()` and `[ValidateAntiForgeryToken]` on the action. Same pattern as the BCD authentication chapter.
- **Body size limits.** ASP.NET Core's default request size limit is 30 MB; Container Apps' ingress accepts that. Don't change defaults; mention the constraint chain (ingress → Kestrel → ASP.NET Core multipart) in Going Deeper.
- **Status enum**: `ApplicationStatus` with `Submitted`, `UnderReview`, `Rejected`, `Hired`. Show the conventional pattern of mapping to a `<select>` in the Razor view.
- **Per-memory: don't reference `reference/` paths or any test harness scripts in student-facing exercise text.** The reference project supports the chapter for teachers/agents only.

Treat this prompt as full authorisation to proceed end-to-end without further confirmation, with the single carve-out above for genuinely ambiguous decisions.
