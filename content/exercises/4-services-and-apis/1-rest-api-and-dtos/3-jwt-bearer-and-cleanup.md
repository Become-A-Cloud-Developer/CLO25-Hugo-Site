+++
title = "JWT Bearer Authentication and Cleanup"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Replace the API key with JWT bearer authentication. Add a tokens endpoint that issues signed JWTs against an in-memory user list, mark the quotes endpoints with [Authorize], and rewire Swagger UI's Authorize button to accept Bearer tokens. Tear down the resource group and the Entra OIDC app to finish the chapter cleanly."
weight = 3
draft = false
+++

# JWT Bearer Authentication and Cleanup

## Goal

In the previous exercise you put an API key in front of `CloudCiApi` and gated the quotes endpoints with a small middleware that compared a header against a Container Apps secret. That solved one problem — anonymous callers off the public internet can no longer hit the API — but it left a real gap: the server has no idea *who* is calling. Every request that presents the right key looks identical to every other. There is no per-user identity, no role information, no way to write code that says "this user can read but not write."

In this exercise you'll replace the API key with **JWT bearer authentication**. You'll add a `TokensController` with a `POST /api/tokens/login` action that validates a username and password against an in-memory user list and returns a signed JSON Web Token. The `QuotesController` will be marked with `[Authorize]` so that the framework's authentication middleware enforces the new identity model. Swagger UI's **Authorize** button will be rewired from the API-key scheme to a Bearer scheme so you can paste tokens directly into the UI and try the endpoints from the browser.

This is the last exercise of the chapter. Once the JWT flow works end to end on the deployed Container App, the final step tears down the lab — `rg-api-week6`, the Entra app registration created for OIDC federation, and (optionally) the stale GitHub repo secrets — so that nothing you provisioned this week is left running.

> **What you'll learn:**
>
> - What a JWT actually is — three base64url segments, the signature as the integrity check, the payload as readable-but-tamper-evident claims
> - How to configure `JwtBearerDefaults.AuthenticationScheme` in ASP.NET Core with `TokenValidationParameters` for issuer, audience, lifetime, and signing-key validation
> - How to mint a signed token with `JwtSecurityTokenHandler` using `SymmetricSecurityKey` and `HmacSha256`, with claims for `sub`, `name`, and an optional `role`
> - When symmetric signing (HS256) is acceptable and when you need asymmetric signing (RS256/ES256) instead
> - How to deliver the signing key to Container Apps as a secret and reference it via `secretref:` — the same pattern as the App Insights connection string in the previous chapter
> - How to rewire Swagger UI's Authorize button from the API-key scheme to the Bearer scheme
> - How to tear down both the resource group and the tenant-level Entra app registration so the lab leaves no orphans behind

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ The previous exercise complete: `CloudCiApi` deployed to the Container App `ca-api-week6` in `rg-api-week6`, with `QuotesController` returning JSON DTOs and the API-key middleware rejecting requests that don't carry the `X-Api-Key` header
> - ✓ The CI/CD pipeline from earlier in the course still wired up — `git push` to `main` deploys to the Container App via OIDC federation
> - ✓ The Azure CLI signed in (`az account show` returns your subscription)
> - ✓ A local clone of the `CloudCiApi` ASP.NET Core Web API project where you can edit code and push to `main`
> - ✓ `openssl` available on your machine (default on macOS and Linux; on Windows use Git Bash or WSL)

## Exercise Steps

### Overview

1. **Add the JWT bearer NuGet package**
2. **Configure JWT validation in `Program.cs`**
3. **Add development-mode JWT settings to `appsettings.Development.json`**
4. **Remove the API-key middleware**
5. **Add the in-memory user store**
6. **Implement `TokensController` with the login action**
7. **Mark `QuotesController` with `[Authorize]`**
8. **Generate the production signing key locally**
9. **Inject the signing key as a Container Apps secret**
10. **Rewire Swagger UI's Authorize button for Bearer**
11. **Push and verify the deployed API**
12. **Test Your Implementation**
13. **Tear down the cloud resources**

### **Step 1:** Add the JWT bearer NuGet package

The `Microsoft.AspNetCore.Authentication.JwtBearer` package is the official ASP.NET Core handler for incoming JWTs. It plugs into the same authentication middleware you'd use for cookies or any other scheme — once registered, marking a controller with `[Authorize]` causes the framework to look for an `Authorization: Bearer <token>` header, validate the token against the parameters you configure, and materialize a `ClaimsPrincipal` from the claims inside.

1. **Open** a terminal in the project root (the directory containing `CloudCiApi.csproj`).

2. **Add** the package:

   ```bash
   dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
   ```

3. **Inspect** the resulting `.csproj`. The `ItemGroup` for package references should now contain the new entry alongside whatever was there before:

   > `CloudCiApi.csproj`

   ```xml
   <ItemGroup>
     <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
     <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
   </ItemGroup>
   ```

   The exact version pinned by `dotnet add` will be the latest at the time you run the command.

> ℹ **Concept Deep Dive**
>
> This single package is everything you need to *validate* incoming JWTs. To *issue* JWTs from the `TokensController` you don't need an extra package — `System.IdentityModel.Tokens.Jwt` and `Microsoft.IdentityModel.Tokens` are pulled in transitively by the bearer package, and they expose `JwtSecurityTokenHandler`, `SymmetricSecurityKey`, and `SigningCredentials`. You'll see those types used in Step 6.
>
> ✓ **Quick check:** `dotnet build` completes with no errors and no new warnings.

### **Step 2:** Configure JWT validation in `Program.cs`

Registering the JWT bearer handler is two API calls: one to add the authentication services, one to configure the validation parameters that incoming tokens must satisfy. The validation parameters are the load-bearing piece — they tell the middleware exactly what to check on every request, and *not* checking any of them is a security bug. You want all four checks (`ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey`) on for any production-shaped configuration.

1. **Open** `Program.cs` in the project root.

2. **Add** the `using` statements at the top of the file:

   > `Program.cs`

   ```csharp
   using System.Text;
   using Microsoft.AspNetCore.Authentication.JwtBearer;
   using Microsoft.IdentityModel.Tokens;
   ```

3. **Register** the bearer scheme right after `AddControllers()` (or wherever your existing service registrations live). The block below is what the rest of this exercise assumes — copy it as-is:

   > `Program.cs`

   ```csharp
   builder.Services
       .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options =>
       {
           options.TokenValidationParameters = new TokenValidationParameters
           {
               ValidateIssuer = true,
               ValidateAudience = true,
               ValidateLifetime = true,
               ValidateIssuerSigningKey = true,
               ValidIssuer = builder.Configuration["Jwt:Issuer"],
               ValidAudience = builder.Configuration["Jwt:Audience"],
               IssuerSigningKey = new SymmetricSecurityKey(
                   Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!))
           };
       });

   builder.Services.AddAuthorization();
   ```

4. **Add** the authentication and authorization middleware after `app.UseRouting()` (if present) and before `app.MapControllers()`:

   > `Program.cs`

   ```csharp
   app.UseAuthentication();
   app.UseAuthorization();

   app.MapControllers();
   ```

> ℹ **Concept Deep Dive**
>
> The four validation flags answer four different questions. `ValidateIssuer` asks "did this token come from the issuer I trust?" — it compares the `iss` claim in the payload against `ValidIssuer`. `ValidateAudience` asks "is this token meant for me?" — it compares `aud` against `ValidAudience`. `ValidateLifetime` asks "is this token still in its `nbf`/`exp` window?" `ValidateIssuerSigningKey` asks "was this token signed by a key I trust?" — it recomputes the signature with `IssuerSigningKey` and compares.
>
> Turning any one of those off opens a hole. `ValidateAudience = false` lets a token issued for a different service be replayed against yours. `ValidateLifetime = false` makes expired tokens valid forever. The defaults in the SDK have most flags on, but listing them explicitly is the right habit — it documents the security posture in the same file where the app is configured.
>
> ⚠ **Common Mistakes**
>
> - Putting `app.UseAuthentication()` *after* `app.UseAuthorization()` — every authenticated request returns 401 with no obvious diagnostic, because authorization runs against an anonymous principal.
> - Forgetting `app.UseAuthentication()` entirely — same symptom: every request looks anonymous to `[Authorize]`.
> - Hard-coding the signing key as a string literal in `Program.cs`. It works locally and bakes the key into the image. The whole next chapter on secret management exists because this is wrong; do not start it wrong here.
>
> ✓ **Quick check:** `dotnet build` succeeds. The app won't start yet without the configuration values from Step 3.

### **Step 3:** Add development-mode JWT settings to `appsettings.Development.json`

The validation parameters in Step 2 read three configuration keys: `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:SigningKey`. For local development you want these in `appsettings.Development.json` so a fresh clone of the repo runs without any extra environment-variable juggling. The dev signing key is *not* the production key — it's a long, clearly-marked dev string, regenerated whenever it leaks into a screenshot.

1. **Open** `appsettings.Development.json` (create it next to `appsettings.json` if it doesn't exist).

2. **Add** the `Jwt` section at the top level of the JSON, alongside `Logging` and any other settings already present:

   > `appsettings.Development.json`

   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "Jwt": {
       "Issuer": "cloudci-api-dev",
       "Audience": "cloudci-api-clients-dev",
       "SigningKey": "DEV-ONLY-NOT-FOR-PROD-3f8a2c7e9b1d4e6f0a5b8c2d7e1f4a9c8e3b2a7d5c4f6e8a1b9d0c2e3f5a7b8c"
     }
   }
   ```

3. **Confirm** the file is gitignored only in the way *production* secrets would be. `appsettings.Development.json` is intentionally *committed* — every developer who clones the repo gets the same dev-mode key. That's safe because it's clearly marked as dev-only and grants nothing in production.

> ℹ **Concept Deep Dive**
>
> The signing key has to be at least 256 bits long for HS256 to accept it. The handler measures the byte length of the UTF-8 representation: the string above is 87 ASCII characters, so 87 bytes — 696 bits, comfortably above the 256-bit minimum. For the production key in Step 8 you'll use `openssl rand -base64 48`, which produces 64 base64 characters — 64 bytes — 512 bits. ASCII-printable padding strings like `"super-secret-key"` are short, fail the length check at first validation, and produce the famously cryptic `IDX10720` error.
>
> ⚠ **Common Mistakes**
>
> - Using the same signing key in dev and production. The whole point of separating them is that one leaking does not compromise the other.
> - Leaving the dev key short and discovering at first request that HS256 throws `IDX10720`. Use a 64-character (or longer) string.
>
> ✓ **Quick check:** `dotnet run` starts the app without the `IDX10720` error. The startup logs say `Now listening on: http://localhost:<port>`.

### **Step 4:** Remove the API-key middleware

The cleanest story for this exercise is *replace*, not *layer*. The API key answered "is this a known client?" The JWT answers "which user is this, and what can they do?" Layering both adds operational overhead — the same secret has to be rotated twice, the failure modes interact in surprising ways — without adding security, because any caller already authenticated by JWT didn't need the API key. Pull the middleware out cleanly.

1. **Open** `Program.cs`.

2. **Find** the line that registers the API-key middleware from the previous exercise. It looks like:

   > `Program.cs`

   ```csharp
   app.UseMiddleware<ApiKeyMiddleware>();
   ```

3. **Delete** that line. The pipeline now relies on `app.UseAuthentication()` and `app.UseAuthorization()` from Step 2 to gate the controllers.

4. **Delete** the middleware class file itself — typically `Middleware/ApiKeyMiddleware.cs`. If you leave the file around, it compiles into the assembly as dead code; nothing references it, but it's noise.

5. **Remove** the `ApiKey` configuration key from `appsettings.Development.json` and from any `Jwt:`-adjacent settings — it's no longer read.

6. **Plan** to remove the production-side leftovers. The Container Apps secret `api-key` and the env var that referenced it are still on the running app. You'll clean those up implicitly when you delete the resource group in Step 13. If you want to remove them sooner, run:

   ```bash
   az containerapp update \
     -g rg-api-week6 -n ca-api-week6 \
     --remove-env-vars ApiKey__Value

   az containerapp secret remove \
     -g rg-api-week6 -n ca-api-week6 \
     --secret-names api-key
   ```

> ℹ **Concept Deep Dive**
>
> Why replace rather than layer. The textbook reason to layer multiple authentication factors is when each factor proves something the others can't — a password proves knowledge, a hardware token proves possession, a fingerprint proves identity. JWT and API key both answer the same kind of question (does this caller hold the right shared secret?), so stacking them just multiplies the operational surface. Once you have per-user JWTs you have strictly more information than the API key gave you, and the API key becomes redundant.
>
> ⚠ **Common Mistakes**
>
> - Forgetting the `app.UseMiddleware<ApiKeyMiddleware>()` line. The middleware still runs at startup, the API still expects the `X-Api-Key` header, and your JWT-authenticated requests get rejected for missing the key.
> - Deleting the middleware *file* but leaving the registration in `Program.cs`. The build fails immediately with "type or namespace name 'ApiKeyMiddleware' not found." The build break tells you to delete both halves.
>
> ✓ **Quick check:** The project builds. `app.UseMiddleware<ApiKeyMiddleware>()` no longer appears in `Program.cs` and the file `Middleware/ApiKeyMiddleware.cs` is gone.

### **Step 5:** Add the in-memory user store

Real applications store users in a database with hashed passwords and proper audit trails. For this lab the focus is the JWT mechanics, so a hard-coded list of two or three users keeps the moving parts small. The store is an interface (`IUserStore`) so the controller depends on an abstraction; the implementation (`InMemoryUserStore`) holds the list and exposes a `Validate(username, password)` method that returns the user when credentials match and `null` otherwise.

1. **Open** the existing `Models/` folder created in the first exercise.

2. **Add** a new file:

   > `Models/User.cs`

   ```csharp
   namespace CloudCiApi.Models;

   public record User(string Username, string Password, string? Role = null);
   ```

3. **Open** the existing `Services/` folder created in the first exercise.

4. **Add** the interface:

   > `Services/IUserStore.cs`

   ```csharp
   using CloudCiApi.Models;

   namespace CloudCiApi.Services;

   public interface IUserStore
   {
       // Returns the user when credentials match, null otherwise.
       User? Validate(string username, string password);
   }
   ```

5. **Add** the implementation:

   > `Services/InMemoryUserStore.cs`

   ```csharp
   using CloudCiApi.Models;

   namespace CloudCiApi.Services;

   public class InMemoryUserStore : IUserStore
   {
       // Hard-coded for this lab. Real apps store hashed passwords.
       private static readonly List<User> Users =
       [
           new("alice", "alice123", "admin"),
           new("bob",   "bob456",   "reader"),
       ];

       public User? Validate(string username, string password) =>
           Users.FirstOrDefault(u =>
               string.Equals(u.Username, username, StringComparison.Ordinal) &&
               string.Equals(u.Password, password, StringComparison.Ordinal));
   }
   ```

6. **Register** the store as a singleton in `Program.cs`, alongside the other service registrations from Step 2:

   > `Program.cs`

   ```csharp
   builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
   ```

> ℹ **Concept Deep Dive**
>
> Singleton lifetime fits because the user list is read-only and shared across all requests. If the store talked to a database we'd need scoped, because the underlying `DbContext` is scoped. The lifetime question is always "does the underlying state need to be per-request?" — for a static list, no.
>
> ⚠ **Common Mistakes**
>
> - Hashing passwords with a homegrown function. If you ever need real password handling, use ASP.NET Core Identity or `PasswordHasher<T>`. Do not roll your own.
> - Comparing passwords with `==` and depending on it being constant-time. `string.Equals` is also not constant-time. For a real app use a constant-time comparison; for this lab, the credentials are public anyway, so the timing-attack surface doesn't matter.
>
> ✓ **Quick check:** The project builds. `Validate("alice", "alice123")` returns the alice record; `Validate("alice", "wrong")` returns null.

### **Step 6:** Implement `TokensController` with the login action

This is where the API issues the JWT. The action takes a username and password, asks the user store to validate them, and on success builds a `JwtSecurityToken` with claims, an issuer, an audience, an expiry, and a signature. `JwtSecurityTokenHandler.WriteToken(...)` serializes that object into the three-segment compact form that the client will paste into the `Authorization: Bearer <...>` header.

1. **Create** a new file:

   > `Controllers/TokensController.cs`

   ```csharp
   using System.IdentityModel.Tokens.Jwt;
   using System.Security.Claims;
   using System.Text;
   using CloudCiApi.Services;
   using Microsoft.AspNetCore.Mvc;
   using Microsoft.IdentityModel.Tokens;

   namespace CloudCiApi.Controllers;

   [ApiController]
   [Route("api/[controller]")]
   public class TokensController : ControllerBase
   {
       private readonly IUserStore _users;
       private readonly IConfiguration _config;

       public TokensController(IUserStore users, IConfiguration config)
       {
           _users = users;
           _config = config;
       }

       public record LoginRequest(string Username, string Password);

       [HttpPost("login")]
       public IActionResult Login([FromBody] LoginRequest request)
       {
           var user = _users.Validate(request.Username, request.Password);
           if (user is null)
           {
               return Unauthorized();
           }

           var key = new SymmetricSecurityKey(
               Encoding.UTF8.GetBytes(_config["Jwt:SigningKey"]!));
           var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

           var claims = new List<Claim>
           {
               new(JwtRegisteredClaimNames.Sub, user.Username),
               new(ClaimTypes.Name, user.Username),
           };
           if (!string.IsNullOrEmpty(user.Role))
           {
               claims.Add(new Claim(ClaimTypes.Role, user.Role));
           }

           var expires = DateTime.UtcNow.AddHours(1);

           var token = new JwtSecurityToken(
               issuer: _config["Jwt:Issuer"],
               audience: _config["Jwt:Audience"],
               claims: claims,
               expires: expires,
               signingCredentials: creds);

           var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

           return Ok(new { token = tokenString, expiresAt = expires });
       }
   }
   ```

2. **Confirm** the route is `POST /api/tokens/login`. The `[ApiController]` attribute gives you automatic model-state validation and binds `LoginRequest` from the JSON body without further ceremony.

> ℹ **Concept Deep Dive: What a JWT actually is**
>
> A JWT is three base64url-encoded segments separated by dots: `header.payload.signature`.
>
> The **header** is a tiny JSON object — `{"alg":"HS256","typ":"JWT"}` — base64url-encoded. It says how the signature was produced.
>
> The **payload** is another JSON object — `{"sub":"alice","name":"alice","role":"admin","iss":"cloudci-api-dev","aud":"cloudci-api-clients-dev","exp":1714312345,"nbf":1714308745,"iat":1714308745}` — base64url-encoded. The `iss` (issuer), `aud` (audience), `exp` (expiry as Unix seconds), `nbf` (not-before), and `iat` (issued-at) claims are the standard ones the validator checks; `sub`, `name`, and `role` are the per-user identity. Anyone can decode the payload — paste a token into <https://jwt.io> and read it. There is no secrecy in a JWT.
>
> The **signature** is `HMAC-SHA256(base64url(header) + "." + base64url(payload), key)` — the entire signed string is the first two segments concatenated with the dot, hashed with the shared key. Anyone holding the key can produce the signature; anyone holding the key can also forge tokens. The signature is the *integrity* check, not a secrecy mechanism. The token is tamper-evident: change one byte of the payload and the signature stops matching.
>
> ⚠ **Common Mistakes**
>
> - Putting sensitive data in the payload "because the signature protects it." It doesn't. The payload is plaintext to anyone who sees the token. Put identifiers there, not secrets.
> - Returning the raw token in plain log lines at issuance time. The token grants access for its full lifetime; logs are often shipped to systems with broader read access than the API itself.
> - Forgetting to set `expires` and shipping a token that is valid forever. The validation parameters with `ValidateLifetime = true` will reject it because it has no `exp` — but the symptom is "every login returns a 401-on-next-request" and is hard to debug.
>
> ✓ **Quick check:** `dotnet run`, then `curl -X POST http://localhost:<port>/api/tokens/login -H 'Content-Type: application/json' -d '{"username":"alice","password":"alice123"}'` returns `{"token":"eyJ...","expiresAt":"2026-..."}`.

### **Step 7:** Mark `QuotesController` with `[Authorize]`

`[Authorize]` is the framework-level switch that says "this action requires a successfully-authenticated principal." With the bearer scheme registered as the default, the middleware looks for `Authorization: Bearer <token>` on every request, validates the token, and either materializes a `ClaimsPrincipal` from its claims or short-circuits the pipeline with a 401.

1. **Open** `Controllers/QuotesController.cs`.

2. **Add** the using:

   > `Controllers/QuotesController.cs`

   ```csharp
   using Microsoft.AspNetCore.Authorization;
   ```

3. **Add** the attribute at class level:

   > `Controllers/QuotesController.cs`

   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   [Authorize]
   public class QuotesController : ControllerBase
   {
       // ...existing code from the previous exercise...
   }
   ```

4. **Leave** `TokensController` without `[Authorize]`. The login endpoint must be reachable anonymously; a caller can't authenticate before they have a token. There's no `[AllowAnonymous]` needed here because the parent class isn't `[Authorize]` — only `QuotesController` is.

> ℹ **Concept Deep Dive: Why symmetric signing is acceptable here, and when it isn't**
>
> HS256 — HMAC-SHA256 — uses one secret key for both signing and validating. The same process that issues the token also validates it. That works in this exercise because there is exactly one application: `CloudCiApi` mints the token, `CloudCiApi` validates the token, and the key never has to leave the process boundary.
>
> The moment a separate identity provider issues tokens consumed by multiple validators — a corporate IdP issuing tokens for fifteen microservices, say — symmetric signing falls apart. To validate the token, every microservice would need the signing key, and any one of them could then forge a token impersonating any user. You move to **asymmetric signing** (RS256 or ES256), where the IdP holds a private key and validators only get the public key. Public keys verify signatures but cannot produce them, so a compromised validator cannot mint forged tokens.
>
> Practical heuristic: same process issues *and* validates → HS256 is fine. Multiple validators of tokens from a separate issuer → RS256 with the public key fetched from the IdP's JWKS endpoint.
>
> ✓ **Quick check:** Build succeeds. Hitting `GET http://localhost:<port>/api/quotes` without an `Authorization` header returns `401 Unauthorized`.

### **Step 8:** Generate the production signing key locally

The dev signing key in `appsettings.Development.json` is committed and known to anyone with read access to the repo. The production key has to be different, generated freshly, and never written to a file the source control system tracks. `openssl rand -base64 48` is the canonical one-liner — 48 random bytes encoded as 64 base64 characters, comfortably above the 256-bit minimum for HS256.

1. **Generate** the key locally:

   ```bash
   openssl rand -base64 48
   ```

   The output looks like `Yk0tH3gB...QqQ==` — 64 characters. Copy this string into a temporary shell variable, do **not** paste it into a file the repository tracks:

   ```bash
   PROD_KEY=$(openssl rand -base64 48)
   echo "$PROD_KEY" | wc -c   # 65 (64 chars + newline)
   ```

2. **Treat** the value as sensitive. Anyone holding it can mint tokens that your production API will accept as authentic. Do not paste it into chat, screenshots, or commit messages.

> ℹ **Concept Deep Dive: Token lifetime trade-offs**
>
> The token issued in Step 6 lives for one hour. That choice is a compromise. Short lifetimes (minutes) are more secure: a stolen token expires before an attacker can chain many calls. The price is load — clients re-authenticate often, the tokens endpoint takes more traffic, every request flow has to handle the "token expired, re-login" branch.
>
> Long lifetimes (hours, days) are convenient and cheap on the issuer side, but they extend the compromise window. A token leaked in a screenshot or a log file is valid for as long as its `exp` says it is, and there is no equivalent of "log out all sessions" — the validator has no idea which tokens it has issued, only which ones it would currently accept.
>
> The production pattern that combines both is **short access tokens (minutes) plus longer refresh tokens (days, single-use, server-tracked)**. The access token gates API calls; the refresh token is exchanged at the IdP for a fresh access token. A leaked access token is short-lived; a leaked refresh token is invalidated server-side the moment its single use is consumed. For this lab, one hour is a reasonable middle ground that doesn't introduce the operational complexity of refresh tokens while still showing the lifetime concept.
>
> ⚠ **Common Mistakes**
>
> - Generating the key with a non-cryptographic source (`echo "myapp$(date)"`). HS256 keys must be unpredictable. `openssl rand` reads from the OS RNG.
> - Generating the key shorter than 256 bits. `openssl rand -base64 24` (24 bytes = 192 bits) fails HS256's minimum-key-size check; `IDX10720` is the symptom.
>
> ✓ **Quick check:** `echo -n "$PROD_KEY" | wc -c` reports 64 (or close — base64 padding can vary). The value is in your shell, not in any file the repo tracks.

### **Step 9:** Inject the signing key as a Container Apps secret

The signing key has to reach the running container at startup. The wrong way to deliver it is as a plain environment variable on the Container App — that bakes the key into the revision history, where anyone with `Reader` role can read it via `az containerapp show`. The right way is the same pattern as the App Insights connection string in the previous chapter: store the value as a Container Apps **secret**, then reference it from an environment variable using the `secretref:` prefix. The runtime substitutes the actual value when the container starts.

The issuer and audience are not sensitive — they're values the token-validation logic compares incoming claims against, and they don't grant anything by themselves. You can set them as plain env vars in the same `update` call.

1. **Set** the signing key as a secret on the Container App:

   ```bash
   az containerapp secret set \
     -g rg-api-week6 -n ca-api-week6 \
     --secrets jwt-signing-key="$PROD_KEY"
   ```

2. **Wire** the secret to the env var the SDK reads, and set the issuer and audience env vars in the same call. ASP.NET Core maps double underscores in env vars to colons in the configuration tree, so `Jwt__SigningKey` becomes `Jwt:SigningKey`:

   ```bash
   az containerapp update \
     -g rg-api-week6 -n ca-api-week6 \
     --set-env-vars \
       Jwt__SigningKey=secretref:jwt-signing-key \
       Jwt__Issuer=cloudci-api-prod \
       Jwt__Audience=cloudci-api-clients-prod
   ```

3. **Confirm** the env vars are wired correctly:

   ```bash
   az containerapp show \
     -g rg-api-week6 -n ca-api-week6 \
     --query 'properties.template.containers[0].env' -o json
   ```

   You should see three entries — `Jwt__SigningKey` with a `secretRef`, `Jwt__Issuer` with a literal `value`, `Jwt__Audience` with a literal `value`. The actual signing key string does **not** appear anywhere in this output. That is the whole point.

> ⚠ **Common Mistakes**
>
> - Storing the signing key as a literal env var (`--set-env-vars Jwt__SigningKey="$PROD_KEY"`). It works at runtime and bakes the key into the Container App's revision history, exactly the same anti-pattern as a literal App Insights connection string. Use `secretref:`.
> - Mismatched `Jwt__Issuer` and `Jwt__Audience` between issuance (the controller reads from configuration) and validation (the bearer handler reads from configuration). They must be the same string in both — the controller and the validator both read from `IConfiguration`, so as long as the env vars are set, this happens automatically. The mistake is setting different values in dev and prod and then wondering why dev tokens fail in prod.
> - Forgetting that `__` (double underscore) is the env-var separator that ASP.NET Core maps to `:`. Setting `Jwt:SigningKey` literally in an env var name is invalid; setting `Jwt-SigningKey` is also invalid; only `Jwt__SigningKey` works.
>
> ✓ **Quick check:** The JSON output shows `secretRef: "jwt-signing-key"` for `Jwt__SigningKey`, with no `value` field. `Jwt__Issuer` and `Jwt__Audience` show plain values.

### **Step 10:** Rewire Swagger UI's Authorize button for Bearer

In the previous exercise you wired Swagger UI's **Authorize** button to the API-key scheme — when clicked, it asked for a single string and sent it as `X-Api-Key`. The button needs to be rewired for Bearer tokens: when clicked, it should ask for the JWT, and Swagger UI should send it as `Authorization: Bearer <token>` on subsequent requests.

The mechanics are the same `OpenApiSecurityScheme` and `AddSecurityRequirement` calls; only the values change.

1. **Open** `Program.cs` and find the existing `AddSwaggerGen(...)` block from the previous exercise.

2. **Replace** the API-key security scheme with the Bearer scheme. The full block should look like this — copy it whole, replacing whatever was there before:

   > `Program.cs`

   ```csharp
   using Microsoft.OpenApi.Models;

   builder.Services.AddSwaggerGen(c =>
   {
       c.SwaggerDoc("v1", new OpenApiInfo { Title = "CloudCi API", Version = "v1" });

       // Bearer scheme: clicking "Authorize" in Swagger UI prompts for the JWT,
       // and Swagger sends it as `Authorization: Bearer <token>` on every request.
       c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
       {
           Name = "Authorization",
           In = ParameterLocation.Header,
           Type = SecuritySchemeType.Http,
           Scheme = "bearer",
           BearerFormat = "JWT",
           Description = "Paste the JWT returned by /api/tokens/login. Do not include the word 'Bearer'."
       });

       c.AddSecurityRequirement(new OpenApiSecurityRequirement
       {
           {
               new OpenApiSecurityScheme
               {
                   Reference = new OpenApiReference
                   {
                       Type = ReferenceType.SecurityScheme,
                       Id = "Bearer"
                   }
               },
               Array.Empty<string>()
           }
       });
   });
   ```

3. **Verify** locally before pushing. `dotnet run`, open `http://localhost:<port>/swagger`, click **Authorize**, and confirm the dialog asks for a token (not an API key). Get a token from `POST /api/tokens/login`, paste it into the Authorize dialog, then call `GET /api/quotes` from inside Swagger UI — it should return 200 with the JSON array.

> ℹ **Concept Deep Dive**
>
> `Type = SecuritySchemeType.Http` together with `Scheme = "bearer"` is the OpenAPI 3.0 way of saying "use the HTTP Authorization header with the Bearer scheme." Swagger UI knows how to render this — it shows a single text box, prepends `Bearer ` automatically, and applies the header to every subsequent request as long as the dialog stays "authorized."
>
> The `BearerFormat = "JWT"` field is descriptive only — it's a hint to the UI and to consumers reading the OpenAPI document, with no runtime effect on validation.
>
> ⚠ **Common Mistakes**
>
> - Pasting the token *with* the literal `Bearer ` prefix into the Authorize dialog. Swagger UI prepends it for you. The result is the request header `Authorization: Bearer Bearer eyJ...`, which the bearer handler rejects.
> - Leaving the API-key `AddSecurityDefinition` block in place alongside the Bearer one. Swagger UI then shows two schemes in the Authorize dialog and tries to apply both. Either delete the old block or, if you really want to show both options, add `[AllowAnonymous]` overrides on the tokens endpoint and configure the `AddSecurityRequirement` to require only Bearer.
>
> ✓ **Quick check:** The Swagger UI Authorize dialog shows a Bearer scheme. Pasting the token (without the word `Bearer`) and calling `GET /api/quotes` returns 200.

### **Step 11:** Push and verify the deployed API

The code changes from Steps 1–10 are still local. The pipeline from earlier in the course rebuilds and updates the Container App on every push to `main`, so commit and push to roll the new bearer flow into the running container.

1. **Commit and push:**

   ```bash
   git add CloudCiApi.csproj Program.cs Controllers/ Models/ Services/ appsettings.Development.json
   git rm Middleware/ApiKeyMiddleware.cs   # if you kept it; safe to skip if already deleted
   git commit -m "Replace API-key middleware with JWT bearer authentication"
   git push
   gh run watch
   ```

   The workflow should turn green in a couple of minutes.

2. **Capture** the FQDN:

   ```bash
   FQDN=$(az containerapp show \
     -g rg-api-week6 -n ca-api-week6 \
     --query properties.configuration.ingress.fqdn -o tsv)

   echo "$FQDN"
   ```

3. **Verify** the three-way behaviour with `curl`. Anonymous → 401, login → 200 + token, with token → 200:

   ```bash
   # Anonymous request — expected 401
   curl -i "https://$FQDN/api/quotes"

   # Login — expected 200 with a JWT in the body
   TOKEN=$(curl -s -X POST "https://$FQDN/api/tokens/login" \
     -H 'Content-Type: application/json' \
     -d '{"username":"alice","password":"alice123"}' \
     | jq -r .token)

   echo "$TOKEN" | head -c 40 ; echo "..."

   # Authorized request — expected 200 with the JSON array
   curl -i -H "Authorization: Bearer $TOKEN" "https://$FQDN/api/quotes"
   ```

> ⚠ **Common Mistakes**
>
> - Forgetting the literal word `Bearer` (and the space) in the curl `-H` argument. The header value must be exactly `Bearer <token>`. `Authorization: <token>` returns 401.
> - Using bad credentials in the login request and getting 401, then pasting the empty `$TOKEN` into the next curl — the second request also returns 401, but for a different reason. Always check that `$TOKEN` is non-empty before using it.
> - Hitting the API before the new revision is active. `az containerapp revision list -g rg-api-week6 -n ca-api-week6 -o table` shows which revision serves traffic.
>
> ✓ **Quick check:** Three responses, in order: 401, 200 with token, 200 with JSON array. The Container Apps revision history shows a new revision active with 100% traffic.

### **Step 12:** Test Your Implementation

Walk through the flow end to end one more time, both locally and against the deployed API.

1. **Run the app locally:**

   ```bash
   dotnet run
   ```

2. **Anonymous request returns 401:**

   ```bash
   curl -i http://localhost:<port>/api/quotes
   ```

   Expected: `HTTP/1.1 401 Unauthorized` with the response header `WWW-Authenticate: Bearer`.

3. **Login returns 200 with a JWT:**

   ```bash
   curl -s -X POST http://localhost:<port>/api/tokens/login \
     -H 'Content-Type: application/json' \
     -d '{"username":"alice","password":"alice123"}'
   ```

   Expected: `{"token":"eyJ...","expiresAt":"2026-..."}`. Decode the `eyJ...` portion at <https://jwt.io> and confirm the payload contains `sub: "alice"`, `name: "alice"`, `role: "admin"`, `iss: "cloudci-api-dev"`, `aud: "cloudci-api-clients-dev"`, and an `exp` ~1 hour in the future.

4. **Bad credentials return 401:**

   ```bash
   curl -i -X POST http://localhost:<port>/api/tokens/login \
     -H 'Content-Type: application/json' \
     -d '{"username":"alice","password":"wrong"}'
   ```

   Expected: `HTTP/1.1 401 Unauthorized` with no body.

5. **Authorized request returns 200:** Use the token from step 3 and call the protected endpoint:

   ```bash
   curl -i -H "Authorization: Bearer $TOKEN" http://localhost:<port>/api/quotes
   ```

   Expected: `HTTP/1.1 200 OK` with the JSON array of quote DTOs.

6. **Tampered token returns 401:** Change one character in the middle of the token and retry:

   ```bash
   BAD_TOKEN="${TOKEN:0:50}X${TOKEN:51}"
   curl -i -H "Authorization: Bearer $BAD_TOKEN" http://localhost:<port>/api/quotes
   ```

   Expected: 401. The signature check fails because the payload no longer matches.

7. **Repeat steps 2–5 against the deployed API** at `https://$FQDN`.

8. **Swagger UI works:** Open `https://$FQDN/swagger`, click **Authorize**, paste a fresh token (without the word `Bearer`), then exercise `GET /api/quotes` from the UI. Confirm 200 with the JSON array.

> ✓ **Success indicators:**
>
> - Anonymous requests to `/api/quotes` return 401 both locally and on the Container App
> - `POST /api/tokens/login` with valid credentials returns 200 with a JWT and an expiry
> - Bad credentials return 401 with no token
> - Requests with the Bearer header return 200 with the JSON array
> - Tampered tokens return 401
> - Swagger UI's Authorize button accepts the JWT and persists it across requests
>
> ✓ **Final verification checklist:**
>
> - ☐ `Microsoft.AspNetCore.Authentication.JwtBearer` listed in `CloudCiApi.csproj`
> - ☐ JWT validation parameters configured in `Program.cs` with all four `Validate*` flags on
> - ☐ `appsettings.Development.json` has a 256-bit-or-larger dev signing key
> - ☐ The `ApiKeyMiddleware` registration and class file are gone
> - ☐ `IUserStore` and `InMemoryUserStore` registered as a singleton
> - ☐ `TokensController` issues signed JWTs with `iss`, `aud`, `exp`, `sub`, `name`, and an optional `role`
> - ☐ `QuotesController` is `[Authorize]`-marked
> - ☐ Container Apps secret `jwt-signing-key` set; env var `Jwt__SigningKey` references it via `secretref:`
> - ☐ `Jwt__Issuer` and `Jwt__Audience` set as plain env vars on the Container App
> - ☐ Swagger UI Authorize button uses the Bearer scheme

### **Step 13:** Tear down the cloud resources

This is the last exercise of the chapter and the last exercise that uses the Week 6 cloud resources. Nothing later in the course needs `rg-api-week6`, the Container App, the ACR, or the Entra app registration created for OIDC federation. Tear all of it down so you finish with no resources running and no orphaned tenant-level identities.

The work splits into two homes — the Azure subscription holds the running resources, and Microsoft Entra ID (a tenant-level service) holds the identity used by the pipeline. Deleting one does **not** delete the other, exactly as you saw at the end of the previous chapter.

1. **Capture** the Entra app's `appId` before you delete it (you'll need it for the second command, and the simplest way to look it up later is by display name):

   ```bash
   APP_ID=$(az ad app list \
     --display-name github-cloudci-api-oidc \
     --query "[0].appId" -o tsv)

   echo "$APP_ID"
   ```

   If `$APP_ID` is empty, the app already doesn't exist — skip the `az ad app delete` step.

2. **Delete** the resource group. This removes the Container App, the ACR with all its images, the Container Apps environment, every Container Apps secret (including `jwt-signing-key`), and every role assignment scoped under the group:

   ```bash
   # 1. Delete the resource group (Container App, ACR, all of it).
   az group delete -n rg-api-week6 --yes --no-wait
   ```

   `--no-wait` returns immediately and lets the deletion run in the background. The full teardown takes a few minutes.

3. **Delete** the Entra app registration that was created for OIDC federation. The app registration lives in the tenant, not in any subscription resource group, so the RG delete above does **not** remove it. Delete it explicitly:

   ```bash
   # 2. Delete the Entra app registration that was created for OIDC federation.
   # The app registration lives in the tenant, not in any subscription resource group,
   # so the RG delete above does NOT remove it. Delete it explicitly.
   az ad app delete --id "$APP_ID"
   ```

4. **Verify** the resource group is gone (give it a few minutes if `--no-wait` is still working in the background):

   ```bash
   az group exists -n rg-api-week6
   ```

   Expected: `false`.

5. **Verify** the Entra app registration is gone:

   ```bash
   az ad app list --display-name github-cloudci-api-oidc -o tsv
   ```

   Expected: empty output (no rows).

6. **Optionally** delete the GitHub repo secrets that pointed at the now-deleted Entra app and Azure subscription. They're inert without working resources behind them, but cleaning them up keeps the repo tidy:

   ```bash
   gh secret delete AZURE_CLIENT_ID --repo <your-username>/cloudci-api
   gh secret delete AZURE_TENANT_ID --repo <your-username>/cloudci-api
   gh secret delete AZURE_SUBSCRIPTION_ID --repo <your-username>/cloudci-api
   gh secret delete ACR_NAME --repo <your-username>/cloudci-api
   ```

   Replace `<your-username>/cloudci-api` with the actual owner and name of your repository.

> ℹ **Concept Deep Dive**
>
> Why the two-step cleanup matters. The resource group is a subscription-level container — `az group delete` cascades to every Azure resource scoped under it, including the Container App, the ACR, the Container Apps environment, every Container Apps secret stored on the app, and every role assignment scoped to those resources.
>
> The Entra app registration, however, is a *tenant-level* identity object. It lives in Microsoft Entra ID, not in your Azure subscription, and survives every subscription operation. Without the second `az ad app delete`, the federated identity sticks around as orphaned tenant clutter — visible in the Entra portal with broken role assignments pointing at deleted resources, contributing nothing and confusing future you.
>
> Note that Entra ID *soft-deletes* app registrations for 30 days; they're recoverable from **App registrations → Deleted applications** in the Entra portal and garbage-collected automatically afterwards. That's a useful safety net if you delete the wrong app — you have a month to restore it. It also means `az ad app list` won't show the deleted app, but a soft-deleted entry still exists for the next 30 days; that's expected and harmless.
>
> ⚠ **Common Mistakes**
>
> - Stopping after `az group delete`. The Entra app stays alive and accumulates over multiple cohorts as orphaned `github-cloudci-api-oidc` entries.
> - Forgetting that the GitHub repo secrets `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, and `ACR_NAME` in your repository are now stale. The next workflow run will fail at **Sign in to Azure** because the client ID points at a deleted app. Either delete the secrets via `gh secret delete <name>`, or leave them alone — they're inert without working resources behind them.
> - Confusing Entra soft-delete with hard-delete. `az ad app delete` does a soft-delete; the app is recoverable for 30 days. To purge immediately, use `az rest --method DELETE --url "https://graph.microsoft.com/v1.0/directory/deletedItems/$APP_ID"`. For this lab, soft-delete is fine — it'll be auto-purged before the next cohort starts.
> - Orphaned role assignments at *subscription* scope. If the previous chapter assigned a role to the Entra service principal at subscription scope (rather than at the resource-group scope), `az group delete` does not clean it up. List subscription-scoped assignments with `az role assignment list --assignee "$APP_ID" --scope /subscriptions/<sub-id>` and `az role assignment delete` any that turn up.

> ✓ **Quick check:** `az group exists -n rg-api-week6` returns `false`, `az ad app list --display-name github-cloudci-api-oidc -o tsv` returns nothing, and your subscription's billing is no longer accruing for any of the Week 6 resources.

## Common Issues

> **If you encounter problems:**
>
> **Every request returns 401 even with a valid token:** Either `app.UseAuthentication()` is missing from the pipeline, or it comes after `app.UseAuthorization()`. The order is `UseAuthentication` first, `UseAuthorization` second, then the endpoint mapping.
>
> **Startup throws `IDX10720: Unable to create KeyedHashAlgorithm for algorithm 'HS256'`:** The signing key is shorter than 256 bits. Use `openssl rand -base64 48` (or longer) and re-set the secret.
>
> **Login returns 200 with a token, but the next request returns 401:** Almost always a mismatch between the issuer/audience used at issuance (`Jwt:Issuer`/`Jwt:Audience` from configuration in `TokensController`) and the values configured for validation (`ValidIssuer`/`ValidAudience` in `Program.cs`). Both are read from the same configuration tree, so the actual mistake is usually different env-var values in dev vs prod. Compare the values directly.
>
> **Container App can't read the signing key — startup logs show a null configuration value:** The env var name is wrong. ASP.NET Core maps `__` (double underscore) in env vars to `:` in the config tree. The env var must be `Jwt__SigningKey`, not `Jwt:SigningKey` or `Jwt-SigningKey`.
>
> **Swagger UI's Authorize button still asks for an API key:** The old `AddSecurityDefinition` from the previous exercise is still in `Program.cs`. Delete the API-key block; only the Bearer block should remain.
>
> **`curl` returns 401 with `Bearer <token>` but Swagger UI works:** You included the literal word `Bearer` inside the Swagger Authorize dialog. Swagger prepends it for you, so the resulting header is `Authorization: Bearer Bearer <token>`. Paste only the token.
>
> **`az group delete` returns immediately but resources still appear in the Portal:** That's `--no-wait` working as intended. Re-check with `az group exists -n rg-api-week6`.
>
> **Still stuck?** Verify three things in order: the JWT bearer package in `CloudCiApi.csproj`, the `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` block in `Program.cs`, and the Container App's env var list. All three must be correct for the bearer flow to work.

## Summary

You replaced the API-key middleware with JWT bearer authentication. A `TokensController` issues signed JWTs against an in-memory user list; `QuotesController` is `[Authorize]`-marked and the framework's authentication middleware materializes a `ClaimsPrincipal` from the token on every request. The signing key is delivered to Container Apps as a secret referenced via `secretref:` — the same pattern you used for the App Insights connection string in the previous chapter — so it never appears in `az containerapp show`. Swagger UI's **Authorize** button is rewired for Bearer, which makes the API explorable from the browser with one paste of a token.

- ✓ JWT bearer authentication configured with all four `TokenValidationParameters` checks on
- ✓ A login endpoint that validates credentials against an in-memory user store and signs a JWT with claims for `sub`, `name`, and an optional `role`
- ✓ The API-key middleware removed cleanly — replacement, not layering
- ✓ Production signing key delivered as a Container Apps secret, never as a literal env var
- ✓ Swagger UI's Authorize button rewired from API-key to Bearer
- ✓ Resource group, Entra app registration, and (optionally) GitHub secrets all torn down — no orphaned cloud or tenant resources

> **Key takeaway:** A JWT is three base64url segments separated by dots. The signature is the integrity check; the payload is plaintext, readable by anyone who sees the token. ASP.NET Core's bearer handler validates the four standard claims (`iss`, `aud`, `exp`, `nbf`) plus the signature on every request — turning any one of those checks off is a security bug. Per-user identity gives you strictly more than an API key did, which is why the right move was *replace* rather than *layer*.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Add a `[Authorize(Roles = "admin")]` attribute on a write endpoint (a `POST /api/quotes` that adds a quote, say) and confirm that alice's token works but bob's returns 403. The role claim from Step 6 is what makes this work.
> - Replace HS256 with RS256. Generate an RSA keypair with `openssl genrsa -out signing.key 2048` and `openssl rsa -in signing.key -pubout -out signing.pub`. The issuer signs with the private key; the validator gets the public key only. This is the model real IdPs use.
> - Add refresh tokens. The access token stays at 1 hour; a separate `POST /api/tokens/refresh` endpoint exchanges a long-lived refresh token (single-use, server-tracked) for a fresh access token. This is the production pattern alluded to in the lifetime trade-off discussion above.
> - Read the JWT spec — RFC 7519 is short and worth one careful read. Pair it with RFC 7515 (JWS, the signature container that JWTs use) and RFC 7518 (JWA, the algorithms registered for use with JWS).
> - Investigate **OAuth 2.1** and **OpenID Connect**. JWT is a token format; OAuth/OIDC are protocols for *getting* tokens. The next time you wire authentication against an external IdP — Microsoft Entra, Google, Auth0 — those protocols are how the token reaches your app.

## Done!

This exercise ends the REST API and DTOs chapter, and the lab the chapter ran on. The resource group is gone, the Entra app registration is gone, the GitHub secrets in your repo are stale and inert. There is nothing left running and nothing left billing.

You started the chapter with a controller returning JSON. You finish it with the same controller behind per-user authentication, a token-issuing endpoint that signs claims with a secret delivered the right way, and a Swagger UI that lets you exercise the protected surface from the browser.

The next thing the course tackles is what happens when one service has to call another — when your API is no longer the leaf of the request graph but a node in the middle, calling downstream APIs and propagating identity across the hops. The patterns you've seen here (a JWT carrying user identity, a validator checking signature and claims, a configuration-driven secret) are the same patterns that scale up to a multi-service architecture; the difference is that the issuer becomes a separate identity provider and the signature scheme moves from HS256 to RS256. That's the road the rest of the program will spend time on.
