+++
title = "Securing the API with an API Key Middleware"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Add a custom ASP.NET Core middleware that demands an X-Api-Key header on every request, with the production key delivered via Container Apps secrets and the development key in appsettings.Development.json. Wire Swagger UI's Authorize button so the in-browser exploration still works."
weight = 2
draft = false
+++

# Securing the API with an API Key Middleware

## Goal

The previous exercise left you with a deployed `CloudCiApi` whose `/api/quotes` endpoint hands a JSON array to anyone on the internet. That is fine as a *shape* but wrong as a posture: a public endpoint with no gate is an abuse vector at minimum, and a data-leakage incident the moment the model behind it stops being seed data. This exercise adds the cheapest possible gate — a shared **API key** the caller must supply in the `X-Api-Key` header — and wires the production key through the same Container Apps secret pattern you used in the logging and monitoring chapter for the App Insights connection string.

The mechanism is a small custom **middleware** that plugs into the pipeline after routing but before the controllers, reads the header, compares it to a value bound from configuration, and either short-circuits with `401 Unauthorized` or `403 Forbidden` or hands off via `await _next(context)`. Locally that value comes from `appsettings.Development.json` (a clearly non-secret placeholder); in production it comes from a Container Apps secret. Swagger UI gets two extra lines so its **Authorize** button keeps the in-browser explorer working after the gate is in place.

This is the second time you see the secret-store pattern, so the *what* is short here. The *why* is longer: an API key proves that *some trusted client holds the key*, not *which* client — that distinction is what the next exercise replaces with JWT bearer tokens.

> **What you'll learn:**
>
> - How to write a custom ASP.NET Core middleware class — `InvokeAsync(HttpContext)`, where it sits in the `Use*` chain
> - The 401-vs-403 split per RFC 7235 and why a 401 must include a `WWW-Authenticate` header
> - Why a credential goes into a Container Apps **secret** + `secretref:` env var rather than a plain env var
> - The convention that maps the env var name `ApiKey__Value` (double underscore) to the configuration key `ApiKey:Value` (colon)
> - How to configure Swagger UI's **Authorize** button so the in-browser explorer keeps working after the gate
> - What an API key actually proves about the caller, and where it stops being enough

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ The previous exercise complete: `CloudCiApi` deployed to `ca-api-week6` in `rg-api-week6`, returning a JSON array on `GET /api/quotes` anonymously, with Swagger UI live at `/swagger`
> - ✓ The CI/CD pipeline still wired up — `git push` on `<your-username>/cloudci-api` deploys a new revision via OIDC federation
> - ✓ App Insights component `cloudci-api-insights` already attached; nothing here changes that wiring
> - ✓ The Azure CLI signed in (`az account show` returns your subscription)
> - ✓ A local clone of `CloudCiApi` and `openssl` available on your shell

## Exercise Steps

### Overview

1. **Implement the `ApiKeyMiddleware` class**
2. **Bind the configuration in `Program.cs` and register the middleware**
3. **Add the local placeholder to `appsettings.Development.json`**
4. **Run locally and verify the 401 / 403 / 200 transitions**
5. **Generate the production key locally**
6. **Store the key as a Container Apps secret**
7. **Reference the secret as the `ApiKey__Value` env var**
8. **Wire Swagger UI's Authorize button**
9. **Push and watch the pipeline deploy**
10. **Probe the deployed API from the terminal**
11. **Test Your Implementation**

### **Step 1:** Implement the `ApiKeyMiddleware` class

Custom middleware in ASP.NET Core is a class with two requirements: a constructor that takes a `RequestDelegate` (the next middleware), and an `InvokeAsync(HttpContext)` method that does the work. The class doesn't implement an interface — the framework discovers the shape by convention when you call `app.UseMiddleware<T>()`. Constructor parameters beyond `RequestDelegate` are resolved from DI at middleware-construction time (once per app), which is fine for a config-bound value like the API key.

1. **Create** a new folder: `Middleware/`

2. **Add** a new file:

   > `Middleware/ApiKeyMiddleware.cs`

   ```csharp
   using Microsoft.AspNetCore.Http;
   using Microsoft.Extensions.Configuration;

   namespace CloudCiApi.Middleware;

   public class ApiKeyMiddleware
   {
       // The header callers must send.
       private const string HeaderName = "X-Api-Key";

       // The configuration key bound from appsettings / env vars / secrets.
       private const string ConfigKey = "ApiKey:Value";

       private readonly RequestDelegate _next;
       private readonly string? _expectedKey;

       public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
       {
           _next = next;
           _expectedKey = configuration[ConfigKey];
       }

       public async Task InvokeAsync(HttpContext context)
       {
           // No header at all: 401 Unauthorized + WWW-Authenticate: ApiKey.
           if (!context.Request.Headers.TryGetValue(HeaderName, out var providedKey)
               || string.IsNullOrWhiteSpace(providedKey))
           {
               context.Response.Headers.Append("WWW-Authenticate", "ApiKey");
               context.Response.StatusCode = StatusCodes.Status401Unauthorized;
               await context.Response.WriteAsync("API key is missing.");
               return;
           }

           // Header present but doesn't match: 403 Forbidden.
           if (string.IsNullOrEmpty(_expectedKey)
               || !string.Equals(providedKey, _expectedKey, StringComparison.Ordinal))
           {
               context.Response.StatusCode = StatusCodes.Status403Forbidden;
               await context.Response.WriteAsync("API key is invalid.");
               return;
           }

           // Happy path: hand off to the next middleware (eventually the controller).
           await _next(context);
       }
   }
   ```

> ℹ **Concept Deep Dive: 401 vs 403**
>
> RFC 7235 is precise about the split. **`401 Unauthorized`** is "I don't know who you are" — no credential or an unparseable one — and the response **must** carry a `WWW-Authenticate` header naming the scheme, because that's how the spec tells the client *how* to authenticate. **`403 Forbidden`** is "I know who you are, and you're not allowed." For an API key the line is mechanical: no header → 401, wrong header → 403.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `await _next(context)` on the success path. The request hangs forever — no controller runs and no response is written.
> - Using a non-`Ordinal` comparison. API keys are byte-for-byte tokens, not human-readable text — culture-aware comparison can normalize Unicode in ways that accept the wrong key.
>
> ✓ **Quick check:** The file compiles with `dotnet build` and no warnings.

### **Step 2:** Bind the configuration in `Program.cs` and register the middleware

The middleware reads `ApiKey:Value` from `IConfiguration` in its constructor — no extra setup needed, since ASP.NET Core's default configuration providers (appsettings, env vars, user secrets) are already in place. The piece you add to `Program.cs` is a single `app.UseMiddleware<T>()` call, **after** `UseRouting()` and **before** `MapControllers()`.

1. **Open** `Program.cs`.

2. **Add** the using at the top:

   > `Program.cs`

   ```csharp
   using CloudCiApi.Middleware;
   ```

3. **Insert** the `UseMiddleware` call between `UseHttpsRedirection()` and `MapControllers()` — keep the Swagger middleware exactly where the previous exercise left it, so `/swagger` stays available in production:

   > `Program.cs`

   ```csharp
   var app = builder.Build();

   // Swagger stays on in BOTH Development and Production for this course
   // (set up in the previous exercise).
   app.UseSwagger();
   app.UseSwaggerUI();

   app.UseHttpsRedirection();

   // Gate every endpoint behind the API key.
   app.UseMiddleware<ApiKeyMiddleware>();

   app.UseAuthorization();
   app.MapControllers();

   app.Run();
   ```

> ℹ **Concept Deep Dive: Where the middleware sits in the pipeline**
>
> The pipeline is a chain of `Func<HttpContext, Task>` calls; each `app.Use*()` adds one link. The `WebApplication` host inserts `UseRouting` automatically just before the endpoint terminus when you call `MapControllers()`, so registering your middleware before `MapControllers()` runs it after routing has matched a path to an endpoint. That gives you room to inspect endpoint metadata later (for example, `[AllowAnonymous]` exemptions — that is exactly how built-in authentication works). Putting the call before `MapControllers()` ensures no controller code runs on a rejected request.
>
> The next exercise's auth — `UseAuthentication()` then `UseAuthorization()` — occupies exactly this slot, between routing and the endpoint.
>
> ⚠ **Common Mistakes**
>
> - Putting `UseMiddleware<ApiKeyMiddleware>()` **after** `MapControllers()`. `MapControllers` is a terminal call that builds the endpoint dataset; middleware after it never runs for those routes.
> - Putting it **before** `UseHttpsRedirection()`. Functionally fine for our case, but pushing it past routing keeps the door open for endpoint-aware exemptions later.
>
> ✓ **Quick check:** `dotnet build` succeeds. The middleware class is referenced exactly once in `Program.cs`. The Swagger middleware lines remain un-gated so `/swagger` keeps working in production.

### **Step 3:** Add the local placeholder to `appsettings.Development.json`

In development you don't need a real secret — a credential on a `localhost` endpoint accessible only from your laptop is no credential at all. Use a clearly-non-secret placeholder so the value is self-documenting; if it leaks the next reviewer sees at a glance that nothing was lost. Putting it in `appsettings.Development.json` (committed, but only loaded when the host is in Development) keeps it out of `appsettings.json` so the production image never carries it.

1. **Open** `appsettings.Development.json`.

2. **Add** the `ApiKey` section alongside the existing `Logging` block:

   > `appsettings.Development.json`

   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "ApiKey": {
       "Value": "local-dev-key"
     }
   }
   ```

> ℹ **Concept Deep Dive: Where dev secrets actually belong**
>
> The strict alternative is `dotnet user-secrets`: a per-developer JSON file outside the repo, identified by a GUID baked into the project file, loaded automatically in Development. That's the right tool when the local credential is real — a connection string to a personal Azure SQL database, an OAuth client secret you can't share. For our placeholder, user-secrets is over-engineering: `local-dev-key` is non-secret by definition since the only thing it grants is access to your own `localhost`. The same question gates both choices — does this value, if read, harm me?
>
> ⚠ **Common Mistakes**
>
> - Putting `ApiKey:Value` in `appsettings.json` (not the Development file). It gets baked into the published image and pushed into the registry, defeating the secret-store mechanism in the next steps.
> - Using a placeholder that looks real (`correcthorsebatterystaple`). Pick something obviously labelled — `local-dev-key` — so future-you doesn't wonder whether it needs rotating.
>
> ✓ **Quick check:** `appsettings.json` has no `ApiKey` section; `appsettings.Development.json` does.

### **Step 4:** Run locally and verify the 401 / 403 / 200 transitions

Three calls demonstrate the three branches: anonymous → 401, wrong key → 403, right key → 200. Doing this against `localhost` before pushing means you fix bugs without burning a deploy cycle.

1. **Run the project** from the `CloudCiApi` directory:

   ```bash
   dotnet run
   ```

   The project starts on `http://localhost:5xxx` (the exact port comes from `launchSettings.json`). Note the URL — call it `$LOCAL` for the rest of this step.

2. **Probe** anonymously (no header). Expect `401`:

   ```bash
   curl -i "$LOCAL/api/quotes"
   ```

   The response should have `HTTP/1.1 401 Unauthorized` on the status line, a `WWW-Authenticate: ApiKey` header in the headers block, and the body `API key is missing.`.

3. **Probe** with a deliberately wrong key. Expect `403`:

   ```bash
   curl -i -H "X-Api-Key: not-the-key" "$LOCAL/api/quotes"
   ```

   Status line `HTTP/1.1 403 Forbidden`, body `API key is invalid.`. **No** `WWW-Authenticate` header — that's only for 401.

4. **Probe** with the placeholder. Expect `200` and the JSON array:

   ```bash
   curl -i -H "X-Api-Key: local-dev-key" "$LOCAL/api/quotes"
   ```

   Status line `HTTP/1.1 200 OK`, body the same JSON array of quotes the previous exercise produced.

5. **Stop** the local server with `Ctrl+C`.

> ✓ **Quick check:** All three responses match the expected status codes. If any one is wrong, fix the middleware before moving on — debugging local code is cheaper than debugging deployed code.

### **Step 5:** Generate the production key locally

Production needs a real key. Generate it on your laptop — `openssl rand -base64 48` produces 48 random bytes (384 bits) of entropy, base64-encoded into a 64-character string. That matches what the next exercise's JWT signing key will use, so the two production secrets in this chapter share one shape.

1. **Generate** the key and capture it in a shell variable:

   ```bash
   API_KEY=$(openssl rand -base64 48)
   echo "$API_KEY"
   ```

   The output is a string like `kQ8j+7Hf4N...lZtw=` — the exact value differs every time. Treat it as sensitive; copy it into a password manager **once** so you can paste it into curl and into Swagger UI later, then forget the terminal ever held it.

2. **Confirm** the variable is set in this shell session — every subsequent command in this exercise reads `$API_KEY`:

   ```bash
   [ -n "$API_KEY" ] && echo "API_KEY is set" || echo "API_KEY is empty"
   ```

   Expected: `API_KEY is set`.

> ℹ **Concept Deep Dive**
>
> 256 bits is the standard floor for shared-secret API keys — the same strength as symmetric TLS keys. 384 bits is comfortable padding above that. The actual exposure surface for an API key isn't brute force; it's leakage (committed to a repo, logged, screenshotted), so bits past ~128 are mostly insurance. Base64 is wire-friendly — no spaces, no quoting issues, safe in HTTP headers.
>
> ⚠ **Common Mistakes**
>
> - Pasting the literal value into the exercise notes, chat, commit message, or `appsettings.json`. The shell variable is the only correct home for it.
> - Generating inside Azure Cloud Shell, whose history retention you don't control. Always generate locally.
>
> ✓ **Quick check:** `echo "$API_KEY" | wc -c` prints `65` (64 base64 chars + newline).

### **Step 6:** Store the key as a Container Apps secret

Same shape of command as the App Insights connection string — only the secret name (`api-key`) and the source variable (`$API_KEY`) change. The secret is stored encrypted in Azure's control plane, injected into the container at process start, and never readable via `az containerapp show`.

1. **Set** the secret. The name must be lowercase and hyphenated:

   ```bash
   az containerapp secret set \
     -g rg-api-week6 \
     -n ca-api-week6 \
     --secrets api-key="$API_KEY"
   ```

2. **Verify** the secret is registered. The CLI lists names but never values — that's by design:

   ```bash
   az containerapp secret list \
     -g rg-api-week6 \
     -n ca-api-week6 \
     -o table
   ```

   You should see a row with name `api-key` (in addition to `appinsights-connstr` from the previous chapter).

> ℹ **Concept Deep Dive: Why a secret store, not an env var?**
>
> Same lesson as the App Insights connection string, different threat. Plain env vars on a Container App are visible in `az containerapp show`, in the Portal's **Containers** blade, and to anyone with `Reader` role — a low-privilege role auditors, on-call engineers, and observability tools are routinely granted. Container Apps secrets sit behind the separate permission `Microsoft.App/containerApps/listSecrets/action`, which Reader does **not** include. The blast radius is smaller.
>
> Compared to App Insights: there, the leaked connection string granted **write** access to telemetry — an attacker pollutes your logs. Here, the API key grants **read** access to every quote. Different threats, same fix.
>
> ⚠ **Common Mistakes**
>
> - Names with uppercase letters or underscores are rejected. `api-key` works; `apiKey`, `api_key`, `ApiKey` all error.
> - Setting the same name twice silently overwrites. There is no version history; rotation is destructive.
>
> ✓ **Quick check:** `az containerapp secret list` shows `api-key` and `appinsights-connstr`.

### **Step 7:** Reference the secret as the `ApiKey__Value` env var

The container reads configuration from environment variables, not from Container Apps secrets directly. The bridge is an env var whose value is the literal string `secretref:api-key` — Container Apps substitutes the actual value at process start. The env var *name* is the load-bearing piece for ASP.NET Core's configuration system.

1. **Update** the Container App so the env var `ApiKey__Value` points at the secret:

   ```bash
   az containerapp update \
     -g rg-api-week6 \
     -n ca-api-week6 \
     --set-env-vars ApiKey__Value=secretref:api-key
   ```

2. **Confirm** the env var is wired correctly:

   ```bash
   az containerapp show \
     -g rg-api-week6 -n ca-api-week6 \
     --query 'properties.template.containers[0].env' -o json
   ```

   Expected: an entry like

   ```json
   {
     "name": "ApiKey__Value",
     "secretRef": "api-key"
   }
   ```

   The literal value of the API key does **not** appear here. That is the whole point.

> ℹ **Concept Deep Dive: Why `__` (double underscore)?**
>
> ASP.NET Core uses `:` as the configuration hierarchy separator — `ApiKey:Value` means key `Value` inside section `ApiKey`. That works in JSON and command-line args, but Linux env vars can't contain colons. The convention .NET adopted is **double underscore as a colon stand-in**: env var `ApiKey__Value` maps to configuration key `ApiKey:Value`.
>
> The middleware in Step 1 reads `configuration["ApiKey:Value"]`. The same read works in development (nested JSON in `appsettings.Development.json`) and in production (the `ApiKey__Value` env var, populated by Container Apps from the `api-key` secret). One key, three sources, one read.
>
> ⚠ **Common Mistakes**
>
> - Naming the env var `ApiKey:Value` instead of `ApiKey__Value`. Linux env vars cannot contain colons.
> - Setting the env var to the literal value (`--set-env-vars ApiKey__Value="$API_KEY"`). That bakes the key into the Container App's revision history, where `az containerapp show` enumerates it forever. Use `secretref:api-key`.
> - Single underscore (`ApiKey_Value`). Treated as a flat key with no hierarchy, so `configuration["ApiKey:Value"]` returns null.
>
> ✓ **Quick check:** The JSON output above shows the env var with `secretRef` (not a `value`).

### **Step 8:** Wire Swagger UI's Authorize button

With the gate live, Swagger UI's "Try it out" stops working — every in-browser call goes anonymous and gets back 401. Swagger has a built-in fix: describe the API key in the OpenAPI document and Swagger UI renders an **Authorize** button where the user pastes the key once, then attaches the `X-Api-Key` header to every subsequent call. The wiring is two extras inside the existing `AddSwaggerGen` registration: `AddSecurityDefinition` describes the scheme, `AddSecurityRequirement` says which operations require it (here: all).

1. **Open** `Program.cs`.

2. **Add** the using at the top:

   > `Program.cs`

   ```csharp
   using Microsoft.OpenApi.Models;
   ```

3. **Replace** the existing `builder.Services.AddSwaggerGen()` line with the configured form:

   > `Program.cs`

   ```csharp
   builder.Services.AddSwaggerGen(options =>
   {
       options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
       {
           Name = "X-Api-Key",
           Type = SecuritySchemeType.ApiKey,
           In = ParameterLocation.Header,
           Description = "API key required to call CloudCiApi. Paste the key generated for your environment."
       });

       options.AddSecurityRequirement(new OpenApiSecurityRequirement
       {
           {
               new OpenApiSecurityScheme
               {
                   Reference = new OpenApiReference
                   {
                       Type = ReferenceType.SecurityScheme,
                       Id = "ApiKey"
                   }
               },
               Array.Empty<string>()
           }
       });
   });
   ```

> ℹ **Concept Deep Dive**
>
> `AddSecurityDefinition` declares a scheme — its name, where it lives (header, query, cookie), and a human-readable description. That alone makes Swagger UI render the **Authorize** button. `AddSecurityRequirement` says **which operations enforce it**. At the global level it applies to every endpoint; that matches our middleware, which is also global. Once the user authorizes, Swagger UI keeps the key in browser memory and attaches `X-Api-Key: <value>` to every request — purely a UI convenience, the same security model is in force regardless of caller.
>
> ⚠ **Common Mistakes**
>
> - Calling `AddSecurityDefinition` without `AddSecurityRequirement`. The button appears, the dialog accepts the key, but no operation actually requires the scheme — Swagger never sends the header. Symptom: every call gets `401` even after authorizing.
> - Using `SecuritySchemeType.Http` with `Scheme = "Bearer"`. That's JWT from the next exercise, not API key.
>
> ✓ **Quick check:** `dotnet build` succeeds. `Program.cs` references both `AddSecurityDefinition` and `AddSecurityRequirement` exactly once each.

### **Step 9:** Push and watch the pipeline deploy

The middleware, configuration binding, and Swagger wiring are still only local. The CI/CD pipeline rebuilds and rolls out a new revision on every push to `main`; the secret + env var from Steps 6 and 7 are already in place, so the new revision picks them up automatically.

1. **Update the smoke test first.** The workflow you wrote in the deployment chapter probes `/api/quotes` to validate the deploy. With the API-key middleware in place, that probe will get back `401` on the very next push and fail the job — even though the deploy itself succeeded. Re-scope the smoke test to a path that's always anonymous, like `/swagger/index.html`:

   > `.github/workflows/ci.yml`

   ```yaml
         - name: Smoke test
           run: |
             FQDN=$(az containerapp show \
               -n ca-api-week6 -g rg-api-week6 \
               --query properties.configuration.ingress.fqdn -o tsv)
             for i in {1..20}; do
               if curl -fsS "https://$FQDN/swagger/index.html" >/dev/null; then
                 echo "Smoke test passed."
                 exit 0
               fi
               sleep 3
             done
             echo "Smoke test failed."
             exit 1
   ```

   Replace just the `curl -fsS` URL — the rest of the step stays the same.

2. **Commit and push:**

   ```bash
   git add Middleware/ApiKeyMiddleware.cs \
           Program.cs \
           appsettings.Development.json \
           .github/workflows/ci.yml
   git commit -m "Add API-key middleware and Swagger Authorize wiring"
   git push
   gh run watch
   ```

   Expected: green workflow within a couple of minutes.

3. **Capture** the FQDN:

   ```bash
   FQDN=$(az containerapp show \
     -g rg-api-week6 -n ca-api-week6 \
     --query properties.configuration.ingress.fqdn -o tsv)

   echo "$FQDN"
   ```

> ✓ **Quick check:** Workflow green. A new revision is `Active` with 100% traffic.

### **Step 10:** Probe the deployed API from the terminal

The same three calls as Step 4, now against the deployed FQDN. The placeholder `local-dev-key` is **not** in production — only the key you generated in Step 5 will pass.

1. **Anonymous** call. Expect `401`:

   ```bash
   curl -i "https://$FQDN/api/quotes"
   ```

   Status `401`, header `WWW-Authenticate: ApiKey`, body `API key is missing.`.

2. **Wrong key** (try the dev placeholder — it's wrong in production):

   ```bash
   curl -i -H "X-Api-Key: local-dev-key" "https://$FQDN/api/quotes"
   ```

   Status `403`, body `API key is invalid.`.

3. **Right key**. Expect `200` and the JSON array:

   ```bash
   curl -i -H "X-Api-Key: $API_KEY" "https://$FQDN/api/quotes"
   ```

   Status `200`, body the JSON array of quotes.

4. **Swagger UI in the browser.** Visit `https://$FQDN/swagger`. Click the **Authorize** button at the top right; paste `$API_KEY` into the dialog; click **Authorize** then **Close**. Click any operation, then **Try it out**, then **Execute**. The "Curl" preview at the bottom of the operation should now include `-H "X-Api-Key: <your-key>"`, and the response body should contain the JSON array.

> ✓ **Quick check:** All three curl calls return the expected status codes. Swagger's Authorize flow accepts the key and subsequent in-browser calls succeed.

### **Step 11:** Test Your Implementation

Walk every signal end to end so you finish knowing the gate works in production and locally and the secret is delivered the right way.

1. **Anonymous → 401 + `WWW-Authenticate`:**

   ```bash
   curl -is "https://$FQDN/api/quotes" | head -n 5
   ```

   Expected: `HTTP/2 401` and `www-authenticate: ApiKey` in the first few lines.

2. **Wrong key → 403:**

   ```bash
   curl -s -o /dev/null -w "%{http_code}\n" \
     -H "X-Api-Key: not-the-key" "https://$FQDN/api/quotes"
   ```

   Expected: `403`.

3. **Right key → 200 + JSON:**

   ```bash
   curl -s -H "X-Api-Key: $API_KEY" "https://$FQDN/api/quotes" | head -c 80
   echo
   ```

   Expected: the start of a JSON array, e.g. `[{"id":1,"author":"...`.

4. **Secret wiring — env var is `secretRef`, not a literal:**

   ```bash
   az containerapp show -g rg-api-week6 -n ca-api-week6 \
     --query 'properties.template.containers[0].env[?name==`ApiKey__Value`]' \
     -o json
   ```

   Expected: an object with `secretRef: "api-key"` and **no** `value` field.

5. **Swagger Authorize flow** — open `https://$FQDN/swagger`, click **Authorize**, paste `$API_KEY`, run any operation. Confirm by hand that the JSON array comes back.

> ✓ **Final verification checklist:**
>
> - ☐ `Middleware/ApiKeyMiddleware.cs` exists and implements the 401/403/200 logic
> - ☐ `Program.cs` calls `app.UseMiddleware<ApiKeyMiddleware>()` between `UseRouting()` and `MapControllers()`
> - ☐ `appsettings.Development.json` has `ApiKey:Value = "local-dev-key"`; `appsettings.json` does not
> - ☐ Container Apps secret `api-key` is set on `ca-api-week6`
> - ☐ Env var `ApiKey__Value` references the secret via `secretRef:`, not a literal value
> - ☐ Anonymous, wrong-key, and right-key probes against the deployed FQDN return 401, 403, 200
> - ☐ Swagger UI's Authorize button accepts the key and subsequent in-browser calls succeed
> - ☐ The pipeline still deploys cleanly on push to `main`

## Common Issues

> **If you encounter problems:**
>
> **Every call returns 401, even with the right key.** The middleware can't read `ApiKey:Value`. Check the env var name is `ApiKey__Value` (double underscore) — `ApiKey_Value` and `ApiKey:Value` both fail silently. Confirm with `az containerapp show ... --query 'properties.template.containers[0].env'`.
>
> **Every call returns 403.** The middleware reads *a* value, but not yours. Either the secret stores a different key than `$API_KEY` (typo, stray newline), or the new revision hasn't rolled out — check `az containerapp revision list -g rg-api-week6 -n ca-api-week6 -o table`.
>
> **Swagger Authorize button accepts the key but every Try-it-out still 401s.** You called `AddSecurityDefinition` but forgot `AddSecurityRequirement`.
>
> **Anonymous calls return 200, not 401.** The middleware isn't running. Either you forgot `app.UseMiddleware<ApiKeyMiddleware>()`, or you placed it after `app.MapControllers()` (terminal for matched endpoints).
>
> **`az containerapp update` fails with "secret not found."** The `secretref:` value must match an existing secret. Run `az containerapp secret list` first.
>
> **Still stuck?** Verify three things in order: the middleware reads `ApiKey:Value` (Step 1), `Program.cs` registers it between `UseRouting` and `MapControllers` (Step 2), and the env var `ApiKey__Value` references the secret via `secretRef:` (Step 7).

## Summary

You added a custom ASP.NET Core middleware that demands an `X-Api-Key` header on every request, returning `401` (with `WWW-Authenticate: ApiKey`) when the header is missing and `403` when the value is wrong. The production key is delivered through the same Container Apps secret + `secretref:` env-var pattern you used for the App Insights connection string. The dev placeholder lives in `appsettings.Development.json`. Swagger UI's Authorize button keeps the in-browser explorer working.

- ✓ Custom middleware with the standard shape — constructor + `InvokeAsync(HttpContext)`
- ✓ Pipeline placement after `UseRouting()` and before `MapControllers()` — the slot real auth will occupy
- ✓ The 401-vs-403 split per RFC 7235 — `WWW-Authenticate: ApiKey` on the 401, none on the 403
- ✓ Production key generated locally with `openssl rand -base64 48`, delivered as a Container Apps secret + `secretref:` env var
- ✓ Env var name `ApiKey__Value` — double underscore mapping to the configuration key `ApiKey:Value`
- ✓ Swagger UI's Authorize button wired via `AddSecurityDefinition` + `AddSecurityRequirement`

> **Key takeaway:** An API key is the cheapest possible gate, and the secret-store pattern that delivers it is the same regardless of what's behind the gate. What changes between credentials is the *threat model*; the mechanism — Container Apps secret + `secretref:` env var, never a plain env var — is the same every time.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Add an `[AllowAnonymous]` equivalent. Inspect `context.GetEndpoint()?.Metadata` in the middleware and skip the check when the endpoint declares itself anonymous — useful for `/health` or `/swagger`.
> - Compare your middleware to the built-in `UseAuthentication` / `UseAuthorization` pair. The framework version separates "who are you?" from "what can you do?", so the 401-vs-403 split falls out naturally; your middleware merges both responsibilities.
> - Constant-time comparison. `string.Equals` short-circuits on the first mismatched byte, leaking timing information. For HTTPS-with-network-jitter the leak is negligible, but the production-grade way is `CryptographicOperations.FixedTimeEquals` over byte arrays.
> - Read about **mutual TLS** (mTLS) — the alternative for machine-to-machine auth. The client presents a certificate; the private key never crosses the wire. Container Apps supports mTLS for ingress.

## Done!

The API has a gate. Anonymous requests bounce with 401, wrong keys bounce with 403, the right key gets through, and Swagger UI still works. The production key lives where a `Reader` on the Container App can't enumerate it; the dev placeholder lives where it doesn't matter that anyone can.

The gate proves only one thing: the caller knows the shared key. It says nothing about *which* caller — every client with the key looks identical to the server, and rotating the key invalidates every client at once. For machine-to-machine traffic that is enough; for per-user attribution it is not. The next exercise replaces the API key with **JWT bearer authentication**: each user gets a signed token carrying their identity, the server validates the signature, and `[Authorize]` does the enforcement.
