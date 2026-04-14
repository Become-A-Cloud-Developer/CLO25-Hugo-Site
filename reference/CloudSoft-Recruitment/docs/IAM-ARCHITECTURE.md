# CloudSoft Recruitment Portal — IAM Architecture

This document describes the authentication and authorization mechanisms used across all boundaries in the solution.

## Authentication Overview

The solution uses four distinct authentication patterns, each chosen for a specific boundary:

| Boundary | Mechanism | Credentials |
|---|---|---|
| GitHub → ACR | OIDC federation (workload identity) | No secrets — federated trust |
| ACA → ACR | User-assigned managed identity | AcrPull RBAC role |
| ACA → Key Vault | User-assigned managed identity | Key Vault Secrets User RBAC role |
| ACA → CosmosDB | Connection string via Key Vault | Key Vault overrides env var placeholder |
| ACA → Blob Storage | Connection string via Key Vault | Key Vault overrides env var placeholder |
| ACA → Application Insights | Connection string | Environment variable |
| Browser → MVC (Admin & Candidate) | Cookie authentication | Email / password |
| Browser → MVC (Candidate) | Google OAuth 2.0 | OAuth redirect flow |
| API consumer → API | JWT bearer token or API key | Token / key in header |

## IAM Diagram

```mermaid
graph TB
    subgraph CI/CD Pipeline
        github["GitHub Actions"]
    end

    subgraph Azure["Azure Production Environment"]
        acr["Azure Container Registry"]

        subgraph ACA["Azure Container Apps"]
            app["CloudSoft.Web"]
            mi["User-Assigned<br/>Managed Identity"]
        end

        cosmosdb["CosmosDB"]
        blob["Blob Storage"]
        keyvault["Key Vault"]
        appinsights["Application Insights"]
    end

    subgraph Users["Users & Consumers"]
        admin["Admin<br/><i>Browser</i>"]
        candidate["Candidate<br/><i>Browser</i>"]
        apiconsumer["API Consumer<br/><i>Partner job board,<br/>mobile app</i>"]
    end

    subgraph External
        google["Google OAuth 2.0"]
    end

    %% CI/CD authentication
    github -- "OIDC federation<br/><i>Workload identity,<br/>no secrets stored</i>" --> acr

    %% ACR to ACA
    acr -- "Image pull<br/><i>Managed identity</i>" --> app

    %% Managed identity to Azure services
    mi -- "RBAC: Key Vault<br/>Secrets User" --> keyvault

    %% Key Vault provides connection strings
    keyvault -. "Connection string<br/><i>via config provider</i>" .-> cosmosdb
    keyvault -. "Connection string<br/><i>via config provider</i>" .-> blob

    %% Direct connection string
    app -. "Connection string<br/><i>Environment variable</i>" .-> appinsights

    %% User authentication
    admin -- "Cookie auth<br/><i>Email + password</i>" --> app
    candidate -- "Cookie auth<br/><i>Email + password</i>" --> app
    candidate -- "Google OAuth<br/><i>Social login</i>" --> google
    google -- "OAuth callback<br/><i>Auto-creates account</i>" --> app

    %% API authentication
    apiconsumer -- "JWT bearer token<br/><i>Authorization header</i><br/><br/>— or —<br/><br/>API key<br/><i>X-API-Key header</i>" --> app

    %% Styling
    classDef azure fill:#0078d4,color:#fff,stroke:#005a9e
    classDef user fill:#107c10,color:#fff,stroke:#0b5e0b
    classDef external fill:#4a4a4a,color:#fff,stroke:#333
    classDef cicd fill:#24292e,color:#fff,stroke:#1a1a1a

    class acr,app,mi,cosmosdb,blob,keyvault,azurefiles,appinsights azure
    class admin,candidate,apiconsumer user
    class google external
    class github cicd
```

## Authentication Details by Boundary

### 1. CI/CD → Azure Container Registry

**Mechanism:** OIDC workload identity federation

GitHub Actions authenticates to Azure using federated credentials — a trust relationship configured in Azure AD/Entra ID that accepts GitHub's OIDC tokens. No secrets are stored in GitHub; only the client ID, tenant ID, and subscription ID are configured as repository variables.

**Alternative:** Service principal with client secret stored as a GitHub secret. Simpler to set up but requires secret rotation.

### 2. Container Apps → Key Vault (Managed Identity)

**Mechanism:** User-assigned managed identity with RBAC role assignment

A user-assigned managed identity (`cloudsoft-{env}-identity`) is created as a standalone Bicep resource and attached to the Container App. It is granted the **Key Vault Secrets User** role, allowing the app to read secrets at runtime via the Azure Key Vault configuration provider.

**Why user-assigned instead of system-assigned:**

- **IaC compatibility** — the identity is created before the Container App exists, so RBAC roles (Key Vault Secrets User, AcrPull) can be assigned in the same Bicep deployment. With system-assigned identity, the Container App must exist first, requiring a two-pass deployment or post-deployment scripts.
- **Lifecycle independence** — deleting and recreating the Container App (common during debugging or Bicep re-deployments) does not destroy the identity or its role assignments.
- **Explicit reference** — the identity's client ID is passed as `AZURE_CLIENT_ID` to `DefaultAzureCredential`, removing ambiguity when multiple identities exist.

| Service | RBAC Role | Purpose |
|---|---|---|
| Key Vault | Key Vault Secrets User | Read connection strings and app secrets |
| ACR | AcrPull | Pull container images |

### 3. Key Vault → CosmosDB and Blob Storage (Connection Strings)

**Mechanism:** Connection strings stored as Key Vault secrets

CosmosDB and Blob Storage use connection strings (required by their respective SDKs — the MongoDB wire protocol driver and Azure.Storage.Blobs). These connection strings are stored in Key Vault as secrets, not as Container App secrets or environment variables.

The .NET Azure Key Vault configuration provider (`AddAzureKeyVault`) is loaded **first** at application startup, giving it the highest precedence. Container App env vars contain placeholder values (`overridden-by-keyvault`) that are overridden by Key Vault before any service registration reads them.

**Key Vault secret naming:** Azure Key Vault uses `--` as a section separator, which the configuration provider translates to `:` for .NET configuration. For example, the secret `MongoDb--ConnectionString` maps to `MongoDb:ConnectionString`.

**Configuration precedence (highest → lowest):**
1. Azure Key Vault (connection strings, JWT key)
2. Environment variables / Container App secrets (feature flags, seed passwords)
3. `appsettings.Production.json` (feature flag defaults)
4. `appsettings.json` (base defaults)

### 4. Container Apps → Application Insights

**Mechanism:** Connection string (environment variable)

Application Insights uses a connection string containing the instrumentation endpoint. This is not sensitive in the same way as a database credential — it identifies where telemetry is sent, and ingestion is protected by the instrumentation key embedded in the connection string. Managed identity authentication is possible but adds complexity without significant security benefit for this scenario.

### 5. Browser → MVC (Cookie Authentication)

**Mechanism:** ASP.NET Core Identity with cookie authentication

Both Admin and Candidate users authenticate via email and password. On successful login, ASP.NET Core Identity issues an encrypted, HttpOnly, SameSite cookie. The cookie contains the user's claims (ID, roles, name) and is validated on each request.

Security measures:
- Account lockout after 5 failed attempts (5-minute lockout)
- Passwords hashed with ASP.NET Core Identity defaults (PBKDF2)
- Cookies configured with `HttpOnly`, `SameSite=Lax`, and `SecurePolicy` per environment
- CSRF protection via `AutoValidateAntiforgeryToken`

### 6. Browser → MVC (Google OAuth)

**Mechanism:** OAuth 2.0 authorization code flow

Candidates can sign in via Google as an alternative to email/password. The flow:
1. User clicks "Sign in with Google"
2. Redirect to Google's authorization endpoint
3. User consents, Google redirects back with an authorization code
4. The app exchanges the code for user info (email, name)
5. If the user doesn't exist, a new Candidate account is created automatically
6. A cookie is issued (same as regular login from this point on)

Google OAuth is conditionally registered — if the client ID and secret are not configured, the login button is hidden. Credentials are stored in User Secrets (local) or Key Vault (production).

### 7. API Consumer → REST API

The API supports two authentication options. Both can be implemented; the choice depends on the use case.

#### Option A: JWT Bearer Tokens (recommended)

**Mechanism:** JSON Web Token issued by the application

The app exposes a token endpoint:
- `POST /api/token` — authenticate with email/password, receive a JWT

The JWT contains the user's claims (role, name, ID) and is signed with a key stored in Key Vault. Subsequent API requests include the token:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

ASP.NET Core validates the token automatically via `AddJwtBearer()`. This runs alongside cookie authentication — MVC controllers use cookies (default scheme), API controllers use `[Authorize(AuthenticationSchemes = "Bearer")]`.

**Pros:** Industry standard, self-contained tokens, supports expiration and refresh, teaches token-based auth.

#### Option B: API Key

**Mechanism:** Static key in request header

The API consumer sends a pre-shared key:
```
X-API-Key: <key>
```

The key is stored in Key Vault and validated in middleware. Simple to implement and easy to understand.

**Pros:** Minimal setup, easy to debug, good for server-to-server integrations.
**Cons:** No user identity in the key (just "authorized or not"), no expiration unless manually rotated, less to teach.

### Multiple Authentication Schemes in ASP.NET Core

The application registers multiple authentication schemes simultaneously:

```
AddAuthentication()
    .AddCookie()          // MVC browser sessions (default)
    .AddJwtBearer()       // API consumers
```

Each controller or endpoint specifies which scheme it accepts. This demonstrates how a single application can serve both browser users and API consumers with appropriate authentication for each.
