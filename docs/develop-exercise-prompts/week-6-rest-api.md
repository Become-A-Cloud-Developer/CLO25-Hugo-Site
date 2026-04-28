# Week 6 prompt — REST API and DTOs

Paste the section below into a fresh `/develop-exercise` invocation when ready
to run Week 6 end-to-end. The prompt is self-contained: every Phase A
decision is pre-aligned, so the skill should skip directly to Phase B
execution.

---

Develop the entire chapter for ACD Course Week 6 (v.20, **May 11–15, 2026**): "REST API och DTOs" — REST API and DTOs. The studieguide schedule:

| Day | Where | Content |
|-----|-------|---------|
| Wed | Campus, full day | API controllers alongside MVC, DTOs, Swagger |
| Thu | Campus, full day | JWT authentication, API-key middleware |
| Fri | Online, half day | Demo: API calls and Swagger documentation |

## Treat this prompt as the alignment

This prompt fully specifies every Phase A decision. **Do NOT enter plan mode and do NOT ask clarifying questions.** Treat the brief below as the approved chapter plan and proceed straight to Phase B execution as defined in `.claude/skills/develop-exercise/PHASES.md`.

If you hit a *genuinely ambiguous* decision during execution (a name collision, a permissions failure you cannot self-recover from, an Azure SKU not available in the region), pause and ask the user. Otherwise run end-to-end without interactive approval. The default `CLAUDE.md` rule "ask before committing" is **explicitly overridden** for this run by the commit authorisation in the *Commit & push* section below.

## Pre-aligned scope

| Decision | Value |
|----------|-------|
| Number of exercises | 3 |
| Directory | `content/exercises/4-services-and-apis/1-rest-api-and-dtos/` (create the parent `4-services-and-apis/` section if it does not exist) |
| Subsection title | `1. REST API and DTOs` |
| Section title | `Services and APIs` (the new parent section, weight 4) |
| Frontmatter (all files) | `program = "CLO"`, `cohort = "25"`, `courses = ["ACD"]`, `weight` matches file order |
| Reference project | **New** `reference/CloudSoft-Api/` (sibling to `CloudSoft-Pipeline`); follow `develop-exercise/REFERENCE-PROJECT.md` |
| Inner project | `CloudCiApi` (new .NET 10 Web API project, controllers-based, NOT minimal API) |
| Cloud target | Azure — fresh `rg-api-week6` resource group (northeurope), new ACR, new Container App, new App Insights, new Entra OIDC app |
| GitHub repo | `larsappel/cloudci-api` (new) |
| External accounts | Same as Week 4/5 — GitHub `larsappel`, Azure subscription Lars Appel. No new third-party accounts. |
| Validation method | Playwright screenshot of `/swagger` UI + `curl` matrix against `/api/quotes` (anonymous → 401, with key → 200, with JWT → 200) |
| Cleanup scope | Final substep tears down `rg-api-week6` AND `az ad app delete` for the new OIDC app — mirror the Week 5 Ex 5.3 Step 14 pattern |

## Pre-flight: confirm Week 5 teardown is complete

Before provisioning anything, run:

```bash
az group exists -n rg-cicd-week4
az ad app list --display-name github-cloudci-oidc -o tsv
```

If either still returns truthy values, run the Week 5 cleanup commands first:

```bash
az group delete -n rg-cicd-week4 --yes --no-wait
az ad app delete --id 7c11e4ce-91cd-4ba3-9fce-820669f397fe
```

Then proceed. The Week 6 chapter creates *fresh* resources; do not extend Week 5's resource group.

## Existing state — what carries over from previous chapters

These are *durable artifacts* the student already has. Do not re-teach them. Reference them as prerequisites in Ex 6.1.

- **CI/CD pipeline pattern** — students built one in Week 4 (Docker → ACR → Container App via OIDC). Ex 6.1's Step 1 abbreviates the setup using the same exact commands but for the new repo and resource group, with cross-references back to the Week 4 chapter for explanations.
- **Observability pattern** — students wired `ILogger<T>`, structured message templates, and Application Insights in Week 5. Ex 6.1 inherits all of it: register App Insights the same way, add `[FromBody]`-bound DTOs that get logged, watch them flow into the Failures blade when validation errors occur.
- **`secretref:` env var pattern** — students injected the App Insights connection string this way in Week 5. Ex 6.2 (API key) and Ex 6.3 (JWT signing key) reuse the exact same pattern.

## Three-exercise arc

The whole chapter builds one application called `CloudCiApi` — a quotes API with three endpoints. The same `Quote` domain (id, author, text, createdAt) survives all three exercises; what changes is the *surface* (controllers + DTOs + Swagger), then the *gate* (API key), then the *identity model* (JWT).

### Exercise 1 — REST API controllers + DTOs + Swagger

Scaffold a new `CloudCiApi` Web API project (`dotnet new webapi -o CloudCiApi --use-controllers`). Add:

- A `Quote` entity (in-memory, no database — that's a later chapter).
- An `IQuoteStore` singleton with seed data (3–4 quotes hard-coded).
- A `QuoteDto` (output shape) and `CreateQuoteRequest` (input shape) — show the *why* of separating database models from wire shapes (we don't have a database yet, but the principle stands: DTOs are wire contracts, entities are domain).
- A `QuotesController : ControllerBase` with `[ApiController]` and `[Route("api/[controller]")]`. Three actions:
  - `[HttpGet]` → `Ok(IEnumerable<QuoteDto>)`
  - `[HttpGet("{id:int}")]` → `Ok(QuoteDto)` or `NotFound()`
  - `[HttpPost]` → `CreatedAtAction(nameof(GetById), new { id = ... }, QuoteDto)` returning 201 + `Location` header.
- `Swashbuckle.AspNetCore` package + `services.AddEndpointsApiExplorer()` + `services.AddSwaggerGen()`. Wire `app.UseSwagger()` and `app.UseSwaggerUI()` only in Development *and* in Production (this is a learning artifact — note the trade-off in a Concept Deep Dive about not exposing the API surface in real production deployments).

Set up the Container Apps + ACR + OIDC pipeline as a single Step 1 ("Set up the pipeline as you did in Week 4 — same commands, new names"), with the new resource names baked in. Do not re-explain OIDC; cross-reference back to the previous chapter.

Visible cue: hit `https://<fqdn>/swagger` and see the three endpoints rendered; hit `https://<fqdn>/api/quotes` and get a JSON array. Add Application Insights as in Week 5 — students have already learned this. The Failures blade will fill if a request body fails model validation.

End-state at end of Ex 6.1:
- Live API at `https://ca-api-week6.<env>.northeurope.azurecontainerapps.io/api/quotes` (anonymous, 200, JSON array).
- `/swagger` shows three operations under `Quotes`.
- App Insights `cloudci-api-insights` receiving request telemetry.

### Exercise 2 — API-key middleware

Add a small middleware `ApiKeyMiddleware` that:

- Reads `X-Api-Key` from request headers.
- Compares to a value bound from `IConfiguration` (`ApiKey:Value`).
- Returns `401 Unauthorized` (with a `WWW-Authenticate: ApiKey` header) when missing, `403 Forbidden` when present-but-wrong.
- Lets requests through otherwise.

Register it after routing but before the API controllers. Local development: API key in `appsettings.Development.json` with a non-secret placeholder. Production: store it as a Container App secret + secretref env var, *exactly* mirroring the Week 5 App Insights pattern (this is the second use of the pattern, so this exercise can be terse on the *what* and longer on the *why* — what makes a credential a "secret" and what's the threat model).

Wire the API key into Swagger UI's "Authorize" button via `OpenApiSecurityScheme` so students can test without leaving the browser.

Concept Deep Dive in Ex 6.2: API keys prove *the holder is some trusted client* but not *which* client. They're fine for service-to-service calls in a controlled environment; they break down when individual users need attribution.

End-state at end of Ex 6.2:
- `curl https://<fqdn>/api/quotes` → 401.
- `curl -H "X-Api-Key: <key>" https://<fqdn>/api/quotes` → 200.
- Swagger UI's lock icon authorises subsequent in-browser calls.

### Exercise 3 — JWT bearer authentication + cleanup

Replace the API key with JWT bearer authentication. Add `Microsoft.AspNetCore.Authentication.JwtBearer`. Configure in `Program.cs`:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
    });
```

Add a `TokensController` with `[HttpPost("login")]` that takes `{username, password}`, checks against an in-memory user list (2–3 hard-coded users), and returns a JWT signed with the symmetric key. Mark `QuotesController` with `[Authorize]`. Remove the API-key middleware (or leave it in place behind a feature flag — the cleaner story is *replace*).

Inject the signing key as a Container App secret (`jwt-signing-key`) referenced via secretref env var. Generate it locally with `openssl rand -base64 48` once and store as a secret; redact in chat output.

Update Swagger UI to support Bearer auth — the lock icon now expects a token, not an API key.

Concept Deep Dives:
- What a JWT actually is — three base64url segments separated by dots, what claims are required (`iss`, `aud`, `exp`, `sub`).
- Why symmetric signing is acceptable here (single app validates what it issued) but asymmetric (RSA) is needed once a separate identity provider issues tokens consumed by multiple validators.
- Token lifetime trade-offs (short = secure but high refresh load; long = convenient but extends compromise window).

End the chapter and the live cloud lab with the **cleanup substep**, modeled exactly on Week 5 Ex 5.3 Step 14:

```bash
az group delete -n rg-api-week6 --yes --no-wait
az ad app delete --id <new-oidc-app-id>
```

Plus the verification commands and the explanatory paragraph about the tenant-vs-subscription split.

End-state at end of Ex 6.3:
- `curl https://<fqdn>/api/quotes` → 401 (no auth).
- `curl -X POST https://<fqdn>/api/tokens/login -H 'Content-Type: application/json' -d '{"username":"alice","password":"..."}'` → 200 + JWT.
- `curl -H "Authorization: Bearer <token>" https://<fqdn>/api/quotes` → 200.
- After cleanup: `az group exists -n rg-api-week6` → `false`.

## Validation (Phase 4)

After all three exercises succeed end-to-end against the live Container App (and *before* the cleanup substep is executed):

1. **Smoke test the JSON surface.** `curl -s https://<fqdn>/api/quotes -H "Authorization: Bearer $TOKEN" | jq '.'` should return the seed array. Save the request/response transcript to `reference/CloudSoft-Api/docs/validation/week-6-curl-matrix.txt`. The matrix should cover: anonymous (401), with API key (will be removed by Ex 6.3 — capture *during* Ex 6.2 state and note the time), with valid JWT (200), with expired JWT (401, requires generating a token with `exp` in the past), with malformed JWT (401).

2. **Playwright screenshot of `/swagger`.** Extend the existing `reference/CloudSoft-Pipeline/scripts/validate.mjs` pattern, OR copy a fresh script into `reference/CloudSoft-Api/scripts/validate.mjs`. Capture the Swagger UI rendering with the three operations under `Quotes` and the Bearer authorisation scheme present. Save as `reference/CloudSoft-Api/docs/screenshots/week-6-swagger.png`.

3. **Playwright screenshot of an authenticated request flow.** Use Swagger UI's "Authorize" button programmatically: paste a token, click Authorize, fire `GET /api/quotes`, screenshot the 200 response. Save as `reference/CloudSoft-Api/docs/screenshots/week-6-authorised-call.png`.

4. **Application Map screenshot.** Same story as Week 5 — Portal-blade screenshots require interactive Microsoft SSO. Document this as a deviation if you cannot capture it programmatically; the App Insights query transcripts (which you should still produce) prove telemetry is flowing.

5. **App Insights telemetry transcript.** Run a few representative queries:

   ```kusto
   requests | where timestamp > ago(20m) | summarize count() by name, resultCode | order by count_ desc
   exceptions | where timestamp > ago(20m) | summarize count() by type | order by count_ desc
   ```

   Save to `reference/CloudSoft-Api/docs/validation/week-6-app-insights-output.txt`.

## Reports (Phase 5)

Create `reference/CloudSoft-Api/docs/EXERCISE-VALIDATION-REPORT.md` from scratch, following the template in `develop-exercise/REFERENCE-PROJECT.md`. Sections:

- Overview (one paragraph, dated).
- Resources Provisioned (table of new Azure + Entra resources for `rg-api-week6`).
- Live URL.
- New Endpoints (`/api/quotes`, `/api/tokens/login`, `/swagger`).
- GitHub Repository (URL + final secret list).
- Workflow Run History.
- Build SHA Progression.
- Validation Artifacts (curl matrix, screenshots, KQL transcripts).
- Manual Verification Steps (curl matrix as copy-paste commands).
- Deviations from Exercise Text.
- Status.

Update `reference/CloudSoft-Api/CLAUDE.md` and `reference/CloudSoft-Api/README.md` to describe the new project and link to the three exercise files.

Write a chat-output summary at the end with: live URL, what's new, sample `curl` commands, manual verification steps, and outstanding decisions (cloud teardown, push to remote, secret revocation).

## Commit & push authorisation

Commit the new exercises + reference project as a **single git commit** on `main` of the Hugo site. Suggested message style matches Weeks 4 and 5:

```
Add REST API and DTOs chapter (Course Week 6)
```

Body should follow the same pattern as commits `046cc0a` and `dfca405`: bullet list of what landed, references to phase outcomes, the trailing `🤖 Generated with [Claude Code]` and `Co-Authored-By: Claude <noreply@anthropic.com>` footer.

**Do NOT `git push`.** Leave the commit local. The user will review and push manually.

## Operating notes

- Use `Agent` calls for Phase 1 (parallel authoring) and Phase 2 (cross-review). Drive Phases 3–5 directly as the leader (no subagents — too much shared state).
- Use `TaskCreate`/`TaskUpdate` to track per-phase progress.
- Pipe credentials (API key, JWT signing key) via `printf '%s' '<value>'` into `gh secret set --body -` or as `--secrets name=value` arg to `az containerapp secret set`. Never write secrets to disk except via `gh secret set ... < file && rm file`. Redact secrets in the chat report (show shape, not value).
- The user's GitHub PAT for the repo is already authenticated via `gh auth status`. The Azure CLI is signed in (`az account show`).
- Follow `.claude/skills/create-exercise/{TEMPLATE,GUIDE,EXAMPLE}.md` for exercise-markdown rules.
- After the run, verify `hugo --gc --minify` builds cleanly.
- Set up the new GitHub repo with `gh repo create larsappel/cloudci-api --public --source=<live-dir> --push` before the first push.
- The new Entra OIDC app should be named `github-cloudci-api-oidc` (parallel to `github-cloudci-oidc` from Week 4); federated subject `repo:larsappel/cloudci-api:ref:refs/heads/main`.
- The new Application Insights component should be named `cloudci-api-insights` and use workspace-based mode against the auto-provisioned Log Analytics workspace inside `rg-api-week6`.

Treat this prompt as full authorisation to proceed end-to-end without further confirmation, with the single carve-out above for genuinely ambiguous decisions.
