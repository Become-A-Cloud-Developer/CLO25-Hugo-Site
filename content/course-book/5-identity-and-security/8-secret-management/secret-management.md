+++
title = "Secret Management"
program = "CLO"
cohort = "25"
courses = ["BCD", "ACD"]
weight = 80
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/5-identity-and-security/8-secret-management.html)

[Se presentationen på svenska](/presentations/course-book/5-identity-and-security/8-secret-management-swe.html)

---

A connection string committed to a public Git repository is the single most common pathway to production breaches. Once a credential enters version-control history, it is effectively permanent — public repositories are scraped within minutes, and even private repositories grant read access to a much wider circle than the production database itself. Application code therefore cannot embed secrets directly, and the configuration files that hold non-sensitive defaults cannot hold sensitive values either. The configuration system that [Part III established](/course-book/3-application-development/5-configuration-and-environments/) extends naturally into production: secrets move out of files and environment variables and into a managed secret store accessed through identity-based authentication.

## Why production needs more than environment variables

The [configuration and environments chapter](/course-book/3-application-development/5-configuration-and-environments/) introduced the provider chain that ASP.NET Core assembles at startup: `appsettings.json`, environment-specific JSON files, user-secrets in development, environment variables, and command-line arguments. That chain is sufficient for development and for non-sensitive deployment settings, but it falls short for production secrets along three axes.

Plaintext at rest is the first weakness. An environment variable set on a virtual machine or in a container's process environment is readable by anyone with shell access to the host. Platform configuration UIs that hold these values typically display them masked, but the underlying storage is unencrypted, and a compromised host exposes every secret the process can read.

Rotation is the second weakness. A leaked credential must be replaced quickly — within minutes for high-value secrets such as a database connection string or a payment-gateway API key. Environment variables are baked into a deployment manifest; rotating them requires editing the manifest and redeploying the application. The window between detecting a leak and completing the redeploy is the window during which the leaked credential remains valid.

Audit is the third weakness. After an incident, responders need to answer two questions: which secrets were readable from the compromised host, and was any of them actually read? Environment variables provide no audit trail. Reading them is an ordinary file-system or memory operation that leaves no record.

Production secret stores address all three weaknesses by encrypting secrets at rest, supporting versioned rotation without redeployment, and logging every read.

## Azure Key Vault as the production secret store

**Azure Key Vault** is a managed service that stores secrets, encryption keys, and certificates with audit logging and access control; applications retrieve secrets at runtime over HTTPS using a token, never embedding secrets in code or configuration files. A vault is a regional resource provisioned inside an Azure subscription and identified by a DNS name such as `cloudsoft-prod.vault.azure.net`. Inside the vault, three object types coexist: *secrets* (arbitrary strings, the focus of this chapter), *keys* (asymmetric and symmetric cryptographic keys whose private material never leaves the vault), and *certificates* (X.509 certificates with their private keys).

Each secret has a name, a current value, and a version history. Setting a new value does not delete the previous one — the vault retains the old version and assigns it a stable identifier, allowing applications to roll back if a rotation breaks something. The current version is the default when an application reads a secret by name, so rotation is transparent to consumers that follow the recommended pattern of always reading the latest version.

Reads happen over HTTPS to the vault's REST endpoint. The caller sends an OAuth-style bearer token in the `Authorization` header (the same `Authorization: Bearer ...` mechanism the [bearer tokens and JWT chapter](/course-book/5-identity-and-security/5-bearer-tokens-and-jwt/) describes for application APIs), and the vault validates that token against Microsoft Entra ID before returning the secret value. The secret never appears in URL paths, request bodies, or log output, and the TLS channel protects it in transit.

### Versioning and rotation

A secret in Key Vault is identified by name and optionally by version. The URL `https://cloudsoft-prod.vault.azure.net/secrets/MongoConnection` returns the current version; appending a version identifier such as `/secrets/MongoConnection/d4e5f6...` returns that specific historical version. Borrowing the term **key rotation** from the [API keys chapter](/course-book/5-identity-and-security/6-api-keys/), the vault makes rotation a one-line operation: setting a new value publishes a new version while leaving the previous version retrievable. Applications that read the current version pick up the new value the next time they refresh their configuration; applications that pin to a specific version continue using the old value until they are updated.

Rotation policy is therefore a deployment concern, not an application concern. The operational team rotates a database password by writing the new value to Key Vault and updating the database server to accept it; the application restarts (or refreshes its configuration) and begins using the new value without a code change.

## Managed identity removes the bootstrap problem

Reading a secret from Key Vault requires authentication to Entra ID, which raises an obvious question: how does the application authenticate without storing a credential, when the entire point of Key Vault is to avoid storing credentials? The answer is **managed identity** — an Entra ID identity that Azure attaches to a compute resource (VM, App Service, Container App, GitHub Actions runner via federation) and rotates automatically; the application authenticates to other Azure services as that identity without storing any credentials.

The mechanism works at the platform level. When an Azure Container App is provisioned with a system-assigned managed identity, the platform creates an Entra ID service principal whose lifecycle matches the container app, generates the underlying credentials, and exposes a local token endpoint inside the container's runtime. Application code calls that endpoint to obtain an access token for any Azure resource, and the platform handles the credential exchange. No secret ever appears in source code, in environment variables, or in deployment manifests.

The `Azure.Identity` library packages this mechanism behind a single class, `DefaultAzureCredential`, that probes a chain of credential sources in order. In production on Azure, the chain finds the managed identity and uses it. On a developer's laptop, the same chain finds the developer's `az login` session and uses that. The application code is identical in both environments — only the surrounding identity context differs.

## RBAC role assignments grant access at the right scope

A managed identity is an authenticated principal but not an authorized one. Reading a secret from Key Vault requires an explicit grant, made through Azure's role-based access control system. An **RBAC role assignment** in Azure pairs a security principal (user, group, service principal, managed identity) with a role definition (built-in or custom) at a specific scope (subscription, resource group, individual resource); the assignment grants exactly the actions the role allows on resources in that scope.

For Key Vault access from an application, the relevant built-in role is `Key Vault Secrets User`, which grants read access to secret values but not the ability to list, create, modify, or delete secrets. The role is assigned at the smallest scope that satisfies the application's needs — typically the individual vault, occasionally a resource group containing several vaults the application depends on, never the subscription. Granting `Key Vault Secrets User` at subscription scope would let the application read every secret in every vault under that subscription, which violates the principle of least privilege and turns a single compromised application into a subscription-wide breach.

Role assignments are auditable: Azure Activity Log records every assignment creation and deletion, with the principal that made the change, the time, and the affected scope. Combined with Key Vault's own access log (which records every secret read with the calling principal's identity), this produces an end-to-end trail from "who can read what" to "who actually read what, when."

| Role | Permissions | When to assign |
|------|-------------|----------------|
| Key Vault Secrets User | Read secret values | Application identities reading secrets at runtime |
| Key Vault Secrets Officer | Read, create, modify, delete secrets | Operators rotating secrets |
| Key Vault Administrator | All vault management actions | Vault owners during initial setup |
| Key Vault Reader | List secrets but not read values | Auditors and inventory tools |

## Wiring Key Vault into the configuration chain

The configuration provider chain that Part III described accepts a Key Vault provider as one more link in the chain. Once registered, secrets stored in the vault appear as ordinary `IConfiguration` keys, and consumer code that reads `configuration["MongoDB:ConnectionString"]` does not know — and does not need to know — whether the value came from `appsettings.json`, an environment variable, or a vault.

Figure 1 registers Key Vault as a configuration provider in `Program.cs` using `DefaultAzureCredential`.

```csharp
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var builder = WebApplication.CreateBuilder(args);

var vaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrEmpty(vaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(vaultUri),
        new DefaultAzureCredential());
}

builder.Services.AddControllersWithViews();

var app = builder.Build();
```

Figure 1: Registering Azure Key Vault as a configuration provider

The vault URI is itself a configuration value, supplied through an environment variable in production (`KeyVault__Uri=https://cloudsoft-prod.vault.azure.net/`) and absent in development so the block is skipped. `DefaultAzureCredential` resolves to the container app's managed identity at runtime; on a developer machine it falls back to `az login`. The `AddAzureKeyVault` call then enumerates every secret the identity has permission to read, normalizes the names by replacing `--` with `:` (the vault's character constraint translated back into the configuration key separator), and registers them as configuration keys.

A controller that needs the connection string reads it the same way it reads any other configuration value:

```csharp
public class SubscriberController : Controller
{
    private readonly string _connectionString;

    public SubscriberController(IConfiguration configuration)
    {
        _connectionString = configuration["MongoDB:ConnectionString"];
    }
}
```

Figure 2: Reading a Key Vault-sourced secret through `IConfiguration`

The secret named `MongoDB--ConnectionString` in the vault becomes the key `MongoDB:ConnectionString` in `IConfiguration`. The controller is identical to the one shown in [the configuration and environments chapter](/course-book/3-application-development/5-configuration-and-environments/) — only the source of the value changed. The companion exercise [data layer](/exercises/10-webapp-development/3-data-layer/) walks through the connection string pattern; introducing Key Vault on top of that exercise requires adding the vault registration in `Program.cs` and writing the [connection string](/course-book/4-data-access/3-connections-and-transactions/) into the vault rather than into an environment variable. The application code does not change.

## Application secrets versus pipeline secrets

Two different categories of secrets exist in a typical deployment, and they belong in different stores. Application secrets are the values the running application needs at request time: database connection strings, API keys for downstream services, encryption keys for data the application owns. These belong in Key Vault, where the application's managed identity reads them through the configuration provider chain.

Pipeline secrets are the values the deployment pipeline needs at build and release time: container registry credentials, deployment-target subscription IDs, signing keys for build artifacts. These belong in the CI/CD platform's own secret store — for GitHub Actions, that means GitHub Actions secrets and, increasingly, OIDC federation rather than long-lived stored credentials.

OIDC federation eliminates the most dangerous pipeline secret entirely. Instead of storing an Azure service principal's client secret as a GitHub Actions secret, the workflow exchanges a short-lived OIDC token (issued by GitHub to the workflow run) for an Azure access token (issued by Entra ID to a federated credential). No long-lived credential ever exists, the token is scoped to the specific workflow run, and a compromised repository cannot leak a credential that was never created. The mechanism is covered in Part VIII (DevOps and Delivery); from the secret-management perspective, the relevant point is that pipeline secrets and application secrets are operationally separate concerns with separate stores.

## Audit logging and incident response

Every read from Key Vault is logged. Each entry records the calling principal (the managed identity or user), the secret name, the timestamp, the source IP address, and whether the request succeeded. Logs flow into Azure Monitor and are retained according to the diagnostic settings configured on the vault.

The audit trail matters most during incident response. After a suspected breach, the first question is which secrets the compromised principal could have read; the second question is which it actually did. The role assignments answer the first question — they enumerate exactly what the principal had permission to access. The Key Vault access log answers the second question — it shows which permissions were exercised and when. Together they let responders bound the blast radius precisely instead of conservatively assuming everything the principal could access was compromised.

## A checklist for handling secrets

The technical mechanisms above prevent the most common failure modes, but they do not replace operational discipline. Four practices reinforce them:

- Never commit a secret to source control. Pre-commit hooks (`gitleaks`, `truffleHog`) and CI scans catch the obvious cases; a `.gitignore` entry for `appsettings.Production.json` and `secrets.json` catches the easy mistakes.
- Never log a secret. Connection strings and tokens that appear in log output land in centralized log systems where access controls are usually weaker than the secret store's. Structured logging libraries support redaction; use it.
- Never email or chat a secret. Mail and chat history are searchable by anyone with mailbox or workspace access, and forwarded messages multiply the exposure. When a secret must be shared, use a one-time-view tool or the vault's own access controls.
- Rotate on suspicion. If a secret may have leaked — a laptop was lost, an employee left, a log was over-shared — rotate immediately. The cost of an unnecessary rotation is a brief reconfiguration; the cost of skipping a needed rotation is a breach.

## Summary

Secrets in source control are the most common production breach pathway, and neither configuration files nor environment variables provide encryption at rest, rotation, or audit. Azure Key Vault addresses these requirements by storing secrets, keys, and certificates in a managed service that authenticates callers through Entra ID and logs every access. Managed identities let Azure-hosted code authenticate to Key Vault without storing any credential — the platform attaches an identity to the compute resource and rotates the underlying credentials automatically. RBAC role assignments grant the identity the `Key Vault Secrets User` role at the smallest scope that satisfies the application, enforcing least privilege. Once registered as a configuration provider, Key Vault appears in `IConfiguration` exactly like any other source, so application code reads `configuration["MongoDB:ConnectionString"]` without knowing the value came from a vault. Pipeline secrets are a separate concern handled through GitHub Actions secrets and OIDC federation, covered in Part VIII. Audit logs and a short discipline checklist — never commit, never log, never email, rotate on suspicion — close the gap between technical controls and operational reality.
