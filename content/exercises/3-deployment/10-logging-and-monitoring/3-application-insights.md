+++
title = "Application Insights and Distributed Tracing"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Wire Application Insights into the deployed app. Send requests, exceptions, and a custom metric. Watch traffic in Live Metrics, follow a single request through the Application Map, and find an induced exception in the Failures blade. Finish by tearing down the resource group and the OIDC Entra app so no resources are left running."
weight = 3
draft = false
+++

# Application Insights and Distributed Tracing

## Goal

Up to this point your app produces structured `ILogger<T>` lines that flow into the Log Analytics workspace, queryable with KQL by correlation ID. That is the *logs* half of observability — what happened, in order, with the right context attached. What it does not tell you is *how the system performed*: which requests were slow, which dependencies were called, where time was spent, and how many exceptions per minute were thrown across all replicas.

In this exercise you'll add **Application Insights**, an Application Performance Management (APM) sink layered on top of the same Log Analytics workspace. In workspace-based mode it writes into the workspace you already have — same store, different shape. Requests, dependencies, exceptions, and custom metrics become first-class entities the Azure Portal already knows how to visualise. You'll see live traffic in **Live Metrics**, follow a request through the **Application Map**, and drill into a deliberately induced exception in the **Failures** blade.

This exercise ends the chapter and the live cloud lab that has run since Week 4. The final step tears down the resource group `rg-cicd-week4` (Container App, ACR, Log Analytics workspace, App Insights, and every role assignment scoped under the group) and deletes the Entra app registration created for OIDC federation. After this exercise nothing you provisioned should still be running.

> **What you'll learn:**
>
> - What Application Insights gives you that raw `ILogger` lines do not — requests, dependencies, exceptions, traces, and custom metrics as structured telemetry
> - Why workspace-based Application Insights is the right default in 2026 — telemetry lands in the same Log Analytics workspace as your container logs, so KQL queries can join across both
> - How to inject a connection string as a Container Apps secret rather than a plain environment variable, and what `secretref:` means at runtime
> - How to read three Portal blades — Live Metrics, Application Map, Failures — and what each is for
> - How to track a custom metric with `TelemetryClient.GetMetric` and find it in the Metrics blade
> - How to tear down both the resource group and the tenant-level Entra app registration so no Azure or Entra clutter is left behind

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ The previous exercise complete: structured `ILogger<T>` lines flowing into Log Analytics, queryable via KQL with correlation IDs, and the Container App back at one replica
> - ✓ The resource group `rg-cicd-week4` still alive, holding ACR, the Container Apps environment, the Container App `ca-cicd-week4`, and the Log Analytics workspace
> - ✓ The CI/CD pipeline from the previous chapter still wired up — `git push` deploys to the Container App via OIDC federation
> - ✓ The Azure CLI signed in (`az account show` returns your subscription)
> - ✓ A local clone of the `CloudCi` ASP.NET Core MVC project where you can edit code and push to `main`

## Exercise Steps

### Overview

1. **What Application Insights gives you that raw logs do not**
2. **Provision the Application Insights component in workspace-based mode**
3. **Add the SDK to the project**
4. **Wire it up in `Program.cs`**
5. **Inject the connection string as a Container Apps secret**
6. **Reference the secret as an environment variable**
7. **Push and deploy**
8. **Open Live Metrics in the Portal**
9. **Explore the Application Map**
10. **Induce an exception with `/Home/Boom`**
11. **Find the exception in the Failures blade**
12. **Add a custom metric for home-page views**
13. **Test Your Implementation**
14. **Tear down the cloud resources**

### **Step 1:** What Application Insights gives you that raw logs do not

Before adding any code, get clear on what Application Insights actually models. The previous exercise gave you `ILogger` lines: free-form structured text with a severity, a category, a timestamp, and a few key/value properties. That is the right shape for *narrative* — "user X did Y at time Z" — but the wrong shape for asking *aggregate* questions like "what is the p95 latency of `/Home/Index` over the last hour?" Application Insights makes those questions cheap by modelling distinct telemetry types from the start.

> ℹ **Concept Deep Dive: Requests, dependencies, and exceptions**
>
> A **request** is one unit of work the application served — for ASP.NET Core, one HTTP request. The SDK auto-instruments this: every request that reaches your middleware pipeline produces a request telemetry item with the URL, the HTTP method, the response code, the duration in milliseconds, and the operation ID. You don't write any code to make this happen. Aggregations like "p95 latency of `/Home/Index`" are queries over this stream.
>
> A **dependency** is one outgoing call your app made to serve a request — a SQL query, an HTTP call to another service, a call to Azure Storage. The SDK auto-instruments common .NET clients (`HttpClient`, `SqlClient`, the Azure SDKs). Each dependency telemetry item is correlated to the parent request via the operation ID. Today your app has none, but as soon as you add a database or call a downstream API, dependencies start appearing — and the Application Map starts having more than one node.
>
> An **exception** is a `System.Exception` that bubbled up far enough to be caught by the framework. The SDK records the type, the message, the stack trace, and any custom properties you attach. Exceptions are correlated to the request that produced them, so you can see "this 500 was caused by this `InvalidOperationException` thrown in this method."

> ℹ **Concept Deep Dive: Traces, custom metrics, the Application Map, sampling**
>
> *Trace* is overloaded in this world and worth pinning down. In Application Insights, **trace telemetry** means a log line — exactly what `ILogger` produces, captured into the same store. **Distributed tracing**, in the OpenTelemetry sense, means following one logical operation across many services using an operation ID. The two share a name and not much else. This exercise covers the first; the foreshadowing at the end of the chapter is about the second.
>
> A **custom metric** is a numeric value you track yourself — "how many home-page views per minute," "how big was the uploaded file." The SDK aggregates these client-side (count, sum, min, max, p50/p95/p99 over a one-minute bucket) and ships the aggregate, not the raw values. That is the cheap path. The expensive path is `TrackEvent` with a numeric property, which sends every individual occurrence — fine for low volume, fatal at high volume.
>
> The **Application Map** is one of the screens the Portal builds for you out of the request and dependency streams. Each node is a logical service; each edge is a dependency relationship annotated with p95 latency and failure rate. With one app and no dependencies, your map is one node — the moment you add a database, an HTTP API, or a queue, the map fills out automatically.
>
> The .NET SDK ships at 100% **sampling** by default — every request, dependency, exception, and trace is sent. That is fine for coursework. For high-traffic production services, *adaptive sampling* kicks in once you exceed five telemetry items per second, and the SDK starts shipping a representative subset; aggregations are reweighted server-side so counts and percentiles stay accurate. For this exercise we leave the defaults.

### **Step 2:** Provision the Application Insights component in workspace-based mode

Application Insights has two storage modes. The legacy mode stored telemetry in its own private store, separate from any Log Analytics workspace. The modern, workspace-based mode stores telemetry inside the Log Analytics workspace you already have — same store, same retention, same KQL queries, same billing. You'll use the workspace-based mode so the App Insights data lands in the same workspace as the container logs from the previous exercise.

1. **Capture** the resource ID of the existing Log Analytics workspace inside `rg-cicd-week4`:

   ```bash
   WS_ID=$(az monitor log-analytics workspace list \
     -g rg-cicd-week4 \
     --query '[0].id' -o tsv)

   echo "$WS_ID"
   ```

   The output is a long path that looks like `/subscriptions/.../resourceGroups/rg-cicd-week4/providers/Microsoft.OperationalInsights/workspaces/<workspace-name>`. If `$WS_ID` is empty, the workspace doesn't exist; revisit the previous exercise.

2. **Create** the Application Insights component, pointing it at the workspace:

   ```bash
   az monitor app-insights component create \
     --app cloudci-insights \
     -g rg-cicd-week4 \
     --location northeurope \
     --workspace "$WS_ID"
   ```

3. **Capture** the connection string. This is what the SDK reads at runtime to know where to send telemetry:

   ```bash
   CONN=$(az monitor app-insights component show \
     --app cloudci-insights \
     -g rg-cicd-week4 \
     --query connectionString -o tsv)

   echo "$CONN"
   ```

   The connection string looks like `InstrumentationKey=<guid>;IngestionEndpoint=https://northeurope-1.in.applicationinsights.azure.com/;LiveEndpoint=https://northeurope.livediagnostics.monitor.azure.com/;ApplicationId=<guid>`. Treat it as sensitive — anyone holding it can write telemetry to your component.

> ℹ **Concept Deep Dive**
>
> Two artefacts matter here. The **instrumentation key** is the legacy identifier — a single GUID. The **connection string** is the modern form: it bundles the instrumentation key together with the regional ingestion endpoint and the live-metrics endpoint. Always pass the connection string; the SDK accepts a bare instrumentation key for backward compatibility, but then has to guess regional endpoints, which fails in sovereign clouds and in regions with non-default endpoints.
>
> ⚠ **Common Mistakes**
>
> - Provisioning Application Insights in *classic* mode (without `--workspace`) splits your telemetry across two stores — App Insights data in the classic backend, container logs in the workspace. KQL queries that try to join across both then fail. The `--workspace` argument is what makes this a single pane of glass.
> - Naming the component the same as the Container App (`ca-cicd-week4`) is allowed but confusing. The convention `<app-name>-insights` keeps it obvious.
>
> ✓ **Quick check:** `az monitor app-insights component show --app cloudci-insights -g rg-cicd-week4 --query workspaceResourceId -o tsv` returns the same workspace path you stored in `$WS_ID`.

### **Step 3:** Add the SDK to the project

The Application Insights SDK for ASP.NET Core is a single NuGet package that auto-instruments requests, dependencies, exceptions, and `ILogger` traces. Adding it does not change any behaviour by itself — it adds the *capability* to send telemetry, which is then activated in `Program.cs` and configured by the connection string at runtime.

1. **Open** a terminal in the project root (the directory containing `CloudCi.csproj`).

2. **Add** the SDK package:

   ```bash
   dotnet add package Microsoft.ApplicationInsights.AspNetCore
   ```

3. **Inspect** the resulting `.csproj`. The `ItemGroup` for package references should now contain the new entry:

   > `CloudCi.csproj`

   ```xml
   <Project Sdk="Microsoft.NET.Sdk.Web">

     <PropertyGroup>
       <TargetFramework>net10.0</TargetFramework>
       <Nullable>enable</Nullable>
       <ImplicitUsings>enable</ImplicitUsings>
     </PropertyGroup>

     <ItemGroup>
       <PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.22.0" />
     </ItemGroup>

   </Project>
   ```

   The exact version pinned by `dotnet add` will be the latest 2.x at the time you run the command.

> ℹ **Concept Deep Dive**
>
> There is no separate "App Insights for `ILogger`" package — the moment you call `AddApplicationInsightsTelemetry()` in the next step, an `ILoggerProvider` is registered and your existing `_logger.LogInformation` lines start flowing into App Insights as **trace** telemetry on top of going to stdout.
>
> ✓ **Quick check:** `dotnet build` completes with no errors and no new warnings.

### **Step 4:** Wire it up in `Program.cs`

A single line registers the SDK with the dependency injection container. By default the SDK reads the connection string from the environment variable `APPLICATIONINSIGHTS_CONNECTION_STRING`. You're going to inject that variable in the next two steps; for now just register the services.

1. **Open** `Program.cs` in the project root.

2. **Add** the call after the existing `builder.Services.AddControllersWithViews()` (or whichever service registration is already there):

   > `Program.cs`

   ```csharp
   var builder = WebApplication.CreateBuilder(args);

   builder.Services.AddControllersWithViews();

   // Register Application Insights. Reads the connection string from the
   // APPLICATIONINSIGHTS_CONNECTION_STRING environment variable by default.
   builder.Services.AddApplicationInsightsTelemetry();

   var app = builder.Build();

   // ... existing middleware pipeline below, including the correlation-ID
   //     middleware from the previous exercise — leave it in place.
   ```

   Leave the correlation-ID middleware from the previous exercise in place. `AddApplicationInsightsTelemetry()` reads `ILogger` scope state automatically, so the `RequestId` you push onto the scope appears as a custom property on every telemetry item without any extra wiring.

> ℹ **Concept Deep Dive**
>
> `AddApplicationInsightsTelemetry()` does several things in one call: it registers the `TelemetryClient` (used for custom telemetry in step 12), wires up an `ITelemetryInitializer` that stamps every item with role name and instance id, hooks into the request pipeline to produce request telemetry, and adds an `ILoggerProvider` so log lines become trace telemetry. For ASP.NET Core in Container Apps, this single call is everything you need.
>
> ⚠ **Common Mistakes**
>
> - Calling `AddApplicationInsightsTelemetry(connectionString: "...")` with a hard-coded string. This works in development but bakes the connection string into the image, which is exactly what the secret injection in the next step is designed to avoid.
> - Adding the call after `var app = builder.Build()`. Service registration must happen on `builder.Services` *before* `Build()`. After `Build()`, the DI container is frozen.
>
> ✓ **Quick check:** The project still builds. The Container App is not yet sending telemetry — without the connection string env var, the SDK silently does nothing. That changes in Step 6.

### **Step 5:** Inject the connection string as a Container Apps secret

The connection string is sensitive: it grants write access to your App Insights component. The wrong way to deliver it to the running container is to set it as a plain environment variable on the Container App. Plain env vars are visible in the output of `az containerapp show`, in the Portal's overview blade, and to anyone with `Reader` role on the resource. That is fine for non-sensitive configuration; it is not fine for credentials. The right way is to store the value as a Container Apps **secret** and reference the secret from an environment variable.

1. **Set** the connection string as a secret on the Container App. The secret name `appinsights-connstr` is conventional; it must be lowercase and hyphenated:

   ```bash
   az containerapp secret set \
     -g rg-cicd-week4 \
     -n ca-cicd-week4 \
     --secrets appinsights-connstr="$CONN"
   ```

2. **Verify** the secret is registered. The CLI lists names but never values — that's by design:

   ```bash
   az containerapp secret list \
     -g rg-cicd-week4 \
     -n ca-cicd-week4 \
     -o table
   ```

   You should see one row with name `appinsights-connstr`.

> ℹ **Concept Deep Dive**
>
> Container Apps secrets are stored encrypted in Azure's control plane and only injected into the container at process start. You cannot read them back via `az containerapp show`; they only leave Azure as values for env vars that explicitly reference them via `secretref:`. This mirrors the Kubernetes `Secret`-mounted-as-env-var pattern intentionally.
>
> ⚠ **Common Mistakes**
>
> - Names with uppercase letters or underscores will be rejected. Use `appinsights-connstr`, not `AppInsightsConnStr` or `appinsights_connstr`.
> - Setting the same name twice with different values silently overwrites. There is no version history; if you need to rotate, the rotation is destructive.
>
> ✓ **Quick check:** `az containerapp secret list` shows `appinsights-connstr` and no other unexpected entries.

### **Step 6:** Reference the secret as an environment variable

The container reads configuration from environment variables, not from Container Apps secrets directly. To bridge the two, you set an environment variable whose *value* is the literal string `secretref:<secret-name>`. The Container Apps runtime intercepts that prefix and substitutes the actual secret value when the container starts. Inside the container, the env var looks like a normal env var.

1. **Update** the Container App so the env var `APPLICATIONINSIGHTS_CONNECTION_STRING` points at the secret you just stored:

   ```bash
   az containerapp update \
     -g rg-cicd-week4 \
     -n ca-cicd-week4 \
     --set-env-vars APPLICATIONINSIGHTS_CONNECTION_STRING=secretref:appinsights-connstr
   ```

2. **Confirm** the env var is wired correctly:

   ```bash
   az containerapp show \
     -g rg-cicd-week4 -n ca-cicd-week4 \
     --query 'properties.template.containers[0].env' -o json
   ```

   You should see an entry like:

   ```json
   {
     "name": "APPLICATIONINSIGHTS_CONNECTION_STRING",
     "secretRef": "appinsights-connstr"
   }
   ```

   The literal value of the connection string does **not** appear here. That is the whole point.

> ℹ **Concept Deep Dive**
>
> The `secretref:` prefix is the load-bearing piece. Inside the container the process sees `APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=...;IngestionEndpoint=...` — a normal-looking env var. From outside, anyone with `Reader` on the Container App can list env vars but only sees the secret reference; reading the actual secret value requires the separate `Microsoft.App/containerApps/listSecrets/action` permission. Env vars are public, secrets are gated — that separation is what makes the pattern useful.
>
> ⚠ **Common Mistakes**
>
> - Setting the env var to the literal connection string (`--set-env-vars APPLICATIONINSIGHTS_CONNECTION_STRING="$CONN"`). This works at runtime but bakes the connection string into the Container App's revision history, where it is enumerable forever via `az containerapp show`. Even though a connection string is not a credential in the strict sense, it grants write access to your App Insights component and should be treated like one.
> - Forgetting to set the env var at all. The secret exists but nothing references it, so the SDK gets a null connection string and silently drops every telemetry item.
>
> ✓ **Quick check:** The JSON output above shows the env var with `secretRef` (not a `value`). The Container App revision is in `RunningStatus: Running`.

### **Step 7:** Push and deploy

The SDK changes from Steps 3 and 4 are still only on your local machine. The pipeline from the previous chapter rebuilds and updates the Container App on every push to `main`, so commit and push to roll the new SDK into the running container.

1. **Commit and push:**

   ```bash
   git add CloudCi.csproj Program.cs
   git commit -m "Add Application Insights SDK"
   git push
   gh run watch
   ```

   The workflow should turn green in a couple of minutes; both `deploy` and `smoke-test` should pass.

2. **Capture** the FQDN and verify the new revision serves traffic:

   ```bash
   FQDN=$(az containerapp show \
     -g rg-cicd-week4 -n ca-cicd-week4 \
     --query properties.configuration.ingress.fqdn -o tsv)

   curl -I "https://$FQDN/"
   ```

   Expected: `HTTP/2 200`. The first request after a deploy may be slower (cold start) but should succeed.

> ✓ **Quick check:** Workflow green, a new revision active with 100% traffic, home page returns `200`.

### **Step 8:** Open Live Metrics in the Portal

Live Metrics proves the SDK is wired correctly. Unlike the rest of Application Insights — which has a one-to-three-minute ingestion delay — Live Metrics streams telemetry over a separate channel with one-second latency. If your container is sending telemetry, you see it within a second.

1. **Open** the Azure Portal at <https://portal.azure.com>, navigate to Application Insights `cloudci-insights`, and click **Live Metrics** in the left navigation.

2. **Generate** traffic from your terminal:

   ```bash
   for i in {1..40}; do curl -s "https://$FQDN/" >/dev/null; sleep 0.25; done
   ```

3. **Watch** the view. Within a second you should see incoming requests climbing to ~4/sec, request duration in the tens of milliseconds, sample requests on the right-hand side with URL/response code/duration, and server health counters (CPU and memory of the running replica).

> ℹ **Concept Deep Dive**
>
> Live Metrics is push-based. The SDK opens an outbound connection to the live-diagnostics endpoint encoded in the connection string and streams a high-frequency summary while the blade is open; closing the blade stops the stream. There is no polling and no historical retention — only the last few minutes. This is why Live Metrics is free on production services: it doesn't go through ingestion.
>
> ⚠ **Common Mistakes**
>
> - Live Metrics shows "Not available" — almost always means the Container App can't reach the live-diagnostics endpoint, or the SDK isn't loaded, or the env var isn't set. Re-check the secretref wiring from Step 6.
> - Confusing Live Metrics with the Metrics blade. They are different screens. Metrics is for historical charts over your aggregated data. Live Metrics is for *now*.
>
> ✓ **Quick check:** Live Metrics shows incoming requests spiking when you run the curl loop, and falling to zero when you stop.

### **Step 9:** Explore the Application Map

The Application Map is built from the request and dependency streams. With one app and no dependencies it is the most boring possible map: one node. That is fine — opening it now establishes the empty starting state, so the day you add a database call or a downstream HTTP API you immediately recognise the new nodes that appear.

1. **Click** **Application Map** in the left navigation. Wait a minute or two for the map to draw itself; if it says "No data," re-run the curl loop from Step 8 and refresh.

2. **Inspect** the single node — labelled with the role name (typically the Container App name) — showing request count, failure rate, and average duration over the selected time range. Click the node to open a side panel with URL breakdowns, slowest requests, and failed requests.

> ℹ **Concept Deep Dive**
>
> The map fills out automatically as the app gains dependencies. The moment you add an `HttpClient` call to a downstream API, the SDK auto-instruments the call and records dependency telemetry; Application Map draws a second node for the target host and an edge labelled with p95 latency and failure rate. The same happens for SQL queries, Azure Storage calls, and service bus calls. When the course later tackles services calling other services, this map gives you the architecture diagram for free — and it surfaces "this slow request was slow because the database call was slow" because dependency timing is correlated to the parent request's operation ID.
>
> ✓ **Quick check:** The Application Map shows exactly one node with a non-zero request count.

### **Step 10:** Induce an exception with `/Home/Boom`

Production exceptions are interesting precisely because they're rare and unpredictable. To see how the Failures blade looks when there's something to look at, you need to throw an exception on purpose. Add a controller action that throws unconditionally; you will hit it a few times, then look at how App Insights presents the result.

1. **Open** `Controllers/HomeController.cs`.

2. **Add** a `Boom` action method to the existing `HomeController` class. Place it next to the existing `Index` action:

   > `Controllers/HomeController.cs`

   ```csharp
   public IActionResult Boom()
   {
       _logger.LogError("About to throw an exception from /Home/Boom");
       throw new InvalidOperationException(
           "Boom! This is a deliberate failure for the Failures blade.");
   }
   ```

3. **Commit, push, and hit** the new endpoint once the new revision is active:

   ```bash
   git add Controllers/HomeController.cs
   git commit -m "Add /Home/Boom action that throws"
   git push && gh run watch

   for i in {1..5}; do curl -s -o /dev/null -w "%{http_code}\n" "https://$FQDN/Home/Boom"; done
   ```

   Expected: five `500` lines. The browser would show the standard ASP.NET Core error page.

> ⚠ **Common Mistakes**
>
> - In Development environment, the `DeveloperExceptionPage` middleware catches the exception, renders a debug page, and the request completes with `500` but the framework treats it as handled. Telemetry still records the exception, but the framing changes. Container Apps run with `ASPNETCORE_ENVIRONMENT=Production` by default, which is what you want here. If you've overridden it to `Development`, the rest of this step still works but the response body looks different.
> - Forgetting to wait for the new revision before curling `/Home/Boom`. If the curl returns `200`, the old revision is still serving. Re-check `az containerapp revision list`.
>
> ✓ **Quick check:** Five `500` responses from `/Home/Boom`. The home page (`/`) still returns `200` — only `/Home/Boom` throws.

### **Step 11:** Find the exception in the Failures blade

App Insights ingestion is not instant. The Live Metrics channel is real-time, but everything indexed for query — including Failures — takes one to three minutes to appear. Wait for ingestion, then explore.

1. **Wait** about one minute after the last `curl` to `/Home/Boom`, then navigate to **Failures** in the App Insights left navigation. Set the time range (top right) to the last 30 minutes.

2. **Inspect** the **Operations** tab — one row for `GET Home/Boom` with five failed requests and 100% failure rate. Click the **Exceptions** tab — one row for `System.InvalidOperationException` with five occurrences.

3. **Drill** in by clicking the exception row, then "View N more" or "Examine samples." A side panel opens showing the exception type, the message, the full stack trace, the request URL, the timestamp, and the operation ID — the same correlation ID you've been using to trace requests through `ILogger`.

> ℹ **Concept Deep Dive**
>
> What App Insights captures by default for an exception: the type, the message, the stack trace, the request URL, the response code, the operation ID, and any custom properties attached via `TelemetryClient.TrackException` or an `ITelemetryInitializer`. What it does **not** capture: the HTTP request body, request headers, or local variables at the throw site. Omitting the body is deliberate — request bodies often contain sensitive data, and shipping them to telemetry is a compliance risk. To enrich a specific exception you catch it, attach properties, and re-throw, or implement an `ITelemetryProcessor`. Both are deliberate, code-level choices.
>
> ⚠ **Common Mistakes**
>
> - Querying immediately after the first `curl` and seeing nothing — ingestion delay is one to three minutes. Wait, then refresh.
> - Searching by exception *message* instead of *type* — the Failures blade groups by type, so a custom message that varies per request fragments the view. Throw exceptions with stable types and put the variable detail in the message.
>
> ✓ **Quick check:** Failures blade shows exactly one exception type (`InvalidOperationException`) with five occurrences, all originating from `GET Home/Boom`.

### **Step 12:** Add a custom metric for home-page views

Requests, dependencies, and exceptions are auto-instrumented. Custom metrics are not — they are the first telemetry where you write code. Inject `TelemetryClient`, call `GetMetric("name").TrackValue(value)` wherever the event happens; the SDK aggregates client-side and ships per-minute summaries, cheap even at high volume.

1. **Open** `Controllers/HomeController.cs`, add `using Microsoft.ApplicationInsights;`, then inject `TelemetryClient` alongside the existing `ILogger` and call `GetMetric` from `Index`:

   > `Controllers/HomeController.cs`

   ```csharp
   private readonly ILogger<HomeController> _logger;
   private readonly TelemetryClient _telemetry;

   public HomeController(ILogger<HomeController> logger, TelemetryClient telemetry)
   {
       _logger = logger;
       _telemetry = telemetry;
   }

   public IActionResult Index()
   {
       // GetMetric returns a pre-aggregated metric that buckets values
       // client-side and ships one-minute summaries. Cheap at any volume.
       _telemetry.GetMetric("home-page-views").TrackValue(1);

       // ...your existing Index code below
       return View();
   }
   ```

   Leave the `Boom` action from Step 10 unchanged.

2. **Commit, push, and generate traffic** once the new revision is running:

   ```bash
   git add Controllers/HomeController.cs
   git commit -m "Track home-page views as a custom metric"
   git push && gh run watch

   for i in {1..30}; do curl -s "https://$FQDN/" >/dev/null; sleep 0.5; done
   ```

3. **Wait** ~2 minutes for the per-minute aggregation to ship and ingestion to complete.

4. **Find** the metric in the Portal: Application Insights `cloudci-insights` → **Metrics**. In the picker, choose namespace `azure.applicationinsights`, metric `home-page-views`, aggregation `Sum` or `Count`. Set the time range to the last 30 minutes — you should see a clear bump where the curl loop ran.

> ℹ **Concept Deep Dive**
>
> `GetMetric` is the cheap path: the SDK keeps an in-memory bucket per metric per one-minute window and ships a single telemetry item with count, sum, min, max, and standard deviation at the end of each window. At a thousand requests per second, you ship one item per minute per metric — not 60,000. The expensive path is `TrackEvent` with a numeric property, which sends one item per occurrence; useful when each occurrence has unique properties you need to keep, but it goes through ingestion, sampling, and storage like any other event.
>
> ⚠ **Common Mistakes**
>
> - Newing up `new TelemetryClient()` instead of injecting it. The injected instance has the connection string and `ITelemetryInitializer` chain wired up; a manually constructed one does not, and silently sends to nowhere.
> - Picking the wrong namespace in the Metrics picker. Custom metrics live under `azure.applicationinsights` (or the namespace `Custom` depending on Portal version). The default `Microsoft.Insights/Components` namespace shows the platform metrics, not yours.
>
> ✓ **Quick check:** The Metrics chart shows a non-zero `home-page-views` count over the last 30 minutes that roughly matches how many times you curled the home page.

### **Step 13:** Test Your Implementation

Walk through every signal you've wired up, end to end.

1. **Live Metrics** shows incoming requests within seconds when you `curl` the home page:

   ```bash
   for i in {1..20}; do curl -s "https://$FQDN/" >/dev/null; sleep 0.5; done
   ```

2. **Application Map** shows exactly one node with non-zero traffic — your app, no dependency edges.

3. **Failures blade** shows `InvalidOperationException` with at least one occurrence after hitting `/Home/Boom` (allow ~1 min for ingestion):

   ```bash
   curl -s -o /dev/null -w "%{http_code}\n" "https://$FQDN/Home/Boom"
   ```

4. **Metrics blade** plots a non-zero `home-page-views` count over the last 30 minutes.

5. **Secret wiring is correct** — the env var is a `secretRef`, not a literal:

   ```bash
   az containerapp show -g rg-cicd-week4 -n ca-cicd-week4 \
     --query 'properties.template.containers[0].env[?name==`APPLICATIONINSIGHTS_CONNECTION_STRING`]' \
     -o json
   ```

   Expected: an object with `secretRef: "appinsights-connstr"` and **no** `value` field.

6. **Workspace-based mode** — App Insights and container logs share a workspace:

   ```bash
   az monitor app-insights component show \
     --app cloudci-insights -g rg-cicd-week4 \
     --query workspaceResourceId -o tsv
   ```

   Expected: the same `$WS_ID` from Step 2.

> ✓ **Final verification checklist:**
>
> - ☐ Application Insights component `cloudci-insights` exists in `rg-cicd-week4` and is workspace-based
> - ☐ Container Apps secret `appinsights-connstr` is set; env var references it via `secretRef:`
> - ☐ The SDK is registered in `Program.cs` via `AddApplicationInsightsTelemetry()`
> - ☐ `/Home/Boom` throws `InvalidOperationException` and the Failures blade lists it
> - ☐ `home-page-views` custom metric appears in the Metrics blade
> - ☐ The pipeline still deploys cleanly on push to `main`

### **Step 14:** Tear down the cloud resources

This is the last exercise of the chapter and the last exercise that uses the Week 4 cloud resources. Nothing later in the course needs `rg-cicd-week4`, the Container App, the ACR, the Log Analytics workspace, the Application Insights component, or the Entra app registration created for OIDC federation. Tear all of it down so you finish with no resources running and no orphaned tenant-level identities.

The work splits into two homes — the Azure subscription holds the running resources, and Microsoft Entra ID (a tenant-level service) holds the identity used by the pipeline. Deleting one does **not** delete the other, exactly as you saw at the end of the previous chapter.

1. **Delete** the resource group. This removes the Container App, the ACR with all its images, the Container Apps environment, the Log Analytics workspace, the Application Insights component (which lives inside the resource group, so the RG delete handles it implicitly), and every role assignment scoped under the group:

   ```bash
   # 1. Delete the resource group (Container App, ACR, Log Analytics, App Insights — all of it).
   az group delete -n rg-cicd-week4 --yes --no-wait
   ```

   `--no-wait` returns immediately and lets the deletion run in the background. The full teardown takes a few minutes (the Container Apps environment is the slowest piece).

2. **Delete** the Entra app registration that was created for OIDC federation in the previous chapter. The app registration lives in the tenant, not in any subscription resource group, so the RG delete above does **not** remove it. Delete it explicitly:

   ```bash
   # 2. Delete the Entra app registration that was created for OIDC federation.
   # The app registration lives in the tenant, not in any subscription resource group,
   # so the RG delete above does NOT remove it. Delete it explicitly.
   az ad app delete --id 7c11e4ce-91cd-4ba3-9fce-820669f397fe
   ```

   Replace the GUID above with your own app's `appId` if it differs — this is the same value you stored as `AZURE_CLIENT_ID` in the previous chapter. You can re-discover it with `az ad app list --display-name "github-cicd-oidc" --query "[0].appId" -o tsv` if you no longer have it handy.

3. **Verify** the resource group is gone (give it a few minutes if the `--no-wait` deletion is still in progress):

   ```bash
   az group exists -n rg-cicd-week4
   ```

   Expected: `false`.

4. **Verify** the Entra app registration is gone:

   ```bash
   az ad app list --display-name github-cicd-oidc -o tsv
   ```

   Expected: empty output (no rows).

> ℹ **Concept Deep Dive**
>
> Why the two-step cleanup matters. The resource group is a subscription-level container — `az group delete` cascades to every Azure resource scoped under it, including the Container App, the ACR, the Log Analytics workspace, the App Insights component, the Container Apps environment, and every role assignment scoped to those resources.
>
> The Entra app registration, however, is a *tenant-level* identity object. It lives in Microsoft Entra ID, not in your Azure subscription, and survives every subscription operation. Without the second `az ad app delete`, the federated identity sticks around as orphaned tenant clutter — visible in the Entra portal with broken role assignments pointing at deleted resources, contributing nothing and confusing future you. Note that Entra ID *soft-deletes* app registrations for 30 days; they're recoverable from **App registrations → Deleted applications** and garbage-collected automatically afterwards.
>
> ⚠ **Common Mistakes**
>
> - Stopping after `az group delete`. The Entra app stays alive and accumulates over multiple cohorts as orphaned `github-cicd-oidc` entries.
> - Forgetting that the GitHub repo secrets `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` in your repository are now stale. The next workflow run will fail at **Sign in to Azure** because the client ID points at a deleted app. Either delete the secrets via `gh secret delete <name>`, or leave them alone — they're inert without a working app behind them.

> ✓ **Quick check:** `az group exists -n rg-cicd-week4` returns `false`, `az ad app list --display-name github-cicd-oidc` returns nothing, and your subscription's billing is no longer accruing for any of the Week 4 / Week 5 resources.

## Common Issues

> **If you encounter problems:**
>
> **No telemetry anywhere — Live Metrics, Application Map, Metrics all empty:** The Container App didn't receive the connection string. Re-check `az containerapp show ... --query 'properties.template.containers[0].env'` for `APPLICATIONINSIGHTS_CONNECTION_STRING` with a `secretRef` field, and `az containerapp secret list` for the underlying secret.
>
> **Telemetry works but the connection string is plainly visible in `az containerapp show`:** You set the env var as a literal string instead of `secretref:`. Remove the env var (`--remove-env-vars`), confirm the secret is set, then re-set with the `secretref:` form from Step 6.
>
> **Failures blade empty after hitting `/Home/Boom`:** Either ingestion delay (wait 1–3 minutes) or `ASPNETCORE_ENVIRONMENT=Development` is letting `DeveloperExceptionPage` swallow the exception in the dev pipeline. Container Apps default to Production.
>
> **Custom metric `home-page-views` missing from the Metrics blade:** Either you used `new TelemetryClient()` instead of injecting it (manually constructed clients have no connection string), the namespace picker is wrong (custom metrics live under `azure.applicationinsights` or `Custom`, not `Microsoft.Insights/Components`), or the per-minute aggregate hasn't shipped yet (wait 2–3 minutes).
>
> **Live Metrics says "Not available":** The Container App can't reach the live-diagnostics endpoint, the SDK didn't load, or the env var isn't set. Check `az containerapp logs show -g rg-cicd-week4 -n ca-cicd-week4 --follow` for SDK errors at startup.
>
> **`az group delete` returns immediately but resources still appear in the Portal:** That's `--no-wait` working as intended. Re-check with `az group exists -n rg-cicd-week4`.
>
> **Still stuck?** Verify three things in order: the SDK package in `CloudCi.csproj`, `AddApplicationInsightsTelemetry()` in `Program.cs`, and the Container App's env list. All three must be correct for telemetry to flow.

## Summary

You added Application Insights to a deployed ASP.NET Core app in workspace-based mode, so telemetry lands in the same Log Analytics workspace as your container logs. The connection string is delivered the right way — as a Container Apps secret referenced via `secretref:` — so it never appears in `az containerapp show`. You exercised three Portal blades, each answering a different question: Live Metrics for "is anything happening right now," Application Map for "what is the architecture," Failures for "what is going wrong." One custom metric gives the Metrics blade something of yours to plot.

- ✓ Workspace-based Application Insights — telemetry lives in the same store as container logs
- ✓ Connection string injected as a Container Apps secret, referenced via `secretref:`
- ✓ Auto-instrumented requests, dependencies, and exceptions, no code changes required
- ✓ Custom metric tracked via `TelemetryClient.GetMetric`, aggregated client-side
- ✓ Resource group and Entra app registration both torn down — no orphaned cloud or tenant resources

> **Key takeaway:** Logs answer "what happened, in order." Metrics and traces answer "how is the system performing, in aggregate." A real production observability stack uses both. Application Insights gives you the metrics and traces side as a default-on capability the moment the SDK is registered — you don't need to instrument anything you wouldn't already be writing. The cost of that capability is one NuGet package, one line in `Program.cs`, and one secret on the Container App. The cost of *not* having it is the next outage you investigate from logs alone.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Configure an alert rule on the `home-page-views` metric or the requests-failed counter (Application Insights → Alerts → New alert rule). Wire it to email or webhook when a threshold is crossed.
> - Build a custom Workbook (Application Insights → Workbooks) that combines request rate, p95 latency, and failure count on one page. Workbooks accept KQL against the workspace, so logs and App Insights data sit side by side.
> - Read up on the two senses of **trace**: trace telemetry in Application Insights (`ILogger` lines) versus distributed tracing in the OpenTelemetry sense (one operation crossing service boundaries via the W3C Trace Context header). The vocabulary collision is the single biggest stumbling block in this space.
> - Investigate **OpenTelemetry** as the future direction. The OpenTelemetry .NET SDK with the Azure Monitor exporter writes to the same Application Insights backend, but lets you re-target Datadog, New Relic, or self-hosted Jaeger with only an exporter change.

## Done!

This exercise ends the deployment block of the course. You have a CI/CD pipeline that ships every push to `main`, an OIDC trust relationship that authenticates without a long-lived secret, structured logs flowing into Log Analytics, and now an APM layer on top of the same workspace that gives you Live Metrics, the Application Map, the Failures blade, and a custom metric. Together those pieces are what people mean when they say "the deployment is observable in production."

The lab is now torn down. The resource group is gone, the Entra app registration is gone, the GitHub secrets in your repo are stale and inert. There is nothing left running and nothing left billing.

The next thing the course tackles is building services that call other services. The Application Map you saw with one node will start to fill out as soon as you have a downstream API to call — the SDK auto-instrumentation does the work for free, and the same operation ID that ties your `ILogger` lines together also ties the request in service A to the request in service B to the SQL query in the database. That is what distributed tracing buys you, and it is what the rest of the program will spend time exploring.
