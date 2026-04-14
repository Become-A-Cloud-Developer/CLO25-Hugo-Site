# BUG-002: SQLite "database is locked" on Azure Files — missing EXCLUSIVE locking mode

**Status:** Open
**Severity:** Critical — production app cannot start
**Date:** 2026-04-07
**Related:** [BUG-001](001-sqlite-mmap-segfault.md) (SIGSEGV — fixed)
**Environment:** Azure Container Apps (Norway East), `cloudsoft-prod-rg`

## Summary

After the SIGSEGV fix (BUG-001), the production app now fails during SQLite Identity database initialization with `SQLite Error 5: 'database is locked'`. All Identity tables are created successfully, but the COMMIT fails because Azure Files (SMB) cannot handle SQLite's `fcntl()` lock upgrades.

The root cause is that `PRAGMA locking_mode=EXCLUSIVE` was accidentally removed during the BUG-001 fix refactoring.

## Affected Revision

| Revision | Image (commit) | Status |
|----------|---------------|--------|
| `--0000002` | `4fe3918` | Failed — "database is locked" |

## Log Timeline (revision `--0000002`, 3rd restart attempt)

```
21:16:49  Container starts — all services initialize OK
21:16:55  PRAGMA mmap_size=0                   ← OK (BUG-001 fix)
          PRAGMA journal_mode=DELETE           ← OK
          SELECT COUNT(*) FROM "sqlite_master"
          CREATE TABLE "AspNetRoles" (...)      ← 7 tables created successfully
          CREATE TABLE "AspNetUsers" (...)
          CREATE TABLE "AspNetRoleClaims" (...)
          CREATE TABLE "AspNetUserClaims" (...)
          CREATE TABLE "AspNetUserLogins" (...)
          CREATE TABLE "AspNetUserRoles" (...)
          CREATE TABLE "AspNetUserTokens" (...)
          CREATE INDEX (7 indexes)             ← all indexes created
          ... 30 second pause ...
21:17:25  SqliteException (0x80004005): SQLite Error 5: 'database is locked'
          at SqliteTransaction.Commit()
          at RelationalDatabaseCreator.EnsureCreated()
          at Program.<Main>$ in /src/Program.cs:line 209
```

## Root Cause

SQLite's default locking mode is NORMAL, which uses a lock escalation protocol during writes:

```
UNLOCKED → SHARED → RESERVED → EXCLUSIVE → UNLOCKED
```

During COMMIT, SQLite must upgrade from SHARED to EXCLUSIVE via `fcntl()` system calls. **Azure Files (SMB) does not support `fcntl()` lock upgrades reliably.** SQLite waits its busy timeout (~30 seconds), then returns Error 5.

With `PRAGMA locking_mode=EXCLUSIVE`, SQLite acquires an exclusive lock once at first access and holds it for the entire connection lifetime — no lock upgrades needed.

## How locking_mode=EXCLUSIVE was lost

| Commit | What happened |
|--------|--------------|
| `aae5e81` | Added `locking_mode=EXCLUSIVE` ← correct |
| `f059b76` | Added `mmap_size=0` but removed `locking_mode=EXCLUSIVE` during refactor |
| `4fe3918` | Switched to `EnsureCreated()` — still no `locking_mode=EXCLUSIVE` |
| `61c9afb` | Added `SqliteAzureFilesInterceptor` — still missing `locking_mode=EXCLUSIVE` |

## Contributing Factor: Concurrent Revision Access

At `21:14:32`, two revisions were scheduled simultaneously on the same Azure Files share:
- `--0000002` (new image `4fe3918`)
- `--byfly1g` (old image `aae5e81`)

Both attempted to open `/app/data/identity.db`. Even after `--byfly1g` was stopped at `21:15:18`, SMB file leases from the old process may have persisted, worsening lock contention for `--0000002` restarts.

## Fix

Add `PRAGMA locking_mode=EXCLUSIVE` to the interceptor in both sync and async paths:

```csharp
// src/CloudSoft.Web/Data/SqliteAzureFilesInterceptor.cs
cmd.CommandText = "PRAGMA mmap_size=0; PRAGMA journal_mode=DELETE; PRAGMA locking_mode=EXCLUSIVE;";
```

All three PRAGMAs are required for SQLite on Azure Files (SMB):

| PRAGMA | Purpose | Failure without it |
|--------|---------|-------------------|
| `mmap_size=0` | Disable memory-mapped I/O | SIGSEGV (exit 139) — BUG-001 |
| `journal_mode=DELETE` | Avoid WAL (requires shared memory on SMB) | Potential WAL corruption |
| `locking_mode=EXCLUSIVE` | Hold exclusive lock, skip lock upgrades | **"database is locked" (Error 5)** |

## References

- [SQLite locking documentation](https://www.sqlite.org/lockingv3.html)
- [PRAGMA locking_mode](https://www.sqlite.org/pragma.html#pragma_locking_mode)
- [Azure Files SMB limitations](https://learn.microsoft.com/en-us/troubleshoot/azure/azure-storage/files/connectivity/files-troubleshoot-smb-connectivity)
