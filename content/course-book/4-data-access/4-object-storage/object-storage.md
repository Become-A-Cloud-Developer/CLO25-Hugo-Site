+++
title = "Object Storage and File Uploads"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 40
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/4-data-access/4-object-storage.html)

[Se presentationen på svenska](/presentations/course-book/4-data-access/4-object-storage-swe.html)

---

Web applications routinely accept files from users — profile pictures, document attachments, generated reports, video uploads. Storing those bytes inside a relational table or document collection wastes the strengths of a database and exposes its weaknesses: query planners cannot index a JPEG, transactions hold locks while megabytes flush to disk, and backup windows balloon as the row count stays the same but the row size grows by a thousand. A database is a poor fit for any binary blob much past a few kilobytes, and the moment a feature reaches profile pictures or attachments the application needs a different home for those bytes — somewhere designed for large payloads, accessed over HTTP and priced per gigabyte rather than per query.

## Why databases struggle with large binary payloads

A database engine optimizes for many small operations: row-level locks, B-tree indexes, query plans that touch a handful of pages. Inserting a 50 MB image into a `BLOB` column or an embedded base64 field upends those assumptions. Each write moves a large value through the transaction log, the log replicates to standbys, and every backup carries the full payload along with the structured data the application actually queries. Read paths suffer too: a `SELECT` that does not project the binary column still pays for the row's expanded storage, and any column that does include the payload sends it across the wire even when the caller only needed a thumbnail URL.

The core mismatch is access pattern. Structured data is read by predicate: "the orders for customer 42." Binary payloads are read by identity: "the image at this URL." A system that excels at the first will be mediocre at the second, so cloud platforms separate the two — application data goes to a database, large binary data goes to an object store, and a simple foreign-key-like reference (a path or URL) ties them together.

[Object storage](/course-book/2-infrastructure/storage/3-storage/) was introduced in Part II as one of three primary storage architectures, alongside block storage and file storage. This chapter narrows the focus to how application code interacts with that storage: how blobs are organized, how access is delegated, and how an upload travels from a browser through a service method into the storage account.

## Blobs and containers

A **blob** (Binary Large Object) is the unit of storage in an object store: an immutable byte sequence with a path-like name, optional metadata key-value pairs, and a content type; blobs are written and read whole through HTTP APIs rather than queried like database rows. A blob can be a 200-byte text file, a 4 GB video, or anything in between — the store treats them uniformly as opaque sequences of bytes addressed by name.

A **container** in object storage is the top-level grouping for blobs, somewhat like a bucket in S3 or a directory in a filesystem; access policies and access tiers are typically configured at the container level. A storage account holds one or more containers, and each container holds zero or more blobs. The container name is the first segment of every blob URL the account exposes:

```text
https://clo25assets.blob.core.windows.net/profile-images/user-42/avatar.png
                                          └─ container ─┘└──── blob name ────┘
```

The slash characters inside the blob name are conventional separators, not real directories. Object storage has a flat namespace per container; the SDK lets code list blobs by prefix (`user-42/`) which makes the namespace look hierarchical, but no `mkdir` operation exists and no rename moves directories. A blob named `user-42/avatar.png` is just a blob whose name happens to contain a slash.

### The immutability model

Blobs do not support partial writes. Uploading a new version of `avatar.png` does not patch the existing blob — it replaces it. The previous bytes are gone the moment the upload completes, unless versioning or soft-delete is enabled at the account level. There is no `UPDATE` that changes byte 1024 of an existing blob; the smallest write is a complete rewrite of the blob (or, for block blobs, a coordinated replacement of one or more named blocks).

This immutability is what allows object storage to scale horizontally and to replicate aggressively across data centers. A row in a relational database has to be the single authoritative copy; a write must invalidate every cached read and propagate to every replica before the next read sees consistent data. A blob is content-addressable enough that the storage system can serve any cached copy until the new write completes, then atomically swap to the new version. Application code benefits from the simplicity (no merge logic, no partial updates) and pays for it in reasoning: if two requests upload the same blob name concurrently, one wins entirely and the other is overwritten entirely.

## Access tiers as the cost-vs-latency knob

Not all stored data has the same access pattern. A user's current profile picture is read every time their profile renders; a five-year-old quarterly report is read once a year, if ever. Charging the same per-gigabyte storage rate for both wastes money on the rarely-read data and leaves no headroom to optimize the hot path.

An **access tier** is the storage class a blob is assigned to — Hot, Cool, or Archive in Azure Blob Storage — that trades retrieval latency for storage cost; Hot tier costs more to store but is read instantly, while Archive tier is cheap to store but takes hours to rehydrate. The tiers form a sliding scale:

| Tier | Storage cost | Read cost | Read latency | Typical use |
|------|--------------|-----------|--------------|-------------|
| Hot | Highest | Lowest | Milliseconds | Active assets, user uploads |
| Cool | Lower | Higher | Milliseconds | Backups, infrequent access |
| Archive | Lowest | Highest | Hours (rehydration) | Long-term retention, compliance |

Hot and Cool blobs respond to GET requests immediately, like any HTTP endpoint. An Archive blob does not — a read against it returns an error until the blob is rehydrated by an explicit request, which moves the data back to Hot or Cool over a window measured in hours. The decision to archive is therefore not just about cost; it is about accepting that any read from this point forward will be a multi-hour operation.

Tiering can be set at the container level (default for new blobs) or per blob, and a lifecycle management policy can move blobs between tiers automatically based on age or access patterns. A common pattern is to upload to Hot, transition to Cool after 30 days of no access, and to Archive after 90 — the application code does not change, only the bill.

## SAS tokens for delegated access

A storage account has a primary key. That key grants full read/write/delete on every blob in every container in the account, and embedding it in client-side code, mobile apps, or short-lived CI jobs is unsafe — the key is impossible to revoke without rotating it, which breaks every other consumer at the same time. The standard mechanism for granting limited access is the SAS token.

A **SAS** (Shared Access Signature) is a time-limited, scope-limited URL signed with the storage account key that grants the bearer permission to read or write a specific blob or container without exposing the account key itself; SAS tokens are the standard way to delegate temporary access to clients. A SAS is just a query string appended to the blob URL — the storage service validates the signature, checks the expiry, and authorizes the operation if everything matches:

```text
https://clo25assets.blob.core.windows.net/profile-images/user-42/avatar.png
  ?sv=2023-11-03
  &sr=b
  &sp=r
  &se=2026-04-28T17:00:00Z
  &sig=...
```

The parameters encode permissions (`sp=r` means read-only), scope (`sr=b` means a single blob), expiry (`se=...` is the deadline after which the signature is invalid), and the signature itself. Anyone holding this URL can issue exactly one type of request against exactly one resource for a bounded window. After the expiry, the URL stops working with no server-side cleanup needed.

SAS tokens come in two flavors that matter for application code. A *service SAS* is signed directly with the account key and is the simpler form. A *user delegation SAS* is signed with a key that the storage service issues against an Entra ID identity, so revoking the identity revokes every SAS it has signed — the better choice when production code uses a managed identity to talk to storage. Either way, the client-facing pattern is the same: the application server generates a SAS scoped to one blob, returns it to the browser, and the browser uploads or downloads directly to storage without the upload bytes flowing through the application server.

## Streaming uploads from the SDK

A naive upload reads the entire file into memory, then writes the buffer to storage. This works for a 100 KB avatar; it fails for a 4 GB video on a server with 512 MB of RAM. The Azure Storage SDK sidesteps the problem with streaming uploads.

A **streaming upload** transfers a blob to the storage service in chunks rather than buffering the entire payload in memory first; the storage SDK handles chunking, parallelism, and retry, allowing the client to upload arbitrarily large files with bounded memory. The SDK breaks the source stream into blocks (typically 4 MB to 100 MB each), uploads them in parallel over multiple HTTP connections, and finally issues a `Commit Block List` operation that atomically assembles the blocks into a single blob. If a block fails, only that block retries — the entire upload does not restart.

For ASP.NET Core, the request body itself is a `Stream`, which means a controller can pipe the multipart form section directly into `BlobClient.UploadAsync` without ever materializing the file in memory. The pipe is the upload — bytes arrive from the browser, flow through the framework's pipeline, and land in storage in a single bounded-memory pass.

### A worked example

The following service method, drawn from the data-layer exercise, uploads a file to a container and returns the blob's public URL. The service is registered with [dependency injection](/course-book/3-application-development/6-dependency-injection/) and reads its [connection string](/course-book/4-data-access/3-connections-and-transactions/) from [IConfiguration](/course-book/3-application-development/5-configuration-and-environments/):

```csharp
public class ImageStorageService
{
    private readonly BlobContainerClient _container;

    public ImageStorageService(IConfiguration config)
    {
        var connectionString = config["AzureStorage:ConnectionString"];
        var serviceClient = new BlobServiceClient(connectionString);
        _container = serviceClient.GetBlobContainerClient("profile-images");
    }

    public async Task<Uri> UploadImageAsync(string filename, Stream content, string contentType)
    {
        var blobClient = _container.GetBlobClient(filename);
        var headers = new BlobHttpHeaders { ContentType = contentType };

        await blobClient.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = headers
        });

        return blobClient.Uri;
    }
}
```

Several details carry weight. `BlobServiceClient` is created once per connection string and is safe to hold for the lifetime of the application — it is a thin wrapper around an HTTP client, registered as a Singleton in `Program.cs` and injected into services that handle file uploads. `GetBlobContainerClient` and `GetBlobClient` are lightweight; they do not make network calls, they just build URLs. The actual network traffic happens inside `UploadAsync`, which streams `content` to storage in blocks. Setting `BlobHttpHeaders.ContentType` to the form upload's MIME type means the browser will later receive the correct `Content-Type` header when it fetches the blob, so an uploaded `image/png` renders as an image rather than downloading as `application/octet-stream`. If the same `filename` already exists, `UploadAsync` throws `BlobAlreadyExists` — adding `new BlobUploadOptions { Conditions = null }` and passing `overwrite: true` via the simpler overload (`await blobClient.UploadAsync(content, overwrite: true)`) makes the upload replace any existing blob of the same name, which is the right default when the filename includes a per-user prefix that is unique by construction.

The presentation layer (a controller action accepting a multipart form) calls this service without knowing anything about block sizes, retry policy, or the structure of a blob URL. The data layer hides the storage SDK behind a service method whose only contract is "stream in, URL out."

## Content type and metadata

A blob carries more than its bytes. Two pieces of information ride alongside every upload and shape how the blob is later consumed.

The *content type* is an HTTP header — `Content-Type: image/png` — that storage records and replays on every download. Setting it correctly at upload is what lets a browser display the image inline, lets a PDF reader open the file, and lets a CDN apply MIME-aware compression. A blob uploaded without an explicit content type defaults to `application/octet-stream`, which most browsers treat as "download, do not preview."

*Metadata* is a dictionary of small string key-value pairs — typically a few hundred bytes total — that the application attaches to the blob. Common uses include the original filename (the blob name often gets sanitized or randomized), the uploading user's ID, a checksum, or a workflow state ("pending-virus-scan"). Metadata does not influence routing or access; it is application-level annotation that travels with the blob and can be read back without downloading the payload. Searching by metadata is not supported — the metadata is per-blob, retrieved when the blob is fetched, and not indexed across the container. Cross-blob queries belong in a database that holds the blob URL plus searchable fields.

## Choosing between blob storage and a database BLOB column

Both options can technically hold a JPEG. The decision is rarely about feasibility and almost always about access pattern, scale, and operations.

| Dimension | Blob storage | Database BLOB column |
|-----------|--------------|----------------------|
| Cost per GB stored | Cents | Dollars (database-tier pricing) |
| Read pattern | URL fetch over HTTP | SQL/driver round-trip |
| Backup impact | Independent of database backups | Inflates database backup size |
| Browser-direct access | Yes (via SAS) | No (must go through app server) |
| Transactional with row | No | Yes |
| Partial reads | Range requests on HTTP | Driver-dependent |
| Maximum size | Terabytes per blob | Typically 1–4 GB, often less |

Choose a database BLOB column when the binary content is small, tightly coupled to the row's transactional lifecycle, and never served directly to clients — for example, a 10 KB encrypted secret that must commit or roll back with the row that owns it. Choose blob storage in essentially every other case: any user-uploaded file, any media served to browsers, any artifact larger than a few hundred kilobytes, and any binary that benefits from CDN distribution or independent lifecycle management. The companion exercise [Azure Blob Storage](/exercises/5-cloud-databases/5-azure-blob-storage/) walks through provisioning a storage account and configuring the container; the [Data Layer](/exercises/10-webapp-development/3-data-layer/) exercise then implements the upload service shown above in a working ASP.NET Core application.

## Summary

Object storage exists because large binary payloads strain the assumptions databases make about row size, transaction logs, and query plans. A blob is an immutable byte sequence with a path-like name, a content type, and optional metadata, grouped into containers that scope access policies and access tiers. Access tiers (Hot, Cool, Archive) trade storage cost for retrieval latency, with Archive requiring an explicit rehydration before any read. SAS tokens delegate time-limited, scope-limited access without exposing the account key, enabling browsers and short-lived clients to talk to storage directly. Streaming uploads in the SDK chunk arbitrarily large files into bounded-memory transfers, and a service method that wraps `BlobClient.UploadAsync` keeps the upload mechanics out of the controller. The decision between blob storage and a database BLOB column comes down to access pattern: blob storage wins for anything served over HTTP or larger than the database is comfortable holding, while a database column is reserved for small, transactionally-coupled binary data.
