+++
title = "Consuming the API from a Browser: CORS and .http Files"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Drive the deployed Quotes API from two clients — a VS Code .http file and a vanilla-JS browser page. Hit the CORS wall the second one exposes, then resolve it with a named CORS policy in ASP.NET Core. Watch the OPTIONS preflight that browsers send before non-simple requests."
weight = 2
draft = false
+++

# Consuming the API from a Browser: CORS and `.http` Files

## Goal

The previous exercise left you with a deployed `CloudCiApi` that responds to `curl` with valid JSON — and that is where most introductory API exercises stop. Real APIs get *consumed*, and the moment you point a browser-based client at one, a new layer of HTTP semantics shows up that `curl` never makes you think about: the **same-origin policy** and the **CORS** handshake browsers use to opt out of it. This exercise walks the API through that gap by adding two clients and resolving the failure the second one provokes.

The first client is a `quotes.http` file — a flat-file format VS Code, Visual Studio, and JetBrains IDEs all parse natively. It replaces the `curl` snippets from the previous exercise with something that lives in the repo and travels with the code. The second client is a single `index.html` page with a few dozen lines of vanilla JavaScript. It uses `fetch` to hit the deployed API, lists the quotes, and offers a form to create new ones. That second client *fails immediately* — and the failure is not a bug but a feature of the browser. Resolving it teaches you what CORS is, why ASP.NET Core ships with it disabled, where the policy must sit in the middleware pipeline, and how the browser's preflight request shows up in DevTools.

> **What you'll learn:**
>
> - How `.http` files work as an in-editor alternative to Postman / Insomnia / Bruno
> - Why the same `GET /api/quotes` request succeeds from `curl` and fails from a browser page
> - What CORS is, what the **same-origin policy** is, and why it is a browser-side rule
> - How to register a named CORS policy in ASP.NET Core with `AddCors` + `UseCors`
> - Why `UseCors` must register before `UseAuthorization` and `MapControllers`
> - When a browser sends an `OPTIONS` **preflight**, and what it checks in the response
> - Why `AllowAnyOrigin()` works for read-only public APIs but breaks the moment credentials are involved

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ The previous exercise complete: `CloudCiApi` is deployed at `https://<fqdn>`, returns four seeded quotes on `GET /api/quotes`, and the OIDC pipeline still ships new revisions on `git push`
> - ✓ A modern browser with DevTools (Chrome, Edge, Firefox, or Safari with the Develop menu enabled)
> - ✓ Either `python3` (preinstalled on macOS and most Linux) or `node` available on your shell — you'll use one of them to serve a static page on `localhost:3000`
> - ✓ VS Code with the **C# Dev Kit** extension installed, or any IDE that natively understands `.http` files (Visual Studio, JetBrains Rider, IntelliJ all do)

## Exercise Steps

### Overview

1. **Add a `quotes.http` file for in-editor requests**
2. **Build a minimal browser client in `web/index.html`**
3. **Serve the page locally and hit the CORS wall**
4. **Add a named CORS policy to the API**
5. **Deploy the CORS fix through the pipeline**
6. **Watch the preflight on POST**
7. **Confirm the policy is restrictive**
8. **Test Your Implementation**

### **Step 1:** Add a `quotes.http` file for in-editor requests

A `.http` file is a plain-text request collection. Each request is separated by `###`, each line follows the wire format you'd send over a socket, and variables are interpolated with `{{name}}`. VS Code (with the C# Dev Kit), Visual Studio 2022+, and JetBrains IDEs all run them inline — a "Send Request" link appears above each request, and the response opens in a side pane. The format is good for the same reason `curl` snippets in the README are good: it is text, it lives next to the code, it shows up in `git diff`, and a future contributor sees the API by reading the repo. Unlike Postman, no account, no cloud sync, and no separate tool is required.

1. **Create** the file at the project root:

   > `quotes.http`

   ```http
   @host = https://<your-fqdn>

   ### List all quotes
   GET {{host}}/api/quotes
   Accept: application/json

   ### Get one by id
   GET {{host}}/api/quotes/1
   Accept: application/json

   ### Get a non-existent id (expect 404)
   GET {{host}}/api/quotes/9999
   Accept: application/json

   ### Create a quote
   POST {{host}}/api/quotes
   Content-Type: application/json

   {
     "author": "Tony Hoare",
     "text": "There are two ways of constructing a software design: One is to make it so simple that there are obviously no deficiencies, and the other is to make it so complicated that there are no obvious deficiencies."
   }

   ### Validation failure (expect 400)
   POST {{host}}/api/quotes
   Content-Type: application/json

   {
     "author": "Anonymous"
   }
   ```

   Substitute the deployed FQDN you captured in the previous exercise for `<your-fqdn>` — the bare hostname, no trailing slash. The `@host` variable at the top of the file is interpolated into every `{{host}}` below, so you change the target in one place when you switch between local and deployed.

2. **Open** the file in VS Code and click the **Send Request** link that appears above the first `GET`. The response panel should show `HTTP/1.1 200 OK`, a `Content-Type: application/json; charset=utf-8` header, and a JSON array of four quotes.

3. **Send** the `POST` request the same way. The response should be `201 Created` with a `Location` header pointing at `/api/Quotes/5`.

> ℹ **Concept Deep Dive: why a `.http` file beats Postman for course work**
>
> The `.http` format is a draft IETF standard (`application/http`) that JetBrains, Microsoft, and the VS Code REST Client extension converged on. The advantage over Postman or Insomnia is that the requests are *files in your repo* — not entries in a vendor-hosted workspace. They diff in pull requests, they survive the contributor leaving the team, and they don't require everyone to have a paid Postman account to share. Bruno is a credible local-first alternative if you want a UI; it stores collections in a similar plain-text format. For everything in this course, a `.http` file is enough.
>
> The `@host = ...` variable is shorthand for "set this when the file loads." More elaborate variants — environment files, prompt-on-send variables, request chaining — exist; the docs at <https://learn.microsoft.com/aspnet/core/test/http-files> cover the surface VS Code supports.
>
> ⚠ **Common Mistakes**
>
> - Leaving the angle brackets in `<your-fqdn>` literally. The request will fail DNS lookup with a confusing error.
> - Pasting the FQDN with `https://` doubled (`https://https://...`). The `@host` variable already includes the scheme.
> - Saving the file inside `web/` (next step's directory). The `.http` file lives at the project root next to `CloudCiApi.csproj`.
>
> ✓ **Quick check:** All five requests run from inside the editor. The `GET /api/quotes` response is a JSON array of four objects. The validation-failure `POST` returns `400` with a `ProblemDetails` body listing `Text` as the missing field.

### **Step 2:** Build a minimal browser client in `web/index.html`

A `.http` file is a *test client* — it sends one request at a time at the developer's command. A browser is a *runtime client* — it sends requests on behalf of an end user, often with credentials, and the security model around it is fundamentally different. To see the difference, you need a page that runs in a browser. Keep it small: one HTML file, vanilla JavaScript, no framework, no build step. The lesson is the network behavior, not the toolchain.

1. **Create** a new folder `web/` next to your project root and add the page:

   > `web/index.html`

   ```html
   <!DOCTYPE html>
   <html lang="en">
   <head>
     <meta charset="utf-8">
     <title>Quotes</title>
     <style>
       body { font-family: system-ui, sans-serif; max-width: 720px;
              margin: 2rem auto; padding: 0 1rem; }
       li { margin: 0.75rem 0; }
       blockquote { margin: 0; font-style: italic; }
       cite { display: block; margin-top: 0.25rem;
              font-size: 0.9em; color: #555; }
       form { margin-top: 2rem; padding-top: 1rem;
              border-top: 1px solid #ddd; }
       label { display: block; margin: 0.5rem 0 0.25rem; }
       input, textarea { width: 100%; padding: 0.4rem; font: inherit; }
       button { margin-top: 1rem; padding: 0.5rem 1rem; }
       .error { color: #b00; }
     </style>
   </head>
   <body>
     <h1>Quotes</h1>
     <button id="load">Reload quotes</button>
     <ul id="quotes"></ul>
     <p id="status" class="error"></p>

     <form id="create-form">
       <h2>Add a quote</h2>
       <label for="author">Author</label>
       <input id="author" name="author" required maxlength="100">
       <label for="text">Text</label>
       <textarea id="text" name="text" required maxlength="500" rows="3"></textarea>
       <button type="submit">Create</button>
     </form>

     <script>
       const API_BASE = "https://<your-fqdn>";

       async function loadQuotes() {
         const status = document.getElementById("status");
         const list = document.getElementById("quotes");
         list.innerHTML = "";
         status.textContent = "";
         try {
           const res = await fetch(`${API_BASE}/api/quotes`);
           if (!res.ok) throw new Error(`HTTP ${res.status}`);
           const quotes = await res.json();
           for (const q of quotes) {
             const li = document.createElement("li");
             li.innerHTML =
               `<blockquote>"${q.text}"<cite>— ${q.author}</cite></blockquote>`;
             list.appendChild(li);
           }
         } catch (err) {
           status.textContent =
             `Failed: ${err.message}. Open DevTools → Console for the real error.`;
         }
       }

       document.getElementById("load").addEventListener("click", loadQuotes);

       document.getElementById("create-form").addEventListener("submit", async (e) => {
         e.preventDefault();
         const status = document.getElementById("status");
         status.textContent = "";
         const body = {
           author: document.getElementById("author").value,
           text: document.getElementById("text").value,
         };
         try {
           const res = await fetch(`${API_BASE}/api/quotes`, {
             method: "POST",
             headers: { "Content-Type": "application/json" },
             body: JSON.stringify(body),
           });
           if (!res.ok) throw new Error(`HTTP ${res.status}`);
           e.target.reset();
           await loadQuotes();
         } catch (err) {
           status.textContent = `Create failed: ${err.message}.`;
         }
       });

       loadQuotes();
     </script>
   </body>
   </html>
   ```

   Substitute the deployed FQDN for `<your-fqdn>` exactly the same way you did in the `.http` file.

> ℹ **Concept Deep Dive: why no framework**
>
> A vanilla HTML+JS page is the smallest unit that exercises the same browser machinery a React/Vue/Svelte app would. Fetch, JSON parsing, DOM updates, form submission — they're all here, just without the build pipeline. For learning where the *protocol* boundaries are, that's the right size. Once you replace this with a real frontend in a future chapter, nothing in the API side of the contract changes.
>
> ✓ **Quick check:** The file is at `web/index.html`. The `API_BASE` constant in the script is your real deployed FQDN. The page is **not yet open** in a browser — that is the next step.

### **Step 3:** Serve the page locally and hit the CORS wall

Opening the file with `file://` would work in a previous browser era, but modern browsers treat the `file://` origin specially — `Origin: null` in the request — and that side-tracks the CORS lesson. Serve the page from a real local origin instead. Either Python's built-in HTTP server or `npx serve` works; use whichever is on your machine. The exact port matters because you'll allow that exact origin in the API in the next step.

1. **Serve** the page from the `web/` directory on port 3000:

   ```bash
   cd web
   python3 -m http.server 3000
   ```

   Or, if you'd rather use Node:

   ```bash
   cd web
   npx serve -l 3000
   ```

   Either command prints something like `Serving HTTP on :: port 3000` and stays in the foreground.

2. **Open** `http://localhost:3000` in your browser. You should see the page, a "Reload quotes" button, and an empty list with a red error message — `Failed: Failed to fetch. Open DevTools → Console for the real error.`

3. **Open** DevTools (`F12` or `Cmd+Option+I`) and switch to the **Console** tab. The real error is there:

   ```text
   Access to fetch at 'https://<your-fqdn>/api/quotes' from origin
   'http://localhost:3000' has been blocked by CORS policy: No
   'Access-Control-Allow-Origin' header is present on the requested resource.
   ```

4. **Switch** to the **Network** tab and click "Reload quotes" again. You'll see a row for `quotes` with status `(failed)` or a red CORS indicator. Click the row, open the **Response Headers** pane, and notice that the response *did* come back — it's a normal `200 OK` from the API's perspective. The browser is the one rejecting it, not the server.

> ℹ **Concept Deep Dive: same-origin policy and CORS in one breath**
>
> The **same-origin policy** is the rule that a script on `https://a.example` cannot read responses from `https://b.example` unless the responding server explicitly opts in. An "origin" is the triple `(scheme, host, port)`: `http://localhost:3000` and `http://localhost:3001` are different origins; `http://localhost:3000` and `https://localhost:3000` are different origins. The policy exists because *cookies and credentials are sent automatically with cross-origin requests*, so without it, any random page on the internet could quietly issue authenticated requests to your bank on your behalf and read the responses.
>
> **CORS** (Cross-Origin Resource Sharing) is the opt-in. The server includes an `Access-Control-Allow-Origin` response header listing the origin(s) it trusts; the browser sees the header and lets the script read the response. Without the header, the browser delivers the response to the network stack but refuses to hand it to JavaScript — that's the asymmetry you saw in DevTools (response received, body unavailable).
>
> Crucially, **CORS is enforced by the browser, not the server**. `curl` and your `.http` file ignore it entirely. That is why the previous exercise's `curl` checks all passed and the moment you load this page it fails — same API, different client, different rules.
>
> ⚠ **Common Mistakes**
>
> - Reading the failure as "the API is down." It isn't — the response came back, the browser simply won't let you read it.
> - Trying to "fix" CORS in JavaScript with `mode: 'no-cors'` on `fetch`. That mode returns an opaque response with no body — useful for fire-and-forget analytics, useless for an API call where you actually want the data.
> - Disabling browser security flags to make development "easier." This works around the lesson rather than learning it, and can't be deployed.
>
> ✓ **Quick check:** The Console tab shows the CORS error mentioning `Access-Control-Allow-Origin`. The Network tab shows the request as failed. The terminal serving `index.html` keeps running — leave it up.

### **Step 4:** Add a named CORS policy to the API

ASP.NET Core ships with CORS *off* by default — you have to opt in deliberately, both at registration time (`AddCors`) and at pipeline time (`UseCors`). The named-policy form is the better starting point even when you only have one policy: it makes the policy's identity explicit, it forces you to name the *intent* (`"LocalDev"` reads better than an anonymous default), and it scales to multi-policy scenarios where different endpoints need different rules.

1. **Edit** `Program.cs`. Add the policy registration alongside the other service registrations, and the middleware call in the pipeline:

   > `Program.cs`

   ```csharp
   using CloudCiApi.Services;

   const string LocalDevCorsPolicy = "LocalDev";

   var builder = WebApplication.CreateBuilder(args);

   builder.Services.AddSingleton<IQuoteStore, InMemoryQuoteStore>();

   builder.Services.AddControllers();

   builder.Services.AddCors(options =>
   {
       options.AddPolicy(LocalDevCorsPolicy, policy =>
       {
           policy.WithOrigins("http://localhost:3000")
                 .AllowAnyHeader()
                 .AllowAnyMethod();
       });
   });

   builder.Services.AddEndpointsApiExplorer();
   builder.Services.AddSwaggerGen();
   builder.Services.AddApplicationInsightsTelemetry();

   var app = builder.Build();

   app.UseSwagger();
   app.UseSwaggerUI();

   app.UseHttpsRedirection();

   // CORS must come before UseAuthorization() and MapControllers().
   app.UseCors(LocalDevCorsPolicy);

   app.UseAuthorization();
   app.MapControllers();

   app.Run();
   ```

   Three things changed: the named-policy constant at the top, the `AddCors(...)` registration, and the `app.UseCors(LocalDevCorsPolicy)` call right before `UseAuthorization()`. Everything else is identical to where the previous exercise left `Program.cs`.

> ℹ **Concept Deep Dive: middleware order is part of the contract**
>
> ASP.NET Core middleware is a pipeline: each component decides whether to handle the request, modify it, or pass it on. `UseCors` only adds CORS headers (and short-circuits preflight `OPTIONS` requests) for what comes *after* it in the pipeline. Put it after `MapControllers()` and the controllers run first, then CORS sees a response that's already been written — too late to add headers. Put it after `UseAuthorization()` and a preflight `OPTIONS` request gets a `401` instead of a CORS response, because the browser is sending preflights *without* credentials and authorization rejects them. The official guidance is: `UseRouting` → `UseCors` → `UseAuthentication` → `UseAuthorization` → `Map*`. The order is enforced by behaviour, not by the framework — get it wrong and the failure is silent.
>
> ⚠ **Common Mistakes**
>
> - Using `AllowAnyOrigin()` with `AllowCredentials()`. Browsers reject the combination — the spec forbids `*` when credentials are in play. If you need credentials, list explicit origins.
> - Calling `UseCors()` *without* an argument when you defined a named policy. There's an unnamed-default form too, but mixing them is a frequent source of "why didn't my policy apply." Always pass the policy name.
> - Forgetting `.AllowAnyHeader()`. The browser's preflight asks "may I send `Content-Type: application/json`?" and without `AllowAnyHeader` (or an explicit `WithHeaders("Content-Type")`) the server refuses, and the actual `POST` never happens.
>
> ✓ **Quick check:** The project builds (`dotnet build`). The named policy `LocalDev` is registered. `app.UseCors(LocalDevCorsPolicy)` sits between `app.UseHttpsRedirection()` and `app.UseAuthorization()`.

### **Step 5:** Deploy the CORS fix through the pipeline

The CORS policy is server-side code — it has to run inside the deployed Container App for the browser to see the header. That means a normal `git push` through the pipeline you set up two exercises ago. There is no separate CORS-management surface in Azure for this case (a real production setup might push the policy onto API Management or Front Door — see *Going Deeper* — but for now, the policy lives in `Program.cs`).

1. **Commit and push** the change:

   ```bash
   git add Program.cs
   git commit -m "Add CORS policy for local browser client"
   git push
   gh run watch
   ```

   Wait for the run to go green — typically two to three minutes for build, push, and Container Apps revision update.

2. **Switch back** to the browser tab serving `http://localhost:3000` and click **Reload quotes**. The list should populate with the four seeded quotes.

3. **Look** at the request in DevTools' Network tab. Click the `quotes` row, open **Response Headers**, and confirm the new headers are present:

   ```text
   access-control-allow-origin: http://localhost:3000
   ```

   That single header is the entire opt-in. The browser saw it, matched it to the page's origin, and handed the response body to JavaScript.

> ℹ **Concept Deep Dive: why echo the origin instead of `*`**
>
> When you call `WithOrigins("http://localhost:3000")`, ASP.NET Core does something subtle: instead of always responding with `Access-Control-Allow-Origin: http://localhost:3000`, it *checks the request's `Origin` header against the allowlist and echoes back the matching origin*. If the request came from a non-allowed origin, no `Access-Control-Allow-Origin` header is added at all and the browser blocks. This is why the response header value is exact, not a wildcard — and why a request from `http://localhost:3001` would get a response with no CORS header at all, not a wrong one.
>
> ✓ **Quick check:** The browser shows the four quotes. The response in Network → Response Headers includes `access-control-allow-origin: http://localhost:3000`. The status of the request is `200 OK` and DevTools no longer flags it.

### **Step 6:** Watch the preflight on POST

A `GET /api/quotes` is what the spec calls a "simple request" — `GET`, no custom headers, default `Content-Type` — and the browser sends it directly and only checks for the CORS header on the response. A `POST` with `Content-Type: application/json`, however, is *non-simple*: before the real request goes out, the browser sends an `OPTIONS` request with `Access-Control-Request-Method` and `Access-Control-Request-Headers` headers asking the server "would you accept this request if I sent it?" That extra round trip is the **preflight**, and it shows up in the Network tab the first time it happens.

1. **Fill** in the form on the page — give it any author and text.

2. **Open** DevTools' Network tab *first*, clear it (the 🚫 button), then submit the form.

3. **Observe** two requests for `quotes`:

   - The first row says **Method: OPTIONS, Status: 204** (No Content). That is the preflight. Click it and look at:

     - **Request headers**: `Access-Control-Request-Method: POST` and `Access-Control-Request-Headers: content-type`
     - **Response headers**: `access-control-allow-origin: http://localhost:3000`, `access-control-allow-methods: POST`, `access-control-allow-headers: content-type`. ASP.NET Core's `.AllowAnyMethod()` and `.AllowAnyHeader()` *echo back the requested values* rather than returning a literal `*` — this is the form the W3C CORS spec requires when credentials may be involved, and the browser accepts it as if it were `*`.

   - The second row says **Method: POST, Status: 201**. That is the real request, sent only because the preflight succeeded. The response is the new `QuoteDto`.

   The browser then follows up with another `GET /api/quotes` (from the script's `loadQuotes()` after a successful create) — that one is simple, no preflight.

> ℹ **Concept Deep Dive: when does a preflight fire?**
>
> The browser sends a preflight when the request is "non-simple" — broadly:
>
> - Methods other than `GET`, `HEAD`, or `POST`
> - `POST` with a `Content-Type` other than `application/x-www-form-urlencoded`, `multipart/form-data`, or `text/plain`
> - Any custom headers (e.g. `Authorization`, `X-Api-Key`)
> - Any request that sets `credentials: "include"` and isn't already simple
>
> `application/json` POSTs *always* preflight. So do every authenticated request the next two exercises will introduce. The browser caches the preflight response for as long as `Access-Control-Max-Age` says (default a few seconds, configurable up to several hours) — that's why you only see the `OPTIONS` once per session unless you force a refresh.
>
> ⚠ **Common Mistakes**
>
> - Wondering why the same `POST` succeeds from the `.http` file and fails from the page. The `.http` runner doesn't preflight — only browsers do.
> - Forgetting the preflight is unauthenticated. A common bug pattern is putting authentication middleware *before* CORS, so the preflight gets `401`, and the browser logs a misleading "CORS error" when the real cause is auth eating the `OPTIONS`.
>
> ✓ **Quick check:** Network tab shows an `OPTIONS` preflight (`204 No Content`) followed by a `POST` (`201 Created`). The new quote appears in the list after the page reloads it.

### **Step 7:** Confirm the policy is restrictive

It's worth seeing the policy *deny* a request, not just allow one — it makes the rule concrete. Stop the server, restart it on a different port, and watch the same page fail.

1. **Stop** the local server (`Ctrl+C` in the terminal running it).

2. **Restart** on port 3001 instead:

   ```bash
   python3 -m http.server 3001
   ```

3. **Open** `http://localhost:3001` in the browser. Click **Reload quotes**. The CORS error returns — same shape as before, except the rejected origin is now `http://localhost:3001`.

4. **Confirm** in the Network tab that the response has *no* `Access-Control-Allow-Origin` header at all. The server saw an `Origin` it doesn't trust and silently omitted the opt-in header. The browser, reading the response and finding no opt-in, blocked the script.

5. **Stop** the server with `Ctrl+C` and start it again on port 3000 — restoring the working state for the final test step.

> ℹ **Concept Deep Dive: where to keep allowed origins**
>
> Hard-coding `"http://localhost:3000"` in `Program.cs` is fine for one-developer-one-frontend, but as soon as you have a staging frontend, a production frontend, and a couple of teammates running on different ports, the list belongs in configuration. The conventional pattern is to put the array in `appsettings.json` under `Cors:AllowedOrigins`, override per environment via `appsettings.Production.json` or env vars, and read it in `Program.cs` with `builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()`. That's a small refactor — see *Going Deeper* — but the named-policy structure already supports it.
>
> ✓ **Quick check:** Loading from `:3001` fails with no `Access-Control-Allow-Origin` header. Loading from `:3000` (after restart) succeeds.

### **Step 8:** Test Your Implementation

End-to-end check across both clients.

1. **From the `.http` file**, run all five requests. Confirm:

   - `GET /api/quotes` → `200` with four (or more, if you POSTed earlier) quotes
   - `GET /api/quotes/1` → `200`
   - `GET /api/quotes/9999` → `404`
   - `POST` with valid body → `201` with `Location` header
   - `POST` with missing `Text` → `400` with `ProblemDetails`

2. **From the browser at `http://localhost:3000`**:

   - Page loads without console errors. List populates with seeded quotes.
   - Submitting the form with valid input adds a new quote and the list refreshes.
   - DevTools Network tab shows an `OPTIONS` preflight before the `POST`, both with `200`/`204`/`201` statuses (no red).
   - Submitting with empty fields is rejected by the browser (HTML5 validation) — not even sent.

3. **Confirm** the response headers on a successful `GET`:

   ```bash
   curl -i -H "Origin: http://localhost:3000" "https://<your-fqdn>/api/quotes" | grep -i access-control
   ```

   Expected: `access-control-allow-origin: http://localhost:3000`. (`curl` doesn't enforce CORS, but you can use it to inspect what the server is sending.)

4. **Confirm** a wrong origin gets no CORS header:

   ```bash
   curl -i -H "Origin: http://evil.example" "https://<your-fqdn>/api/quotes" | grep -i access-control
   ```

   Expected: empty output — no `Access-Control-*` headers in the response.

> ✓ **Success indicators:**
>
> - All five `.http` requests succeed against the deployed FQDN
> - Browser page at `http://localhost:3000` lists quotes and creates new ones without errors
> - DevTools Network tab shows the `OPTIONS` preflight before each `POST`
> - `curl` with the right `Origin` echoes it back; `curl` with a wrong `Origin` omits the CORS header
>
> ✓ **Final verification checklist:**
>
> - ☐ `quotes.http` exists at the project root with the deployed FQDN as `@host`
> - ☐ `web/index.html` exists with the deployed FQDN as `API_BASE`
> - ☐ `Program.cs` registers the `LocalDev` named policy with `WithOrigins("http://localhost:3000")`, `AllowAnyHeader`, `AllowAnyMethod`
> - ☐ `app.UseCors(LocalDevCorsPolicy)` is registered between `UseHttpsRedirection` and `UseAuthorization`
> - ☐ The deployed Container App revision serves the CORS headers (verified via `curl -i -H "Origin: ..."`)

## Common Issues

> **If you encounter problems:**
>
> **Page loads but says "Failed to fetch":** Open DevTools → Console. The real error is there. Most often it's the CORS error before the policy is deployed, or a typo in `API_BASE`.
>
> **Pipeline run is green but the browser still fails:** Container Apps revisions take a few seconds after a successful deploy to start serving. Hard-refresh (`Cmd+Shift+R` / `Ctrl+Shift+R`) to bypass the browser's preflight cache.
>
> **`OPTIONS` returns `401` instead of `204`:** `app.UseCors()` is registered after `app.UseAuthorization()`. Authorization is rejecting the unauthenticated preflight. Move `UseCors` *before* `UseAuthorization`.
>
> **`POST` from the form fails but `GET` works:** `.AllowAnyHeader()` is missing from the policy. The preflight asks for `Content-Type: application/json` and is told no.
>
> **Console shows `The 'Access-Control-Allow-Origin' header contains the invalid value '*' when the request's credentials mode is 'include'`:** You added `AllowCredentials()` and `AllowAnyOrigin()` together. Replace `AllowAnyOrigin()` with `WithOrigins(...)`. Browsers refuse the wildcard with credentials.
>
> **Response headers show the right CORS headers in `curl` but the browser still blocks:** The browser cached an earlier preflight with a different policy. Open DevTools → Network → check **Disable cache** while DevTools is open, then reload.
>
> **Still stuck?** The MDN page <https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS> is the canonical reference. The ASP.NET Core docs at <https://learn.microsoft.com/aspnet/core/security/cors> cover the framework-side configuration in depth.

## Summary

You now have two clients exercising the same deployed API: a `.http` file that lives in the repo and travels with the code, and a browser page served from `http://localhost:3000` that talks to the deployed FQDN over HTTPS. The browser failed first; resolving the failure is what taught you what CORS actually is. The named policy in `Program.cs` allows exactly one origin, the deployed pipeline carries the change to Azure, and DevTools' Network tab shows the preflight handshake that browsers send before any non-simple cross-origin request.

- ✓ A `.http` file is the right shape for course-tracked, in-editor request examples — no Postman account, no separate tool, plain text in `git`
- ✓ The same-origin policy is *browser*-side — `curl` and `.http` clients ignore CORS entirely; that asymmetry is why a deployed API can pass `curl` checks and still fail from a page
- ✓ ASP.NET Core CORS is two calls: `AddCors(...)` to register, `UseCors(name)` to attach to the pipeline; order matters and `UseCors` must come before `UseAuthorization`
- ✓ `application/json` POSTs trigger an `OPTIONS` preflight; `AllowAnyHeader()` is what makes the `Content-Type` header acceptable
- ✓ A named policy with explicit origins is the right default — `AllowAnyOrigin()` is fine for read-only public APIs but breaks the moment credentials are added

> **Key takeaway:** CORS is not a security feature your *server* enforces; it is a security feature your *browser* enforces, with the server providing opt-in headers. Get that mental model right and CORS stops being mysterious. The header is data; the policy is code; the enforcement happens far away on the client.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - **Move the allowed-origins list into configuration.** Put `Cors:AllowedOrigins` in `appsettings.json` as a string array, override per-environment, and read it in `Program.cs` with `builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()`. The named-policy structure already supports it — only `WithOrigins(...)` changes.
> - **Read the OPTIONS preflight raw.** Re-run `curl` with `-X OPTIONS -H "Origin: http://localhost:3000" -H "Access-Control-Request-Method: POST" -H "Access-Control-Request-Headers: content-type" -i "https://<fqdn>/api/quotes"` and watch the `204` response carry every `Access-Control-Allow-*` header. Compare with the same flags but `Origin: http://evil.example`.
> - **Tune `Access-Control-Max-Age`.** Add `.SetPreflightMaxAge(TimeSpan.FromHours(1))` to the policy and reload — the preflight now happens once per hour instead of per session. Trade-off: faster requests vs. slower propagation of policy changes.
> - **Move the policy to the gateway.** In production setups, CORS often lives on Azure API Management, Front Door, or an ingress controller, not in the application. The trade-off is *configuration vs. code*: gateway-side policies change without redeploy, but the application loses the ability to vary CORS by route. Read the API Management CORS policy docs for one example.
> - **Try the Static Web Apps "linked backend" pattern.** A future chapter will deploy the frontend to Azure Static Web Apps, link it to the Container App, and route API calls through `/api/*` on the SWA hostname — eliminating the cross-origin call entirely. The CORS work in this exercise is what you'd otherwise need; the linked-backend pattern is one alternative.

## Done!

The deployed API now plays nicely with a browser-based client running on your laptop, and you have seen, with the Network tab open, exactly what the browser does and why. Two exercises from now, the same API will require an `Authorization: Bearer ...` header on every request — and that header makes every request "non-simple," which means *every* request will preflight, which means CORS and auth will be debugged together. Doing the CORS work now, with the gate still wide open, makes that next debugging session much shorter.

The next exercise closes the open-to-the-world gap with a shared API key — the simplest gate that's still better than nothing. The exercise after that replaces the API key with JWT bearer tokens and a `[Authorize]` attribute, and tears down the resource group when you're finished.
