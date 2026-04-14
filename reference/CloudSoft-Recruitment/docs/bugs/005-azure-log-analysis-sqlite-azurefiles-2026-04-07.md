# Azure Log Analysis: SQLite on Azure Files still failing after latest fixes

**Status:** Investigated  
**Date:** 2026-04-07  
**Environment:** Azure Container Apps, `cloudsoft-prod-rg`  
**App:** `cloudsoft-prod-app`  
**Storage:** Azure Files share `identity-data` on `csprodst3tfb22c4kec6k`  

## Scope

This report is based on direct Azure CLI investigation of:

- Container App revisions and replica state
- Container App console logs
- Container App system/platform logs
- Azure Files share contents

No code was changed during this investigation.

## Executive Summary

The production failure is **not** caused by a missing `locking_mode=EXCLUSIVE` anymore. The deployed code already contains all three SQLite PRAGMAs:

- `mmap_size=0`
- `journal_mode=DELETE`
- `locking_mode=EXCLUSIVE`

Despite that, the app still fails on the Azure Files mount. The logs show two separate SQLite failure modes are still present:

1. `SQLite Error 5: 'database is locked'`
2. Container termination with exit code `139`

The strongest evidence is that this still happens on the newest deployments, including:

- revision `cloudsoft-prod-app--4r0hvtu` with image `997943a142a1ddf475341f47348e1553caf4d1d3`
- revision `cloudsoft-prod-app--0000002` created at `2026-04-07T21:46:19Z` with image `d0ec8541f2437cbe497df03a8ef4633bd86187a7`

The problem is now clearly at the platform/storage boundary: **SQLite is still not reliable on the Azure Files SMB mount used for `/app/data/identity.db`**.

## What I Verified

### 1. The deployed app still mounts Azure Files for the Identity DB

Current Container App config still uses:

- `ConnectionStrings__Identity = Data Source=/app/data/identity.db`
- volume mount `/app/data`
- volume `identity-volume`
- `storageType = AzureFile`

### 2. The local workspace code already contains all three PRAGMAs

Current workspace file:

- `src/CloudSoft.Web/Data/SqliteAzureFilesInterceptor.cs`

Current interceptor command text:

```csharp
PRAGMA mmap_size=0; PRAGMA journal_mode=DELETE; PRAGMA locking_mode=EXCLUSIVE;
```

So the earlier BUG-002 diagnosis is now outdated as the primary explanation.

### 3. EF Core still executes SQLite operations that are incompatible with Azure Files

For revision `cloudsoft-prod-app--4r0hvtu`, Log Analytics shows:

- `2026-04-07T21:34:32Z`: `PRAGMA journal_mode = 'wal';`
- same timestamp: `Unhandled exception. Microsoft.Data.Sqlite.SqliteException (0x80004005): SQLite Error 5: 'database is locked'.`

This matters because it shows the interceptor is not enough to fully control SQLite behavior on this path. EF Core still reaches a WAL-related create/init path during `EnsureCreated()`.

### 4. The newest deployment still fails with the same lock error

For revision `cloudsoft-prod-app--0000002` created at `2026-04-07T21:46:19Z`:

- the app starts and logs schema creation statements
- `2026-04-07T21:47:15Z`: `fail: Microsoft.EntityFrameworkCore.Database.Transaction[20205]`
- `2026-04-07T21:47:15Z`: `Unhandled exception. Microsoft.Data.Sqlite.SqliteException (0x80004005): SQLite Error 5: 'database is locked'.`

So the latest deployment did **not** resolve the issue.

### 5. Exit code 139 is still observed on the latest PRAGMA-enabled rollout

For revision `cloudsoft-prod-app--4r0hvtu`, system logs show:

- `2026-04-07T21:35:17Z`: `Container 'recruitment-api' was terminated with exit code '139'`

That same revision also logged `SQLite Error 5` immediately before the crash loop. In other words, the PRAGMA-complete build still does not produce stable startup behavior on Azure Files.

### 6. The Azure Files share is currently empty

Using the storage account key, `az storage file list` returned:

```json
[]
```

That means the earlier "stale journal file is definitely present right now" claim is **not true at the time of this investigation**.

Important nuance:

- this weakens the "stale journal is the current blocker" theory
- it does **not** clear Azure Files as the root problem
- the latest revisions still fail on the Azure Files-backed path even with an empty share

## Most Important New Finding

The most useful correction to the earlier analysis is this:

> The current failure is no longer best explained as "one missing PRAGMA" or "one stale journal file".

What the logs now support is broader:

- EF Core + SQLite startup/init still reaches WAL and transactional behaviors on this database path
- Azure Files SMB still cannot support those behaviors reliably
- repeated tuning around PRAGMAs is not stabilizing the deployment

## Why the current workaround is insufficient

`Program.cs` now contains this comment and flow:

```csharp
// Pre-create the DB file so EF Core's EnsureCreated skips SqliteDatabaseCreator.Create(),
// which would set PRAGMA journal_mode=wal (incompatible with Azure Files/SMB).
context.Database.OpenConnection();
context.Database.CloseConnection();
context.Database.EnsureCreated();
```

But production logs prove `EnsureCreated()` still reaches the create path on Azure:

- `SqliteDatabaseCreator.Create()`
- `PRAGMA journal_mode = 'wal';`

So the intended mitigation is not holding in the real Azure Files deployment.

## Conclusion

The current production issue should be treated as:

**SQLite on Azure Files (SMB) is not a deployable design for this app's Identity database.**

That conclusion is now supported by direct Azure evidence from multiple revisions, including the latest deployment attempt on `2026-04-07`.

## Recommended Solution

### Recommended for this teaching/demo environment

Stop storing the Identity SQLite database on Azure Files.

Use one of these instead:

1. **Best short-term fix:** remove the Azure Files mount and keep SQLite on the container's local filesystem.
2. **Best long-term fix:** move Identity to a real service database instead of SQLite-on-SMB.

### Why option 1 is the right immediate fix

For this repo and environment, local ephemeral SQLite is the fastest way to restore a working deployment because:

- it removes the Azure Files SMB incompatibility entirely
- it avoids more time spent chasing PRAGMA and EF Core startup edge cases
- the app already recreates schema/users on startup, which is acceptable for a teaching environment

### Trade-off

If SQLite is moved to local container storage:

- Identity data will not survive container replacement/redeployment

For a course/demo environment, that is likely acceptable.

If persistence is required, SQLite should be replaced rather than kept on Azure Files.

## What I would do next

1. Remove the Azure Files mount from the Container App for the Identity database path.
2. Use local container storage for `identity.db`.
3. Keep the interceptor PRAGMAs if desired, but treat them as harmless defense-in-depth rather than the real fix.
4. If persistent identity data is required later, move Identity to a supported database service.

## Azure CLI evidence used

The investigation used `az` commands in these areas:

- `az containerapp show`
- `az containerapp revision list`
- `az containerapp revision show`
- `az containerapp replica list`
- `az containerapp logs show`
- `az monitor log-analytics query`
- `az storage file list`

## Bottom Line

Do **not** spend more time trying to stabilize SQLite on the Azure Files mount with more PRAGMAs or startup-order tweaks. The logs now show that the latest fixes still fail in production. The practical fix is to **remove Azure Files from the Identity SQLite path** or **replace SQLite with a supported persistent database**.
