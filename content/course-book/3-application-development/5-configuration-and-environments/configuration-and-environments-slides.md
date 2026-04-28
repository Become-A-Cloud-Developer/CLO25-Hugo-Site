+++
title = "Configuration and Environments"
program = "CLO"
cohort = "25"
courses = ["BCD"]
type = "slide"
date = 2026-04-28
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls = true
progress = true
history = true
center = true
+++

## Configuration and Environments
Part III — Application Development

---

## Why Externalize Configuration
- The same binary must run in **dev, staging, and production** unchanged.
- **Secrets** must not enter source control — git history is permanent.
- Operators change settings without rebuilding the application.
- Multiple deploys of one binary coexist with different settings.

---

## The Provider Chain
- `appsettings.json` — base defaults shipped in source control.
- `appsettings.{Environment}.json` — per-environment **override**.
- **User-secrets** — local development credentials only.
- **Environment variables** — per-deployment values and production secrets.
- Command-line arguments — last-write-wins for diagnostic runs.

---

## IConfiguration as the Read Interface
- `IConfiguration` collapses the provider chain into one **key-value tree**.
- Hierarchical keys use a colon: `MongoDB:ConnectionString`.
- Injected through DI; consumers never know the source provider.
- Returns `null` for keys no provider supplies.

---

## Environment Selection
- `ASPNETCORE_ENVIRONMENT` selects the active environment.
- Defaults to `Production` when unset — safer than dev defaults.
- Loads `appsettings.<env>.json` and gates developer-only features.
- `app.Environment.IsDevelopment()` controls runtime behaviour.

---

## User-Secrets Workflow
- Stored outside the project tree, in the user's profile.
- Linked to the project by `UserSecretsId` GUID in `.csproj`.
- CLI: `dotnet user-secrets init`, `set`, `list`.
- Active **only** when the environment is `Development`.

---

## Environment Variable Override Pattern
- `MongoDB:ConnectionString` becomes `MongoDB__ConnectionString`.
- Double underscore replaces colon — most OSes ban colons in var names.
- Provider sits later in the chain, so it overrides JSON files.
- Standard mechanism for production secrets and per-deployment values.

---

## Strongly-Typed IOptions
- `IOptions<T>` binds a configuration section to a typed C# class.
- Removes magic strings — typos become compile errors.
- Section path lives in one registration call, not every consumer.
- `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` for change-aware cases.

---

## Dev vs Production Secret Stores
- User-secrets and env vars store secrets in **plaintext at rest**.
- Production needs encryption, access audit, rotation.
- Managed secret stores (Azure Key Vault) plug into the same chain.
- Detailed in Part V — Managed Identities and Key Vault.

---

## Questions?
