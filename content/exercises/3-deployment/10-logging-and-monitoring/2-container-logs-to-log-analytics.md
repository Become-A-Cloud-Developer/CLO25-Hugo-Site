+++
title = "Container Logs to Log Analytics"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Discover the Log Analytics workspace that the Container Apps environment streams stdout to, then query the structured log lines from the previous exercise using Kusto Query Language. Add a request-scoped correlation ID via ILogger.BeginScope so every log line in a single HTTP request can be tied together."
weight = 2
draft = false
+++

# Container Logs to Log Analytics

## Goal

In the previous exercise you replaced `Console.WriteLine` with `ILogger<HomeController>` and a pair of semantic message templates — `{HostName}` and `{BuildSha}`. The lines now leave the application as structured records, but they still vanish into the container's stdout the moment a replica restarts or scales out. `docker logs` and `az containerapp logs show --follow` both work for an interactive look at *one* replica, but neither lets you ask "what happened across the fleet between 14:30 and 14:45?"

In this exercise you'll connect those structured log lines to a centralised, queryable store. You'll discover the **Log Analytics workspace** that Azure auto-created when you provisioned the Container Apps environment in the previous chapter, learn enough **Kusto Query Language (KQL)** to filter and project log records, and attach a request-scoped **correlation ID** so every log line from a single HTTP request can be tied together.

> **What you'll learn:**
>
> - How a Container Apps environment streams stdout into a Log Analytics workspace, and what tables that produces
> - Enough KQL to filter, project, and aggregate log records over a time window
> - The difference between the legacy `_CL` tables and the modern Container Apps-managed tables
> - How `ILogger.BeginScope` attaches structured fields to *every* subsequent log call inside a request without changing each call site
> - Why centralised logs are the baseline for any production workload — `docker logs` and per-replica tailing stop scaling the moment you have more than one container

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Completed the previous exercise: `HomeController` uses `ILogger<HomeController>` with the message template `"Home page rendered for {HostName} build {BuildSha}"`
> - ✓ A working CI/CD pipeline that deploys to the Container App `ca-cicd-week4` in resource group `rg-cicd-week4` (region `northeurope`)
> - ✓ Azure CLI signed in (`az account show` returns your subscription)
> - ✓ GitHub CLI signed in (`gh auth status`)
> - ✓ Familiarity with the Azure portal — most of this exercise lives in the **Logs** blade

## Exercise Steps

### Overview

1. **Push the structured-logging changes and confirm a green deploy**
2. **Locate the Log Analytics workspace**
3. **Generate traffic to populate the workspace**
4. **Run your first KQL query — find log lines from the Container App**
5. **Filter to the structured "Home page rendered" line**
6. **Add a request-scoped correlation ID middleware**
7. **Update `HomeController.Index()` to log inside the scope**
8. **Push, deploy, and query by correlation ID**
9. **Save the query for reuse**
10. **Test Your Implementation**

### **Step 1:** Push the structured-logging changes and confirm a green deploy

Before you can query anything, the new logging code from the previous exercise needs to be running in the cloud. The pipeline handles the build and deploy — your only job is to push and verify.

1. **Commit and push** the structured-logging changes from the previous exercise. The previous chapter touched the controller, the view, and both `appsettings*.json` files, so stage everything in the project root in one go:

   ```bash
   git add .
   git commit -m "Use ILogger with structured fields and per-category log levels"
   git push
   ```

   > A `git status` first is always a good idea — it shows you exactly what `git add .` is about to stage. For this exercise you should see `Controllers/HomeController.cs`, `Views/Home/Index.cshtml`, `appsettings.json`, and `appsettings.Development.json`.

2. **Watch** the pipeline finish:

   ```bash
   gh run list --limit 1
   gh run watch
   ```

3. **Confirm** the new revision is serving requests. Capture the FQDN and curl it:

   ```bash
   FQDN=$(az containerapp show \
     -g rg-cicd-week4 \
     -n ca-cicd-week4 \
     --query properties.configuration.ingress.fqdn -o tsv)
   curl -I "https://$FQDN/"
   ```

   Expected: `HTTP/2 200`.

> ✓ **Quick check:** The most recent workflow run is green, both jobs (`deploy` and `smoke-test`) passed, and the FQDN responds with `200`.

### **Step 2:** Locate the Log Analytics workspace

When you provisioned the Container Apps environment in the previous chapter, Azure quietly created a **Log Analytics workspace** to hold its logs. The workspace name was auto-generated and follows the pattern `workspace-rgcicdweek4XXXX`, where `XXXX` is a random suffix unique to your subscription. There are two equivalent ways to find it.

1. **Portal path.** Open the resource group `rg-cicd-week4` in the portal. Click the Container Apps environment named `cae-cicd-week4`, then choose **Logs** from the left-hand blade. The workspace name is displayed in the page header — note it down.

2. **CLI path.** From your terminal:

   ```bash
   az monitor log-analytics workspace list \
     -g rg-cicd-week4 \
     -o table
   ```

   You should see one workspace whose name begins with `workspace-rgcicdweek4`.

3. **Capture** the workspace name into a shell variable for the queries below:

   ```bash
   WS=$(az monitor log-analytics workspace list \
     -g rg-cicd-week4 \
     --query '[0].name' -o tsv)
   echo "$WS"
   ```

> ℹ **Concept Deep Dive**
>
> The Container Apps environment is the network and observability boundary inside which one or more Container Apps run. When the environment was created, Azure auto-provisioned a Log Analytics workspace and configured the environment to stream stdout/stderr from every container into it. You did not write code to make this happen — it is the platform default. With `az containerapp env create --logs-destination none` you get no workspace, and console output disappears once it scrolls off the per-replica buffer.
>
> ⚠ **Common Mistakes**
>
> - More than one workspace in your subscription? Filter by resource group (`-g rg-cicd-week4`) so you don't grab the wrong one.
> - The `[0]` JMESPath index assumes a single workspace in the group. If multiple match, pick the name explicitly.
>
> ✓ **Quick check:** `echo "$WS"` prints a name like `workspace-rgcicdweek4ab12`.

### **Step 3:** Generate traffic to populate the workspace

Log Analytics ingests data in batches — there is a one to three minute delay between when the application writes a line and when KQL can query it. Send a small burst of traffic now so the rows are in flight while you read on.

1. **Hit** the home page twenty times in a tight loop:

   ```bash
   for i in {1..20}; do curl -s "https://$FQDN/" >/dev/null; done
   ```

2. **Note the time.** Ingestion takes one to three minutes. If the first query returns zero rows, wait sixty seconds and rerun it.

> ℹ **Concept Deep Dive**
>
> Container Apps batches log lines per replica and ships them to Log Analytics on a short interval; Log Analytics in turn batches records into the search index every few seconds to a minute. The combined delay is rarely under thirty seconds and rarely over three minutes for a healthy environment. For real-time-ish observability you would add a metrics pipeline; for log queries the latency is the price of a queryable history.
>
> ⚠ **Common Mistakes**
>
> - Running the KQL query immediately after a single curl and concluding "logs aren't working." They are — they just have not arrived yet.
> - Generating thousands of requests in a runaway loop. Twenty is plenty.

### **Step 4:** Run your first KQL query — find log lines from the Container App

Now write the canonical query: "show me console-log records from `ca-cicd-week4` in the last thirty minutes." This is the query you will tweak over and over once the system is in production — it is the single most useful KQL pattern for Container Apps.

The Container Apps environment streams stdout from every container into a single console-log table inside the workspace — there is no per-app table, so you filter by app name. That table comes in two schemas: the legacy `ContainerAppConsoleLogs_CL` (with `_s`/`_d` column suffixes) and the modern `ContainerAppConsoleLogs` (plain column names). The query below uses the legacy schema; if it errors with `failed to resolve table or column expression`, drop the `_CL` and the `_s` suffixes.

1. **Open** the Container App's Logs blade in the portal (`ca-cicd-week4` → **Logs**). The blade auto-scopes KQL to your app, which is why the query "just works" without extra setup.

2. **Paste** the following query and click **Run**:

   ```kusto
   ContainerAppConsoleLogs_CL
   | where TimeGenerated > ago(30m)
   | where ContainerAppName_s == "ca-cicd-week4"
   | project TimeGenerated, RevisionName_s, ContainerGroupName_s, Log_s
   | order by TimeGenerated desc
   | take 50
   ```

3. **Read** the result. Each `ILogger` call produces *two* records: a header line with the category and event id (`info: CloudCi.Controllers.HomeController[0]`) and a body line with the formatted message (`Home page rendered for ca-cicd-week4--rev-0000007-7c8d build a1b2c3d`). Both share the same `TimeGenerated`.

> ℹ **Concept Deep Dive**
>
> The pipeline reads as English. `ContainerAppConsoleLogs_CL` is the source table. `where TimeGenerated > ago(30m)` filters to the last thirty minutes (`ago` returns "now minus N"). `where ContainerAppName_s == "ca-cicd-week4"` narrows to one app — without it (and with a query run from the workspace-level Logs blade), you'd see logs from every Container App in the environment. `project` selects columns (like SQL `SELECT`); `order by` sorts; `take` limits. KQL operators always read top-to-bottom, each transforming the table flowing through it. On the modern schema the column is `Log` and the filter becomes `where ContainerAppName == "ca-cicd-week4"`. The `_s` suffix means "string"; `_d` means "double"; `_b` means "boolean"; `_t` means "datetime"; `_g` means "guid."
>
> ⚠ **Common Mistakes**
>
> - Querying `_CL` on a modern-schema environment. Drop the suffix.
> - Running the query before ingestion catches up. Wait, then rerun.
>
> ✓ **Quick check:** The result has at least 20 rows from the curl burst, all with `ContainerAppName_s == "ca-cicd-week4"`.

### **Step 5:** Filter to the structured "Home page rendered" line

The `ILogger` formatter renders your message template into the human-readable line in `Log_s`. The structured fields — `HostName`, `BuildSha` — are *in* the text but are not their own columns in the legacy schema, because the default console formatter writes plain text rather than JSON.

1. **Run** this query in the Logs blade:

   ```kusto
   ContainerAppConsoleLogs_CL
   | where TimeGenerated > ago(30m)
   | where ContainerAppName_s == "ca-cicd-week4"
   | where Log_s has "Home page rendered"
   | project TimeGenerated, ContainerGroupName_s, Log_s
   | order by TimeGenerated desc
   ```

2. **Inspect** the rows. The `Log_s` column contains the rendered message, e.g. `Home page rendered for ca-cicd-week4--rev-0000007-7c8d build a1b2c3d`.

3. **Pull** the `HostName` back out with KQL's `extract` function — for example, count requests per replica:

   ```kusto
   ContainerAppConsoleLogs_CL
   | where TimeGenerated > ago(30m)
   | where ContainerAppName_s == "ca-cicd-week4"
   | where Log_s has "Home page rendered"
   | extend HostName = extract(@"for (\S+)", 1, Log_s)
   | summarize requests = count() by HostName
   ```

> ℹ **Concept Deep Dive**
>
> The `has` operator is KQL's tokenized substring match — faster than `contains` because the indexer pre-tokenizes the column. Use `has` for whole words or phrases; `contains` only for partial-word matches. The previous exercise already showed the JSON console formatter at work locally — that formatter only fires under `ASPNETCORE_ENVIRONMENT=Development`, while the running container in Azure defaults to Production and uses the simple text formatter. That is why `Log_s` arrives here as text and not as JSON. If you want first-class structured fields in KQL, set `ASPNETCORE_ENVIRONMENT=Development` on the Container App (or wire the JSON formatter unconditionally in `Program.cs`), then `parse_json(Log_s)` projects the embedded fields.
>
> ⚠ **Common Mistakes**
>
> - Treating `_s` columns as JSON. They are strings even if the content is JSON-shaped.
> - Using `==` for a partial string. `where Log_s == "Home page rendered"` matches nothing — use `has`.
>
> ✓ **Quick check:** The query returns rows whose `Log_s` starts with `Home page rendered for`.

### **Step 6:** Add a request-scoped correlation ID middleware

Right now you can find every log line for `ca-cicd-week4`, and every "Home page rendered" line. What you cannot do is take *one* log line and find the *other* log lines from the same HTTP request. In a single-page render this hardly matters; in a real application that calls a database, an external API, and writes its own diagnostics, it matters enormously.

The fix is a small middleware that generates a `Guid` per request and attaches it to a logging scope. `ILogger.BeginScope` adds key-value fields to *every* subsequent log call inside the scope — including library calls you did not write.

1. **Open** `Program.cs` in the project root.

2. **Add** the following middleware *before* `app.MapControllerRoute(...)`:

   > `Program.cs`

   ```csharp
   // Generates a per-request correlation ID and pushes it onto the
   // logger scope so every log call inside the request carries it.
   app.Use(async (context, next) =>
   {
       var correlationId = Guid.NewGuid().ToString("N");
       context.Items["CorrelationId"] = correlationId;

       var logger = context.RequestServices
           .GetRequiredService<ILoggerFactory>()
           .CreateLogger("CorrelationIdMiddleware");

       using (logger.BeginScope(new Dictionary<string, object>
       {
           ["CorrelationId"] = correlationId
       }))
       {
           await next();
       }
   });
   ```

3. **Enable scope rendering in the console formatters.** This is the gotcha that catches everyone: scopes are silently *off* by default. `BeginScope` adds `CorrelationId` to the underlying `LogRecord`, but neither the simple text formatter nor the JSON formatter renders it unless you explicitly opt in.

   Put the setting in **`appsettings.json`** (the base file, not the Development overlay) so it applies in every environment — including the container running in Azure under `ASPNETCORE_ENVIRONMENT=Production`. Without that, your KQL query in Step 8 would return zero rows, because the production formatter would still be dropping scopes.

   Open `appsettings.json` from the previous exercise and add a `Console.FormatterOptions` block:

   > `appsettings.json` *(after)*

   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning",
         "CloudCi.Controllers.HomeController": "Information"
       },
       "Console": {
         "FormatterOptions": {
           "IncludeScopes": true
         }
       }
     },
     "AllowedHosts": "*"
   }
   ```

   `appsettings.Development.json` keeps the `FormatterName: "json"` line from the previous exercise — that part *is* per-environment. The merged effective config in each environment then becomes:

   - **Local Dev**: JSON formatter (from the Development overlay) + scopes (from the base) → JSON output with a `Scopes` array.
   - **Azure Production**: simple text formatter (the framework default — no overlay loads) + scopes (from the base) → plain-text stdout with parenthesised `=> CorrelationId:...` lines, which Log Analytics ingests into `Log_s` as text.

4. **Verify** locally that the app still starts:

   ```bash
   ASPNETCORE_URLS=http://localhost:5000 dotnet run
   ```

   Press Ctrl+C once you see the `Now listening on:` line — you do not need to hit any pages locally for this step.

> ℹ **Line by line**
>
> Every line of this small block earns its place. Read it once row-by-row before moving on:
>
> | Code | What it does |
> |------|--------------|
> | `app.Use(async (context, next) => { ... });` | Registers an inline middleware delegate in the pipeline. `async` is required because the body awaits. |
> | `var correlationId = Guid.NewGuid().ToString("N");` | Generates a fresh 128-bit GUID for this request. The `"N"` format strips the hyphens, producing a compact 32-character hex token. |
> | `context.Items["CorrelationId"] = correlationId;` | Stashes the ID in the per-request `HttpContext.Items` dictionary so any later code in the same request (a controller, another middleware) can read it back without going through the logger. |
> | `var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("CorrelationIdMiddleware");` | Resolves `ILoggerFactory` from the per-request DI container and builds a logger whose category is the string `"CorrelationIdMiddleware"`. We use the factory (not `ILogger<T>`) because this middleware is a lambda, not a class — there is no constructor to inject into. |
> | `using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))` | Opens a logging scope with one field, `CorrelationId`. `BeginScope` returns an `IDisposable`; the `using` guarantees `Dispose()` is called when the request ends, so the scope cannot leak into the next request. |
> | `await next();` | Invokes the rest of the pipeline. Everything downstream — the controller, view rendering, framework log lines — runs *inside* the scope, which is what attaches `CorrelationId` to those log calls. The `await` is critical: without it, the scope would dispose before the controller had a chance to log. |
>
> ℹ **Why `CorrelationId` and not `RequestId`?**
>
> ASP.NET Core's hosting middleware (`Microsoft.AspNetCore.Hosting.Diagnostics`) already pushes a per-request scope onto every request *before* your middleware runs, and that scope already contains a field named `RequestId` — set to the connection trace identifier (`0HN0123456789:00000001` style). If your scope key were also `RequestId`, the JSON formatter's `Scopes` array would carry two entries with the same field name, one from the framework and one from you, and a query like `payload.Scopes[0].RequestId` would return the framework's trace ID — useless for correlating across log lines, because that ID is not stable across separate `ILogger` calls in the same request. Picking the name `CorrelationId` sidesteps the collision and matches the value you already stashed in `HttpContext.Items["CorrelationId"]` two lines earlier. As a bonus, `CorrelationId` is the term the rest of the observability ecosystem uses (W3C Trace Context, Application Insights operation correlation, OpenTelemetry trace IDs).
>
> ℹ **Concept Deep Dive**
>
> `ILogger.BeginScope` is the right tool for "attach this field to every log call in this region of code." Internally the scope is a stack — when you call `_logger.LogInformation(...)`, the logger walks the stack and merges all active scope fields into the structured payload. With `IncludeScopes: true`, the simple text formatter renders scopes in parentheses (`=> CorrelationId:abc...`) and the JSON formatter emits them as a `Scopes` array; with the default `IncludeScopes: false`, both formatters silently drop them on the floor even though the data is still present in the underlying `LogRecord`. The alternative to scopes — passing `correlationId` into every method signature — works but rots. Diagnostics libraries (Serilog, OpenTelemetry, Application Insights) all read scope state automatically.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `using` on `BeginScope`. Without it, the scope leaks across requests and every line gets the same wrong ID.
> - Putting the middleware *after* `app.MapControllerRoute(...)`. Middleware runs in registration order; anything past `MapControllerRoute` is unreachable for controller log calls.
> - Forgetting `IncludeScopes: true`. The middleware looks correct, the JSON output looks correct, but `CorrelationId` is nowhere to be found because the formatter is silently dropping it.
>
> ✓ **Quick check:** `dotnet run` starts cleanly and the app responds at `http://localhost:5000/`.

### **Step 7:** Update `HomeController.Index()` to log inside the scope

The middleware sets the scope on every request before the controller runs. Your existing `_logger.LogInformation(...)` call already runs *inside* that scope — no code change needed. Verify the scope is attached locally.

1. **Confirm** the existing log line in `Controllers/HomeController.cs` is still the one from the previous exercise — `_logger.LogInformation("Home page rendered for {HostName} build {BuildSha}", hostName, buildSha);`.

2. **Run locally** with the JSON formatter active (Development is the default):

   ```bash
   ASPNETCORE_URLS=http://localhost:5000 dotnet run
   ```

3. **Open** `http://localhost:5000/`. The JSON line for the controller's `LogInformation` call should now include a `Scopes` array. **Two** entries appear: ASP.NET Core's hosting middleware always pushes its own scope first (with the framework's trace identifier, request path, and connection ID), and your middleware adds yours on top:

   ```json
   {
     "EventId": 0,
     "LogLevel": "Information",
     "Category": "CloudCi.Controllers.HomeController",
     "Message": "Home page rendered for Mac build local",
     "State": {
       "HostName": "Mac",
       "BuildSha": "local",
       "{OriginalFormat}": "Home page rendered for {HostName} build {BuildSha}"
     },
     "Scopes": [
       { "RequestId": "0HN0123456789:00000001", "RequestPath": "/", "ConnectionId": "0HN0123456789" },
       { "Message": "", "CorrelationId": "7c8d4a3f1b2e4d5e6f7a8b9c0d1e2f3a" }
     ]
   }
   ```

   The framework's `RequestId` (the trace identifier) is *not* what we use for correlation — it's a connection-level handle, not a stable per-HTTP-request token. Our `CorrelationId` in the second scope entry is the one downstream KQL queries will pivot on.

4. **Press Ctrl+C.**

   If you switch to the simple text formatter (remove `FormatterName: "json"` from `appsettings.Development.json`, or run with the production defaults), the same scopes render as parenthesised lines just before each log entry: one `=> RequestId:0HN..., RequestPath:/, ConnectionId:0HN...` line and one `=> CorrelationId:7c8d4a3f...` line. Same data, different presentation.

> ✓ **Quick check:** The local console shows `CorrelationId` either as a key inside a `Scopes` array (JSON formatter) or as a parenthesised `=> CorrelationId:...` line (simple formatter) for at least one request.

### **Step 8:** Push, deploy, and query by correlation ID

Ship the middleware to Azure and watch the correlation ID surface in Log Analytics.

1. **Commit and push.** Step 6 touched both `Program.cs` (the middleware) and `appsettings.Development.json` (the `IncludeScopes` flag); stage everything in one go:

   ```bash
   git add .
   git commit -m "Add request-scoped correlation ID via ILogger.BeginScope"
   git push
   gh run watch
   ```

2. **Wait** for the smoke test to pass, then generate a fresh burst of traffic and note the time:

   ```bash
   for i in {1..10}; do curl -s "https://$FQDN/" >/dev/null; done
   ```

3. **Wait** sixty to ninety seconds for ingestion.

4. **Run** this KQL query in the Logs blade:

   ```kusto
   ContainerAppConsoleLogs_CL
   | where TimeGenerated > ago(15m)
   | where ContainerAppName_s == "ca-cicd-week4"
   | where Log_s has "CorrelationId"
   | project TimeGenerated, Log_s
   | order by TimeGenerated desc
   | take 30
   ```

5. **Inspect** a row. The `Log_s` text now contains a fragment like `CorrelationId:abc123...` — the correlation ID generated by the middleware. Pick one ID, then run a second query (`where Log_s has "abc123"`) to find every line from that single request. For a page render that's a handful of lines; in a real app touching a database, an HTTP client, and a queue, the same query surfaces dozens of lines that together tell the story of one user's interaction.

> ℹ **Concept Deep Dive**
>
> If you switched the running container's environment to Development (so the JSON formatter from the previous exercise actually fires in production), `Log_s` arrives as JSON and you can pivot on the scope field directly. ASP.NET Core's hosting middleware pushes its *own* scope onto every request first — the one with the framework's trace identifier, request path, and connection ID — so the `Scopes` array in each record contains two entries: the framework's at index `0`, and ours at index `1`. The `mv-apply` form below walks every scope and picks ours by name, which is robust regardless of how many extra scopes other middleware adds:
>
> ```kusto
> ContainerAppConsoleLogs_CL
> | where TimeGenerated > ago(15m)
> | where ContainerAppName_s == "ca-cicd-week4"
> | extend payload = parse_json(Log_s)
> | mv-apply scope = payload.Scopes on (
>     where isnotempty(scope.CorrelationId)
>     | extend CorrelationId = tostring(scope.CorrelationId)
> )
> | summarize lineCount = count() by CorrelationId
> | order by lineCount desc
> ```
>
> If you trust the index, the shorter form `extend CorrelationId = tostring(payload.Scopes[1].CorrelationId)` works today but breaks the moment another middleware pushes a scope before yours. Prefer `mv-apply` in production.
>
> JSON formatting is the production-grade choice once logs are routinely consumed by KQL. For coursework the text formatter is fine.
>
> ✓ **Quick check:** A query for one specific `CorrelationId` returns at least two rows that share that ID.

### **Step 9:** Save the query for reuse

Queries you write often deserve a name. Log Analytics saves them either to your personal account or, with the right RBAC, to the workspace itself.

1. **In the Logs blade**, paste the canonical "Home page rendered" query from Step 5 back into the editor, then click **Save** → **Save as Query**:

   ```kusto
   ContainerAppConsoleLogs_CL
   | where TimeGenerated > ago(30m)
   | where ContainerAppName_s == "ca-cicd-week4"
   | where Log_s has "Home page rendered"
   | project TimeGenerated, ContainerGroupName_s, Log_s
   | order by TimeGenerated desc
   ```

2. **Name** it `home-page-traffic` with a short description, then **Save**. Reopen later from **Queries** in the left-hand pane.

> ℹ **Concept Deep Dive**
>
> Saved queries come in two flavours. **User queries** are scoped to your account and follow you across workspaces. **Workspace queries** are shared with anyone who has read access on the workspace and require the `Log Analytics Contributor` role to create. Above saved queries are **functions**, which behave like KQL stored procedures — a `ca_logs(name:string)` function can encapsulate the standard filter prefix so the rest of the query becomes a one-liner.
>
> ✓ **Quick check:** Reopening **Queries → Saved Queries → home-page-traffic** loads the same KQL back into the editor.

### **Step 10:** Test Your Implementation

Walk the full set of behaviours in one pass.

1. **Verify** the workspace is captured:

   ```bash
   echo "$WS"
   ```

   Expected: a workspace name like `workspace-rgcicdweek4ab12`.

2. **Generate** traffic and wait sixty seconds:

   ```bash
   for i in {1..10}; do curl -s "https://$FQDN/" >/dev/null; done
   sleep 60
   ```

3. **Run** the canonical query in the Logs blade — expected: at least ten rows:

   ```kusto
   ContainerAppConsoleLogs_CL
   | where TimeGenerated > ago(15m)
   | where ContainerAppName_s == "ca-cicd-week4"
   | where Log_s has "Home page rendered"
   | project TimeGenerated, ContainerGroupName_s, Log_s
   | order by TimeGenerated desc
   ```

4. **Confirm** correlation IDs — expected: at least five rows with `CorrelationId:` in `Log_s`:

   ```kusto
   ContainerAppConsoleLogs_CL
   | where TimeGenerated > ago(15m)
   | where ContainerAppName_s == "ca-cicd-week4"
   | where Log_s has "CorrelationId"
   | take 5
   ```

> ✓ **Success indicators:**
>
> - The workspace name is visible from both the portal and `az monitor log-analytics workspace list`
> - KQL queries return rows for `ContainerAppName_s == "ca-cicd-week4"` over a recent time window
> - The "Home page rendered" line appears with the rendered structured fields in `Log_s`
> - Each request carries a unique `CorrelationId` correlation ID in the logging scope
>
> ✓ **Final verification checklist:**
>
> - ☐ Log Analytics workspace name captured in `$WS`
> - ☐ KQL query against `ContainerAppConsoleLogs_CL` (or modern equivalent) returns rows
> - ☐ "Home page rendered" line is visible with the structured fields rendered into the message text
> - ☐ Correlation-ID middleware is registered before `MapControllerRoute` in `Program.cs`
> - ☐ `IncludeScopes: true` is set under `Logging.Console.FormatterOptions` in `appsettings.json` (the base file — applies in Azure Production)
> - ☐ Each log line carries a `CorrelationId` value from the scope
> - ☐ `home-page-traffic` saved query exists in the workspace

## Common Issues

> **If you encounter problems:**
>
> **`'ContainerAppConsoleLogs_CL': failed to resolve table or column expression`:** Your environment uses the modern schema. Drop the `_CL` suffix and the `_s`/`_d` column suffixes — `Log_s` becomes `Log`, `ContainerAppName_s` becomes `ContainerAppName`.
>
> **Queries return zero rows after several minutes:** Either no traffic reached the app (curl the FQDN and check `HTTP/2 200`), or the time-window excludes your data. Widen with `where TimeGenerated > ago(2h)` and rerun.
>
> **The `CorrelationId` token never appears in `Log_s`:** Two likely causes. (1) `IncludeScopes` is not enabled in `appsettings.json` — the base file is what applies in Azure (Production); putting the flag only in `appsettings.Development.json` will work locally but produce empty queries in Log Analytics. Confirm `Logging.Console.FormatterOptions.IncludeScopes` is `true` in `appsettings.json`. Scopes are silently *off* by default and the formatter drops them with no error. (2) The middleware was registered *after* `app.MapControllerRoute(...)`. Move the `app.Use(...)` block above it, redeploy, and re-query.
>
> **`extract` returns empty values:** The regex did not match. Inspect a single `Log_s` row first, then refine.
>
> **`_s` columns treated as JSON:** They are not — legacy `_s` columns are flat strings. Wrap with `parse_json()` only if you switched to the JSON formatter.
>
> **Logs from unrelated apps:** Always include `where ContainerAppName_s == "..."`. The unfiltered table is noisy and slow.
>
> **Still stuck?** Re-run Step 4's canonical query verbatim. If it errors with `failed to resolve table or column expression`, drop the `_CL` and the `_s` suffixes — your environment uses the modern schema. Most other KQL failures here are typos.

## Summary

You connected the structured log lines from the previous exercise to a centralised, queryable store. You discovered the auto-provisioned Log Analytics workspace, learned which schema your environment uses, and wrote KQL to filter, project, and aggregate console-log records. You added a request-scoped correlation ID via `ILogger.BeginScope` so every log line from a single HTTP request carries the same `CorrelationId`.

- ✓ A Log Analytics workspace was already streaming your container's stdout — you only had to learn how to query it
- ✓ KQL's pipeline operators (`where | project | summarize | order by | take`) compose into queries that read like English
- ✓ `ILogger.BeginScope` attaches a correlation ID to every log call inside a request without changing any call site
- ✓ The `ContainerGroupName_s` column is per-replica: even on a single replica today, the same query keeps working unchanged when a future workload runs across many

> **Key takeaway:** The moment a workload runs on more than one replica, per-replica log inspection becomes useless. Centralised logs are not an "advanced" feature — they are the baseline for any production environment, and they're already wired up here whether you ever scale out or not.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Promote `home-page-traffic` to a **workspace function** so the team can call `home_page_traffic(30m)` from any other query.
> - Configure a **log-based alert rule**: run `ContainerAppConsoleLogs_CL | where Log_s has "ERROR" | count`, click **New alert rule**, set a threshold of `> 0` over five minutes. Log-based alerts catch errors before users complain — though metric-based alerts are usually faster and cheaper.
> - Investigate **retention and cost tuning.** The Basic SKU charges per ingested and per retained gigabyte; default retention is 30 days but can go to two years.
> - Promote the JSON console formatter from the previous exercise to fire unconditionally (or set `ASPNETCORE_ENVIRONMENT=Development` on the Container App) and rewrite the queries with `parse_json(Log_s)` for first-class structured-field access.
> - Read the [KQL quick reference](https://learn.microsoft.com/azure/data-explorer/kql-quick-reference).

## Done!

You can now query the structured logs across replicas, time windows, and individual requests. That is a real operator superpower — most of the on-call playbook in any cloud-native shop starts with a KQL query.

But notice what you cannot easily ask of these logs. "What was the p95 response time across the last hour?" needs you to parse a number out of a string field and aggregate it. "Show me error rate over the last day correlated with deployment events" needs more than one source pivoted on a shared timeline. Text logs are the wrong shape for those questions — the right shape is **metrics**, sampled at a regular interval, plus **traces**, which stitch correlated spans into a request waterfall. Those live in a different sink, with a different query model and a different cost profile. The next exercise wires that in.
