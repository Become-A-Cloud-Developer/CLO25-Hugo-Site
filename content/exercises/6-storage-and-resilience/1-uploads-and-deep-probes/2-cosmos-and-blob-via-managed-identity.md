+++
title = "CosmosDB and Azure Blob via Managed Identity"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Replace the in-memory application store with Azure CosmosDB (serverless) and the local-file blob with Azure Blob Storage. Both authenticate via the Container App's system-assigned managed identity — no connection strings, no secretref shortcuts. Includes a Concept Deep Dive on Cosmos DB's data-plane RBAC."
weight = 2
draft = false
+++

# CosmosDB and Azure Blob via Managed Identity

## Goal

The previous exercise left you with `CloudCiCareers.Web` deployed at `https://ca-careers-week7.<env>.northeurope.azurecontainerapps.io/`, an OIDC-federated pipeline pushing to ACR on every commit, and App Insights ingesting telemetry. It also left you with two pieces of state that survive only as long as the current container does: `IApplicationStore` is registered as `InMemoryApplicationStore` (a `ConcurrentDictionary<string, Application>` in process memory), and `IBlobService` writes uploaded CVs to `./uploads/` on the local filesystem. Every revision rollover wipes both — that fragility was deliberate, and it exists to motivate this exercise.

This exercise replaces both with managed Azure services. Application records move into **CosmosDB serverless**; CV PDFs move into **Azure Blob Storage**. Crucially, neither uses a connection string or an account key — both authenticate via the Container App's **system-assigned managed identity**. There is no `secretref:` for storage credentials here because there are no storage credentials to deliver. This is a deliberate contrast with the prior chapter's API key and JWT signing key, which lived behind `secretref:`. Those were shared secrets; managed identity is an attested compute identity. The trust-boundary shift is the load-bearing lesson.

> **What you'll learn:**
>
> - How to provision CosmosDB serverless and Azure Blob Storage from the CLI and grant data-plane access without account keys
> - The difference between **Cosmos DB control-plane RBAC** (Azure RBAC roles like Cosmos DB Account Reader) and **Cosmos DB data-plane RBAC** (the GUID role definition `00000000-0000-0000-0000-000000000002`)
> - How `DefaultAzureCredential` from `Azure.Identity` picks up the managed identity in Container Apps and your `az login` token on a developer laptop, with no code changes
> - Why managed identity beats connection strings on every axis that matters — rotation, attribution, blast radius
> - When `secretref:` is still the right answer (Application Insights ingestion) and when it stops being (Cosmos and Blob)
> - How to register cloud implementations conditionally so `dotnet run` from localhost still works without any Azure resources

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ The previous exercise complete: `ca-careers-week7` deployed in `rg-careers-week7`, system-assigned managed identity already on, App Insights wired via `secretref:appinsights-connstr`
> - ✓ The CI/CD workflow still wired up — `git push` on `main` deploys a new revision via OIDC federation
> - ✓ Azure CLI signed in (`az account show` returns your subscription) with permission to create resources and assign roles in `rg-careers-week7`
> - ✓ A local clone of `CloudCiCareers.Web` and a working .NET 10 SDK (`dotnet --version` reports a `10.*` version)
> - ✓ The system-assigned managed identity's `principalId` accessible via `az containerapp show -n ca-careers-week7 -g rg-careers-week7 --query identity.principalId -o tsv`

## Exercise Steps

### Overview

1. **Provision the Storage Account and the `cvs` blob container**
2. **Provision a CosmosDB serverless account, database, and container**
3. **Grant managed identity Storage Blob Data Contributor on the Storage Account**
4. **Grant managed identity Cosmos DB data-plane access**
5. **Add the SDK packages**
6. **Refactor `IApplicationStore` to async and implement `CosmosApplicationStore`**
7. **Implement `AzureBlobService`**
8. **Wire DI in `Program.cs` with a dev/prod registration switch**
9. **Update Container App env vars (no `secretref:` — managed identity is the auth)**
10. **Push, deploy a new revision, verify persistence survives the rollover**
11. **Test Your Implementation**

### **Step 1:** Provision the Storage Account and the `cvs` blob container

Storage comes first because the role assignment in Step 3 needs the account to already exist. Pick a 5–6 character random suffix for the account name — globally unique, lowercase, alphanumeric only. The example below uses `7g8h2j`; substitute your own and **keep using the same suffix in every command**. The `--allow-blob-public-access false` flag is non-negotiable: this account holds CVs containing personal data, and a public-access default would mean a single misconfigured container could leak everything.

1. **Create** the Storage Account in `rg-careers-week7`:

   ```bash
   az storage account create -n stcareers7g8h2j -g rg-careers-week7 \
     -l northeurope --sku Standard_LRS --allow-blob-public-access false
   ```

2. **Create** the `cvs` container, authenticating as your signed-in user (not via account key):

   ```bash
   az storage container create --account-name stcareers7g8h2j --name cvs --auth-mode login
   ```

> ℹ **Concept Deep Dive: `--auth-mode login`**
>
> By default `az storage` reaches for an account key — a string that grants full plane-wide access to every container, blob, queue, and table in the account. That key is exactly what this chapter is moving away from. `--auth-mode login` tells the CLI to authenticate as your signed-in Azure AD user instead, using the role assignments your subscription account already has. No account key is read, none is cached. The flag works on every `az storage` subcommand and is the right default for any chapter taking the no-shared-secrets posture seriously.
>
> ⚠ **Common Mistakes**
>
> - Omitting `--auth-mode login` and silently falling back to account-key lookup — works locally but trains you for the wrong pattern.
> - Picking a suffix with uppercase letters or hyphens. Storage account names allow only `a-z0-9` and must be 3–24 characters.
>
> ✓ **Quick check:** `az storage container list --account-name stcareers7g8h2j --auth-mode login -o table` lists exactly one container named `cvs`.

### **Step 2:** Provision a CosmosDB serverless account, database, and container

CosmosDB has two billing modes: **provisioned throughput** (pre-purchase RU/s, pay whether you use them or not) and **serverless** (per-request billing, no minimum). Provisioned's 400 RU/s floor runs roughly 24 USD per month even idle; serverless runs cents per chapter. Use serverless. It also sidesteps the free-tier-already-claimed question — every subscription gets one free-tier Cosmos account, and if you've used it elsewhere, provisioned stops being free. Serverless doesn't depend on the free-tier slot.

1. **Create** the CosmosDB account in serverless mode (same suffix as Step 1):

   ```bash
   az cosmosdb create -n cosmos-careers-7g8h2j -g rg-careers-week7 \
     --capabilities EnableServerless --default-consistency-level Session
   ```

   This takes 4–6 minutes. Cosmos accounts are slow to provision because the control plane spins up regional infrastructure even for a single region.

2. **Create** the `careers` database:

   ```bash
   az cosmosdb sql database create -a cosmos-careers-7g8h2j -g rg-careers-week7 -n careers
   ```

3. **Create** the `applications` container, partitioned on `/id`:

   ```bash
   az cosmosdb sql container create -a cosmos-careers-7g8h2j -g rg-careers-week7 \
     -d careers -n applications --partition-key-path /id
   ```

> ℹ **Concept Deep Dive: Why serverless**
>
> Serverless charges per request unit consumed, not per provisioned RU/s. For an app that handles a handful of reads and writes per minute the total runs in cents per chapter, predictable and bounded. No RU/s tuning step, no autoscale curve, no idle cost. When the chapter ends and you delete `rg-careers-week7`, the resource group teardown deletes the account too — no "did I forget to scale this back down?" trap. Serverless trades off advanced features (no multi-region writes, no analytical store) that don't matter for a single-tenant teaching app.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `--capabilities EnableServerless`. The default is provisioned throughput, which silently costs 24 USD per month per database.
> - Using a different suffix than the storage account. The rest of the chapter assumes both names share one suffix.
>
> ✓ **Quick check:** `az cosmosdb sql container show -a cosmos-careers-7g8h2j -g rg-careers-week7 -d careers -n applications --query 'resource.partitionKey.paths' -o tsv` prints `/id`.

### **Step 3:** Grant managed identity Storage Blob Data Contributor on the Storage Account

Storage uses **regular Azure RBAC**. The role is `Storage Blob Data Contributor` — data-plane only (read, write, delete blobs), not control-plane (no key rotation, no encryption-setting changes). Scope it to the Storage Account, not the subscription — the Container App's identity should not have access to *every* storage account in your subscription.

1. **Capture** the Container App's managed identity `principalId` and the Storage Account resource ID:

   ```bash
   PRINCIPAL_ID=$(az containerapp show -n ca-careers-week7 -g rg-careers-week7 --query identity.principalId -o tsv)
   STORAGE_ID=$(az storage account show -n stcareers7g8h2j -g rg-careers-week7 --query id -o tsv)
   echo "PRINCIPAL_ID=$PRINCIPAL_ID  STORAGE_ID=$STORAGE_ID"
   ```

   Both should be non-empty. `PRINCIPAL_ID` is a GUID; `STORAGE_ID` is an ARM resource path.

2. **Assign** the role at the Storage Account scope:

   ```bash
   az role assignment create \
     --assignee "$PRINCIPAL_ID" \
     --role "Storage Blob Data Contributor" \
     --scope "$STORAGE_ID"
   ```

> ℹ **Concept Deep Dive**
>
> "Data Contributor" vs "Account Contributor" is the load-bearing distinction. **Account Contributor** is control-plane: rename, rotate keys, change firewall rules. **Data Contributor** is data-plane: actually read and write blobs. The Container App needs only data-plane access; granting Account Contributor would mean a compromised app could rotate keys or delete the account. Always grant the narrowest data-plane role that does the job.
>
> ⚠ **Common Mistakes**
>
> - Granting at subscription scope. Works, but the identity now has blob access to every storage account in your subscription — far more blast radius than needed.
> - Granting `Storage Account Contributor` because the name sounds adjacent. That's control-plane and won't grant blob read/write at all.
>
> ✓ **Quick check:** `az role assignment list --assignee "$PRINCIPAL_ID" --scope "$STORAGE_ID" -o table` lists exactly one row with role `Storage Blob Data Contributor`.

### **Step 4:** Grant managed identity Cosmos DB data-plane access

Cosmos DB has its own RBAC system parallel to Azure RBAC, and the day you discover this is usually the day you've already lost two hours to 403s. The role definition ID `00000000-0000-0000-0000-000000000002` is the built-in **Cosmos DB Built-in Data Contributor** — read, write, delete documents within the account. It is *not* discoverable via `az role definition list`; it lives in a separate registry queried via `az cosmosdb sql role definition list`.

1. **Assign** the data-plane role at the account scope:

   ```bash
   az cosmosdb sql role assignment create \
     --account-name cosmos-careers-7g8h2j -g rg-careers-week7 \
     --scope "/" --principal-id "$PRINCIPAL_ID" \
     --role-definition-id 00000000-0000-0000-0000-000000000002
   ```

2. **List** the data-plane role definitions for orientation:

   ```bash
   az cosmosdb sql role definition list --account-name cosmos-careers-7g8h2j -g rg-careers-week7 -o table
   ```

   Expected: two built-in entries — `Cosmos DB Built-in Data Reader` (`...0001`) and `Cosmos DB Built-in Data Contributor` (`...0002`).

> ℹ **Concept Deep Dive: Cosmos data-plane RBAC vs control-plane RBAC**
>
> Why two RBAC systems? Because the operations split cleanly. **Control-plane operations** go through regular Azure RBAC: rename the database, increase RU/s, change consistency level, enable multi-region writes. Roles like `Cosmos DB Account Reader Role` and `DocumentDB Account Contributor` cover those. **Data-plane operations** — read, write, query, delete documents — go through the Cosmos-specific registry with GUID role definition IDs you'll never recognise on first sight. The split exists because data-plane permissions are enforced by the data-plane endpoints, not by ARM, and at a per-document granularity ARM doesn't model. The scope `/` here means "everything in this account"; tighter scopes are possible but rarely worth the complexity for a single-tenant app. The role-definition ID is mechanically the same shape as an Azure RBAC ID, but **lives in a different registry** — that's why `az role assignment list` won't show it. Use `az cosmosdb sql role assignment list` to see Cosmos data-plane assignments.
>
> ⚠ **Common Mistakes**
>
> - Granting `Cosmos DB Account Reader Role` (control-plane) and expecting reads to work. They won't — that role lets you `az cosmosdb show`, not `ReadItemAsync`. Symptom: 403 on every data-plane SDK call.
> - Forgetting `--scope "/"`. The scope is required, and `/` is the right value for "the whole account".
> - Looking for the role in `az role definition list` and concluding it doesn't exist. Wrong registry.
>
> ✓ **Quick check:** `az cosmosdb sql role assignment list --account-name cosmos-careers-7g8h2j -g rg-careers-week7 -o table` lists one row with `RoleDefinitionId` ending in `...0002`.

### **Step 5:** Add the data-plane SDK packages

Four NuGet packages: the Cosmos SDK, the Blob SDK, `Azure.Identity` for `DefaultAzureCredential`, and an explicit `Newtonsoft.Json` reference. The first three are obvious; the fourth needs a one-paragraph explanation. `Microsoft.Azure.Cosmos` 3.x uses **Newtonsoft.Json internally** for document (de)serialisation — not `System.Text.Json`. The Cosmos targets file fails the build with `The Newtonsoft.Json package must be explicitly referenced with version >= 10.0.2` if you don't add it yourself, even though it's already a transitive dependency. Pinning it removes that surprise. `Azure.Identity` is the load-bearing one — it provides the credential type that picks up the managed identity in Container Apps and your `az login` token on a laptop, with no code changes.

1. **From the project root** of `CloudCiCareers.Web`:

   ```bash
   dotnet add package Microsoft.Azure.Cosmos --version 3.49.0
   dotnet add package Azure.Storage.Blobs --version 12.24.0
   dotnet add package Azure.Identity --version 1.13.1
   dotnet add package Newtonsoft.Json --version 13.0.3
   ```

2. **Verify** with `dotnet list package` — all four should be present at the versions above.

> ℹ **Concept Deep Dive: `DefaultAzureCredential`**
>
> A chain-of-responsibility credential that tries auth sources in order and returns the first that works. The chain is roughly: env vars → workload identity → managed identity → Visual Studio → Azure CLI → interactive browser. In Container Apps the managed identity step succeeds; on a laptop with `az login` the CLI step succeeds. **The same code works in both environments without changes** — that's the value proposition.
>
> ⚠ **Common Mistakes**
>
> - Using `DefaultAzureCredential` from a laptop without `az login`. The chain falls through to interactive browser auth.
> - Mixing `Azure.Storage.Blobs` (data-plane SDK) with `Microsoft.Azure.Storage` (legacy SDK). Use only `Azure.Storage.Blobs`.
>
> ✓ **Quick check:** `dotnet build` succeeds with no missing-package errors.

### **Step 6:** Refactor `IApplicationStore` to async and implement `CosmosApplicationStore`

The existing `IApplicationStore` has synchronous methods. The Cosmos SDK is async-only, so the cleanest path is to refactor the interface to async, update `InMemoryApplicationStore` to wrap each body in `Task.FromResult`, and update controllers to `await`. This is more code change than blocking with `.GetAwaiter().GetResult()` would be, but it's the right ergonomic fit — async is what real Cosmos code looks like. The `Application` domain itself is unchanged — same fields, same `ApplicationStatus` enum, same `string` Id (the previous exercise generated it as `Guid.NewGuid().ToString()`). All you add is two `Newtonsoft.Json` annotations — Cosmos 3.x uses Newtonsoft internally — so Cosmos can find the partition-key value and so the status is stored as a readable string instead of an integer.

1. **Open** `Models/Application.cs` and add two attributes — `[JsonProperty("id")]` on `Id` (Cosmos requires the partition-key property to serialise to lowercase `id`), and `[JsonConverter(typeof(StringEnumConverter))]` on `Status` so the enum lands in Cosmos as `"Submitted"` instead of `0`. Keep every other field exactly as it was:

   > `Models/Application.cs`

   ```csharp
   using Newtonsoft.Json;
   using Newtonsoft.Json.Converters;

   namespace CloudCiCareers.Web.Models;

   public class Application
   {
       [JsonProperty("id")]
       public string Id { get; set; } = string.Empty;
       public int JobId { get; set; }
       public string ApplicantName { get; set; } = string.Empty;
       public string ApplicantEmail { get; set; } = string.Empty;
       public string CvBlobName { get; set; } = string.Empty;
       public DateTimeOffset SubmittedAt { get; set; }
       [JsonConverter(typeof(StringEnumConverter))]
       public ApplicationStatus Status { get; set; }
       public string? Notes { get; set; }
   }

   public enum ApplicationStatus
   {
       Submitted, UnderReview, Rejected, Hired
   }
   ```

2. **Refactor** `Services/IApplicationStore.cs` to async signatures. Same five methods, same parameters, same `string` Id, same `ApplicationStatus` enum, same nullable `Notes` — only the return shapes change to `Task<…>`:

   > `Services/IApplicationStore.cs`

   ```csharp
   using CloudCiCareers.Web.Models;

   namespace CloudCiCareers.Web.Services;

   public interface IApplicationStore
   {
       Task<IEnumerable<Application>> GetAllAsync(CancellationToken ct = default);
       Task<Application?> GetByIdAsync(string id, CancellationToken ct = default);
       Task<Application> CreateAsync(Application application, CancellationToken ct = default);
       Task<bool> UpdateStatusAsync(string id, ApplicationStatus newStatus, string? notes, CancellationToken ct = default);
       Task<bool> DeleteAsync(string id, CancellationToken ct = default);
   }
   ```

3. **Update** `Services/InMemoryApplicationStore.cs` so each method wraps the existing logic in `Task.FromResult` — kept as the dev fallback:

   > `Services/InMemoryApplicationStore.cs`

   ```csharp
   using CloudCiCareers.Web.Models;
   using System.Collections.Concurrent;

   namespace CloudCiCareers.Web.Services;

   public class InMemoryApplicationStore : IApplicationStore
   {
       private readonly ConcurrentDictionary<string, Application> _store = new();

       public Task<IEnumerable<Application>> GetAllAsync(CancellationToken ct = default)
           => Task.FromResult<IEnumerable<Application>>(_store.Values.ToList());

       public Task<Application?> GetByIdAsync(string id, CancellationToken ct = default)
           => Task.FromResult(_store.TryGetValue(id, out var a) ? a : null);

       public Task<Application> CreateAsync(Application application, CancellationToken ct = default)
       {
           _store[application.Id] = application;
           return Task.FromResult(application);
       }

       public Task<bool> UpdateStatusAsync(string id, ApplicationStatus newStatus, string? notes, CancellationToken ct = default)
       {
           if (!_store.TryGetValue(id, out var existing)) return Task.FromResult(false);
           existing.Status = newStatus;
           existing.Notes = notes;
           return Task.FromResult(true);
       }

       public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
           => Task.FromResult(_store.TryRemove(id, out _));
   }
   ```

4. **Create** the production implementation. The `Id` property is a `string` (matching the partition-key path `/id`), so the Cosmos calls take the string directly — no `.ToString()` on a Guid:

   > `Services/CosmosApplicationStore.cs`

   ```csharp
   using CloudCiCareers.Web.Models;
   using Microsoft.Azure.Cosmos;
   using System.Net;

   namespace CloudCiCareers.Web.Services;

   public class CosmosApplicationStore : IApplicationStore
   {
       private readonly Container _container;

       public CosmosApplicationStore(CosmosClient client, IConfiguration config)
       {
           var db = config["Cosmos:Database"] ?? throw new InvalidOperationException("Cosmos:Database");
           var name = config["Cosmos:Container"] ?? throw new InvalidOperationException("Cosmos:Container");
           _container = client.GetContainer(db, name);
       }

       public async Task<IEnumerable<Application>> GetAllAsync(CancellationToken ct = default)
       {
           var query = _container.GetItemQueryIterator<Application>(new QueryDefinition("SELECT * FROM c"));
           var results = new List<Application>();
           while (query.HasMoreResults)
               foreach (var item in await query.ReadNextAsync(ct)) results.Add(item);
           return results;
       }

       public async Task<Application?> GetByIdAsync(string id, CancellationToken ct = default)
       {
           try
           {
               var r = await _container.ReadItemAsync<Application>(
                   id, new PartitionKey(id), cancellationToken: ct);
               return r.Resource;
           }
           catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { return null; }
       }

       public async Task<Application> CreateAsync(Application application, CancellationToken ct = default)
       {
           var r = await _container.CreateItemAsync(application, new PartitionKey(application.Id), cancellationToken: ct);
           return r.Resource;
       }

       public async Task<bool> UpdateStatusAsync(string id, ApplicationStatus newStatus, string? notes, CancellationToken ct = default)
       {
           try
           {
               await _container.PatchItemAsync<Application>(
                   id, new PartitionKey(id),
                   patchOperations: new[]
                   {
                       PatchOperation.Replace("/Status", newStatus.ToString()),
                       PatchOperation.Replace("/Notes", notes)
                   },
                   cancellationToken: ct);
               return true;
           }
           catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { return false; }
       }

       public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
       {
           try
           {
               await _container.DeleteItemAsync<Application>(id, new PartitionKey(id), cancellationToken: ct);
               return true;
           }
           catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) { return false; }
       }
   }
   ```

5. **Update** the two controllers so every call site of the old synchronous store methods becomes `await` of the async equivalent. The action methods that touch the store change from `IActionResult` to `async Task<IActionResult>`, and each call gets renamed and `await`'d (`_store.GetAll()` → `await _store.GetAllAsync()`, `_store.GetById(id)` → `await _store.GetByIdAsync(id)`, `_store.Create(application)` → `await _store.CreateAsync(application)`, `_store.UpdateStatus(id, newStatus, notes)` → `await _store.UpdateStatusAsync(id, newStatus, notes)`, `_store.Delete(id)` → `await _store.DeleteAsync(id)`). The compiler guides you — every old call site produces a build error pointing at exactly the line that needs updating. Touch `Controllers/JobsController.cs` and `Controllers/ApplicationsController.cs`.

> ℹ **Concept Deep Dive: `PatchItemAsync` over `ReplaceItemAsync`**
>
> `ReplaceItemAsync` requires reading the document, mutating the field, and writing the whole thing back — extra latency and a potential lost-update on races. `PatchItemAsync` is a single round trip and changes only the named properties. For a status + notes update that's the obvious win. Trade-off: patch operations are limited to a fixed set (replace, add, remove, increment) and can't do conditional updates without ETags.
>
> The patch paths above use **PascalCase** (`/Status`, `/Notes`) because the model has no `[JsonProperty]` on those properties — Newtonsoft.Json defaults to PascalCase serialisation, so the document keys are `Status` and `Notes`. Only `Id` is renamed to lowercase `id` (because Cosmos requires that for the partition key). Patch paths are case-sensitive; mismatching them silently no-ops.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `[JsonProperty("id")]` on `Id`. Cosmos rejects writes with `Required property 'id' is missing`.
> - Forgetting `[JsonConverter(typeof(StringEnumConverter))]` on `Status`. The status lands as `0`/`1`/`2`/`3` in Cosmos, the patch path then mismatches the type, and `az cosmosdb sql query` results become unreadable.
> - Reaching for `System.Text.Json`'s `[JsonPropertyName]` instead of Newtonsoft's `[JsonProperty]`. Cosmos 3.x ignores STJ attributes — they apply to ASP.NET Core's HTTP serialisation but not to what Cosmos writes to disk.
> - Using `new PartitionKey(applicationId)` with the wrong casing on the path. The partition-key path is `/id` (lowercase, set when you created the container in Step 2); the JSON property is also `id` because of `JsonPropertyName`. All consistent — keep them that way.
>
> ✓ **Quick check:** `dotnet build` succeeds. Every controller action that uses `_store` is `async Task<IActionResult>` and every store call has `await` in front of it.

### **Step 7:** Implement `AzureBlobService`

The blob side mirrors the Cosmos side. The `IBlobService` interface keeps its existing async signatures. The new implementation takes a `BlobServiceClient` from DI, reads the container name from config, and forwards calls to `BlobContainerClient`. Same `DefaultAzureCredential` story — every call rides on the managed identity.

1. **Create** the production implementation:

   > `Services/AzureBlobService.cs`

   ```csharp
   using Azure.Storage.Blobs;
   using Azure.Storage.Blobs.Models;

   namespace CloudCiCareers.Web.Services;

   public class AzureBlobService : IBlobService
   {
       private readonly BlobContainerClient _container;

       public AzureBlobService(BlobServiceClient client, IConfiguration config)
       {
           var name = config["Storage:Container"]
               ?? throw new InvalidOperationException("Storage:Container not configured");
           _container = client.GetBlobContainerClient(name);
       }

       public Task UploadAsync(string name, Stream content, CancellationToken ct = default)
           => _container.GetBlobClient(name).UploadAsync(
               content,
               new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "application/pdf" } },
               cancellationToken: ct);

       public async Task<Stream> OpenReadAsync(string name, CancellationToken ct = default)
           => await _container.GetBlobClient(name).OpenReadAsync(cancellationToken: ct);

       public Task DeleteAsync(string name, CancellationToken ct = default)
           => _container.GetBlobClient(name).DeleteIfExistsAsync(cancellationToken: ct);
   }
   ```

> ℹ **Concept Deep Dive: setting `ContentType` on upload**
>
> Without `ContentType = "application/pdf"` the blob defaults to `application/octet-stream`, and browsers force a download instead of rendering the PDF inline. Setting it at upload time means the metadata is correct from the start; no rewrite later.
>
> ⚠ **Common Mistakes**
>
> - Calling `UploadAsync(stream, overwrite: true)`. GUID-based naming makes collisions improbable but catastrophic if they happen — keep the default `overwrite: false`.
>
> ✓ **Quick check:** `dotnet build` succeeds.

### **Step 8:** Wire DI in `Program.cs` with a dev/prod registration switch

You now have two implementations of each interface. The registration logic picks between them based on whether `Cosmos:Endpoint` is configured: present → cloud implementations; absent → in-memory and local-file fallbacks. This keeps `dotnet run` working locally without any Azure resources, while the deployed Container App always picks the cloud path because its env vars are always set.

1. **Open** `Program.cs`.

2. **Add** the usings at the top:

   > `Program.cs`

   ```csharp
   using Azure.Identity;
   using Azure.Storage.Blobs;
   using CloudCiCareers.Web.Services;
   using Microsoft.Azure.Cosmos;
   ```

3. **Replace** the existing single-line registrations of `IApplicationStore` and `IBlobService` with the conditional block:

   > `Program.cs`

   ```csharp
   var cosmosEndpoint = builder.Configuration["Cosmos:Endpoint"];
   var blobEndpoint = builder.Configuration["Storage:BlobEndpoint"];

   if (!string.IsNullOrWhiteSpace(cosmosEndpoint) && !string.IsNullOrWhiteSpace(blobEndpoint))
   {
       // Production / cloud path: managed identity authenticates both clients.
       builder.Services.AddSingleton(_ =>
           new CosmosClient(cosmosEndpoint, new DefaultAzureCredential()));
       builder.Services.AddSingleton(_ =>
           new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential()));

       builder.Services.AddSingleton<IApplicationStore, CosmosApplicationStore>();
       builder.Services.AddSingleton<IBlobService, AzureBlobService>();
   }
   else
   {
       // Dev fallback: no Azure resources required for `dotnet run`.
       builder.Services.AddSingleton<IApplicationStore, InMemoryApplicationStore>();
       builder.Services.AddSingleton<IBlobService, LocalFileBlobService>();
   }
   ```

> ℹ **Concept Deep Dive: convention, not feature flags**
>
> This is deliberately the simplest possible "if cloud config is present, use cloud impls" switch — five lines of `if/else`, no library, no abstraction. Production-grade systems would use `IOptions<CloudStorageOptions>` with `[Required]` validation, fail-fast at startup if half the cloud config is provided, and centralise the decision in a typed extension method. The rough shape is here so the lesson is the registration logic, not a feature-flag library like `Microsoft.FeatureManagement`.
>
> ⚠ **Common Mistakes**
>
> - Using `IsDevelopment()` as the switch instead of "is the cloud config present?". A locally-run app pointed at real Azure resources would then refuse to use them. Configure based on configuration, not on environment name.
> - Registering both pairs unconditionally. You'd construct a `CosmosClient` that opens TCP connections at startup and fails if the endpoint is missing.
>
> ✓ **Quick check:** `dotnet build` succeeds. `dotnet run` from localhost (with no `Cosmos:Endpoint` set) starts and serves `/Applications` from the in-memory store.

### **Step 9:** Update Container App env vars

Five env vars carry the cloud configuration, **none** of them a secret — managed identity replaces every connection string in this design. The Cosmos endpoint URL, the blob endpoint, the database, container, and account names are all public information. Compare with the previous chapter's API key and JWT signing key, both of which lived behind `secretref:` because they were shared secrets.

1. **Set** all five env vars in one `az containerapp update` call:

   ```bash
   az containerapp update -n ca-careers-week7 -g rg-careers-week7 \
     --set-env-vars \
       Cosmos__Endpoint=https://cosmos-careers-7g8h2j.documents.azure.com:443/ \
       Cosmos__Database=careers Cosmos__Container=applications \
       Storage__BlobEndpoint=https://stcareers7g8h2j.blob.core.windows.net \
       Storage__Container=cvs
   ```

2. **Verify** the env vars are set with `value` (not `secretRef`):

   ```bash
   az containerapp show -n ca-careers-week7 -g rg-careers-week7 \
     --query 'properties.template.containers[0].env' -o json
   ```

   Expected: five new entries with `name` + `value` keys (the cloud config), plus the existing `secretRef: appinsights-connstr` entry.

> ℹ **Concept Deep Dive: Why managed identity beats connection strings**
>
> Three axes, all in managed identity's favour. **Rotation**: a connection string is "fresh" only when you rotate it; a managed identity token is reissued every hour automatically by the platform. **Attribution**: a connection string in a log line tells you "someone with this string did this"; a managed identity entry in Azure Activity Log tells you exactly which Container App revision did it. **Blast radius**: a connection string in `.env` or a Container Apps secret is a string that, if exfiltrated, lets an attacker call the storage account from anywhere; a managed identity token can only be obtained by the compute identity it's bound to.
>
> The exception is App Insights — the app-side telemetry SDK doesn't yet support managed identity on the ingestion path, so the connection string stays as a `secretref:`. Pick the right tool per resource: managed identity wherever the SDK supports it, `secretref:` for the cases that still need a shared secret.
>
> ⚠ **Common Mistakes**
>
> - Falling back to `Storage__ConnectionString=secretref:storage-connstr` "because it's easier". It is — for two minutes. Then you have a key to rotate, audit, and worry about leaking.
> - Putting `Cosmos__AccountKey` anywhere. There is no account key in this design.
>
> ✓ **Quick check:** None of the five new env vars use `secretRef`. The only `secretRef` in the env list is `APPLICATIONINSIGHTS_CONNECTION_STRING`.

### **Step 10:** Push, deploy, and verify persistence survives a revision rollover

Push the code; the CI/CD workflow rebuilds, pushes to ACR, and updates the Container App. The new revision picks up the env vars from Step 9 and the role assignments from Steps 3 and 4. Once `Active`, submit a fresh application; then deliberately roll over and confirm it's still there.

1. **Commit and push:**

   ```bash
   git add Models/Application.cs Services/ Controllers/ Program.cs CloudCiCareers.Web.csproj
   git commit -m "Move application store to Cosmos and CV blobs to Azure Blob Storage"
   git push
   gh run watch
   ```

   Expected: green workflow within a couple of minutes.

2. **Capture** the FQDN:

   ```bash
   FQDN=$(az containerapp show -n ca-careers-week7 -g rg-careers-week7 \
     --query properties.configuration.ingress.fqdn -o tsv)
   echo "$FQDN"
   ```

3. **Submit** a fresh application in the browser. Open `https://$FQDN/`, click any of the four jobs, fill in name + email, attach a small PDF, submit. After the redirect, you land on `https://$FQDN/Applications/Details/<id>` — copy that `<id>` for the next step.

4. **Force** a new revision rollover. Bumping a harmless env var is enough — Container Apps treats any template change as a new revision:

   ```bash
   az containerapp update -n ca-careers-week7 -g rg-careers-week7 --set-env-vars BUMP=$(date +%s)
   az containerapp revision list -n ca-careers-week7 -g rg-careers-week7 -o table
   ```

5. **Reload** `https://$FQDN/Applications/<id>` from Step 3. The application is still there — the Cosmos document and the blob both survived the rollover. This is the moment the chapter's investment pays off.

6. **Verify** the data directly in Azure:

   ```bash
   az cosmosdb sql query --account-name cosmos-careers-7g8h2j \
     --database-name careers --container-name applications --query-text "SELECT * FROM c"

   az storage blob list --account-name stcareers7g8h2j --container-name cvs --auth-mode login -o table
   ```

> ✓ **Quick check:** The application submitted before the rollover survives the rollover. The Cosmos query and blob list both show the record.

### **Step 11:** Test Your Implementation

Walk every signal end to end so you finish knowing both stores work, the managed identity is the auth path, and App Insights sees the dependencies.

1. **Submit** a fresh application from `https://$FQDN/` (pick any job, fill the apply form) with a recognisable name like `Test Sigrid Larsson`.

2. **Force** a revision rollover (`az containerapp update -n ca-careers-week7 -g rg-careers-week7 --set-env-vars BUMP=$(date +%s)`) and confirm the application is still listed at `https://$FQDN/Applications`.

3. **Verify** Cosmos has the document:

   ```bash
   az cosmosdb sql query \
     --account-name cosmos-careers-7g8h2j \
     --database-name careers --container-name applications \
     --query-text "SELECT * FROM c WHERE c.candidateName = 'Test Sigrid Larsson'"
   ```

4. **Verify** the blob exists:

   ```bash
   az storage blob list --account-name stcareers7g8h2j --container-name cvs --auth-mode login -o table
   ```

   Expected: a `<guid>.pdf` whose name matches `cvBlobName` from Step 3.

5. **Open** the detail page `https://$FQDN/Applications/<id>` and click the CV link. The PDF renders inline (because `ContentType` was set at upload). The link is proxied through `IBlobService.OpenReadAsync`, not a direct blob URL — that's how the managed-identity path stays in force.

6. **Check** App Insights sees the dependency calls:

   ```bash
   az monitor app-insights query \
     --app cloudci-careers-insights -g rg-careers-week7 \
     --analytics-query "dependencies | where timestamp > ago(15m) | project timestamp, type, name, success | take 20"
   ```

   Expected: rows with `type = "Azure DocumentDB"` (Cosmos) and `type = "Azure blob"` (Storage), all `success = true`.

> ✓ **Success indicators:**
>
> - The browser flow (submit → list → detail → CV inline) works against the deployed FQDN
> - A revision rollover does not lose the data
> - Cosmos and Blob both contain the records you submitted
> - App Insights shows successful `Azure DocumentDB` and `Azure blob` dependency entries
> - Nothing in `az containerapp show ... --query 'properties.template.containers[0].env'` carries a `secretRef` for storage or Cosmos
>
> ✓ **Final verification checklist:**
>
> - ☐ Storage Account `stcareers<rand>` and container `cvs` exist
> - ☐ CosmosDB serverless account, `careers` database, `applications` container with `/id` partition key all exist
> - ☐ `Storage Blob Data Contributor` role is assigned to the Container App's principal at the Storage Account scope
> - ☐ `Cosmos DB Built-in Data Contributor` (`...0002`) is assigned to the same principal at account scope
> - ☐ `Models/Application.cs` carries `[JsonPropertyName("id")]` on `Id`
> - ☐ `IApplicationStore` is async; `InMemoryApplicationStore` and `CosmosApplicationStore` both implement it
> - ☐ `Program.cs` switches on `Cosmos:Endpoint` to pick cloud vs in-memory implementations
> - ☐ Five env vars on the Container App carry the cloud config; none uses `secretRef`
> - ☐ A new revision rollover does not wipe applications or CVs
> - ☐ The CV link on the detail page renders the PDF inline

## Common Issues

> **If you encounter problems:**
>
> **403 on the very first request after role assignment.** Azure RBAC and Cosmos data-plane RBAC both have propagation lag of 5–30 seconds; retry. If 403s persist beyond a minute, re-run `az role assignment list` and `az cosmosdb sql role assignment list`.
>
> **`CosmosException: Unauthorized` mentioning "Request blocked by Auth".** Wrong Cosmos data-plane role definition ID. The right value is `00000000-0000-0000-0000-000000000002`. Regular Azure RBAC roles like `DocumentDB Account Contributor` don't apply to data-plane calls.
>
> **Reads return zero results but writes succeed.** Partition key path is `/Id` (capital) but the JSON property is `id` (lowercase), or vice versa. Confirm `[JsonPropertyName("id")]` and `--partition-key-path /id`.
>
> **`Required property 'id' is missing` on writes.** `[JsonPropertyName("id")]` is missing from `Models/Application.cs`.
>
> **Local `dotnet run` hangs at startup.** `DefaultAzureCredential` is trying every source and one is timing out. Confirm `az login` works and `az account show` returns the right subscription. If on multiple subscriptions, `az account set --subscription <id>` to disambiguate.
>
> **The CV link downloads instead of rendering inline.** Blob `ContentType` wasn't set. Re-upload with `BlobHttpHeaders { ContentType = "application/pdf" }`.
>
> **Still stuck?** Check in order: are the role assignments listed (Steps 3 and 4)? Is `[JsonPropertyName("id")]` on the `Id` property? Are the env vars set with `value`, not `secretRef`?

## Summary

You replaced the in-memory application store with **CosmosDB serverless** and the local-file blob store with **Azure Blob Storage**. Both authenticate via the Container App's system-assigned managed identity, with **regular Azure RBAC** for Storage (`Storage Blob Data Contributor`) and **Cosmos data-plane RBAC** (`Cosmos DB Built-in Data Contributor`, role definition `...0002`) for Cosmos. No connection strings, no `secretref:`, no shared keys. The DI registration switches on whether the cloud config is present, so `dotnet run` still works on a developer laptop without any Azure resources.

- ✓ CosmosDB serverless + database + container with `/id` partition key
- ✓ Azure Blob Storage with `--allow-blob-public-access false` and a `cvs` container
- ✓ Two role assignments: regular Azure RBAC for Storage, Cosmos-specific RBAC for Cosmos
- ✓ `DefaultAzureCredential` from `Azure.Identity` — picks managed identity in Azure, `az login` token locally, no code changes
- ✓ Async `IApplicationStore` interface with both `InMemoryApplicationStore` (dev) and `CosmosApplicationStore` (prod)
- ✓ Conditional DI registration in `Program.cs` based on `Cosmos:Endpoint` presence
- ✓ Five non-secret env vars carrying the cloud configuration; App Insights `secretref:` stays where it is

> **Key takeaway:** Managed identity is the right answer wherever the Azure service supports it. The trust boundary moves from "anyone holding this string" to "this specific compute identity, attested by Azure", and rotation, attribution, and blast radius all improve. The remaining `secretref:` for App Insights is the exception that proves the rule — use the platform-attested identity wherever you can; reserve shared secrets for the resources whose SDKs don't yet support the better path.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - **Cross-region writes**: enable Cosmos multi-region writes for a globally-distributed setup. Serverless doesn't support it; switch to provisioned throughput first.
> - **Private endpoints**: put the Storage Account and Cosmos account behind private endpoints in a VNet so the public endpoint goes away entirely. Container Apps environments can be deployed into a VNet to reach them.
> - **Customer-managed keys (CMK)**: replace the platform-managed encryption keys on the Storage Account with keys you hold in Key Vault, with the Container App's identity granted `wrap`/`unwrap` permissions on the key.
> - **Azure Cosmos DB analytical store + Synapse Link**: enable the column-store sibling of the transactional store and query application records from a Synapse workspace without affecting the OLTP path. Note: not available in serverless mode.

## Done!

The application's state survives the box it runs on. CosmosDB holds the records, Azure Blob Storage holds the CVs, and both are reachable only via the Container App's managed identity — no connection strings to leak, no keys to rotate manually, no `.env` file to forget. Revision rollovers, scale-to-zero recoveries, full redeploys: all lose nothing.

In the next exercise we add deep health probes that distinguish liveness (am I running?) from readiness (am I able to serve real traffic?), wire them as Container Apps probes so the platform stops routing to revisions that have lost their dependencies, and end the chapter with a cleanup substep that tears down the resource group cleanly.
