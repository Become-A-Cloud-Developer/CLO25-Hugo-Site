+++
title = "Object Storage and File Uploads"
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

## Object Storage and File Uploads
Part IV — Data Access

---

## Why not the database
- Databases optimize for **small, indexed** operations, not multi-megabyte payloads
- Large `BLOB` columns inflate **transaction logs** and backup windows
- Structured data is read by predicate; binary data is read by **identity** (URL)
- A foreign-key-like reference ties the database row to the blob URL

---

## Blobs and containers
- A **blob** is an immutable byte sequence with a path-like name, content type, and metadata
- A **container** is the top-level grouping for blobs in a storage account
- The blob namespace is **flat per container** — slashes are conventional, not real directories
- Access policies and access tiers attach at the container level

---

## The immutability model
- Blobs do not support **partial writes** — uploads replace the entire blob
- No `UPDATE` of byte 1024 — the smallest write is a complete rewrite
- Concurrent uploads to the same name: one wins entirely, the other is overwritten
- Immutability is what lets object storage **replicate aggressively** across data centers

---

## Access tiers
- **Hot** — highest storage cost, lowest read cost, instant access
- **Cool** — lower storage cost, higher read cost, instant access
- **Archive** — cheapest storage, hours-long **rehydration** before any read
- Lifecycle policies move blobs between tiers automatically by age or access pattern

---

## SAS tokens
- A **SAS** is a time-limited, scope-limited URL signed with the account key
- Encodes permissions, scope, expiry, and signature in the query string
- Lets browsers upload or download **directly** without bytes flowing through the app server
- User delegation SAS (signed via Entra ID) is preferred over service SAS in production

---

## Streaming uploads
- The SDK breaks the source stream into **blocks** (4–100 MB)
- Blocks upload in **parallel** and retry independently on failure
- A final `Commit Block List` atomically assembles them into one blob
- An ASP.NET request body is a `Stream` — pipe it straight into `BlobClient.UploadAsync`

---

## A service method
- `BlobServiceClient` is created once and reused — it is a thin HTTP-client wrapper
- `GetBlobContainerClient` and `GetBlobClient` are **lightweight** — no network calls
- `UploadAsync(stream, overwrite: true)` does the actual streaming transfer
- Setting `BlobHttpHeaders.ContentType` ensures correct rendering on later download

---

## Blob storage vs database BLOB
- **Blob storage** wins for anything served over HTTP or larger than a few hundred KB
- **Database column** wins for small, transactionally-coupled binary data
- Blob storage costs cents per GB; database storage costs dollars per GB
- SAS-based browser-direct access is **only possible** with blob storage

---

## Questions?
