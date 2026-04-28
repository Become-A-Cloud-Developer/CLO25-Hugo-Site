# Part IV — Data Access — Mining Notes

## Studieguide alignment

- Companion course weeks: BCD week 5 (v.9) "Web Development fördjupning" (3-tier + databases) and BCD week 6 (v.10) "Storage"
- Reflection questions across these weeks (extract verbatim):
  - Vad är en 3-tier arkitektur?
  - Hur hanteras konfiguration i olika miljöer?
  - Varför separerar man presentation, logik och data?
  - Hur skiljer sig olika lagringsalternativ åt?
  - Vad är fördelarna med blob storage?
  - När använder man en databas vs blob storage?

## Companion exercises

- Path 1: `content/exercises/5-cloud-databases/` — Provisioning managed data services (databases and blob storage)
  - 1-portal-interface: Azure Cosmos DB via graphical UI
  - 4-connecting-from-code: MongoDB-compatible API from .NET code
  - 5-azure-blob-storage: Static asset storage via HTTP-based APIs
- Path 2: `content/exercises/10-webapp-development/3-data-layer/` — Implementing repository pattern with cloud databases and storage
- Key code patterns: IRepository interface, async data access, MongoClient, BlobClient, connection strings
- Key file names: Repositories/, appsettings.json, Program.cs dependency injection
- Key library / API surface: IAsyncCursor<T>, FilterDefinition, MongoDB.Driver, Azure.Storage.Blobs

## Per-chapter brief

### Chapter 1 — Relational vs NoSQL data models (slug: 1-relational-vs-nosql)
- Owns terms: relational model, NoSQL, schema, document database, key-value store, flexible schema, denormalization, CAP theorem, eventual consistency, ACID (restated from Part II with application-developer emphasis)
- Borrows: database (from Part II ch 2-databases), persistence, structured organization, data integrity, concurrent access, query capabilities, durability (all from Part II), partition tolerance (from Part II CAP theorem)
- Reflection questions to answer: How do relational and NoSQL data models differ in structure? When should you choose a relational database vs a NoSQL database for your application? What are trade-offs between schema rigidity and flexibility?
- Worked example: Exercise 5-cloud-databases demonstrates both approaches. The portal-interface exercise shows provisioning an Azure Cosmos DB with MongoDB-compatible API—a NoSQL document store. Students see how to create a database and collection without defining a rigid schema. The connecting-from-code exercise shows querying this Cosmos DB using MongoClient, demonstrating flexible document queries (e.g., filtering by fields that may vary between documents). This contrasts with the relational model mentioned in Part II databases chapter, where schema changes require explicit migrations.
- Slide-pair: yes
- Course tag: BCD
- Cross-link target: /exercises/5-cloud-databases/1-portal-interface/ and /exercises/5-cloud-databases/4-connecting-from-code/

### Chapter 2 — ORM and the repository pattern (slug: 2-orm-and-repository-pattern)
- Owns terms: repository pattern, data access abstraction, IRepository interface, ORM (Object-Relational Mapping), CRUD operations, FindAsync, InsertAsync, UpdateAsync, DeleteAsync, FindOptions, FilterDefinition, specification pattern, data mapper, implicit data layer coupling
- Borrows: three-tier architecture (from Part III ch 4), separation of concerns (from Part III ch 4), dependency injection (from Part III ch 6), service layer (from Part III ch 2)
- Reflection questions to answer: What is the repository pattern and why should you use it? How does the repository pattern separate application logic from data access? What is the difference between a generic repository and a specialized repository?
- Worked example: Exercise 10-webapp-development/3-data-layer implements the repository pattern. Students define an IRepository<T> interface with async methods like `Task<T> FindByIdAsync(string id)` and `Task InsertAsync(T entity)`. A MongoRepository<T> concrete implementation uses FilterDefinition<T> to construct MongoDB queries, demonstrating how the repository abstracts MongoDB-specific query syntax from the service layer. The service layer calls repository methods without knowing MongoDB details. In Program.cs, dependency injection registers the concrete repository behind the interface: `services.AddScoped<INewsletterRepository, MongoRepository<Newsletter>>()`. This decouples the application from database implementation—swapping MongoDB for SQL requires only changing the concrete class, not the service layer.
- Slide-pair: yes
- Course tag: BCD
- Cross-link target: /exercises/10-webapp-development/3-data-layer/

### Chapter 3 — Connections, pooling, and transactions (slug: 3-connections-and-transactions)
- Owns terms: connection string, connection pool, connection pooling, MaxPoolSize, MinPoolSize, connection lifetime, connection reuse, transaction, atomicity, rollback, isolation level, deadlock, optimistic locking, pessimistic locking, distributed transaction
- Borrows: ACID properties (from Part II ch 2-databases), durability (from Part II), Singleton lifetime (from Part III ch 6), Scoped lifetime (from Part III ch 6)
- Reflection questions to answer: How do connection pools improve application performance? What is a transaction and why is it important? How does isolation prevent data corruption with concurrent access? What are the trade-offs between optimistic and pessimistic locking?
- Worked example: Exercise 10-webapp-development/3-data-layer shows connection pooling implicitly. MongoClient is registered as Singleton in Program.cs—a single instance serves the entire application, reusing connections across requests. The exercise does not directly show transaction examples (transactions are less prominent in MongoDB than SQL), but the connection reuse demonstrates pool efficiency. Configuration reveals pooling parameters: appsettings.json contains the MongoDB connection string with implicit pooling defaults. The exercise demonstrates that creating MongoClient once and reusing it is cheaper than creating new connections per operation. A SQL equivalent would show SqlConnection pooling more explicitly, with MIN_POOL_SIZE and MAX_POOL_SIZE configuration.
- Slide-pair: yes
- Course tag: BCD
- Cross-link target: /exercises/10-webapp-development/3-data-layer/

### Chapter 4 — Object storage and file uploads (slug: 4-object-storage)
- Owns terms: object storage, blob, Azure Blob Storage, immutable objects, block blob, append blob, page blob, access tier (hot/cool/archive), SAS (Shared Access Signature), streaming upload, multipart upload, content-type, metadata, rehydration, object key/path, HTTP-based access, CDN integration
- Borrows: storage (from Part II ch 3-storage), object storage definition (from Part II), HTTP protocol (from Part III ch 1), REST API (from Part III ch 1 implicitly), scalability (from Part II)
- Reflection questions to answer: What is object storage and when should you use it instead of a database? How do you upload files to cloud storage? What are access tiers and when should you use each one? How do you serve static assets from blob storage?
- Worked example: Exercise 5-cloud-databases/5-azure-blob-storage guides students through provisioning Blob Storage and configuring access tiers via the portal. The exercise shows creating containers (logical groupings) and understanding that blobs are immutable—updating an image means uploading a new version. Exercise 10-webapp-development/3-data-layer extends this: students upload image files using BlobClient, with code like `await blobClient.UploadAsync(fileStream, overwrite: true)`. This demonstrates streaming upload—the SDK handles chunking large files automatically. The service layer abstracts blob upload details behind a method like `Task UploadImageAsync(string filename, Stream content)`, allowing the presentation layer (form upload controller) to call this without understanding blob details. Configuration includes the blob storage connection string and SAS token generation for time-limited access—a security pattern showing how to grant temporary upload permission without exposing account keys.
- Slide-pair: yes
- Course tag: BCD
- Cross-link target: /exercises/5-cloud-databases/5-azure-blob-storage/ and /exercises/10-webapp-development/3-data-layer/

## Cross-Part dependencies (forward references)

- **Connection string security**: Connection strings and blob storage keys must be protected. Part III Ch 5 covers basic IConfiguration and user-secrets for local development. Part V (Identity & Security) covers Azure Key Vault and managed identities for production secret rotation.
- **Repository pattern uses dependency injection**: Part III Ch 6 (DI) established the framework. This Part applies it heavily—every repository is registered in DI container and injected into services.
- **File upload validation**: Checking file extensions and magic bytes (content inspection) is mentioned briefly here but detailed security implications belong to Part V (Input Validation & Security).
- **NoSQL design patterns**: This Part introduces document databases. Advanced topics like sharding strategy, denormalization for query performance, and eventual consistency implications are touched but deeper distributed systems theory belongs beyond this course scope.

## Tonal reference

Use `content/course-book/2-infrastructure/storage/2-databases/databases.md` as the gold standard. Key features to emulate:
- **Motivation paragraph** opens each chapter before definitions — e.g., "Applications that serve multiple users must persist data reliably. The repository pattern ensures that only a single, testable layer handles database communication, making the application easier to maintain and test."
- **Bold on first use** of every key term — "A **repository** (or data access object) abstracts database queries behind an interface..."
- **Worked examples** drawn from actual exercises with interpretation — show MongoClient initialization, repository method call, or BlobClient upload with explanation of what the code demonstrates.
- **Closing Summary section** recapping load-bearing claims — tie together connection pooling, transaction isolation, and stored procedure usage as an integrated system for reliable data access.
- **1500–3500 words** per chapter — sufficient depth for both conceptual and practical understanding.

## Important boundaries

**Part II defines infrastructure-level concepts** (storage types, database categories, ACID, CAP theorem, persistence mechanisms). **Part IV applies these to application development**—how code accesses data, how queries work, how connection pooling improves performance, how the repository pattern hides database details.

Do NOT duplicate Part II's terms in the "Owns" list. Instead:
- Part II owns: database, relational model, NoSQL, structured organization, ACID, CAP theorem, object storage, block storage, file storage
- Part IV owns: IRepository, FindAsync, FilterDefinition, connection pooling, transaction isolation levels, access tiers, SAS token, streaming upload

This keeps clear ownership and prevents term duplication.
