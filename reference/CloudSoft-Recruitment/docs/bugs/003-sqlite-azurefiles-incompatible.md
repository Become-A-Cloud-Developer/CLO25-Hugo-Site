# BUG-003: SQLite fundamentally incompatible with Azure Files (SMB) — stale journal + broken locking

**Status:** Open
**Severity:** Critical — production app cannot start
**Date:** 2026-04-07
**Supersedes:** [BUG-001](001-sqlite-mmap-segfault.md), [BUG-002](002-sqlite-database-locked.md)
**Environment:** Azure Container Apps (Norway East), `cloudsoft-prod-rg`

## Summary

After 6 fix attempts across 8+ revisions, the production app still cannot start. The root cause is deeper than individual PRAGMAs can fix: **SQLite's file I/O model is fundamentally incompatible with Azure Files (SMB)**. Three separate mechanisms fail:

1. **mmap** — causes SIGSEGV (BUG-001)
2. **fcntl() lock upgrades** — causes "database is locked" (BUG-002)
3. **Hot journal recovery** — triggers both mmap and locking failures before any PRAGMA can be set

## Evidence: Azure Files Share Contents

```
identity.db          0 bytes     Last modified: 21:22:46
identity.db-journal  512 bytes   Last modified: 21:31:13
```

- The database is **empty** — no transaction has ever committed successfully
- A **stale journal file** persists from a crashed transaction, causing a cascading failure loop on every restart

## The Crash Loop

```
┌─ Container starts
│  App initializes services (OAuth, Blob, MongoDB, Key Vault, Insights)
│
├─ SQLite opens /app/data/identity.db
│  Finds identity.db-journal → triggers hot journal recovery
│  Recovery uses mmap before PRAGMA mmap_size=0 can be set
│
├─ Path A: SIGSEGV (exit 139)                     ← most restarts
│  mmap page fault on SMB share during journal recovery
│
├─ Path B: "database is locked" (SQLite Error 5)   ← some restarts
│  Journal recovery succeeds, EnsureCreated() creates tables,
│  COMMIT fails because fcntl() lock upgrade fails on SMB
│  (RESERVED → EXCLUSIVE upgrade not supported)
│
├─ Transaction rolls back → identity.db stays 0 bytes
│  New stale journal file is written
│
└─ Container killed by startup probe → restart → same loop
```

## Why PRAGMAs Cannot Fix This

The interceptor (`SqliteAzureFilesInterceptor`) sets PRAGMAs on the `ConnectionOpened` event:

```csharp
cmd.CommandText = "PRAGMA mmap_size=0; PRAGMA journal_mode=DELETE; PRAGMA locking_mode=EXCLUSIVE;";
```

**The timing problem:** When SQLite opens a database and finds a journal file, it performs hot journal recovery DURING the first `sqlite3_prepare_v2()` call — which is the call used to execute the PRAGMAs themselves. The recovery process may use mmap and file locking before the PRAGMAs take effect.

| PRAGMA | Prevents | But... |
|--------|----------|--------|
| `mmap_size=0` | SIGSEGV from mmap reads | Journal recovery may mmap before this runs |
| `journal_mode=DELETE` | WAL corruption on SMB | Doesn't prevent recovery of existing journal |
| `locking_mode=EXCLUSIVE` | Lock upgrade failures | First lock still requires fcntl() upgrade |

## Complete Revision History

| Revision | Image (commit) | Failure | Notes |
|----------|---------------|---------|-------|
| `--0000005` | `25adc18` | Exit 139 (SIGSEGV) | journal_mode=DELETE only |
| `--0000006` | `aae5e81` | Exit 139 (SIGSEGV) | Added locking_mode=EXCLUSIVE |
| `--0000001` | `f059b76` | Exit 139 (SIGSEGV) | Added mmap_size=0 |
| `--0000002` | `4fe3918` | "database is locked" | Switched to EnsureCreated, removed locking_mode |
| `--byfly1g` | `aae5e81` | Startup probe 404 | Concurrent revision with old image |
| `--0000003` | `61c9afb` | Exit 139 (SIGSEGV) | Added interceptor (no locking_mode) |
| `--0000004` | `997943a` | Exit 139 + "database is locked" | All 3 PRAGMAs — still fails |

## Root Cause (actual)

The Azure Files volume mount was added to give the non-root container user a writable path for SQLite. **But the Dockerfile already handles this:**

```dockerfile
RUN mkdir -p /app/data/keys && chown -R $APP_UID /app/data
USER $APP_UID
```

The directory `/app/data` exists with correct ownership inside the container image. The Azure Files volume mount **overwrites** it at runtime, replacing a working local directory with a broken SMB share. The mount is unnecessary and is the sole source of all failures.

## Fix: Remove the Azure Files volume mount

Remove the volume mount from the container app. SQLite will use the container's local ext4 filesystem at `/app/data/identity.db`, which supports mmap and POSIX locking correctly.

**Bicep changes needed:**
1. `infra/bicep/modules/container-app.bicep` — remove `volumeMounts` and `volumes` sections
2. `infra/bicep/modules/container-apps-env.bicep` — remove `identity-storage` resource
3. Optionally remove the storage account file share (cleanup)

**Code cleanup:**
1. Remove `SqliteAzureFilesInterceptor` — not needed on local filesystem
2. Revert `EnsureCreated()` back to `Migrate()` — migration locking works fine on ext4
3. Remove SMB PRAGMA workarounds from `Program.cs`

**Trade-off:** Identity data (user accounts) is ephemeral — lost on container restart. But `EnsureCreated()`/`Migrate()` recreates the schema and the seed code recreates admin/candidate users on every startup. For a teaching environment this is acceptable. (Identity persistence can be solved later with CosmosDB if needed.)

## Infrastructure Context

```
Volume mount:    /app/data → identity-volume
Storage:         identity-storage (AzureFile, SMB)
Account:         csprodst3tfb22c4kec6k
Share:           identity-data
Access mode:     ReadWrite
Container:       0.5 CPU / 1Gi memory
Revision mode:   Single
```

## References

- [SQLite on network filesystems](https://www.sqlite.org/useovernet.html) — "Do not use SQLite on a network filesystem"
- [SQLite mmap documentation](https://www.sqlite.org/mmap.html)
- [SQLite hot journal recovery](https://www.sqlite.org/lockingv3.html#hot_journals)
- [Azure Files SMB limitations](https://learn.microsoft.com/en-us/troubleshoot/azure/azure-storage/files/connectivity/files-troubleshoot-smb-connectivity)
