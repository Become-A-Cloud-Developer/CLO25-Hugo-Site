# BUG-001: SQLite failures on Azure Files (SMB) — crash loop in production

**Status:** Open (partially fixed, still failing)
**Severity:** Critical — production app cannot start
**Date:** 2026-04-07
**Environment:** Azure Container Apps (Norway East), `cloudsoft-prod-rg`

## Summary

The production container app fails to start due to SQLite incompatibilities with the Azure Files (SMB) volume mount used for the Identity database. The failure has evolved through multiple fix attempts, progressing from SIGSEGV crashes to "database is locked" errors. The app never reaches the point of listening on port 8080, so all health probes fail and the container enters a crash loop.

## Failure Timeline (all revisions)

### Phase 1 — SIGSEGV crashes (revisions prior to mmap fix)

| Revision | Image (commit) | Failure | Root Cause |
|----------|---------------|---------|------------|
| `--0000005` | `25adc18` | Exit 139 (SIGSEGV) | mmap on SMB |
| `--0000006` | `aae5e81` | Exit 139 (SIGSEGV) | mmap on SMB |
| `--0000001` | `f059b76` | Exit 139 (SIGSEGV) | Concurrent revision w/ old image |

**Fix applied:** Commit `f059b76` added `PRAGMA mmap_size=0;` — resolved SIGSEGV.

### Phase 2 — "database is locked" errors (current)

| Revision | Image (commit) | Failure | Root Cause |
|----------|---------------|---------|------------|
| `--0000002` | `4fe3918` | SQLite Error 5: "database is locked" | Missing `locking_mode=EXCLUSIVE` |

**Partial fix applied:** Commit `4fe3918` switched `Migrate()` → `EnsureCreated()` and added `SqliteAzureFilesInterceptor`. But interceptor is missing `PRAGMA locking_mode=EXCLUSIVE`, so COMMIT still fails.

## Detailed Log Analysis

### Phase 1: Revision `--0000006` (commit `aae5e81`)

```
20:59:57  Container starts — services initialize OK
          OAuth disabled / Azure Blob Storage / MongoDB / Countries API
20:59:58  Key Vault enabled / Insights enabled
21:00:03  PRAGMA journal_mode=DELETE           ← OK
          PRAGMA locking_mode=EXCLUSIVE        ← OK
          Acquiring an exclusive lock for migration application...
          SELECT COUNT(*) FROM "sqlite_master" ← last statement before crash
21:00:35  ContainerBackOff
21:00:38  Container terminated with exit code 139 (SIGSEGV)
```

**Cause:** SQLite used mmap to read database pages. Azure Files (SMB) cannot service mmap page faults → SIGSEGV.

### Phase 2: Revision `--0000002` (commit `4fe3918`) — 3rd restart attempt

```
21:16:49  Container starts — services initialize OK
21:16:50  Key Vault enabled / Insights enabled
21:16:55  PRAGMA mmap_size=0                   ← NEW — prevents SIGSEGV
          PRAGMA journal_mode=DELETE           ← OK
          SELECT COUNT(*) FROM "sqlite_master" ← checks for existing tables
          CREATE TABLE "AspNetRoles" (...)      ← all 7 Identity tables created
          CREATE TABLE "AspNetUsers" (...)
          CREATE TABLE "AspNetRoleClaims" (...)
          CREATE TABLE "AspNetUserClaims" (...)
          CREATE TABLE "AspNetUserLogins" (...)
          CREATE TABLE "AspNetUserRoles" (...)
          CREATE TABLE "AspNetUserTokens" (...)
          CREATE INDEX (7 indexes)             ← all indexes created
          ... 30 second pause ...
21:17:25  ERROR: SqliteException (0x80004005): SQLite Error 5: 'database is locked'
          at SqliteTransaction.Commit()
          at MigrationCommandExecutor.Execute()
          at RelationalDatabaseCreator.EnsureCreated()
          at Program.<Main>$(String[] args) in /src/Program.cs:line 209
```

**Cause:** Without `locking_mode=EXCLUSIVE`, SQLite uses default NORMAL locking. During COMMIT, SQLite must upgrade from SHARED → RESERVED → EXCLUSIVE file lock. Azure Files (SMB) does not support `fcntl()` lock upgrades reliably. SQLite waits 30 seconds (default busy timeout), then gives up with Error 5.

### Concurrent Revision Issue

At `21:14:32`, two revisions were scheduled simultaneously:
- `--0000002` (image `4fe3918` — new code)
- `--byfly1g` (image `aae5e81` — old code without mmap fix)

Both attempted to access the same SQLite database on the shared Azure Files mount. The old revision (`--byfly1g`) was eventually stopped, but its SMB file lease may have persisted, contributing to locking failures in subsequent `--0000002` restart attempts.

## Root Causes

### 1. mmap on Azure Files (SMB) — **FIXED** in `f059b76`

SQLite uses memory-mapped I/O by default. Azure Files (SMB) cannot handle mmap → SIGSEGV (exit 139).

**Fix:** `PRAGMA mmap_size=0;` disables mmap entirely.

### 2. fcntl() lock upgrades on Azure Files (SMB) — **NOT FIXED**

SQLite's default NORMAL locking requires `fcntl()` lock upgrades (SHARED → EXCLUSIVE) during COMMIT. Azure Files (SMB) doesn't support these lock upgrades reliably → "database is locked" (Error 5).

**Fix needed:** `PRAGMA locking_mode=EXCLUSIVE;` acquires an exclusive lock once and holds it for the connection lifetime, avoiding lock upgrades entirely.

### 3. Concurrent revisions accessing same SQLite file — **NOT ADDRESSED**

Multiple revisions deployed simultaneously both tried to access `/app/data/identity.db`. With `Single` revision mode, this shouldn't happen, but during rolling transitions the old and new revision overlap briefly.

## Infrastructure Context

```
Volume mount:    /app/data → identity-volume
Storage:         identity-storage (AzureFile)
Account:         csprodst3tfb22c4kec6k
Share:           identity-data
Access mode:     ReadWrite
Container:       0.5 CPU / 1Gi memory
Revision mode:   Single
```

## Current State of Code (commit `61c9afb`, not yet deployed)

The other agent added a `SqliteAzureFilesInterceptor` that applies PRAGMAs to every EF Core connection:

```csharp
// src/CloudSoft.Web/Data/SqliteAzureFilesInterceptor.cs
cmd.CommandText = "PRAGMA mmap_size=0; PRAGMA journal_mode=DELETE;";
```

**Problem:** The interceptor is missing `PRAGMA locking_mode=EXCLUSIVE`. This means the "database is locked" error will persist in the next deployment.

## Required Fix

The interceptor must include all three PRAGMAs:

```csharp
// src/CloudSoft.Web/Data/SqliteAzureFilesInterceptor.cs
cmd.CommandText = "PRAGMA mmap_size=0; PRAGMA journal_mode=DELETE; PRAGMA locking_mode=EXCLUSIVE;";
```

| PRAGMA | Purpose | What happens without it |
|--------|---------|------------------------|
| `mmap_size=0` | Disable memory-mapped I/O | SIGSEGV (exit 139) |
| `journal_mode=DELETE` | Avoid WAL mode (requires shared memory) | Potential WAL corruption on SMB |
| `locking_mode=EXCLUSIVE` | Hold exclusive lock for connection lifetime | "database is locked" (Error 5) on COMMIT |

## Commit History

| Commit | Description | Result |
|--------|------------|--------|
| `25adc18` | Added `journal_mode=DELETE` | Still crashed (mmap) |
| `aae5e81` | Added `locking_mode=EXCLUSIVE` | Still crashed (mmap) |
| `f059b76` | Added `mmap_size=0` | Resolved SIGSEGV, but removed `locking_mode` |
| `4fe3918` | Switched `Migrate()` → `EnsureCreated()` | "database is locked" on COMMIT |
| `61c9afb` | Added `SqliteAzureFilesInterceptor` | Not deployed yet; still missing `locking_mode=EXCLUSIVE` |

## References

- [SQLite mmap documentation](https://www.sqlite.org/mmap.html)
- [SQLite locking modes](https://www.sqlite.org/pragma.html#pragma_locking_mode)
- [Azure Files SMB known limitations](https://learn.microsoft.com/en-us/troubleshoot/azure/azure-storage/files/connectivity/files-troubleshoot-smb-connectivity)
