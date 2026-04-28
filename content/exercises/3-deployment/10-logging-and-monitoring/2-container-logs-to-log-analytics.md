+++
title = "Container Logs to Log Analytics"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Discover the Log Analytics workspace that the Container Apps environment streams stdout to, then query the structured log lines from the previous exercise using Kusto Query Language. Add a request-scoped correlation ID, scale the app to two replicas, and feel why centralised logs beat 'docker logs'."
weight = 2
draft = false
+++

# Container Logs to Log Analytics

## Goal

In the previous exercise you replaced `Console.WriteLine` with `ILogger<HomeController>` and a pair of semantic message templates — `{HostName}` and `{BuildSha}`. The lines now leave the application as structured records, but they still vanish into the container's stdout the moment a replica restarts or scales out. `docker logs` and `az containerapp logs show --follow` both work for an interactive look at *one* replica, but neither lets you ask "what happened across the fleet between 14:30 and 14:45?"

In this exercise you'll connect those structured log lines to a centralised, queryable store. You'll discover the **Log Analytics workspace** that Azure auto-created when you provisioned the Container Apps environment in the previous chapter, learn enough **Kusto Query Language (KQL)** to filter and project log records, attach a request-scoped **correlation ID** so every log line from a single HTTP request can be tied together, and scale the app to two replicas to see log lines from both in one query.

> **What you'll learn:**
>
> - How a Container Apps environment streams stdout into a Log Analytics workspace, and what tables that produces
> - Enough KQL to filter, project, and aggregate log records over a time window
> - The difference between the legacy `_CL` tables and the modern Container Apps-managed tables
> - How `ILogger.BeginScope` attaches structured fields to *every* subsequent log call inside a request without changing each call site
> - Why centralised logs beat per-replica `docker logs` the moment you have more than one replica

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
3. **Understand the workspace–environment–table relationship**
4. **Discover which log table schema your environment uses**
5. **Generate traffic to populate the workspace**
6. **Run your first KQL query — find log lines from the Container App**
7. **Filter to the structured "Home page rendered" line**
8. **Add a request-scoped correlation ID middleware**
9. **Update `HomeController.Index()` to log inside the scope**
10. **Push, deploy, and query by correlation ID**
11. **Scale the app to two replicas and query across both**
12. **Save the query for reuse**
13. **Scale back to a single replica**
14. **Test Your Implementation**

### **Step 1:** Push the structured-logging changes and confirm a green deploy

Before you can query anything, the new logging code from the previous exercise needs to be running in the cloud. The pipeline handles the build and deploy — your only job is to push and verify.

1. **Commit and push** the structured-logging changes from the previous exercise:

   ```bash
   git add Controllers/HomeController.cs
   git commit -m "Use ILogger with structured fields"
   git push
   ```

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

### **Step 3:** Understand the workspace–environment–table relationship

A **Log Analytics workspace** is a multi-tenant store: many Azure services can stream data into the same workspace, each into its own **table**. A **Container Apps environment** is one such producer. Inside the workspace, every Container App in the environment writes to the same console-log table — there is no per-app table. To filter to one app, you add a `where ContainerAppName_s == "..."` clause.

There are two table schemas in the wild, and you are about to find out which one your environment uses.

> ℹ **Concept Deep Dive**
>
> The legacy schema is `ContainerAppConsoleLogs_CL` — the `_CL` suffix stands for "Custom Log." The newer schema, `ContainerAppConsoleLogs` (no suffix), is part of a managed-table family Microsoft introduced as Container Apps moved out of preview. New environments default to the modern schema; older environments may still emit the legacy one. The columns differ — `Log_s` versus `Log`, `_s`/`_d` suffixes versus plain names — so a query written for one schema fails on the other with `failed to resolve table or column expression`. The `_s` suffix means "string"; `_d` means "double"; `_b` means "boolean"; `_t` means "datetime"; `_g` means "guid."

### **Step 4:** Discover which log table schema your environment uses

Ask the workspace what tables it has and how many rows are in each. This is also a first taste of KQL — a pipeline language where each `|` transforms the table flowing through it.

1. **Open** the Logs blade in the portal (Container Apps environment → **Logs**).

2. **Paste** the following query and click **Run**:

   ```kusto
   search ""
   | summarize count() by Type
   | top 20 by count_
   ```

3. **Read** the result. Look for one of these:

   - `ContainerAppConsoleLogs_CL` — legacy schema
   - `ContainerAppConsoleLogs` — modern schema

   The queries below are written against the legacy `_CL` schema. If yours is modern, drop the `_CL` and the `_s`/`_d` column suffixes.

> ℹ **Concept Deep Dive**
>
> `search ""` is "across all tables" — expensive, never run without a downstream `summarize` or `take`. KQL renames `count()` to `count_` (note the trailing underscore) automatically. The pattern `source | filter | aggregate | sort | take` is how every KQL query is built.
>
> ⚠ **Common Mistakes**
>
> - Writing `top 20 by count` (no underscore) silently sorts on a non-existent column.
> - If neither table shows up, the app has not yet emitted logs. Hit the FQDN and wait.
>
> ✓ **Quick check:** The result includes `ContainerAppConsoleLogs_CL` or `ContainerAppConsoleLogs` with a non-zero row count.

### **Step 5:** Generate traffic to populate the workspace

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

### **Step 6:** Run your first KQL query — find log lines from the Container App

Now write the canonical query: "show me console-log records from `ca-cicd-week4` in the last thirty minutes." This is the query you will tweak over and over once the system is in production — it is the single most useful KQL pattern for Container Apps.

1. **In the Logs blade**, paste the following query and click **Run**:

   ```kusto
   ContainerAppConsoleLogs_CL
   | where TimeGenerated > ago(30m)
   | where ContainerAppName_s == "ca-cicd-week4"
   | project TimeGenerated, RevisionName_s, ContainerGroupName_s, Log_s
   | order by TimeGenerated desc
   | take 50
   ```

2. **Read** the result. Each `ILogger` call produces *two* records: a header line with the category and event id (`info: CloudCi.Controllers.HomeController[0]`) and a body line with the formatted message (`Home page rendered for ca-cicd-week4--rev-0000007-7c8d build a1b2c3d`). Both share the same `TimeGenerated`.

> ℹ **Concept Deep Dive**
>
> The pipeline reads as English. `ContainerAppConsoleLogs_CL` is the source table. `where TimeGenerated > ago(30m)` filters to the last thirty minutes (`ago` returns "now minus N"). `where ContainerAppName_s == "ca-cicd-week4"` narrows to one app — without it, you'd see logs from every Container App in the environment. `project` selects columns (like SQL `SELECT`); `order by` sorts; `take` limits. KQL operators always read top-to-bottom, each transforming the table flowing through it. The `_s` suffixes are the legacy-schema fingerprint; on the modern schema the column is `Log` and the filter becomes `where ContainerAppName == "ca-cicd-week4"`.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `where ContainerAppName_s == "..."` and getting flooded by other apps. Always filter to one app first.
> - Querying `_CL` on a modern-schema environment. Drop the suffix.
> - Running the query before ingestion catches up. Wait, then rerun.
>
> ✓ **Quick check:** The result has at least 20 rows from the curl burst, all with `ContainerAppName_s == "ca-cicd-week4"`.

### **Step 7:** Filter to the structured "Home page rendered" line

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

### **Step 8:** Add a request-scoped correlation ID middleware

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
           ["RequestId"] = correlationId
       }))
       {
           await next();
       }
   });
   ```

3. **Verify** locally that the app still starts:

   ```bash
   dotnet run
   ```

   Press Ctrl+C once you see the `Now listening on:` line — you do not need to hit any pages locally for this step.

> ℹ **Concept Deep Dive**
>
> `ILogger.BeginScope` is the right tool for "attach this field to every log call in this region of code." Internally the scope is a stack — when you call `_logger.LogInformation(...)`, the logger walks the stack and merges all active scope fields into the structured payload. Default console formatters render scopes in parentheses; the JSON formatter emits them as JSON object members. The alternative — passing `correlationId` into every method signature — works but rots. Diagnostics libraries (Serilog, OpenTelemetry, Application Insights) all read scope state automatically.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `using` on `BeginScope`. Without it, the scope leaks across requests and every line gets the same wrong ID.
> - Putting the middleware *after* `app.MapControllerRoute(...)`. Middleware runs in registration order; anything past `MapControllerRoute` is unreachable for controller log calls.
>
> ✓ **Quick check:** `dotnet run` starts cleanly and the app responds at `http://localhost:5000/`.

### **Step 9:** Update `HomeController.Index()` to log inside the scope

The middleware sets the scope on every request before the controller runs. Your existing `_logger.LogInformation(...)` call already runs *inside* that scope — no code change needed. Verify the scope is attached locally.

1. **Confirm** the existing log line in `Controllers/HomeController.cs` is still the one from the previous exercise — `_logger.LogInformation("Home page rendered for {HostName} build {BuildSha}", hostName, buildSha);`.

2. **Run locally:**

   ```bash
   dotnet run
   ```

   Open `http://localhost:5000/`. The console line should now include `RequestId` in the parenthesised scope, like `RequestId:7c8d...`. Press Ctrl+C.

> ✓ **Quick check:** The local console shows `RequestId:` in the scope output for at least one request.

### **Step 10:** Push, deploy, and query by correlation ID

Ship the middleware to Azure and watch the correlation ID surface in Log Analytics.

1. **Commit and push:**

   ```bash
   git add Program.cs
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
   | where Log_s has "RequestId"
   | project TimeGenerated, Log_s
   | order by TimeGenerated desc
   | take 30
   ```

5. **Inspect** a row. The `Log_s` text now contains a fragment like `RequestId:abc123...` — the correlation ID generated by the middleware. Pick one ID, then run a second query (`where Log_s has "abc123"`) to find every line from that single request. For a page render that's a handful of lines; in a real app touching a database, an HTTP client, and a queue, the same query surfaces dozens of lines that together tell the story of one user's interaction.

> ℹ **Concept Deep Dive**
>
> If you switched the running container's environment to Development (so the JSON formatter from the previous exercise actually fires in production), `Log_s` arrives as JSON and you can pivot more cleanly:
>
> ```kusto
> ContainerAppConsoleLogs_CL
> | where TimeGenerated > ago(15m)
> | where ContainerAppName_s == "ca-cicd-week4"
> | extend payload = parse_json(Log_s)
> | extend RequestId = tostring(payload.Scopes[0].RequestId)
> | where isnotempty(RequestId)
> | summarize lineCount = count() by RequestId
> | order by lineCount desc
> ```
>
> JSON formatting is the production-grade choice once logs are routinely consumed by KQL. For coursework the text formatter is fine.
>
> ✓ **Quick check:** A query for one specific `RequestId` returns at least two rows that share that ID.

### **Step 11:** Scale the app to two replicas and query across both

Centralised logs earn their keep when there is more than one replica running. With one, `az containerapp logs show --follow` is enough. With two, you immediately need to ask "is the slow request hitting replica A or replica B?" — and the answer lives in `ContainerGroupName_s`.

1. **Pin** the Container App to two replicas. `--min-replicas 2 --max-replicas 2` fixes the count so you do not wait for autoscale:

   ```bash
   az containerapp update \
     -g rg-cicd-week4 \
     -n ca-cicd-week4 \
     --min-replicas 2 \
     --max-replicas 2
   ```

2. **Wait** for both replicas. The command returns once the revision is accepted; the platform brings up the second replica over the next minute. Confirm:

   ```bash
   az containerapp replica list \
     -g rg-cicd-week4 \
     -n ca-cicd-week4 \
     -o table
   ```

   Expected: two rows.

3. **Generate** traffic so both replicas get hit. The ingress load-balances roughly round-robin:

   ```bash
   for i in {1..40}; do curl -s "https://$FQDN/" >/dev/null; done
   ```

4. **Wait** ninety seconds, then run this query — it groups "Home page rendered" lines by replica:

   ```kusto
   ContainerAppConsoleLogs_CL
   | where TimeGenerated > ago(10m)
   | where ContainerAppName_s == "ca-cicd-week4"
   | where Log_s has "Home page rendered"
   | summarize requests = count() by ContainerGroupName_s
   | order by requests desc
   ```

   You should see two rows — one per replica — each with roughly half the total. This is the moment centralised logs prove their worth: a single query, both replicas visible at once. The same answer with `docker logs` would require knowing both replica names and tailing two terminals side by side.

> ℹ **Concept Deep Dive**
>
> `ContainerGroupName_s` includes the revision name as a prefix, so values look like `ca-cicd-week4--rev-0000008-7c8d`. If you push a new image and trigger a new revision, the replica names change — old log lines stay associated with the old revision name forever, which is how you investigate "did the bug start with revision 8 or revision 9?"
>
> ⚠ **Common Mistakes**
>
> - Running `summarize` before the second replica has actually started serving. If only one row appears, wait, generate more traffic, and rerun.
> - Pinning `--min-replicas 2 --max-replicas 2` disables scale-to-zero. You are now paying for two always-on replicas.
>
> ✓ **Quick check:** The result has two `ContainerGroupName_s` rows, each with at least five requests.

### **Step 12:** Save the query for reuse

Queries you write often deserve a name. Log Analytics saves them either to your personal account or, with the right RBAC, to the workspace itself.

1. **In the Logs blade**, with the "requests by replica" query in the editor, click **Save** → **Save as Query**.

2. **Name** it `home-page-traffic` with a short description, then **Save**. Reopen later from **Queries** in the left-hand pane.

> ℹ **Concept Deep Dive**
>
> Saved queries come in two flavours. **User queries** are scoped to your account and follow you across workspaces. **Workspace queries** are shared with anyone who has read access on the workspace and require the `Log Analytics Contributor` role to create. Above saved queries are **functions**, which behave like KQL stored procedures — a `ca_logs(name:string)` function can encapsulate the standard filter prefix so the rest of the query becomes a one-liner.
>
> ✓ **Quick check:** Reopening **Queries → Saved Queries → home-page-traffic** loads the same KQL back into the editor.

### **Step 13:** Scale back to a single replica

Two always-on replicas cost roughly twice as much as one. Scale back before moving on.

1. **Update** to one replica:

   ```bash
   az containerapp update \
     -g rg-cicd-week4 \
     -n ca-cicd-week4 \
     --min-replicas 1 \
     --max-replicas 1
   ```

2. **Confirm** only one replica is running:

   ```bash
   az containerapp replica list \
     -g rg-cicd-week4 \
     -n ca-cicd-week4 \
     -o table
   ```

   Expected: one row.

> ⚠ **Common Mistakes**
>
> - Leaving the lab in two-replica mode at the end of the exercise. The cost is small per hour but accumulates over a course week. Always scale back.
> - Setting `--min-replicas 0` thinking it returns the app to the default. The original setting from the previous chapter was likely `1/1`; setting `0/1` enables scale-to-zero, which has different cold-start behaviour. Stick with `1/1` unless you intentionally want scale-to-zero.

### **Step 14:** Test Your Implementation

Walk the full set of behaviours in one pass.

1. **Verify** the workspace and a single replica:

   ```bash
   echo "$WS"
   az containerapp replica list -g rg-cicd-week4 -n ca-cicd-week4 -o table
   ```

   Expected: one workspace name, one replica row.

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

4. **Confirm** correlation IDs — expected: at least five rows with `RequestId:` in `Log_s`:

   ```kusto
   ContainerAppConsoleLogs_CL
   | where TimeGenerated > ago(15m)
   | where ContainerAppName_s == "ca-cicd-week4"
   | where Log_s has "RequestId"
   | take 5
   ```

> ✓ **Success indicators:**
>
> - The workspace name is visible from both the portal and `az monitor log-analytics workspace list`
> - KQL queries return rows for `ContainerAppName_s == "ca-cicd-week4"` over a recent time window
> - The "Home page rendered" line appears with the rendered structured fields in `Log_s`
> - Each request carries a unique `RequestId` correlation ID in the logging scope
> - Scaling to two replicas produced two `ContainerGroupName_s` values in a single query result
> - The Container App is back to a single replica
>
> ✓ **Final verification checklist:**
>
> - ☐ Log Analytics workspace name captured in `$WS`
> - ☐ KQL query against `ContainerAppConsoleLogs_CL` (or modern equivalent) returns rows
> - ☐ "Home page rendered" line is visible with the structured fields rendered into the message text
> - ☐ Correlation-ID middleware is registered before `MapControllerRoute` in `Program.cs`
> - ☐ Each log line carries a `RequestId` value from the scope
> - ☐ `home-page-traffic` saved query exists in the workspace
> - ☐ Container App `ca-cicd-week4` is back at `--min-replicas 1 --max-replicas 1`

## Common Issues

> **If you encounter problems:**
>
> **`'ContainerAppConsoleLogs_CL': failed to resolve table or column expression`:** Your environment uses the modern schema. Drop the `_CL` suffix and the `_s`/`_d` column suffixes — `Log_s` becomes `Log`, `ContainerAppName_s` becomes `ContainerAppName`.
>
> **Queries return zero rows after several minutes:** Either no traffic reached the app (curl the FQDN and check `HTTP/2 200`), or the time-window excludes your data. Widen with `where TimeGenerated > ago(2h)` and rerun.
>
> **The `RequestId` token never appears in `Log_s`:** The middleware was registered *after* `app.MapControllerRoute(...)`. Move the `app.Use(...)` block above it, redeploy, and re-query.
>
> **`az containerapp replica list` shows only one replica after scaling to two:** The platform is still bringing up the second. Wait 60–90 seconds. If still one, check `az containerapp revision list` for any `Failed` revision.
>
> **`extract` returns empty values:** The regex did not match. Inspect a single `Log_s` row first, then refine.
>
> **`_s` columns treated as JSON:** They are not — legacy `_s` columns are flat strings. Wrap with `parse_json()` only if you switched to the JSON formatter.
>
> **Logs from unrelated apps:** Always include `where ContainerAppName_s == "..."`. The unfiltered table is noisy and slow.
>
> **Still stuck?** Re-run step 4 to confirm the table name, then re-run step 6's query verbatim. Most KQL failures here are typos.

## Summary

You connected the structured log lines from the previous exercise to a centralised, queryable store. You discovered the auto-provisioned Log Analytics workspace, learned which schema your environment uses, and wrote KQL to filter, project, and aggregate console-log records. You added a request-scoped correlation ID via `ILogger.BeginScope` and scaled to two replicas to see the difference between centralised logs and per-replica `docker logs`.

- ✓ A Log Analytics workspace was already streaming your container's stdout — you only had to learn how to query it
- ✓ KQL's pipeline operators (`where | project | summarize | order by | take`) compose into queries that read like English
- ✓ `ILogger.BeginScope` attaches a correlation ID to every log call inside a request without changing any call site
- ✓ Centralised logs make a multi-replica fleet visible in a single query; `docker logs` does not scale past one container

> **Key takeaway:** The moment a workload runs on more than one replica, per-replica log inspection becomes useless. Centralised logs are not an "advanced" feature — they are the baseline for any production environment.

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
