# SOLUTION: Remove Azure Files volume mount from production

**Date:** 2026-04-07
**Resolves:** BUG-001 (SIGSEGV), BUG-002 ("database is locked"), BUG-003 (SMB incompatibility)
**Environment:** Azure Container Apps (Norway East), `cloudsoft-prod-rg`

## Problem

The production container app has never successfully started. SQLite Identity database operations crash or deadlock on the Azure Files (SMB) volume mount at `/app/data`.

## Root Cause

An Azure Files volume mount was added to production to persist SQLite Identity data across restarts. This mount overwrites `/app/data` — a directory the Dockerfile already creates with correct non-root ownership — with an SMB share that is fundamentally incompatible with SQLite's file I/O (mmap, fcntl locking, journal recovery).

## Proof: Staging works without the mount

The staging environment uses the identical Docker image, the same connection string (`Data Source=/app/data/identity.db`), and has been running successfully. The only difference:

| | Staging (works) | Production (broken) |
|---|---|---|
| Volume mount | None | Azure Files (SMB) at `/app/data` |
| `/app/data` source | Dockerfile `chown -R $APP_UID` | SMB share (overrides Dockerfile) |
| SQLite filesystem | Container ext4 | Azure Files SMB |

## What failed in production (8 revisions, 6 commits)

Every fix attempted to work around SMB limitations with SQLite PRAGMAs. None succeeded because the incompatibility is fundamental — SQLite's own docs state "Do not use SQLite on a network filesystem."

| Commit | Fix attempted | Result |
|--------|--------------|--------|
| `25adc18` | `PRAGMA journal_mode=DELETE` | SIGSEGV (exit 139) |
| `aae5e81` | + `PRAGMA locking_mode=EXCLUSIVE` | SIGSEGV (exit 139) |
| `f059b76` | + `PRAGMA mmap_size=0` | SIGSEGV (exit 139) |
| `4fe3918` | `EnsureCreated()` instead of `Migrate()` | "database is locked" (Error 5) |
| `61c9afb` | `SqliteAzureFilesInterceptor` | SIGSEGV (exit 139) |
| `997943a` | All 3 PRAGMAs in interceptor | SIGSEGV + "database is locked" |

The PRAGMAs cannot prevent the crash because SQLite performs hot journal recovery (using mmap and file locking) during the same call that executes the PRAGMAs. It's a race they can never win.

The Azure Files share confirms the loop: `identity.db` is 0 bytes (no transaction ever committed), and a stale `identity.db-journal` (512 bytes) persists from the last crashed attempt, triggering the same failure on every restart.

## Fix

Remove the Azure Files volume mount from production. Match the staging configuration.

### Bicep changes

**`infra/bicep/modules/container-app.bicep`** — remove `volumeMounts` and `volumes`:

```diff
- volumeMounts: [
-   {
-     mountPath: '/app/data'
-     volumeName: 'identity-volume'
-   }
- ]
  ...
- volumes: [
-   {
-     name: 'identity-volume'
-     storageName: 'identity-storage'
-     storageType: 'AzureFile'
-   }
- ]
```

**`infra/bicep/modules/container-apps-env.bicep`** — remove `identity-storage` resource.

### Code cleanup

Revert the PRAGMA workarounds added in commits `f059b76` through `997943a`:

- Delete `src/CloudSoft.Web/Data/SqliteAzureFilesInterceptor.cs`
- Remove the interceptor registration from `Program.cs` (lines 29–32)
- Revert `EnsureCreated()` back to `Migrate()` in `Program.cs`
- Remove the `if (!app.Environment.IsDevelopment())` branch around database initialization

### Infrastructure cleanup (optional)

- Delete `identity.db` and `identity.db-journal` from the Azure Files share
- Remove the file share and storage account if no longer needed

## Trade-off

Identity data (user accounts, roles) is ephemeral — lost on container restart. This is acceptable because:

1. `Migrate()` recreates the schema on startup
2. The seed code recreates admin and candidate users on startup
3. This is a teaching environment, not a production SaaS
4. Staging already operates this way successfully

If durable Identity storage is needed in the future, the correct path is CosmosDB (already used for domain data), not SQLite on a network filesystem.
