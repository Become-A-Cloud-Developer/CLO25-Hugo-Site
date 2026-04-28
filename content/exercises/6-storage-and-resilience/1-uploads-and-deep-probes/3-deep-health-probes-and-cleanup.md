+++
title = "Deep Health Probes and Cleanup"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Add three health-check endpoints with distinct semantics — /health/live for liveness, /health/ready gated on Cosmos and Blob, /health for diagnostic JSON — wire Container Apps probes, then tear down the resource group and the Entra OIDC app."
weight = 3
draft = false
+++

# Deep Health Probes and Cleanup

## Goal

In the previous exercise you finished wiring `CloudCiCareers.Web` to Cosmos and Blob via the Container App's system-assigned managed identity. `CosmosApplicationStore` and `AzureBlobService` are the registered production implementations, the App Insights connection is still in place, the pipeline still rolls a fresh image on every push to `main`, and persistence survives revision rollovers. What's missing is a way for the platform — and for you — to tell whether the running replica is actually healthy. There are no health-check endpoints in the app, and Container Apps' liveness and readiness probes are at their defaults, which means the platform infers liveness from the TCP listener alone. A replica whose Cosmos data-plane role assignment got revoked still listens on port 8080 and still receives traffic; it just returns 500 on every request that touches the database.

In this exercise you'll add three health endpoints with three distinct semantics, wire two of them as Container Apps probes, and end the chapter with a cleanup substep that tears down `rg-careers-week7` and the `github-cloudci-careers-oidc` app registration in Entra. The three endpoints are `/health/live` (liveness — answers "is this process alive"), `/health/ready` (readiness — answers "is this replica ready to serve traffic, given its dependencies"), and `/health` (diagnostic — JSON breakdown for humans, dashboards, and on-call investigation). The first two are consumed by the platform; the third is consumed by people.

This is the last exercise of the chapter. Once the cleanup substep finishes, the cloud lab is fully torn down and you can re-run the chapter from a clean slate any time.

> **What you'll learn:**
>
> - How to register the ASP.NET Core health-checks pipeline and tag individual checks so different endpoints expose different subsets
> - Why liveness must answer "is *this process* alive" and must NOT depend on external services
> - Why readiness gates on dependencies, and how to wire it into Container Apps so the platform drains traffic from a degraded replica
> - How to write a tiny custom `IHealthCheck` that probes Cosmos and Blob with explicit timeouts
> - How to hand-roll a JSON `ResponseWriter` for the diagnostic endpoint, and which library replaces it in production
> - How to tear down both subscription resources (resource group) and tenant-level identity objects (Entra app registration) so the lab leaves no orphans

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ The previous exercise complete: `CloudCiCareers.Web` deployed to `https://ca-careers-week7.<env>.northeurope.azurecontainerapps.io/`, with `CosmosApplicationStore` and `AzureBlobService` registered as the production implementations, both authenticated via the Container App's system-assigned managed identity
> - ✓ Cosmos and Storage reachable from the running container; persistence verified to survive revision rollovers
> - ✓ App Insights still wired and the CI/CD pipeline still rolling a fresh image on every push to `main`
> - ✓ The Azure CLI signed in (`az account show` returns your subscription)
> - ✓ A local clone of `CloudCiCareers.Web` where you can edit code and push to `main`
> - ✓ `curl` and `jq` available on your machine

## Exercise Steps

### Overview

1. **Add the health-checks package and registration scaffold**
2. **Implement `CosmosHealthCheck`**
3. **Implement `BlobHealthCheck`**
4. **Map the three endpoints with distinct predicates**
5. **Hand-roll the JSON `ResponseWriter`**
6. **Push, deploy, and verify the endpoints respond**
7. **Update the smoke test in the workflow to target `/health/live`**
8. **Wire Container Apps liveness and readiness probes**
9. **Tear down the cloud resources**
10. **Test Your Implementation**

### **Step 1:** Register the health-checks pipeline scaffold

The ASP.NET Core health-checks pipeline lives in `Microsoft.Extensions.Diagnostics.HealthChecks` — a package that's already on the implicit `Microsoft.AspNetCore.App` framework reference, so no `dotnet add package` is required. Register the services once with `AddHealthChecks()`, then attach individual checks via `.AddCheck(...)`. The first check is the always-true `self` probe — it reports `Healthy` unconditionally and is the load-bearing check behind `/health/live`. Liveness asks one question: "is this process running and able to execute code?" If the always-true delegate returns, the answer is yes.

1. **Open** `Program.cs`.

2. **Register** the health-checks pipeline alongside the other service registrations:

   > `Program.cs`

   ```csharp
   builder.Services.AddHealthChecks()
       .AddCheck("self", () => HealthCheckResult.Healthy());
   ```

3. **Add** the `using` statement at the top of the file:

   > `Program.cs`

   ```csharp
   using Microsoft.Extensions.Diagnostics.HealthChecks;
   ```

> ℹ **Concept Deep Dive**
>
> The `self` check seems trivial — and it is — but it's the right primitive for liveness. The whole point of liveness is to ask a question that has nothing to do with dependencies: if the delegate runs, the process is alive. The endpoint that maps to this check returns 200 the moment ASP.NET Core can execute middleware, and 503 only if the runtime is so wedged that even an always-true callback can't return. That's exactly what Container Apps wants to consume to decide whether to restart the replica.
>
> ✓ **Quick check:** `dotnet build` succeeds with no new warnings.

### **Step 2:** Implement `CosmosHealthCheck`

`/health/ready` needs to know whether Cosmos is reachable from this replica. A custom `IHealthCheck` is the right shape — it has access to the DI container (so it can resolve `CosmosClient` and `IConfiguration`), it returns a `HealthCheckResult` that the pipeline aggregates with the other checks, and it's where you put the explicit timeout so a hung Cosmos call doesn't freeze the readiness endpoint.

1. **Navigate to** the `Services` directory.

2. **Create a new file** named `CosmosHealthCheck.cs`.

3. **Add the following code:**

   > `Services/CosmosHealthCheck.cs`

   ```csharp
   using Microsoft.Azure.Cosmos;
   using Microsoft.Extensions.Diagnostics.HealthChecks;

   namespace CloudCiCareers.Web.Services;

   public class CosmosHealthCheck : IHealthCheck
   {
       private readonly CosmosClient _client;
       private readonly string _databaseName;
       private readonly string _containerName;

       public CosmosHealthCheck(CosmosClient client, IConfiguration config)
       {
           _client = client;
           _databaseName = config["Cosmos:Database"]!;
           _containerName = config["Cosmos:Container"]!;
       }

       public async Task<HealthCheckResult> CheckHealthAsync(
           HealthCheckContext context,
           CancellationToken cancellationToken = default)
       {
           using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
           cts.CancelAfter(TimeSpan.FromSeconds(5));

           try
           {
               var container = _client.GetContainer(_databaseName, _containerName);
               await container.ReadContainerAsync(cancellationToken: cts.Token);
               return HealthCheckResult.Healthy("Cosmos reachable");
           }
           catch (Exception ex)
           {
               return HealthCheckResult.Unhealthy("Cosmos unreachable", ex);
           }
       }
   }
   ```

4. **Open** `Program.cs` and extend the existing health-checks registration:

   > `Program.cs`

   ```csharp
   builder.Services.AddHealthChecks()
       .AddCheck("self", () => HealthCheckResult.Healthy())
       .AddCheck<CosmosHealthCheck>("cosmos", tags: new[] { "ready" });
   ```

> ℹ **Concept Deep Dive: Health-check timeouts**
>
> Without an explicit timeout, a hung Cosmos call freezes the readiness endpoint, which makes Container Apps think the app is failing. The 5-second `CancellationTokenSource` here is the maximum time the check is willing to wait before declaring the dependency unreachable. Tune the value based on the dependency's SLA — Cosmos in the same region typically responds in tens of milliseconds, so 5 seconds is generous. For a slower or noisier dependency, bump it; for a fast in-VNet service, lower it. The point is that the timeout exists at all.
>
> ⚠ **Common Mistakes**
>
> - Letting the check inherit the ambient request cancellation token without adding its own deadline. The pipeline's outer timeout is much longer than what you want for a single-dependency probe.
> - Returning `Healthy` on a swallowed exception. If `ReadContainerAsync` throws, the dependency is *not* healthy from this replica's point of view; surface that.
>
> ✓ **Quick check:** `dotnet build` succeeds. The new check is registered with the tag `"ready"`.

### **Step 3:** Implement `BlobHealthCheck`

The same shape as the Cosmos check, except the probe is `BlobContainerClient.ExistsAsync(...)`. That call is cheap — it's a HEAD on the container — and it covers the two failure modes that matter: the storage account is unreachable, or the managed identity can't authenticate against it.

1. **Navigate to** the `Services` directory.

2. **Create a new file** named `BlobHealthCheck.cs`.

3. **Add the following code:**

   > `Services/BlobHealthCheck.cs`

   ```csharp
   using Azure.Storage.Blobs;
   using Microsoft.Extensions.Diagnostics.HealthChecks;

   namespace CloudCiCareers.Web.Services;

   public class BlobHealthCheck : IHealthCheck
   {
       private readonly BlobServiceClient _client;
       private readonly string _containerName;

       public BlobHealthCheck(BlobServiceClient client, IConfiguration config)
       {
           _client = client;
           _containerName = config["Storage:Container"]!;
       }

       public async Task<HealthCheckResult> CheckHealthAsync(
           HealthCheckContext context,
           CancellationToken cancellationToken = default)
       {
           using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
           cts.CancelAfter(TimeSpan.FromSeconds(5));

           try
           {
               var containerClient = _client.GetBlobContainerClient(_containerName);
               var response = await containerClient.ExistsAsync(cancellationToken: cts.Token);
               return response.Value
                   ? HealthCheckResult.Healthy("Blob container reachable")
                   : HealthCheckResult.Unhealthy("Blob container missing");
           }
           catch (Exception ex)
           {
               return HealthCheckResult.Unhealthy("Blob unreachable", ex);
           }
       }
   }
   ```

4. **Extend** the registration in `Program.cs`:

   > `Program.cs`

   ```csharp
   builder.Services.AddHealthChecks()
       .AddCheck("self", () => HealthCheckResult.Healthy())
       .AddCheck<CosmosHealthCheck>("cosmos", tags: new[] { "ready" })
       .AddCheck<BlobHealthCheck>("blob", tags: new[] { "ready" });
   ```

> ⚠ **Common Mistakes**
>
> - Using `GetPropertiesAsync` instead of `ExistsAsync`. `GetPropertiesAsync` requires `Storage Blob Data Reader` at minimum and pulls more data; `ExistsAsync` is cheaper and works with the same RBAC role you already granted the managed identity in the previous exercise.
> - Forgetting the `tags: new[] { "ready" }` argument. Without the tag, the check still runs on `/health` (the diagnostic endpoint) but is invisible to `/health/ready` because the predicate filters by tag.
>
> ✓ **Quick check:** Both custom checks build, both are registered, and both carry the `"ready"` tag.

### **Step 4:** Map the three endpoints with distinct predicates

This is the load-bearing piece of the exercise: three endpoints, three audiences, three predicates. `/health/live` runs no checks at all — its predicate is `_ => false`, which means "include nothing." If the pipeline can run the predicate, the process is alive; that's the entire assertion. `/health/ready` runs only checks tagged `"ready"`, so it gates on Cosmos and Blob. `/health` runs every registered check and uses a custom JSON writer to render a per-check breakdown.

1. **Open** `Program.cs`.

2. **Locate** the existing endpoint mappings (after `var app = builder.Build();`).

3. **Add** the three health-check mappings before `app.MapControllers()` (or wherever your other endpoint mappings live):

   > `Program.cs`

   ```csharp
   app.MapHealthChecks("/health/live", new HealthCheckOptions
   {
       Predicate = _ => false   // No checks. Process is alive — that's all this endpoint asserts.
   });

   app.MapHealthChecks("/health/ready", new HealthCheckOptions
   {
       Predicate = c => c.Tags.Contains("ready")
   });

   app.MapHealthChecks("/health", new HealthCheckOptions
   {
       ResponseWriter = WriteJsonResponse
   });
   ```

4. **Add** the `using` for `HealthCheckOptions`:

   > `Program.cs`

   ```csharp
   using Microsoft.AspNetCore.Diagnostics.HealthChecks;
   ```

5. **Leave** `WriteJsonResponse` undefined for now — Step 5 adds it as a static local method in the same file.

> ℹ **Concept Deep Dive: Three endpoints, three audiences**
>
> The temptation is to ship a single `/health` endpoint and wire every consumer to it. Resist. Each of the three endpoints answers a different question for a different consumer.
>
> `/health/live` is for the platform's liveness probe. The platform reads it and decides whether to restart the replica. The right answer is "yes, the process is alive" or "no, restart it." Anything more nuanced — "Cosmos is degraded but the process is fine" — confuses the platform into restarting replicas that don't need restarting.
>
> `/health/ready` is for the platform's readiness probe. The platform reads it and decides whether to send the replica traffic. If Cosmos is unreachable from this replica, traffic should drain to other replicas (which may have working connections) until the dependency recovers.
>
> `/health` is for *humans* — dashboards, alerting, on-call investigation. It returns a per-check JSON breakdown. It is **not** wired as a probe. Container Apps shouldn't react to "blob check is degraded but cosmos is fine"; that's a human signal that needs human judgement.
>
> Three endpoints, three audiences. That distinction is what makes the difference between a service that recovers gracefully when its dependencies wobble and one that flaps on every transient hiccup.
>
> ✓ **Quick check:** All three mappings compile. The endpoints aren't reachable yet because `WriteJsonResponse` doesn't exist; Step 5 fixes that.

### **Step 5:** Hand-roll the JSON `ResponseWriter`

The default response writer for `MapHealthChecks` writes the plain-text status (`Healthy`, `Degraded`, `Unhealthy`) and nothing else. That's fine for `/health/live` and `/health/ready` — the platform only cares about the HTTP status code, not the body — but `/health` is for humans, and humans want a per-check breakdown. Hand-roll a tiny `System.Text.Json` writer; ~10 lines, no extra NuGet dependency.

1. **Open** `Program.cs`.

2. **Add** the `using`:

   > `Program.cs`

   ```csharp
   using System.Text.Json;
   ```

3. **Add** the static helper at the bottom of `Program.cs` (after `app.Run()` is fine — top-level files allow trailing static locals via a regular method declaration):

   > `Program.cs`

   ```csharp
   static Task WriteJsonResponse(HttpContext ctx, HealthReport report)
   {
       ctx.Response.ContentType = "application/json";
       var payload = JsonSerializer.Serialize(new
       {
           status = report.Status.ToString(),
           checks = report.Entries.Select(e => new
           {
               name = e.Key,
               status = e.Value.Status.ToString(),
               duration_ms = (int)e.Value.Duration.TotalMilliseconds
           })
       });
       return ctx.Response.WriteAsync(payload);
   }
   ```

4. **Verify** the shape of the JSON. Locally, `dotnet run` then `curl -s http://localhost:<port>/health | jq .` should return:

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

> ℹ **Concept Deep Dive**
>
> Real services usually take a NuGet dependency on `AspNetCore.HealthChecks.UI.Client` and use `UIResponseWriter.WriteHealthCheckUIResponse` instead of a hand-rolled writer. That writer produces a richer schema (durations, descriptions, tags, exceptions in non-production environments) and is the canonical shape parsed by the `AspNetCore.HealthChecks.UI` dashboard. Hand-rolling here keeps the dependency surface small and shows what the writer is actually doing under the hood; once you've seen the moving parts, switching to the library writer is a one-line change.
>
> ✓ **Quick check:** Locally, `curl -s http://localhost:<port>/health | jq .` returns the JSON shown above with `"status": "Healthy"` and three entries.

### **Step 6:** Push, deploy, and verify the endpoints respond

The endpoint code is ready. Push to `main`, let the pipeline roll a new revision, and verify all three endpoints respond correctly against the deployed Container App.

1. **Commit and push:**

   ```bash
   git add CloudCiCareers.Web.csproj Program.cs Services/CosmosHealthCheck.cs Services/BlobHealthCheck.cs
   git commit -m "Add deep health probes for liveness, readiness, and diagnostics"
   git push
   gh run watch
   ```

   The workflow should turn green in a couple of minutes.

2. **Capture** the FQDN:

   ```bash
   FQDN=$(az containerapp show \
     -g rg-careers-week7 -n ca-careers-week7 \
     --query properties.configuration.ingress.fqdn -o tsv)

   echo "$FQDN"
   ```

3. **Verify** all three endpoints with `curl`:

   ```bash
   curl -is "https://$FQDN/health/live"   # 200, body "Healthy"
   curl -is "https://$FQDN/health/ready"  # 200, body "Healthy"
   curl -s  "https://$FQDN/health" | jq . # full JSON
   ```

> ⚠ **Common Mistakes**
>
> - Hitting the API before the new revision is active. `az containerapp revision list -g rg-careers-week7 -n ca-careers-week7 -o table` shows which revision serves traffic.
> - `/health/ready` returning `503` from a fresh deploy because role-assignment propagation is still settling. Wait a minute and retry; the smoke-test loop in Step 7 handles this automatically.
>
> ✓ **Quick check:** Three responses: `/health/live` and `/health/ready` return 200 with body `Healthy`; `/health` returns a JSON document with three entries, all `Healthy`.

### **Step 7:** Update the smoke test in the workflow to target `/health/live`

The earlier chapters smoke-tested `/` (the home page). That worked, but `/health/live` is the better probe target now that it exists — it doesn't depend on Razor view rendering, doesn't depend on session state, and doesn't need any database round-trip. It's the cleanest "is the process up" assertion the app exposes.

The order matters: push the code change first (Step 6) so `/health/live` is reachable on the deployed revision *before* the workflow change goes in. Otherwise the very first deploy of the new workflow smoke-tests an endpoint that doesn't exist yet on the running revision.

1. **Open** the workflow file at `.github/workflows/ci.yml`.

2. **Locate** the smoke-test step that hits `/`.

3. **Replace** the smoke-test loop body with one that targets `/health/live`:

   > `.github/workflows/ci.yml`

   ```yaml
   - name: Smoke test
     run: |
       for i in {1..20}; do
         curl -fsS https://$FQDN/health/live && break || sleep 6
       done
   ```

4. **Commit and push:**

   ```bash
   git add .github/workflows/ci.yml
   git commit -m "Smoke-test /health/live instead of /"
   git push
   gh run watch
   ```

> ⚠ **Common Mistakes**
>
> - Pushing the workflow change *before* the code change. The deploy succeeds, the smoke test runs against `/health/live`, the endpoint doesn't exist yet on the live revision, and the workflow turns red. Push code first, watch deploy succeed, then update the smoke target.
> - Using `/health` (the diagnostic endpoint) as the smoke target. The 200/503 mapping for that endpoint is "did at least one check fail?" — which is the wrong signal for "is the deploy healthy enough to call green?" `/health/live` is the right target.
>
> ✓ **Quick check:** The smoke-test step in the workflow file targets `/health/live`. The next pipeline run is green, with the smoke-test step passing on the first or second iteration.

### **Step 8:** Wire Container Apps liveness and readiness probes

The endpoints exist; the platform doesn't know to consume them yet. `az containerapp update --probe-type Liveness` adds a liveness probe to the container template; the same command with `--probe-type Readiness` adds a readiness probe. Both go on port 8080 (the container's listening port) and use the paths from Step 4.

1. **Wire** the liveness probe:

   ```bash
   az containerapp update -n ca-careers-week7 -g rg-careers-week7 \
     --probe-type Liveness \
     --probe-path /health/live \
     --probe-port 8080 \
     --probe-period-seconds 10 \
     --probe-timeout-seconds 3 \
     --probe-failure-threshold 3
   ```

2. **Wire** the readiness probe:

   ```bash
   az containerapp update -n ca-careers-week7 -g rg-careers-week7 \
     --probe-type Readiness \
     --probe-path /health/ready \
     --probe-port 8080 \
     --probe-period-seconds 10 \
     --probe-timeout-seconds 3 \
     --probe-failure-threshold 3
   ```

3. **Verify** the probes are wired:

   ```bash
   az containerapp show \
     -g rg-careers-week7 -n ca-careers-week7 \
     --query 'properties.template.containers[0].probes' -o json
   ```

   The output should show two probes: one with `type: Liveness` and `httpGet.path: /health/live`, one with `type: Readiness` and `httpGet.path: /health/ready`. Both on port 8080.

> ℹ **Concept Deep Dive: Why liveness must NOT depend on external services**
>
> The temptation is to point liveness at the same endpoint as readiness — "if anything is wrong, restart the replica." That is the wrong instinct, and it's the source of the most expensive production outages.
>
> Imagine Cosmos is down. If liveness checks Cosmos, every replica fails its liveness probe, every replica gets killed, and every replacement replica fails its first liveness probe and gets killed before it can serve a single request. The platform is now in a restart loop that does nothing to bring Cosmos back AND prevents the few read-only paths that didn't need Cosmos from serving traffic at all. The blast radius is amplified — a Cosmos outage becomes an application outage, plus a thundering-herd recovery once Cosmos returns.
>
> Liveness asks "is *this process* alive?" — restart trigger. Readiness asks "should I send this replica traffic right now?" — traffic gate. Two different questions, two different probes, two different endpoints.
>
> Optional operational simulation — if you have a few extra minutes, try this. Remove the Cosmos data-plane role assignment to deliberately break readiness:
>
> ```bash
> # Find the role assignment
> SP=$(az containerapp identity show -g rg-careers-week7 -n ca-careers-week7 \
>      --query principalId -o tsv)
> COSMOS=$(az cosmosdb list -g rg-careers-week7 --query '[0].name' -o tsv)
> az cosmosdb sql role assignment list \
>   --account-name "$COSMOS" -g rg-careers-week7 -o table
> # Delete the matching assignment, then watch readiness flip:
> while true; do curl -is "https://$FQDN/health/ready" | head -1; sleep 5; done   # Ctrl+C to stop. macOS users without `watch` can keep this loop running in a side terminal.
> ```
>
> Within a probe period, `/health/ready` returns 503 and the platform stops sending the replica traffic. Liveness keeps returning 200 the whole time — the process is fine; it just can't serve. Restore the role assignment with `az cosmosdb sql role assignment create ...` and watch recovery.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `--probe-port 8080`. The default port for the probe is not the container's listening port; if you omit it, every probe lands somewhere the container isn't listening, every probe fails, and you've engineered a restart loop.
> - Wiring `/health` (the diagnostic endpoint) as a probe. The 200/503 mapping is "did at least one check fail?" — which is wrong for liveness (it shouldn't depend on Cosmos) and slightly wrong for readiness (it includes the trivial `self` check, which doesn't matter). Use the dedicated paths.
> - Setting `--probe-failure-threshold 1`. One failed probe should not be enough to kill a replica; transient blips happen. Three is a reasonable default.
>
> ✓ **Quick check:** `az containerapp show ... --query 'properties.template.containers[0].probes'` shows both a Liveness and a Readiness probe wired to the right paths and port 8080.

### **Step 9:** Tear down the cloud resources

This is the last exercise of the chapter and the last exercise that uses the Week 7 cloud resources. Nothing later in the course needs `rg-careers-week7`, the Container App, the storage account, the Cosmos account, the ACR, or the Entra app registration created for OIDC federation. Tear all of it down so you finish with no resources running and no orphaned tenant-level identities.

The work splits into two homes — the Azure subscription holds the running resources, and Microsoft Entra ID (a tenant-level service) holds the identity the pipeline used. Deleting the resource group does **not** delete the Entra app registration. They live in different planes; cleanup needs two commands.

1. **Capture** the Entra app's `appId` before you delete it:

   ```bash
   APP_ID=$(az ad app list \
     --display-name github-cloudci-careers-oidc \
     --query "[0].appId" -o tsv)

   echo "$APP_ID"
   ```

   If `$APP_ID` is empty, the app already doesn't exist — skip the `az ad app delete` step.

2. **Delete** the resource group. This removes the Container App, the storage account, the Cosmos account, the ACR with all its images, the Container Apps environment, every Container Apps secret, and every role assignment scoped under the group:

   ```bash
   az group delete -n rg-careers-week7 --yes --no-wait
   ```

   `--no-wait` returns immediately and lets the deletion run in the background. The full teardown takes 3–5 minutes.

3. **Delete** the Entra app registration. The app registration lives in the tenant, not in any subscription resource group, so the RG delete above does **not** remove it. Delete it explicitly:

   ```bash
   az ad app delete --id "$APP_ID"
   ```

4. **Verify** the resource group is gone (give it a few minutes if `--no-wait` is still working in the background):

   ```bash
   az group exists -n rg-careers-week7
   ```

   Expected: `false`.

5. **Verify** the Entra app registration is gone:

   ```bash
   az ad app list --display-name github-cloudci-careers-oidc -o tsv
   ```

   Expected: empty output (no rows).

6. **Optionally** delete the GitHub repo secrets that pointed at the now-deleted Entra app and Azure subscription. They're inert without working resources behind them — the Entra app they reference no longer exists — but cleaning them up is good hygiene so a future viewer doesn't think the pipeline is still wired:

   ```bash
   gh secret delete ACR_NAME --repo <your-gh-user>/cloudci-careers
   gh secret delete AZURE_CLIENT_ID --repo <your-gh-user>/cloudci-careers
   gh secret delete AZURE_TENANT_ID --repo <your-gh-user>/cloudci-careers
   gh secret delete AZURE_SUBSCRIPTION_ID --repo <your-gh-user>/cloudci-careers
   ```

   Replace `<your-gh-user>/cloudci-careers` with the actual owner and name of your repository.

> ℹ **Concept Deep Dive: Tenant-vs-subscription split for cleanup**
>
> Azure resources and Entra identities live on two different planes. The resource group is a subscription-level container — `az group delete` cascades to every Azure resource scoped under it (Container App, storage account, Cosmos account, ACR, Container Apps environment, all secrets, all role assignments scoped to those resources).
>
> The Entra app registration is a *tenant-level* identity object. It lives in Microsoft Entra ID, not in your Azure subscription, and survives every subscription operation. Without the second `az ad app delete`, the federated identity sticks around as orphaned tenant clutter — visible in the Entra portal with broken role assignments pointing at deleted resources, contributing nothing and confusing future you.
>
> Note that Entra ID *soft-deletes* app registrations for 30 days; they're recoverable from **App registrations → Deleted applications** in the Entra portal and garbage-collected automatically afterwards. That's a useful safety net if you delete the wrong app — you have a month to restore it.
>
> ⚠ **Common Mistakes**
>
> - Stopping after `az group delete`. The Entra app stays alive and accumulates over multiple cohorts as orphaned `github-cloudci-careers-oidc` entries.
> - Passing the display name to `az ad app delete --id`. The flag wants the `appId` GUID, not the display name. The `az ad app list --query "[0].appId"` extraction in step 1 is what makes this work.
> - Orphaned role assignments at *subscription* scope. If anything assigned a role to the Entra service principal at subscription scope (rather than at the resource-group scope), `az group delete` does not clean it up. List subscription-scoped assignments with `az role assignment list --assignee "$APP_ID" --scope /subscriptions/<sub-id>` and `az role assignment delete` any that turn up.
>
> ✓ **Quick check:** `az group exists -n rg-careers-week7` returns `false`, `az ad app list --display-name github-cloudci-careers-oidc -o tsv` returns nothing.

### **Step 10:** Test Your Implementation

Walk through the assertions one more time, in order. Run the first four against the deployed Container App *before* the cleanup substep; run the last two *after*.

1. **Liveness returns 200:**

   ```bash
   curl -i https://$FQDN/health/live
   ```

   Expected: `HTTP/2 200`, body `Healthy`.

2. **Readiness returns 200:**

   ```bash
   curl -i https://$FQDN/health/ready
   ```

   Expected: `HTTP/2 200`, body `Healthy` (Cosmos and Blob both healthy).

3. **Diagnostic endpoint returns the JSON breakdown:**

   ```bash
   curl -s https://$FQDN/health | jq .
   ```

   Expected: a JSON object with `"status": "Healthy"` and three entries — `self`, `cosmos`, `blob` — all `Healthy`.

4. **Probes are wired on the Container App:**

   ```bash
   az containerapp show -g rg-careers-week7 -n ca-careers-week7 \
     --query 'properties.template.containers[0].probes' -o json
   ```

   Expected: a Liveness probe at `/health/live` and a Readiness probe at `/health/ready`, both on port 8080.

5. **After cleanup, the resource group is gone:**

   ```bash
   az group exists -n rg-careers-week7
   ```

   Expected: `false`.

6. **After cleanup, the Entra app is gone:**

   ```bash
   az ad app list --display-name github-cloudci-careers-oidc -o tsv
   ```

   Expected: empty output.

> ✓ **Success indicators:**
>
> - `/health/live` returns 200 with body `Healthy`
> - `/health/ready` returns 200 with body `Healthy` when Cosmos and Blob are reachable
> - `/health` returns a JSON breakdown with three healthy entries
> - `az containerapp show` confirms both probes are wired to the right paths and port
> - After cleanup: `az group exists` returns `false`, `az ad app list` returns nothing
>
> ✓ **Final verification checklist:**
>
> - ☐ `Microsoft.Extensions.Diagnostics.HealthChecks` package added
> - ☐ `self`, `cosmos`, `blob` checks all registered; `cosmos` and `blob` tagged `"ready"`
> - ☐ Three endpoints mapped: `/health/live` (no checks), `/health/ready` (tag predicate), `/health` (custom JSON writer)
> - ☐ Smoke test in the workflow targets `/health/live`
> - ☐ Container Apps Liveness and Readiness probes wired to `/health/live` and `/health/ready` on port 8080
> - ☐ Resource group `rg-careers-week7` deleted
> - ☐ Entra app `github-cloudci-careers-oidc` deleted
> - ☐ (Optional) GitHub repo secrets cleaned up

## Common Issues

> **If you encounter problems:**
>
> **`/health/ready` returns 503 in CI from a fresh deploy:** Role-assignment propagation is still settling. The smoke-test loop in Step 7 retries up to 20 times with 6-second backoff, which usually covers it. If it doesn't, the role assignment is missing or scoped wrong — re-run the role-assignment commands from the previous exercise.
>
> **First deploy after the workflow change fails the smoke test:** You pushed the workflow change before the code change. `/health/live` doesn't exist on the live revision yet. Push the code first, watch the deploy go green, then update the smoke target.
>
> **`az containerapp update --probe-type` fails with "unrecognized arguments":** Some `az` versions are positional about the order of flags. Try moving `--probe-type Liveness` immediately after `-n`/`-g`, or upgrade `az` with `az upgrade`.
>
> **Every probe fails and the replica is in a restart loop:** Almost always missing `--probe-port 8080`. The default port for the probe is not the container's listening port; specify it explicitly.
>
> **`/health` returns 503 but `/health/live` and `/health/ready` return 200:** A check that's *not* tagged `"ready"` is failing. The diagnostic endpoint runs every check, including `self` — if `self` is somehow unhealthy, the aggregated status flips. Inspect the JSON body to see which entry is failing.
>
> **`az ad app delete` says "Resource not found":** You passed the display name instead of the `appId`. The `az ad app list --query "[0].appId"` extraction in Step 9 returns the GUID; pass that to `--id`.
>
> **`az group delete` returns immediately but resources still appear in the Portal:** That's `--no-wait` working as intended. Re-check with `az group exists -n rg-careers-week7` after a few minutes.
>
> **Still stuck?** Verify three things in order: the three checks are registered with the right tags (`AddCheck<CosmosHealthCheck>("cosmos", tags: new[] { "ready" })`), the three endpoint mappings have the right predicates (`_ => false` for live, `c => c.Tags.Contains("ready")` for ready), and the Container Apps probes are wired to the matching paths. All three must agree for the platform-level signals to make sense.

## Summary

You added three health-check endpoints with three distinct semantics, wired two of them as Container Apps probes, and finished the chapter with a clean teardown. `/health/live` is the always-true liveness probe — it answers "is this process alive" and is consumed by the platform's Liveness probe to decide whether to restart the replica. `/health/ready` runs the `cosmos` and `blob` checks via the `"ready"` tag predicate and is consumed by the Readiness probe to decide whether to send the replica traffic. `/health` is the diagnostic endpoint — a per-check JSON breakdown for humans, dashboards, and on-call investigation; it is **not** wired as a probe, because the platform shouldn't react to "blob check is degraded but cosmos is fine." Three endpoints, three audiences. The cleanup substep tore down the resource group and the Entra app registration in two separate commands, because they live on two different planes.

- ✓ ASP.NET Core health-checks pipeline registered with `self`, `cosmos`, and `blob` checks
- ✓ `CosmosHealthCheck` and `BlobHealthCheck` use 5-second timeouts so a hung dependency doesn't freeze the readiness endpoint
- ✓ Three endpoints mapped with distinct predicates: `_ => false` for liveness, `Tags.Contains("ready")` for readiness, custom JSON writer for diagnostics
- ✓ Container Apps Liveness and Readiness probes wired to `/health/live` and `/health/ready` on port 8080
- ✓ Smoke test in the workflow updated to target `/health/live`
- ✓ Resource group, Entra app registration, and (optionally) GitHub secrets all torn down — no orphaned cloud or tenant resources

> **Key takeaway:** Liveness asks "is *this process* alive?" and must NOT depend on external services. Readiness asks "should I send this replica traffic right now?" and gates on dependencies. Two questions; two endpoints; two probes. A diagnostic JSON endpoint exists alongside, for humans — never wire it as a probe. And cleanup needs two commands, because Azure resources and Entra identities live on different planes.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Replace the hand-rolled writer with `AspNetCore.HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse`. It's the canonical JSON writer that ships with the AspNetCore.HealthChecks NuGet ecosystem, and it produces a richer schema parsed by the `AspNetCore.HealthChecks.UI` dashboard.
> - Tune `--probe-failure-threshold` for noisier dependencies. The default of 3 is fine for in-region Cosmos and Blob; for cross-region or third-party dependencies, bump to 5 or 7 so transient blips don't flap the replica.
> - Use `--probe-initial-delay-seconds` to give slow-warming apps a grace period before the first probe runs. Useful for apps that do significant startup work — JIT warmup, connection-pool priming, schema migration on boot.
> - Read the Microsoft docs for Container Apps health probes: <https://learn.microsoft.com/en-us/azure/container-apps/health-probes>. The page covers the full set of probe knobs (`--probe-success-threshold`, TCP vs HTTPS schemes, custom HTTP headers) that aren't covered here.

## Done!

That's the chapter. Storage and resilience — durable Cosmos and Blob through managed identity, file uploads with explicit lifecycle, deep health probes that the platform consumes to make restart and traffic decisions. The cloud lab is fully torn down; you can re-run the chapter from a clean slate any time.

The next chapter shifts gears entirely — from durable persistence to async messaging and queue-driven workflows. Many of the patterns you've leaned on so far (synchronous request/response, an HTTP API as the leaf of every interaction, durable state read inline with the request) start to creak when work needs to happen out-of-band. The next chapter introduces the primitives — queues, message envelopes, idempotent handlers — that let you take long-running work off the critical path without losing reliability.
