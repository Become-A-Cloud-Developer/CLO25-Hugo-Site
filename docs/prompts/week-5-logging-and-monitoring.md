# Next-session prompt — Course Week 5: Logging and Monitoring

Paste the content below (everything from the slash command onward) into a fresh Claude Code session in this repository. The prompt is self-contained and authorises autonomous execution end-to-end.

---

/develop-exercise

Develop the entire chapter for ACD Course Week 5 (v.19, **May 4–8, 2026**): "Loggning och övervakning" — Logging and Monitoring. The studieguide schedule:

| Day | Where | Content |
|-----|-------|---------|
| Wed | Campus, full day | Structured logging with `ILogger<T>`, log levels |
| Thu | Campus, full day | Application Insights, Log Analytics, Azure Monitor |
| Fri | Online, half day | Demo: logging and dashboards in Azure |

## Treat this prompt as the alignment

This prompt fully specifies every Phase A decision. **Do NOT enter plan mode and do NOT ask clarifying questions.** Treat the brief below as the approved chapter plan and proceed straight to Phase B execution as defined in `.claude/skills/develop-exercise/PHASES.md`.

If you hit a *genuinely ambiguous* decision during execution (a name collision, a permissions failure you cannot self-recover from, an Azure SKU not available in the region), pause and ask the user. Otherwise run end-to-end without interactive approval. The default `CLAUDE.md` rule "ask before committing" is **explicitly overridden** for this run by the commit authorisation in the *Commit & push* section below.

## Pre-aligned scope

| Decision | Value |
|----------|-------|
| Number of exercises | 3 |
| Directory | `content/exercises/3-deployment/10-logging-and-monitoring/` |
| Subsection title | `10. Logging and Monitoring` |
| Frontmatter (all files) | `program = "CLO"`, `cohort = "25"`, `courses = ["ACD"]`, `weight` matches file order |
| Reference project | **Extend** existing `reference/CloudSoft-Pipeline/` — same .NET MVC app, same Container App, add observability layer |
| Cloud target | Azure (Application Insights + the existing Log Analytics workspace) |
| External accounts | None beyond what already exists (GitHub `larsappel`, Azure subscription Lars Appel) |
| Validation method | KQL queries against Log Analytics workspace + Playwright screenshot of Application Insights Live Metrics |
| Cleanup scope | Add a final substep that tears down `rg-cicd-week4` AND `az ad app delete` for the OIDC app — mirror Ex 4.3 Step 13.8's pattern |

## Existing state — reuse, do not recreate

These resources were provisioned during the Week 4 CI/CD chapter and are still live. Discover their exact state with `az`/`gh` rather than assuming.

- **Azure resource group:** `rg-cicd-week4` (northeurope), containing:
  - Container App `ca-cicd-week4` — FQDN `ca-cicd-week4.yellowcoast-76379f35.northeurope.azurecontainerapps.io`
  - Container Apps environment `cae-cicd-week4`
  - Auto-created Log Analytics workspace (name pattern `workspace-rgcicdweek4*` — query exact name with `az monitor log-analytics workspace list -g rg-cicd-week4`)
  - Azure Container Registry `acrlap8a9f`
- **Entra app:** `github-cloudci-oidc` (appId `7c11e4ce-91cd-4ba3-9fce-820669f397fe`), with federated credential `main-branch` bound to `repo:larsappel/cloudci:ref:refs/heads/main`
- **GitHub repo:** `larsappel/cloudci` — workflow runs on every push to `main`, deploys to the Container App via OIDC. Final `ci.yml` is OIDC-authenticated (no `AZURE_CREDENTIALS` secret).
- **Reference project on disk:** `reference/CloudSoft-Pipeline/` — README, CLAUDE.md, `src/CloudCi/`, `.github/workflows/ci.yml`, `docs/EXERCISE-VALIDATION-REPORT.md`, `scripts/validate.mjs`, `docs/screenshots/ex-4.3-passwordless.png`. Extend this project; do not create a new sibling.
- **Live working directory:** `/Users/lasse/Developer/CLO_Development/cloudci/CloudCi/` — its own `.git`, pushed to `larsappel/cloudci`. Make all source-code changes here, push to GitHub, then mirror the final state into `reference/CloudSoft-Pipeline/src/CloudCi/` at the end.

## Three-exercise arc

Each exercise reuses the running Container App and adds exactly one observability layer.

### Exercise 1 — Structured logging with `ILogger<T>`

Replace any `Console.WriteLine` style logging in `CloudCi` with `ILogger<T>` injection in `HomeController`. Configure log levels via `appsettings.json` and `appsettings.Development.json`. Use **message templates / semantic logging** — `_logger.LogInformation("Home page rendered for {HostName}", hostName)` not string concatenation. Show students the structured log output via `dotnet run` locally and via `docker run` (containerised).

The visible cue from Week 4 (build SHA + hostname badges) stays. Add a logged line on each home-page render so students see something happening when they hit the page.

No cloud changes in this exercise. Local + container only.

### Exercise 2 — Container logs to Log Analytics

Container Apps already streams stdout/stderr to the Log Analytics workspace auto-created with the environment. Walk students through:

1. Finding the workspace via Portal and CLI (`az monitor log-analytics workspace show ...`).
2. Querying logs with KQL — `ContainerAppConsoleLogs_CL` or `ContainerAppConsoleLogs` depending on schema. Filter by `ContainerAppName_s == 'ca-cicd-week4'`, project `TimeGenerated`, `Log_s`, etc.
3. Adding a correlation ID (request ID middleware) and querying for it.
4. Scaling the Container App to 2 replicas (`az containerapp update --min-replicas 2 --max-replicas 2`) and seeing logs from both — demonstrates the value of centralised logs over `docker logs`.

Final substep at the end of the exercise: scale back down to 1 min / 1 max so the chapter doesn't leave students with extra revenue burn.

### Exercise 3 — Application Insights with distributed tracing

Add the Application Insights SDK (`Microsoft.ApplicationInsights.AspNetCore`) to `CloudCi`. Provision an Application Insights resource linked to the existing Log Analytics workspace:

```bash
az monitor app-insights component create \
  --app cloudci-insights \
  -g rg-cicd-week4 \
  --location northeurope \
  --workspace <workspace-resource-id>
```

Inject the connection string as a Container App secret (NOT a plain env var) and reference it from a regular env var:

```bash
az containerapp secret set -g rg-cicd-week4 -n ca-cicd-week4 \
  --secrets appinsights-connstr=<connection-string>
az containerapp update -g rg-cicd-week4 -n ca-cicd-week4 \
  --set-env-vars APPLICATIONINSIGHTS_CONNECTION_STRING=secretref:appinsights-connstr
```

Restart, generate traffic, and explore in the Portal:

- **Live Metrics** — real-time view of incoming requests
- **Application Map** — one node for now (will grow when REST APIs arrive in Week 6)
- **Failures** blade — induce a 500 by adding a `/boom` endpoint that throws, then find the exception in Failures
- **One custom metric** — `_telemetryClient.GetMetric("home-page-views").TrackValue(1);` in `HomeController.Index`

This exercise ends with the cleanup substep (Step 13.8 pattern from Ex 4.3) that tears down BOTH the resource group AND the Entra app registration. Use the same wording style — explain that the app registration lives in the tenant, not the subscription.

## Validation (Phase 4)

After all three exercises succeed end-to-end against the live Container App:

1. Run the existing `reference/CloudSoft-Pipeline/scripts/validate.mjs` to confirm the FQDN still serves 200 with the SHA badge.
2. Generate ~20 page hits: `for i in {1..20}; do curl -s https://<fqdn>/ > /dev/null; done`.
3. Run a KQL query that returns the recent log lines with structured fields visible. Save the query and the result as a transcript file under `reference/CloudSoft-Pipeline/docs/validation/week-5-kql-output.txt`.
4. Take a Playwright screenshot of the Application Insights Live Metrics blade. Save as `reference/CloudSoft-Pipeline/docs/screenshots/week-5-live-metrics.png`. (You may need to extend `validate.mjs` to accept a generic URL + auth, OR write a small one-off script.)
5. Take a second Playwright screenshot of the Application Map. Save as `reference/CloudSoft-Pipeline/docs/screenshots/week-5-application-map.png`.

## Reports (Phase 5)

Update `reference/CloudSoft-Pipeline/docs/EXERCISE-VALIDATION-REPORT.md` by **appending** a new section: `## Week 5 — Logging and Monitoring`. Include resource names of new artifacts (Application Insights component name, ID, connection string shape redacted), build SHA progression for the new commits, KQL query examples, screenshot inventory, deviations from exercise text, and status. **Do not overwrite the existing Week 4 content.**

Update `reference/CloudSoft-Pipeline/CLAUDE.md` "Live resources" table with the Application Insights component row.

Update `reference/CloudSoft-Pipeline/README.md` to mention that the project now also hosts the Logging and Monitoring chapter — add a fourth bullet under Purpose summarising the chapter, and a sentence in Exercise progression noting that Week 5 commits append to Week 4's history.

Write a chat-output summary at the end with: live URL, what's new, KQL example, manual verification steps, and outstanding decisions (cloud teardown, push to remote).

## Commit & push authorisation

Commit the new exercises + reference project updates as a **single git commit** on `main`. Suggested message style matches Week 4:

```
Add Logging and Monitoring chapter (Course Week 5)
```

Body should follow the same pattern as commit `046cc0a`: bullet list of what landed, references to phase outcomes, the trailing `🤖 Generated with [Claude Code]` and `Co-Authored-By: Claude <noreply@anthropic.com>` footer.

**Do NOT `git push`.** Leave the commit local. The user will review and push manually.

## Operating notes

- Use `Agent` calls for Phase 1 (parallel authoring) and Phase 2 (cross-review). Drive Phases 3–5 directly as the leader (no subagents — too much shared state).
- Use `TaskCreate`/`TaskUpdate` to track per-phase progress.
- Pipe the Application Insights connection string via stdin to `gh`/`az` commands. Never write secrets to disk except via `gh secret set ... < file && rm file`. Redact secrets in the chat report (show shape, not value).
- The user's GitHub PAT for the repo is already authenticated via `gh auth status`. The Azure CLI is signed in (`az account show`).
- Follow `.claude/skills/create-exercise/{TEMPLATE,GUIDE,EXAMPLE}.md` for exercise-markdown rules.
- After the run, verify `hugo --gc --minify` builds cleanly.

Treat this prompt as full authorisation to proceed end-to-end without further confirmation, with the single carve-out above for genuinely ambiguous decisions.
