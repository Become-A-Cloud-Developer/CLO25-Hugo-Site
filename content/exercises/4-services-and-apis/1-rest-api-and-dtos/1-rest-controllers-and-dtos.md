+++
title = "REST Controllers, DTOs, and Swagger"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Scaffold a controllers-based ASP.NET Core Web API with separate DTO and entity types, wire up Swagger, then deploy it to Azure Container Apps using the same OIDC-federated pipeline pattern from the deployment chapter."
weight = 1
draft = false
+++

# REST Controllers, DTOs, and Swagger

## Goal

Stand up a fresh ASP.NET Core Web API called `CloudCiApi` and ship it to Azure Container Apps. The surface is intentionally small — a `Quotes` resource backed by an in-memory store — so the focus stays on the patterns that scale: a controllers-based project layout, a deliberate split between domain entities and wire-format DTOs, and Swagger as the runtime contract document. Once it builds locally, you'll wrap it in a Dockerfile and reuse the OIDC-federated pipeline from the previous chapter to deploy it, with Application Insights layered on top from the first request.

> **What you'll learn:**
>
> - When a controllers-based Web API is the right shape and when minimal APIs are
> - Why entity types and DTOs should be separate even before a database exists
> - How `[ApiController]` plus attribute routing produces idiomatic 200 / 201 / 404 / 400 responses
> - How `CreatedAtAction` builds the `Location` header on POST for free
> - The trade-off in leaving Swagger on in production
> - How to apply the OIDC-federated pipeline pattern to a brand-new repo with new resource names

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ .NET 10 SDK installed (`dotnet --version` reports a `10.*` version)
> - ✓ Docker Desktop running, and `docker --version` succeeds
> - ✓ The `az` CLI signed in (`az login`) and the `gh` CLI signed in (`gh auth status`)
> - ✓ Completed the deployment chapter — you've previously built a GitHub Actions pipeline that uses OIDC federation to push to ACR and update a Container App
> - ✓ Completed the logging and monitoring chapter — you've previously injected an Application Insights connection string as a Container Apps secret via `secretref:`
> - ✓ Permission to create Entra app registrations in your tenant and Owner (or User Access Administrator) on the subscription

## Exercise Steps

### Overview

1. **Scaffold the `CloudCiApi` Web API project**
2. **Create the `Quote` entity**
3. **Create the `IQuoteStore` and seed data**
4. **Create the `QuoteDto` and `CreateQuoteRequest`**
5. **Register the store and Swagger in `Program.cs`**
6. **Build the `QuotesController` with three actions**
7. **Run locally and verify Swagger and the JSON endpoint**
8. **Containerize with a Dockerfile and `.dockerignore`**
9. **Push the project to a fresh GitHub repo**
10. **Set up the OIDC-federated pipeline against new Azure resources**
11. **Add Application Insights in workspace-based mode**
12. **Test Your Implementation**

### **Step 1:** Scaffold the `CloudCiApi` Web API project

Start from the `webapi` template with `--use-controllers`. The default `dotnet new webapi` template in .NET 8 and later is minimal-API by default; the explicit flag asks for the older controllers-based shape, which is what you want for an API that will grow beyond a few endpoints.

1. **Create** the project folder and scaffold the API:

   ```bash
   dotnet new webapi -o CloudCiApi --use-controllers --framework net10.0
   cd CloudCiApi
   ```

   The `--framework net10.0` argument pins the project to .NET 10 so the `<TargetFramework>` in the `.csproj` matches the `mcr.microsoft.com/dotnet/sdk:10.0` and `aspnet:10.0` base images you'll use in the Dockerfile later in this exercise.

2. **Verify** it builds and runs:

   ```bash
   dotnet build
   dotnet run --launch-profile http
   ```

   The terminal should report `Now listening on: http://localhost:<port>`. Stop the server with `Ctrl+C` once you've seen it start.

3. **Delete** the demo `WeatherForecast.cs` model and `Controllers/WeatherForecastController.cs`. They're scaffold noise and you're about to replace them with your own resource.

   ```bash
   rm WeatherForecast.cs Controllers/WeatherForecastController.cs
   ```

4. **Remove** the built-in OpenAPI package. The .NET 10 template auto-includes `Microsoft.AspNetCore.OpenApi`, which competes with Swashbuckle (the OpenAPI generator this exercise uses). Strip it now so the package set stays consistent.

   ```bash
   dotnet remove package Microsoft.AspNetCore.OpenApi
   ```

> ℹ **Concept Deep Dive: controllers vs minimal APIs**
>
> ASP.NET Core ships two shapes for HTTP services. **Minimal APIs** declare endpoints inline in `Program.cs` with `app.MapGet("/path", handler)` — terse, low ceremony, ideal for tiny services where the routing table fits on one screen. **Controllers** declare endpoints as methods on a class decorated with `[ApiController]` and attribute routes — more boilerplate per endpoint, but they pay off the moment you need cross-cutting concerns like model binding, action filters, attribute-based authorization, content negotiation, or per-action OpenAPI metadata. Pick minimal APIs when the entire service is one or two endpoints with no auth, no validation, and no shared filters; pick controllers as soon as any of those become true.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `--use-controllers` and ending up with a minimal-API scaffold whose `Program.cs` has `app.MapGet("/weatherforecast", ...)` inline. Re-run with `--force` to overwrite.
>
> ✓ **Quick check:** `CloudCiApi.csproj` exists, `Controllers/` is empty after the deletion, and `dotnet build` returns no errors.

### **Step 2:** Create the `Quote` entity

The `Quote` is your domain type — the shape your application code reasons about. There is no database in this chapter; storage comes later. That does not change what the entity is for. Even in memory, the entity captures the *concept* the API is about, with whatever fields the rest of the code needs to do work — including fields you may not want on the wire.

1. **Create** a `Models` directory and add the entity:

   > `Models/Quote.cs`

   ```csharp
   namespace CloudCiApi.Models;

   public class Quote
   {
       public int Id { get; set; }
       public string Author { get; set; } = string.Empty;
       public string Text { get; set; } = string.Empty;
       public DateTimeOffset CreatedAt { get; set; }
   }
   ```

> ℹ **Concept Deep Dive**
>
> The entity is plain — no attributes, no validation, no JSON serializer hints. Those concerns belong on the DTO. Keeping the entity attribute-free is what lets you later add Entity Framework mappings, persistence-only fields like `RowVersion`, or audit columns like `ModifiedBy`, without those leaking into the wire format.
>
> ✓ **Quick check:** `dotnet build` succeeds. The file lives at `Models/Quote.cs` with namespace `CloudCiApi.Models`.

### **Step 3:** Create the `IQuoteStore` and seed data

The store is the abstraction the controller depends on. By coding to an interface from day one, you can swap the in-memory implementation for a database-backed one in a later chapter without touching the controller. The seeded quotes are what the API serves out of the box, so a freshly-deployed instance is never empty.

1. **Create** a `Services` directory and add the interface:

   > `Services/IQuoteStore.cs`

   ```csharp
   using CloudCiApi.Models;

   namespace CloudCiApi.Services;

   public interface IQuoteStore
   {
       IReadOnlyList<Quote> GetAll();
       Quote? GetById(int id);
       Quote Add(string author, string text);
   }
   ```

2. **Add** the in-memory implementation:

   > `Services/InMemoryQuoteStore.cs`

   ```csharp
   using CloudCiApi.Models;

   namespace CloudCiApi.Services;

   public class InMemoryQuoteStore : IQuoteStore
   {
       private readonly List<Quote> _quotes = new();
       private int _nextId = 1;
       private readonly object _gate = new();

       public InMemoryQuoteStore()
       {
           Add("Edsger W. Dijkstra",
               "Simplicity is prerequisite for reliability.");
           Add("Grace Hopper",
               "The most dangerous phrase in the language is 'we've always done it this way.'");
           Add("Alan Kay",
               "The best way to predict the future is to invent it.");
           Add("Linus Torvalds",
               "Talk is cheap. Show me the code.");
       }

       public IReadOnlyList<Quote> GetAll()
       {
           lock (_gate)
           {
               return _quotes.ToList();
           }
       }

       public Quote? GetById(int id)
       {
           lock (_gate)
           {
               return _quotes.FirstOrDefault(q => q.Id == id);
           }
       }

       public Quote Add(string author, string text)
       {
           lock (_gate)
           {
               var quote = new Quote
               {
                   Id = _nextId++,
                   Author = author,
                   Text = text,
                   CreatedAt = DateTimeOffset.UtcNow,
               };
               _quotes.Add(quote);
               return quote;
           }
       }
   }
   ```

> ℹ **Concept Deep Dive**
>
> The store is registered as a **singleton** in the next step — one instance services every request across every thread. That's why mutable state is guarded with a `lock`: without it, two simultaneous POSTs could read `_nextId` at the same instant and both write the same id. A real production app would use a database transaction or `Interlocked.Increment`; the lock is the smallest correct version for an in-memory teaching store.
>
> ⚠ **Common Mistakes**
>
> - Registering this as `Scoped` instead of `Singleton`. Each request would get a fresh empty list — the API would respond `[]` to the very next GET after a POST.
>
> ✓ **Quick check:** Both files compile.

### **Step 4:** Create the `QuoteDto` and `CreateQuoteRequest`

This is the step where the entity-vs-DTO distinction earns its keep. The `Quote` entity is the *domain* shape — what your application code uses internally. The DTOs are the *wire* shapes — what the API speaks to clients. Keeping them separate from the start, even when the fields are nearly identical, is what stops the wire contract from changing every time the domain evolves.

1. **Create** a `Dtos` directory and add the output DTO:

   > `Dtos/QuoteDto.cs`

   ```csharp
   namespace CloudCiApi.Dtos;

   public class QuoteDto
   {
       public int Id { get; set; }
       public string Author { get; set; } = string.Empty;
       public string Text { get; set; } = string.Empty;
       public DateTimeOffset CreatedAt { get; set; }
   }
   ```

2. **Add** the input DTO:

   > `Dtos/CreateQuoteRequest.cs`

   ```csharp
   using System.ComponentModel.DataAnnotations;

   namespace CloudCiApi.Dtos;

   public class CreateQuoteRequest
   {
       [Required]
       [StringLength(100, MinimumLength = 1)]
       public string Author { get; set; } = string.Empty;

       [Required]
       [StringLength(500, MinimumLength = 1)]
       public string Text { get; set; } = string.Empty;
   }
   ```

> ℹ **Concept Deep Dive: why DTOs exist before there's a database**
>
> A DTO ("Data Transfer Object") is the **wire contract** — the exact shape clients send and receive. The entity is the **domain model** — the shape your code reasons about. Even when both look identical today, three forces will pull them apart:
>
> - **Input is not the same as output.** A client creating a quote should not choose the `Id` (the server assigns it) or `CreatedAt` (the server stamps it). `CreateQuoteRequest` has neither field. The compiler enforces this — there is no way for client JSON to set them.
> - **The domain grows fields the wire shouldn't see.** As the entity gains internal-only fields (audit columns, soft-delete flags, EF navigation properties), the DTO stays trimmed to what clients legitimately need. Without the split, you'd either leak internals or break the wire format every time you refactor internals.
> - **Validation lives on the wire.** `[Required]` and `[StringLength]` describe what a *client request* must look like. The entity represents already-valid in-memory state. Putting validation on the entity couples request validation and domain invariants, which evolve at different rates.
>
> Entity types are the domain, DTO types are the wire contract, and decoupling them lets each evolve independently. You'll feel the saving the first time you add a database — the entity gains EF attributes the wire never sees.
>
> ⚠ **Common Mistakes**
>
> - "But it's the same fields" — yes, today. The point is that the *types* are different, so when they diverge tomorrow the change is local.
> - Reusing one type for both input and output and excluding `Id` with `[JsonIgnore]`. That works at runtime but invites bugs the moment someone removes the attribute.
>
> ✓ **Quick check:** Both DTO files compile in the `CloudCiApi.Dtos` namespace.

### **Step 5:** Register the store and Swagger in `Program.cs`

Now wire the store into dependency injection and add Swagger. Swagger is a runtime contract document — it reads the controller's signatures via reflection and produces an OpenAPI JSON file plus a browsable UI. Anyone with the URL can discover the API's shape without seeing your source.

1. **Add** the Swashbuckle package, pinned to 6.6.2:

   ```bash
   dotnet add package Swashbuckle.AspNetCore --version 6.6.2
   ```

   The version pin is deliberate. Swashbuckle 10.x (the unpinned latest) consolidates the legacy `Microsoft.OpenApi.Models` namespace into the root `Microsoft.OpenApi` namespace; the security-scheme code in the next two exercises uses the legacy `.Models` namespace, so pinning to 6.6.2 keeps every snippet copy-pasteable.

2. **Replace** the contents of `Program.cs`:

   > `Program.cs`

   ```csharp
   using CloudCiApi.Services;

   var builder = WebApplication.CreateBuilder(args);

   // Singleton — one in-memory store shared across every request.
   builder.Services.AddSingleton<IQuoteStore, InMemoryQuoteStore>();

   builder.Services.AddControllers();

   // OpenAPI / Swagger surface.
   builder.Services.AddEndpointsApiExplorer();
   builder.Services.AddSwaggerGen();

   var app = builder.Build();

   // Swagger is enabled in BOTH Development AND Production for this course.
   // See the Concept Deep Dive below for the trade-off.
   app.UseSwagger();
   app.UseSwaggerUI();

   app.UseHttpsRedirection();
   app.UseAuthorization();
   app.MapControllers();

   app.Run();
   ```

> ℹ **Concept Deep Dive: Swagger in production — useful default, real trade-off**
>
> The default `dotnet new webapi` template wraps `app.UseSwagger()` and `app.UseSwaggerUI()` in `if (app.Environment.IsDevelopment())`, hiding the docs from anything that's not your laptop. This exercise deliberately removes that gate.
>
> *Why leave it on in production:* for internal services and learning artifacts, Swagger is the contract. Hiding it forces consumers to read source or scrape the OpenAPI JSON from a build artifact. For this exercise the deployed `/swagger` page is your demo URL — proof that the API is alive.
>
> *Why a real public-facing API typically does not:* Swagger reveals every route, parameter, and response shape — fine for a known public contract, reconnaissance for an attacker on an internal admin API. The interactive UI is also a chunk of JavaScript you don't strictly need to ship; keeping it means a thing to patch and audit.
>
> The middle ground in real production: ship the OpenAPI JSON at `/swagger/v1/swagger.json` but gate the interactive UI behind auth, or remove the UI from production builds and host the docs separately. For this chapter we leave both on — "Swagger on in production" is a deliberate choice, not the only right answer.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `AddEndpointsApiExplorer()` — `AddSwaggerGen()` then has nothing to introspect, and the `/swagger` page renders empty.
> - Calling `UseSwagger()` after `MapControllers()` — Swagger middleware must register before the endpoint routing terminates the pipeline.
>
> ✓ **Quick check:** `dotnet build` succeeds with no warnings.

### **Step 6:** Build the `QuotesController` with three actions

The controller is the HTTP-facing surface. With `[ApiController]` and `[Route("api/[controller]")]`, ASP.NET Core gives you attribute-routed endpoints, automatic 400 responses on validation failures, and convention-based binding from the request body. Three actions cover the shape of a typical resource: list, get-by-id, and create.

1. **Create** the controller file:

   > `Controllers/QuotesController.cs`

   ```csharp
   using CloudCiApi.Dtos;
   using CloudCiApi.Models;
   using CloudCiApi.Services;
   using Microsoft.AspNetCore.Mvc;

   namespace CloudCiApi.Controllers;

   [ApiController]
   [Route("api/[controller]")]
   public class QuotesController : ControllerBase
   {
       private readonly IQuoteStore _store;

       public QuotesController(IQuoteStore store)
       {
           _store = store;
       }

       [HttpGet]
       public ActionResult<IEnumerable<QuoteDto>> GetAll()
       {
           var dtos = _store.GetAll().Select(ToDto);
           return Ok(dtos);
       }

       [HttpGet("{id:int}")]
       public ActionResult<QuoteDto> GetById(int id)
       {
           var quote = _store.GetById(id);
           if (quote is null)
           {
               return NotFound();
           }
           return Ok(ToDto(quote));
       }

       [HttpPost]
       public ActionResult<QuoteDto> Create([FromBody] CreateQuoteRequest request)
       {
           var quote = _store.Add(request.Author, request.Text);
           var dto = ToDto(quote);

           // 201 Created with a Location header pointing back at GetById.
           return CreatedAtAction(
               nameof(GetById),
               new { id = quote.Id },
               dto);
       }

       private static QuoteDto ToDto(Quote q) => new()
       {
           Id = q.Id,
           Author = q.Author,
           Text = q.Text,
           CreatedAt = q.CreatedAt,
       };
   }
   ```

> ℹ **Concept Deep Dive**
>
> `[ApiController]` opts the controller into automatic model-state validation (a malformed request body returns `400` with a `ProblemDetails` body before your action runs), into binding source inference (`[FromBody]` is implied for complex types), and into attribute-based routing only.
>
> `CreatedAtAction(nameof(GetById), new { id = quote.Id }, dto)` produces three things in one call: a `201 Created` status, a `Location` response header pointing at the freshly-created resource, and the resource itself in the body. The framework computes the URL from the named action — you never hard-code `"/api/quotes/" + id`, so a route refactor doesn't break clients.
>
> The controller speaks DTOs both ways: `Quote` is converted to `QuoteDto` before going out, and `CreateQuoteRequest` is unpacked into `_store.Add(...)` rather than passed through. The entity never appears in any method signature. That's the discipline from Step 4 paying off.
>
> ⚠ **Common Mistakes**
>
> - Returning the entity directly (`return Ok(quote)`). Works today — the JSON serializer produces almost-the-same shape — but the moment the entity gains an internal field, that field leaks. Return the DTO every time.
> - Computing the `Location` header by hand. Use `CreatedAtAction` so the URL stays correct under refactoring.
> - Forgetting the `:int` route constraint. With it, `/api/quotes/abc` returns `404`; without it, you get a `400` from a binding failure.
>
> ✓ **Quick check:** `dotnet build` succeeds.

### **Step 7:** Run locally and verify Swagger and the JSON endpoint

A quick local round-trip proves all three pieces — the store, the controller, the Swagger pipeline — are wired correctly before you start adding deployment plumbing.

1. **Start** the API:

   ```bash
   dotnet run --launch-profile http
   ```

   Note the port the terminal reports (typically `5xxx`); call it `<port>` below.

2. **Open** `http://localhost:<port>/swagger` in a browser. You should see one tag, **Quotes**, expanded to three operations: `GET /api/quotes`, `GET /api/quotes/{id}`, and `POST /api/quotes`.

3. **Hit** the JSON endpoint directly:

   ```bash
   curl -s http://localhost:<port>/api/quotes | jq
   ```

   Expected: a JSON array of four objects with `id`, `author`, `text`, and `createdAt` fields.

4. **Try** a `POST` with valid JSON:

   ```bash
   curl -i -X POST http://localhost:<port>/api/quotes \
     -H "Content-Type: application/json" \
     -d '{"author":"Donald Knuth","text":"Premature optimization is the root of all evil."}'
   ```

   Expected: `HTTP/1.1 201 Created`, a `Location: http://localhost:<port>/api/Quotes/5` header, and a body containing the new quote with `id` set to `5` and `createdAt` populated.

5. **Try** a `POST` with a missing field:

   ```bash
   curl -i -X POST http://localhost:<port>/api/quotes \
     -H "Content-Type: application/json" \
     -d '{"author":"Anonymous"}'
   ```

   Expected: `HTTP/1.1 400 Bad Request` with a `ProblemDetails` body listing `Text` as the validation error. This is `[ApiController]` doing automatic model-state validation.

6. **Stop** the server with `Ctrl+C`.

> ✓ **Quick check:** Swagger renders three operations under `Quotes`. `GET /api/quotes` returns four seeded items. `POST` with valid input returns `201` with a `Location` header. `POST` with missing fields returns `400`.

### **Step 8:** Containerize with a Dockerfile and `.dockerignore`

The pipeline you'll set up next builds a Docker image and pushes it to ACR — that requires a Dockerfile. Use the same multi-stage `.NET SDK → ASP.NET runtime` pattern from the deployment chapter, listening on port `8080` (Container Apps' default ingress port).

1. **Add** the Dockerfile at the project root:

   > `Dockerfile`

   ```dockerfile
   # Build stage — full SDK, restores and publishes a Release build.
   FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
   WORKDIR /src
   COPY *.csproj ./
   RUN dotnet restore
   COPY . ./
   RUN dotnet publish -c Release -o /app /p:UseAppHost=false

   # Runtime stage — slim ASP.NET image, no SDK.
   FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
   WORKDIR /app
   COPY --from=build /app ./

   # Container Apps' default ingress targets 8080.
   EXPOSE 8080
   ENV ASPNETCORE_URLS=http://+:8080

   ENTRYPOINT ["dotnet", "CloudCiApi.dll"]
   ```

2. **Add** a `.dockerignore` to keep the build context small:

   > `.dockerignore`

   ```text
   bin/
   obj/
   .vs/
   .vscode/
   .idea/
   *.user
   .git/
   .github/
   Dockerfile
   .dockerignore
   README.md
   ```

3. **Verify** the image builds locally:

   ```bash
   docker build -t cloudci-api:local .
   docker run --rm -p 8080:8080 cloudci-api:local
   ```

   In another terminal:

   ```bash
   curl -s http://localhost:8080/api/quotes | jq '. | length'
   ```

   Expected: `4`. Stop the container with `Ctrl+C`.

> ℹ **Concept Deep Dive**
>
> The two-stage build keeps the runtime image small. The build stage uses the full .NET SDK image (~700 MB) but is discarded; only `/app` is copied into the runtime stage's ASP.NET-only image (~220 MB). `ENV ASPNETCORE_URLS=http://+:8080` makes Kestrel listen on the port Container Apps expects — without it, Kestrel binds to its default `5xxx` and the platform health probe fails.
>
> ✓ **Quick check:** `docker run` serves `/api/quotes` on port 8080 with four seeded entries.

### **Step 9:** Push the project to a fresh GitHub repo

The federated credential you create in the next step authenticates from a specific repo on a specific branch — the repo must exist before the credential. Create it now and push.

1. **Initialize** a git repo and make a first commit:

   ```bash
   git init
   git add .
   git commit -m "Scaffold CloudCiApi with Quotes controller and Dockerfile"
   ```

2. **Create** the GitHub repository and push (substitute your GitHub username for `<your-username>`):

   ```bash
   gh repo create <your-username>/cloudci-api --public --source=. --push
   ```

> ⚠ **Common Mistakes**
>
> - Forgetting the federated credential subject will be `repo:<your-username>/cloudci-api:ref:refs/heads/main`. Renaming the repo or moving it to a GitHub organisation changes the subject and breaks authentication.
>
> ✓ **Quick check:** `gh repo view <your-username>/cloudci-api --web` opens the repo and the source tree matches your local working copy.

### **Step 10:** Set up the OIDC-federated pipeline against new Azure resources

You set this up in detail in the deployment chapter — a fresh resource group, an ACR, a Container Apps environment, a Container App, an Entra app registration with two role assignments and a federated credential, and a workflow that signs in via OIDC, pushes the image, and updates the Container App. Here it's a quick recap with new names. If any sub-step feels unfamiliar, jump back to that chapter for the full explanation.

1. **Provision** the Azure resources. Pick a four-character random suffix for the ACR name (it must be globally unique). Substitute it into `<rand>` below:

   ```bash
   az group create -n rg-api-week6 -l northeurope

   az acr create -n acrapi<rand> -g rg-api-week6 \
     --sku Basic --admin-enabled false

   az containerapp env create -n cae-api-week6 -g rg-api-week6 \
     -l northeurope

   az containerapp create \
     -n ca-api-week6 \
     -g rg-api-week6 \
     --environment cae-api-week6 \
     --image mcr.microsoft.com/k8se/quickstart:latest \
     --target-port 8080 \
     --ingress external \
     --min-replicas 1 \
     --max-replicas 1
   ```

   The placeholder `mcr.microsoft.com/k8se/quickstart:latest` image is just so the Container App can be created before your image exists; the pipeline will replace it on the first push.

2. **Create** the Entra app, the service principal, the two role assignments, and the federated credential. **Substitute your GitHub username** for `<your-username>` in the federated subject — leaving the angle brackets in literally is the single most common authentication failure in this exercise.

   ```bash
   az ad app create --display-name "github-cloudci-api-oidc"
   export APP_ID="<paste appId from previous output>"
   az ad sp create --id "$APP_ID"

   ACR_ID=$(az acr show -n acrapi<rand> --query id -o tsv)
   CA_ID=$(az containerapp show -n ca-api-week6 -g rg-api-week6 --query id -o tsv)

   az role assignment create --assignee "$APP_ID" --role AcrPush --scope "$ACR_ID"
   az role assignment create --assignee "$APP_ID" \
     --role "Container Apps Contributor" --scope "$CA_ID"

   az ad app federated-credential create \
     --id "$APP_ID" \
     --parameters '{
       "name": "main-branch",
       "issuer": "https://token.actions.githubusercontent.com",
       "subject": "repo:<your-username>/cloudci-api:ref:refs/heads/main",
       "audiences": ["api://AzureADTokenExchange"],
       "description": "GitHub Actions, main branch only"
     }'
   ```

3. **Add** the four GitHub secrets:

   ```bash
   gh secret set AZURE_CLIENT_ID --body "$APP_ID"
   gh secret set AZURE_TENANT_ID --body "$(az account show --query tenantId -o tsv)"
   gh secret set AZURE_SUBSCRIPTION_ID --body "$(az account show --query id -o tsv)"
   gh secret set ACR_NAME --body "acrapi<rand>"
   ```

4. **Add** the workflow at `.github/workflows/ci.yml`:

   > `.github/workflows/ci.yml`

   ```yaml
   name: build-and-deploy

   on:
     push:
       branches: [main]
     workflow_dispatch:

   jobs:
     deploy:
       runs-on: ubuntu-latest
       permissions:
         id-token: write
         contents: read
       steps:
         - uses: actions/checkout@v4

         - name: Sign in to Azure
           uses: azure/login@v2
           with:
             client-id: ${{ secrets.AZURE_CLIENT_ID }}
             tenant-id: ${{ secrets.AZURE_TENANT_ID }}
             subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

         - name: Log in to ACR
           run: az acr login --name ${{ secrets.ACR_NAME }}

         - name: Build and push image
           run: |
             IMAGE="${{ secrets.ACR_NAME }}.azurecr.io/cloudci-api:${{ github.sha }}"
             docker build -t "$IMAGE" .
             docker push "$IMAGE"
             echo "IMAGE=$IMAGE" >> "$GITHUB_ENV"

         - name: Update Container App
           run: |
             az containerapp update \
               -n ca-api-week6 \
               -g rg-api-week6 \
               --image "$IMAGE"

         - name: Smoke test
           run: |
             FQDN=$(az containerapp show \
               -n ca-api-week6 -g rg-api-week6 \
               --query properties.configuration.ingress.fqdn -o tsv)
             for i in {1..20}; do
               if curl -fsS "https://$FQDN/api/quotes" >/dev/null; then
                 echo "Smoke test passed."
                 exit 0
               fi
               sleep 3
             done
             echo "Smoke test failed."
             exit 1
   ```

5. **Grant** the Container App pull access to ACR. Container Apps uses managed identity to pull images; the simplest path is to enable the system-assigned identity and grant it `AcrPull`:

   ```bash
   az containerapp identity assign \
     -n ca-api-week6 -g rg-api-week6 \
     --system-assigned

   IDENTITY_PRINCIPAL_ID=$(az containerapp show \
     -n ca-api-week6 -g rg-api-week6 \
     --query identity.principalId -o tsv)

   az role assignment create \
     --assignee "$IDENTITY_PRINCIPAL_ID" \
     --role AcrPull \
     --scope "$ACR_ID"

   az containerapp registry set \
     -n ca-api-week6 -g rg-api-week6 \
     --server acrapi<rand>.azurecr.io \
     --identity system
   ```

6. **Push** to trigger the first real deployment:

   ```bash
   git add .github/workflows/ci.yml
   git commit -m "Add OIDC-federated build-and-deploy workflow"
   git push
   gh run watch
   ```

7. **Verify** the deployed FQDN serves your quotes:

   ```bash
   FQDN=$(az containerapp show \
     -n ca-api-week6 -g rg-api-week6 \
     --query properties.configuration.ingress.fqdn -o tsv)
   echo "https://$FQDN/swagger"
   curl -s "https://$FQDN/api/quotes" | jq '. | length'
   ```

   Expected: `4`. Open the printed `https://$FQDN/swagger` URL — you should see the same three operations under `Quotes` you saw locally.

> ℹ **Concept Deep Dive**
>
> Nothing in this step is conceptually new — it's the same chain of objects from the previous chapter (resource group → ACR → Container Apps environment → Container App → Entra app → service principal → role assignments → federated credential → workflow) with new names. Re-applying a pattern to a new project is when you find out which parts you actually internalised. If any sub-step felt blurry, that's the spot to revisit.
>
> ⚠ **Common Mistakes**
>
> - `--target-port` must be `8080` to match the Dockerfile.
> - Leaving angle brackets in the federated credential subject literally — substitute your username.
> - Skipping the managed-identity / `AcrPull` setup. The pipeline would push the image successfully, but the new revision would fail to pull from ACR.
>
> ✓ **Quick check:** The workflow run is green, `gh secret list` shows the four secrets, and `https://$FQDN/api/quotes` returns four seeded items.

### **Step 11:** Add Application Insights in workspace-based mode

Same pattern as the logging and monitoring chapter — a workspace-based App Insights component, the SDK in the project, the connection string injected as a Container Apps secret referenced via `secretref:`. Only the names change. If any sub-step is fuzzy, revisit that chapter for the full reasoning.

1. **Provision** the App Insights component. The Container Apps environment created an auto-managed Log Analytics workspace inside `rg-api-week6` — point App Insights at it:

   ```bash
   WS_ID=$(az monitor log-analytics workspace list \
     -g rg-api-week6 \
     --query '[0].id' -o tsv)

   az monitor app-insights component create \
     --app cloudci-api-insights \
     -g rg-api-week6 \
     --location northeurope \
     --workspace "$WS_ID"

   CONN=$(az monitor app-insights component show \
     --app cloudci-api-insights \
     -g rg-api-week6 \
     --query connectionString -o tsv)
   ```

2. **Add** the SDK (pinned to 2.22.0) and register it:

   ```bash
   dotnet add package Microsoft.ApplicationInsights.AspNetCore --version 2.22.0
   ```

   The pin is the same one Week 5 used. The unpinned latest is 3.x, which is the new OpenTelemetry-backed line and **throws at host startup** if `APPLICATIONINSIGHTS_CONNECTION_STRING` is not set — fine in production but an annoying paper-cut for `dotnet run` against `localhost`. The 2.x line silently drops telemetry when the connection string is missing, which matches the behaviour every other exercise in this chapter assumes.

   In `Program.cs`, add the registration alongside the other service registrations:

   > `Program.cs`

   ```csharp
   builder.Services.AddApplicationInsightsTelemetry();
   ```

3. **Inject** the connection string as a Container Apps secret and bind it to the env var:

   ```bash
   az containerapp secret set \
     -g rg-api-week6 -n ca-api-week6 \
     --secrets appinsights-connstr="$CONN"

   az containerapp update \
     -g rg-api-week6 -n ca-api-week6 \
     --set-env-vars APPLICATIONINSIGHTS_CONNECTION_STRING=secretref:appinsights-connstr
   ```

4. **Commit and push** the SDK changes:

   ```bash
   git add CloudCiApi.csproj Program.cs
   git commit -m "Add Application Insights SDK"
   git push
   gh run watch
   ```

5. **Generate** traffic and look at Live Metrics in the Portal:

   ```bash
   for i in {1..30}; do curl -s "https://$FQDN/api/quotes" >/dev/null; sleep 0.3; done
   ```

   Open Application Insights `cloudci-api-insights` → **Live Metrics** in the Azure Portal. You should see incoming requests within seconds.

> ✓ **Quick check:** Live Metrics shows incoming requests when you run the curl loop. `az containerapp show -g rg-api-week6 -n ca-api-week6 --query 'properties.template.containers[0].env'` shows `APPLICATIONINSIGHTS_CONNECTION_STRING` with a `secretRef`, not a literal value.

### **Step 12:** Test Your Implementation

End-to-end check against the live deployment.

1. **Capture** the FQDN and open Swagger in a browser. Verify the **Quotes** tag shows `GET /api/quotes`, `GET /api/quotes/{id}`, and `POST /api/quotes`:

   ```bash
   FQDN=$(az containerapp show \
     -n ca-api-week6 -g rg-api-week6 \
     --query properties.configuration.ingress.fqdn -o tsv)
   echo "https://$FQDN/swagger"
   ```

2. **Exercise** all four code paths:

   ```bash
   # Happy path: list and get-by-id
   curl -s "https://$FQDN/api/quotes" | jq '. | length'      # expect 4
   curl -i "https://$FQDN/api/quotes/1"                      # expect 200
   curl -i "https://$FQDN/api/quotes/9999"                   # expect 404

   # Create
   curl -i -X POST "https://$FQDN/api/quotes" \
     -H "Content-Type: application/json" \
     -d '{"author":"Bjarne Stroustrup","text":"Within C++, there is a much smaller and cleaner language struggling to get out."}'
   # expect 201 with Location: https://$FQDN/api/Quotes/5

   # Validation failure
   curl -i -X POST "https://$FQDN/api/quotes" \
     -H "Content-Type: application/json" \
     -d '{"author":"Anonymous"}'
   # expect 400 with ProblemDetails listing Text as the missing field
   ```

   The Container App runs one replica, so subsequent GETs from the same FQDN return the new quote — until the replica restarts, when the in-memory store resets.

3. **Confirm** App Insights records the failure. Wait one to three minutes after the bad POST, then in the Azure Portal navigate to Application Insights `cloudci-api-insights` → **Failures**. The `400` shows up — `[ApiController]`'s automatic model-state failures are counted as request failures even though no exception was thrown.

> ✓ **Success indicators:**
>
> - `https://$FQDN/swagger` renders the three operations under `Quotes`
> - `GET /api/quotes` returns the four seeded entries as JSON
> - `GET /api/quotes/9999` returns `404`
> - `POST /api/quotes` with valid input returns `201` with a `Location` header
> - `POST /api/quotes` with a missing field returns `400` with a `ProblemDetails` body
> - Live Metrics shows incoming requests when the API is hit
>
> ✓ **Final verification checklist:**
>
> - ☐ `CloudCiApi` project scaffolded with `--use-controllers` and the demo `WeatherForecast` controller removed
> - ☐ `Quote` entity, `IQuoteStore` + `InMemoryQuoteStore`, and `QuoteDto` + `CreateQuoteRequest` all in their conventional folders
> - ☐ `QuotesController` with `[ApiController]`, attribute routing, and three actions returning DTOs
> - ☐ Swagger registered and middleware enabled in both Development and Production
> - ☐ Dockerfile produces a working image listening on `:8080`
> - ☐ GitHub repo `<your-username>/cloudci-api` pushed and federated against an Entra app `github-cloudci-api-oidc`
> - ☐ Workflow runs green on push to `main`, deploys to Container App `ca-api-week6` in `rg-api-week6`
> - ☐ App Insights component `cloudci-api-insights` workspace-based, connection string delivered via `secretref:`

## Common Issues

> **If you encounter problems:**
>
> **Swagger page renders empty:** `AddEndpointsApiExplorer()` is missing, or `UseSwagger()` is registered after `MapControllers()`. Swagger middleware must come before endpoint routing.
>
> **`POST` returns the new quote but the very next `GET` doesn't include it:** The store is registered as `Scoped` or `Transient` instead of `Singleton`. Each request gets a fresh empty list. Confirm `builder.Services.AddSingleton<IQuoteStore, InMemoryQuoteStore>();`.
>
> **`CreatedAtAction` returns `500` with `No route matches the supplied values`:** `nameof(GetById)` doesn't match an action with a `{id}` route. Confirm the GET-by-id action is `[HttpGet("{id:int}")]` and named `GetById`.
>
> **Pipeline run fails at `docker push` with `unauthorized: authentication required`:** The `AcrPush` role assignment from Step 10 hasn't propagated yet — Entra ID takes a few seconds. Re-run the workflow.
>
> **Container App revision goes `Failed` with `Failed to pull image`:** Either the managed identity wasn't granted `AcrPull`, or the registry wasn't registered with `az containerapp registry set`. Both are required.
>
> **Live Metrics shows "Not available":** The connection string env var didn't make it into the container. Re-check the env list for a `secretRef` and the secret list for the underlying value.
>
> **`AADSTS70021: No matching federated identity record found for presented assertion`:** The `subject` in the token doesn't match the federated credential. Exact form is `repo:<your-username>/cloudci-api:ref:refs/heads/main` — watch for `head` vs `heads`, wrong username, or angle brackets left in literally.
>
> **Still stuck?** Re-read the OIDC exercise from the deployment chapter and the App Insights exercise from the logging and monitoring chapter. The patterns are identical; only the names changed.

## Summary

You have a controllers-based ASP.NET Core Web API with a clean entity-vs-DTO split, three idiomatic CRUD-shaped actions, Swagger surfacing the contract, and a multi-stage Docker image. The repo is wired to a fresh OIDC-federated GitHub Actions pipeline that pushes to ACR and updates a Container App on every push to `main`. App Insights is layered on top in workspace-based mode with the connection string delivered as a Container Apps secret.

- ✓ `dotnet new webapi --use-controllers` produces a project shape that grows beyond a few endpoints
- ✓ Entity types are the domain; DTOs are the wire contract; keeping them separate from day one pays off when either evolves
- ✓ `[ApiController]` plus attribute routing gives you 200 / 201 / 404 / 400 responses without extra ceremony, and `CreatedAtAction` builds the `Location` header for free
- ✓ Swagger in production is a deliberate trade-off — fine for internal services and learning, gated for public-facing APIs
- ✓ The OIDC-federated pipeline pattern from the previous chapter ports cleanly to a new repo with new resource names

> **Key takeaway:** A Web API is three things stacked on top of each other — a domain, a wire contract, and a deployment pipeline. Treating each as a separate concern (entities for the domain, DTOs for the wire, OIDC federation for the deploy) is what lets each one evolve without breaking the others. You'll feel the saving the first time the domain grows a field, or the wire contract changes, or the cluster moves regions. Each change touches one layer.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Add `[ProducesResponseType(StatusCodes.Status201Created)]` and friends to each action so Swagger documents the response shapes precisely.
> - Replace the hand-written `ToDto` mapper with **Mapster** or **AutoMapper** and compare the trade-off — magic vs. boilerplate.
> - Read the OpenAPI 3.0 JSON at `https://$FQDN/swagger/v1/swagger.json`. Notice how the request body schema for `POST` is `CreateQuoteRequest` while the response body is `QuoteDto` — the entity-vs-DTO split surfaces in the contract document.
> - Investigate the .NET 9+ built-in `AddOpenApi()` / `MapOpenApi()` as an alternative to Swashbuckle, often paired with the standalone Scalar UI.

## Done!

The API runs in the cloud. Anyone with the FQDN can list quotes, fetch one by id, and create new ones — there is no authentication of any kind. That is fine for a learning artifact; it is not fine for anything you want to charge for, rate-limit, or hold accountable. The next exercise closes that gap by gating every request behind a shared API key, the simplest possible authentication scheme that's still better than nothing.
