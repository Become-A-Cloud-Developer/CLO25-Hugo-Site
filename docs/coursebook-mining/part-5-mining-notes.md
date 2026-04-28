# Part V — Identity & Security — Mining Notes

## Studieguide alignment

- Companion course weeks: 
  - BCD week 8 (v.12) "Secret Management" 
  - ACD week 3 (v.17) "Autentisering och auktorisering"
  - ACD week 6 (v.20) "REST API och DTOs" (JWT and API-key chapters)
- Reflection questions across these weeks (extract verbatim):
  - Vad är skillnaden mellan autentisering och auktorisering?
  - Hur fungerar cookie-baserad inloggning i en webbapplikation?
  - Varför behövs CSRF-skydd och hur implementeras det i ASP.NET Core?
  - Hur hanterar man secrets säkert i molnmiljöer?
  - Vad är skillnaden mellan secrets och identiteter?
  - Hur fungerar Azure Key Vault?
  - Hur skiljer sig JWT-autentisering från cookie-baserad inloggning?

## Companion exercises

- Path 1: `content/exercises/10-webapp-development/4-authentication-authorization/` — ACD
  - 1-hardcoded-user-and-login-form
  - 2-roles-on-claims
  - 3-claims-principal
  - 4-csrf-protection
- Path 2: `content/exercises/10-webapp-development/5-identity-and-user-stores/` — ACD
  - 1-introducing-asp-net-core-identity
  - 2-user-store-feature-flag
  - 3-seeding-the-first-admin
  - 4-registration-and-role-promotion
- Path 3: `content/exercises/4-services-and-apis/1-rest-api-and-dtos/` — ACD (JWT and API key)
  - 1-basic-api-structure
  - 2-api-key-middleware
  - 3-jwt-bearer-authentication

Key code patterns: `[Authorize]`, `ClaimsPrincipal`, `HttpContext.User`, `IAuthenticationHandler`, `UserManager<TUser>`, `SignInManager<TUser>`, `JwtBearerHandler`, `ApiKeyMiddleware`, CSRF token, bearer token, JWT, API key.

Key file names: `Program.cs` (auth registration), `appsettings.json` (JWT key config), `appsettings.{Env}.json`, `.csproj` (`UserSecretsId`), Controllers/, `appsettings.Development.json`.

Key library / API surface: ASP.NET Core Identity, `Microsoft.AspNetCore.Authentication`, `Microsoft.AspNetCore.Authentication.Cookies`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.Security.Claims`, `IdentityUser`, `IdentityRole`, custom middleware, Container Apps secrets (`secretref:`).

## Per-chapter brief

### Chapter 1 — Authentication vs authorization (slug: 1-authentication-vs-authorization)
- Owns terms: authentication, authorization, identity, credential, principal, claim, subject.
- Borrows: HTTP (from Part III ch 1), stateless (from Part II), TCP (from Part II).
- Reflection questions to answer: What is the difference between authentication and authorization? Why are these concepts fundamental to web security?
- Worked example: The "Who Am I?" page in Exercise 4 (authentication-authorization) provides the arc. A user navigates to `/whoami` without logging in — the server cannot answer "who are you?" because no HTTP session established identity. The user fills a login form, POST redirects to a handler that validates credentials, sets a cookie, and redirects back to `/whoami`. Now the server reads `HttpContext.User` (the `ClaimsPrincipal`) and displays the identity claim. Authentication is the credential validation; authorization would be checking if that user is allowed to access this resource.
- Slide-pair: yes
- Course tag: ACD
- Cross-link target: /exercises/10-webapp-development/4-authentication-authorization/
- Companion section in Part IV: None (data layer is not security)

### Chapter 2 — Cookie-based authentication and sessions (slug: 2-cookie-authentication)
- Owns terms: cookie, session, session cookie, cookie attributes (Secure, HttpOnly, SameSite), stateful authentication, Set-Cookie, cookie jar, cookie rotation, anti-forgery token (CSRF).
- Borrows: HTTP protocol (from Part III ch 1), HTTP header (from Part III ch 1), client-server (from Part II), stateless protocol (from Part II).
- Reflection questions to answer: How does cookie-based login work in a web application? Why is HttpOnly important? What does SameSite prevent?
- Worked example: Exercise 4, Step 1 shows the cookie-auth flow. The login form POSTs to `/Account/Login`. The handler validates hardcoded credentials (username and password), then calls `await HttpContext.SignInAsync(...)` with a `ClaimsPrincipal` carrying the user's identity claim. ASP.NET Core emits a `Set-Cookie` response header with the authentication ticket (encrypted by the data protection API). The browser adds it to the cookie jar. On every subsequent request, the browser attaches the cookie, the middleware decrypts the ticket, reconstructs the `ClaimsPrincipal`, and populates `HttpContext.User`. The `/whoami` page reads `User.Identity.Name` and displays it. The cookie itself is opaque to JavaScript and the browser (`HttpOnly` flag), and is sent only over HTTPS in this course (`Secure` flag); modern browsers also enforce `SameSite=Lax` by default, preventing CSRF. This is the foundation on which Chapter 5 (ASP.NET Core Identity) builds.
- Slide-pair: yes
- Course tag: ACD
- Cross-link target: /exercises/10-webapp-development/4-authentication-authorization/1-hardcoded-user-and-login-form/

### Chapter 3 — ASP.NET Core Identity (slug: 3-aspnet-core-identity)
- Owns terms: ASP.NET Core Identity, `UserManager<TUser>`, `SignInManager<TUser>`, `IdentityUser`, `IdentityRole`, password hash, password validator, hashing algorithm (PBKDF2), user store, persistence, registration, role assignment, claim factory.
- Borrows: cookies (from Chapter 2), claims and roles (from Chapter 4 — forward reference used here, introduced there), dependency injection (from Part III ch 6), Entity Framework Core (from Part IV ch 2 — repository pattern applied to Identity), three-tier architecture (from Part III ch 4).
- Reflection questions to answer: What is ASP.NET Core Identity and what problem does it solve? How does it manage passwords safely? Why is `UserManager` a service and not a static class?
- Worked example: Exercise 5, Steps 1–4 progressively layer Identity into the CloudSoft Recruitment Portal. Step 1 replaces the hardcoded user list from Exercise 4 with `UserManager<IdentityUser>` and `SignInManager<IdentityUser>` backed by EF Core in-memory store. The login controller now calls `var result = await signInManager.PasswordSignInAsync(username, password, rememberMe: false, lockoutOnFailure: true)`, which internally calls `UserManager.FindByNameAsync`, retrieves the user's password hash, calls `PasswordHasher.VerifyHashedPassword()` (PBKDF2 by default with configurable iterations), compares the result, and if it matches, emits the same `Set-Cookie` response as Chapter 2 hand-rolled. Steps 2–3 feature-flag between in-memory and SQLite persistence; the same `UserManager` API works against both stores because it abstracts the data layer. Step 4 adds a registration flow: the POST endpoint calls `UserManager.CreateAsync(newUser, password)`, which validates the password against configured rules (length, complexity), hashes it, persists the user, and emits a success response. The role assignment happens via `UserManager.AddToRoleAsync(user, "Candidate")`, again abstracted from storage.
- Slide-pair: yes
- Course tag: ACD
- Cross-link target: /exercises/10-webapp-development/5-identity-and-user-stores/

### Chapter 4 — Roles, claims, and policies (slug: 4-roles-claims-and-policies)
- Owns terms: claim, claimType, claimValue, role, policy, named policy, `[Authorize]` attribute, `[Authorize(Roles = "...")]`, `[Authorize(Policy = "...")]`, `ClaimsPrincipal`, claims-based authorization, role-based authorization, policy-based authorization, claim factory, claim transformation.
- Borrows: authentication (from Chapter 1), cookies (from Chapter 2), ASP.NET Core Identity (from Chapter 3 — built on top).
- Reflection questions to answer: What is a claim and how does it differ from a role? Why use named policies instead of `[Authorize(Roles = "Admin")]`? How does `ClaimsPrincipal` represent identity?
- Worked example: Exercise 4, Steps 2–3 build claims and roles on the cookie foundation from Step 1. Step 2 modifies the login handler to construct a `ClaimsPrincipal` with multiple claims: the identity claim (username) plus an additional claim with type `"role"` and value `"Admin"` or `"Candidate"` depending on hardcoded user. The `/whoami` page now reads `User.FindFirst("role")?.Value` to display the role. The `[Authorize]` attribute on the page handler enforces authentication — unauthenticated users bounce to login. Step 3 adds `[Authorize(Roles = "Admin")]` to a separate page, demonstrating that the framework checks `HttpContext.User.IsInRole("Admin")` by examining claims with type `"role"`. Both attributes use the same underlying mechanism: the `ClaimsPrincipal` is a collection of claims; authorization checks whether required claims are present. Named policies (introduced as optional deepening) let you encode reusable business logic: a policy `"AdminOrRecruiter"` might require `claim.Type == "role" && (claim.Value == "Admin" || claim.Value == "Recruiter")`. The policy is registered once in `Program.cs`, then applied as `[Authorize(Policy = "AdminOrRecruiter")]` without repeating the logic. This chapter introduces the abstraction; Chapter 3 showed how Identity populates claims; Chapter 5 shows JWT carrying claims across API boundaries.
- Slide-pair: yes
- Course tag: ACD
- Cross-link target: /exercises/10-webapp-development/4-authentication-authorization/

### Chapter 5 — Bearer tokens and JWT (slug: 5-bearer-tokens-and-jwt)
- Owns terms: bearer token, JWT (JSON Web Token), JWS (JSON Web Signature), payload, header, signature, `kid` (key ID), `alg` (algorithm), `iss` (issuer), `sub` (subject), `aud` (audience), `exp` (expiration), `iat` (issued at), stateless authentication, token lifetime, refresh token, asymmetric signing (RSA, ECDSA), symmetric signing (HMAC), token revocation, token introspection.
- Borrows: HTTP header (from Part III ch 1), claims (from Chapter 4), authentication (from Chapter 1), stateless (from Part II).
- Reflection questions to answer: Why are bearer tokens used in APIs instead of cookies? What does the signature in a JWT prove? How does the client validate a JWT without contacting the server?
- Worked example: Exercise 4 serves a traditional MVC app with cookies. Exercise 6 (REST API and DTOs in ACD week 6) adds an API controller (`CloudCiApi` or similar) that serves the same data over JSON without cookies. An external client calls `POST /api/auth` with a credential, receives back a JWT (header.payload.signature encoded in base64url), and includes it in subsequent requests via `Authorization: Bearer eyJ...`. The JWT payload contains claims (user ID, roles, expiration). The server's `JwtBearerHandler` (middleware registered via `builder.Services.AddAuthentication("Bearer").AddJwtBearer(...)`) reads the `Authorization` header, extracts the token, decodes the header to learn which key signed it (the `kid` field), loads that public key from configuration, and calls `VerifySignature(token, publicKey)`. If valid and not expired, the same `ClaimsPrincipal` that cookie auth produced is populated from the JWT payload. Controllers use `[Authorize]` exactly as in the MVC world; the credential transport changed (bearer token instead of cookie), but the authorization logic is identical. This pattern is covered in part in Exercise 4 as forward-reference material, detailed in Exercise 6.
- Slide-pair: yes
- Course tag: ACD
- Cross-link target: /exercises/4-services-and-apis/1-rest-api-and-dtos/

### Chapter 6 — API keys and machine-to-machine (slug: 6-api-keys)
- Owns terms: API key, shared secret, client authentication, machine-to-machine (M2M), X-Api-Key header, stateless secret, threat model (who can call this API), key rotation, API key leakage, symmetric key, constant-time comparison.
- Borrows: HTTP header (from Part III ch 1), middleware (from Part III ch 2 — service layer pattern applied), secret management (from Chapter 8 — forward reference used here), configuration (from Part III ch 5).
- Reflection questions to answer: What is an API key and what does it prove about the caller? When is an API key enough, and when is JWT required? How should API keys be stored and transmitted?
- Worked example: Exercise 6 (API-key-middleware in ACD) walks the complete pattern. A public endpoint `/api/quotes` is gated with a custom middleware (`ApiKeyMiddleware`) that demands the `X-Api-Key` header. No header → 401 Unauthorized (with `WWW-Authenticate: ApiKey`). Wrong key → 403 Forbidden. Right key → 200 OK. The key itself is 64 random base64 characters (generated via `openssl rand -base64 48`), stored in development in `appsettings.Development.json` (clearly labelled `local-dev-key` so the non-secret nature is obvious), and in production as a Container Apps secret referenced via `secretref:` in the environment variable `ApiKey__Value`. The middleware reads `configuration["ApiKey:Value"]`, which resolves to the development placeholder locally and the production secret in Azure. The threat model is simple: an API key proves "some client holds this shared secret," not "which client." Rotating the key invalidates every client at once. For user-attribution APIs, JWT is the next step (Chapter 5); for machine-to-machine service-to-service calls where both parties trust each other and a single key can be rotated together, an API key is sufficient and simpler.
- Slide-pair: yes
- Course tag: ACD
- Cross-link target: /exercises/4-services-and-apis/1-rest-api-and-dtos/2-api-key-middleware/

### Chapter 7 — OAuth 2.0 and OpenID Connect (slug: 7-oauth-and-oidc)
- Owns terms: OAuth 2.0, OpenID Connect (OIDC), authorization code flow, implicit flow, client credentials flow, authorization server, resource server, ID token, access token, refresh token, scope, `client_id`, `client_secret`, redirect URI, `state` parameter (CSRF prevention), `nonce` parameter, consent screen, federation, federated identity, third-party authentication (e.g., GitHub, Google).
- Borrows: HTTP (from Part III ch 1), bearer token (from Chapter 5), JWT (from Chapter 5), authentication vs authorization (from Chapter 1).
- Reflection questions to answer: Why use OAuth instead of asking for passwords? What is the difference between OAuth 2.0 and OpenID Connect? How does the authorization code flow prevent CSRF?
- Worked example: ACD week 7 briefly touches health checks and "Google OAuth" for file uploads (mentioned in studieguide v.21). The core exercise flow is conceptual: a user clicks "Login with GitHub" on a webapp. The app redirects to `https://github.com/login/oauth/authorize?client_id=...&redirect_uri=...&state=...&scope=read:user`. The user logs in to GitHub (or is already logged in), GitHub asks for consent, then redirects back to `http://localhost/auth/callback?code=XXXXX&state=XXXXX`. The app exchanges the code for an ID token (OIDC) and access token (OAuth) by calling `POST https://github.com/login/oauth/access_token`. The ID token is a JWT carrying the user's identity (email, name) signed by GitHub; the app verifies the signature using GitHub's public key (fetched from `https://github.com/.well-known/openid-configuration`). No password exchanged, no credential stored locally — only the identity assertion. This delegates the user directory and password hashing to GitHub, reducing the app's attack surface. In Part VIII (DevOps), OIDC federation is used for GitHub Actions → Azure deployments; this Part covers the user-authentication side.
- Slide-pair: yes
- Course tag: ACD
- Cross-link target: Forward reference; full exercise in optional deepening

### Chapter 8 — Secret management (slug: 8-secret-management)
- Owns terms: secret, API key (re-stated with secret-management lens), password hash, connection string, encryption key, key rotation, secret store, Azure Key Vault, managed identity, access control, secret versioning, secret audit log, secret leakage, environment variable vs secret store, principle of least privilege.
- Borrows: environment variable (from Part III ch 5), configuration provider (from Part III ch 5), IConfiguration (from Part III ch 5), middleware (from Part III ch 2), user-secrets (from Part III ch 5), container (from ACD week 2 Docker), Azure services (from BCD week 1–2), firewalls and network segmentation (from Part II ch 4), RBAC (from Azure docs, not a course chapter).
- Reflection questions to answer: How should you handle secrets in cloud applications? What is the difference between user-secrets, environment variables, and Azure Key Vault? Why is a managed identity better than a shared account password?
- Worked example: Exercise 6 (API-key-middleware, also Exercise 5.1–5.3 in identity chapter) demonstrates the progression. Local development: API key in `appsettings.Development.json` with a non-secret placeholder (`local-dev-key`). CI/CD pipeline: the GitHub Actions job has no secrets baked in; it uses OIDC federation to exchange the GitHub Actions OIDC token for an Azure managed identity, then calls Azure REST APIs as that identity (no stored `AZURE_CREDENTIALS` ever created). Production deployment: the API key is generated locally with `openssl rand -base64 48`, stored in Azure Container Apps as a secret (encrypted in the control plane), and injected into the running container via the `secretref:` mechanism on the environment variable `ApiKey__Value`. The app reads `configuration["ApiKey:Value"]`, which transparently resolves to the Container Apps secret without ever seeing the raw value in logs or `az containerapp show` output (requires the `listSecrets` permission, separate from `Reader`). The boundary is operational: development uses user-secrets (plaintext in home directory, safe because only the dev machine has it); staging and production use the platform secret store (encrypted at rest, rotated without redeployment, audited). Part III ch 5 introduced `IConfiguration` and user-secrets for local development. This chapter extends that by showing Azure Key Vault and managed identity as the production side of the same provider chain — application code does not change, only the surrounding infrastructure.
- Slide-pair: yes
- Course tag: BCD (fundamental to all cloud apps; identity exercises build on this but are ACD)
- Cross-link target: /course-book/3-application-development/5-configuration-and-environments/ (Part III ch 5, the base layer) and /exercises/4-services-and-apis/1-rest-api-and-dtos/2-api-key-middleware/ (Exercise 6, practical pattern)
- Companion section in Part II: /course-book/2-infrastructure/network/4-firewalls/ (network-layer access control is a sibling, not a parent)

## Cross-Part dependencies (forward references)

- **Part II defines firewalls and network security** — a peer to identity & security, operating at different layers. Part II focuses on packet-level access control; Part V focuses on application-level identity and secret management. The two are complementary, not hierarchical.
- **Part III ch 5 (Configuration and Environments)** — the base layer. Part V ch 8 (Secret Management) extends that with Key Vault and managed identities. Application code uses the same `IConfiguration` interface; the difference is which providers are in the chain.
- **Part III ch 2 (Service Layer)** and **ch 6 (Dependency Injection)** — the API-key middleware in Chapter 6 and the identity infrastructure in Chapters 2–5 follow these patterns. Middleware is a service; DI registers `UserManager`, `SignInManager`, `IAuthenticationHandler` implementations.
- **Part IV (Data Access)** — ASP.NET Core Identity's `UserManager` and `SignInManager` delegate to a user store (repository pattern, Part IV ch 2). The exercises show in-memory and SQLite stores; the manager API is abstracted from storage.
- **Part VI (Services and APIs)** — forward reference from Chapter 5 (JWT) and Chapter 6 (API keys). The full REST/DTO mechanics are in Part VI; Part V chapters focus on the identity/credential side.
- **Part VIII (DevOps & Delivery)** — forward reference from Chapter 7 (OIDC federation for GitHub Actions → Azure); the deploy-pipeline uses OIDC tokens for automated access, while Part V Chapter 7 covers the user-authentication side.
- **Chapter 4 (Roles, Claims, Policies)** is introduced early (conceptually) in Exercise 4 but formalized as the basis for Chapters 3 and 5. The ordering is: Ch 1 (concepts) → Ch 2 (cookies, the transport) → Ch 4 (claims/roles, the payload) → Ch 3 (persistence via Identity) and Ch 5 (APIs via JWT).

## Tonal reference

Use `content/course-book/2-infrastructure/network/2-ip-addresses-and-cidr-ranges/ip-addresses-and-cidr-ranges.md` as the gold standard. Key features to emulate:
- **Motivation paragraph** opens each chapter before definitions — e.g., "Web applications that serve multiple users must answer two questions: who is this person? and what are they allowed to do? Authentication answers the first; authorization answers the second."
- **Bold on first use** of every key term — "A **JWT** (JSON Web Token) is a digitally signed data structure..." and "The **signature** proves the token was not tampered with..."
- **Worked examples** drawn from actual exercises with interpretation — show the login form POST, the middleware checking the key, the JWT payload inspection, the policy evaluation.
- **Closing Summary section** recapping load-bearing claims — tie together authentication, authorization, identity, and credentials as an integrated system for securing web applications.
- **1500–3500 words** per chapter — sufficient depth for both conceptual and practical understanding.

## Important boundaries

**Part III (Web Development)** introduces the MVC presentation layer, controller routing, and configuration. Part V *applies* configuration to secrets and extends MVC with authentication middleware. The configuration infrastructure is Part III's; the security application is Part V's.

**Part IV (Data Access)** covers repositories and persistence. Identity's user store is a repository; the pattern is Part IV's, the application is Part V's.

**Part II (Infrastructure)** covers firewalls and network security at the packet level. Part V covers application-level identity and secret management. Both secure systems but operate at different layers.

Do NOT duplicate Part III or Part IV terms in the "Owns" list. Instead:
- Part III owns: `IConfiguration`, configuration provider, appsettings.json, user-secrets, environment variable, `IOptions<T>`
- Part V owns: `ClaimsPrincipal`, cookie attributes (Secure, HttpOnly, SameSite), JWT structure (header/payload/signature), API key, managed identity, Key Vault integration
- Part IV owns: repository pattern, user store abstraction
- Part V applies these, not duplicates them

This keeps clear ownership and prevents term duplication.

