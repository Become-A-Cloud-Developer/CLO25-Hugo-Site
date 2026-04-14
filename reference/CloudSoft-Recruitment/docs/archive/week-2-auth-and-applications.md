# Week 2: Authentication and Applications

## Purpose

Add authentication/authorization and the Application entity. Students learn ASP.NET Core Identity, Google OAuth, role-based access control, and deploy to Azure Container Apps for the first time.

## What Students Build

1. **ASP.NET Core Identity** — ApplicationUser with SQLite storage (Identity only — domain data stays in MongoDB)
2. **Admin login** — Email/password authentication, admin account seeded from environment variables
3. **Google OAuth** — External authentication for Candidate users, auto-registration on first login
4. **Role-based authorization** — Admin can create/edit/delete jobs; Candidate can apply; public can browse
5. **Application entity** — Fully denormalized document with BSON attributes
6. **Application service** — Duplicate check, server-side data assembly (never trust form input for user identity)
7. **4 additional unit tests** — ApplicationService tests

## Identity Setup

### Admin Seeding

The admin account is seeded at startup. The password must come from environment variables or User Secrets — never from `appsettings.json`.

```bash
# User Secrets (local development)
dotnet user-secrets set "AdminSeed:Password" "YourSecurePassword123!" --project src/CloudSoft.Web

# Environment variable (Docker/production)
AdminSeed__Password=YourSecurePassword123!
```

### Google OAuth Configuration

1. Create credentials at [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
2. Set authorized redirect URI: `https://localhost:port/Account/GoogleCallback`
3. Configure via User Secrets or environment variables:

```bash
dotnet user-secrets set "Google:ClientId" "your-client-id" --project src/CloudSoft.Web
dotnet user-secrets set "Google:ClientSecret" "your-client-secret" --project src/CloudSoft.Web
```

## Azure Container Apps Deployment

### First Deployment

```bash
cd infra
./deploy.sh
```

This script:
1. Creates the resource group
2. Deploys Bicep templates (ACR, ACA, CosmosDB, Key Vault, Storage)
3. Builds and pushes the Docker image to ACR
4. Creates the initial container app revision
5. Prompts to set Key Vault secrets

### Key Vault Secrets to Configure

| Secret | Purpose |
|--------|---------|
| `AdminSeedPassword` | Admin account password |
| `GoogleClientId` | Google OAuth client ID |
| `GoogleClientSecret` | Google OAuth client secret |

## End State

After Week 2, students have:

- Two-path authentication (Admin: email/password, Candidate: Google OAuth)
- Application submission with duplicate prevention
- 13 passing unit tests (9 Job + 4 Application)
- Application deployed to Azure Container Apps

## Key Learning

- **ASP.NET Core Identity**: User management, roles, claims
- **OAuth 2.0**: External providers, callback handling, user creation
- **Authorization**: `[Authorize(Roles)]`, role-conditional UI
- **Security**: Over-posting prevention with `[Bind]`, server-side data assembly, account lockout
- **Azure Container Apps**: Deployment, environment variables, managed environments
- **Azure Container Registry**: Build, push, pull container images
