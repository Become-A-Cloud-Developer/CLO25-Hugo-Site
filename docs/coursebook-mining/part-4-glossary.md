# Part IV — Glossary

Terminology contract for the four chapters of Part IV — Data Access. Workers in B2 receive this verbatim and follow the rules in `develop-theory-chapter/GLOSSARY-PROTOCOL.md`.

## Terms owned by this Part

### Schema
- **Owner chapter**: `1-relational-vs-nosql`
- **Canonical definition**: A **schema** is the formal description of the structure a data store enforces — table and column definitions in a relational database, or the set of fields a document is expected to carry in a NoSQL store; relational schemas are rigid and require migrations to change, while document schemas are flexible and can vary per document.
- **Used by chapters**: 1-relational-vs-nosql (owner), 2-orm-and-repository-pattern

### Document database
- **Owner chapter**: `1-relational-vs-nosql`
- **Canonical definition**: A **document database** stores each record as a self-describing document (typically JSON or BSON), grouped into collections; queries match documents by field values without the join machinery a relational database uses, and individual documents may have heterogeneous shapes.
- **Used by chapters**: 1-relational-vs-nosql (owner), 2-orm-and-repository-pattern, 3-connections-and-transactions

### Denormalization
- **Owner chapter**: `1-relational-vs-nosql`
- **Canonical definition**: **Denormalization** is the deliberate duplication of data across documents or tables so that a single read can satisfy a query without joining; common in document databases, it trades write complexity (keeping copies in sync) for query simplicity.
- **Used by chapters**: 1-relational-vs-nosql (owner)

### Eventual consistency
- **Owner chapter**: `1-relational-vs-nosql`
- **Canonical definition**: **Eventual consistency** is the guarantee that all replicas of a piece of data will converge to the same value once writes stop, but at any single moment a read may return stale data; this is the consistency model many distributed NoSQL stores choose in exchange for availability under network partitions.
- **Used by chapters**: 1-relational-vs-nosql (owner)

### Repository pattern
- **Owner chapter**: `2-orm-and-repository-pattern`
- **Canonical definition**: The **repository pattern** is a design pattern in which a single class (the repository) hides all data-access code behind an interface; service-layer code calls methods like `FindByIdAsync` or `InsertAsync` and the repository translates these into the underlying database client's calls, keeping query syntax out of business logic.
- **Used by chapters**: 2-orm-and-repository-pattern (owner), 3-connections-and-transactions, 4-object-storage

### ORM
- **Owner chapter**: `2-orm-and-repository-pattern`
- **Canonical definition**: An **ORM** (Object-Relational Mapper) is a library that translates between database rows and in-memory objects; the developer writes class definitions, and the ORM generates the SQL needed to load, save, and query those classes, often with change tracking that detects modified properties and writes only the deltas.
- **Used by chapters**: 2-orm-and-repository-pattern (owner)

### CRUD
- **Owner chapter**: `2-orm-and-repository-pattern`
- **Canonical definition**: **CRUD** is the four basic data operations every persistence layer supports: **Create** (insert new records), **Read** (query existing records), **Update** (modify records), and **Delete** (remove records). Repository interfaces typically expose one method per operation.
- **Used by chapters**: 2-orm-and-repository-pattern (owner), 3-connections-and-transactions

### Connection string
- **Owner chapter**: `3-connections-and-transactions`
- **Canonical definition**: A **connection string** is the configuration value a client library uses to connect to a database, encoding the host, port, database name, credentials, and optional parameters such as pool size or timeout.
- **Used by chapters**: 3-connections-and-transactions (owner), 4-object-storage

### Connection pool
- **Owner chapter**: `3-connections-and-transactions`
- **Canonical definition**: A **connection pool** is a cache of open database connections that a client library reuses across requests, avoiding the cost of opening a new TCP connection and re-authenticating for each query; pool size is typically configured through the connection string.
- **Used by chapters**: 3-connections-and-transactions (owner)

### Transaction
- **Owner chapter**: `3-connections-and-transactions`
- **Canonical definition**: A **transaction** is a unit of work that the database treats atomically: either every operation in the transaction succeeds and the changes commit, or any failure causes all changes to roll back as if they never happened.
- **Used by chapters**: 3-connections-and-transactions (owner)

### Isolation level
- **Owner chapter**: `3-connections-and-transactions`
- **Canonical definition**: An **isolation level** governs what concurrent transactions are allowed to see of each other's in-flight changes; common levels — Read Uncommitted, Read Committed, Repeatable Read, Serializable — trade performance for stronger guarantees against lost-update, dirty-read, and phantom-read anomalies.
- **Used by chapters**: 3-connections-and-transactions (owner)

### Optimistic locking
- **Owner chapter**: `3-connections-and-transactions`
- **Canonical definition**: **Optimistic locking** detects conflicting updates by attaching a version field (or rowversion) to each record; an update succeeds only if the version still matches what was read, otherwise the application retries — useful when conflicts are rare and locking would hurt throughput.
- **Used by chapters**: 3-connections-and-transactions (owner)

### Pessimistic locking
- **Owner chapter**: `3-connections-and-transactions`
- **Canonical definition**: **Pessimistic locking** prevents conflicts upfront by acquiring a database lock on a row before reading or modifying it; other transactions touching the same row block until the lock releases — useful when conflicts are common but expensive at scale.
- **Used by chapters**: 3-connections-and-transactions (owner)

### Blob
- **Owner chapter**: `4-object-storage`
- **Canonical definition**: A **blob** (Binary Large Object) is the unit of storage in an object store: an immutable byte sequence with a path-like name, optional metadata key-value pairs, and a content type; blobs are written and read whole through HTTP APIs rather than queried like database rows.
- **Used by chapters**: 4-object-storage (owner)

### Container (object storage)
- **Owner chapter**: `4-object-storage`
- **Canonical definition**: A **container** in object storage is the top-level grouping for blobs, somewhat like a bucket in S3 or a directory in a filesystem; access policies and access tiers are typically configured at the container level.
- **Used by chapters**: 4-object-storage (owner)

### Access tier
- **Owner chapter**: `4-object-storage`
- **Canonical definition**: An **access tier** is the storage class a blob is assigned to — Hot, Cool, or Archive in Azure Blob Storage — that trades retrieval latency for storage cost; Hot tier costs more to store but is read instantly, while Archive tier is cheap to store but takes hours to rehydrate.
- **Used by chapters**: 4-object-storage (owner)

### SAS token
- **Owner chapter**: `4-object-storage`
- **Canonical definition**: A **SAS** (Shared Access Signature) is a time-limited, scope-limited URL signed with the storage account key that grants the bearer permission to read or write a specific blob or container without exposing the account key itself; SAS tokens are the standard way to delegate temporary access to clients.
- **Used by chapters**: 4-object-storage (owner)

### Streaming upload
- **Owner chapter**: `4-object-storage`
- **Canonical definition**: A **streaming upload** transfers a blob to the storage service in chunks rather than buffering the entire payload in memory first; the storage SDK handles chunking, parallelism, and retry, allowing the client to upload arbitrarily large files with bounded memory.
- **Used by chapters**: 4-object-storage (owner)

## Terms borrowed from earlier Parts

### Database (general concept)
- **Defined in**: Part II — Infrastructure / Storage / `2-databases`
- **Reference link**: `/course-book/2-infrastructure/storage/2-databases/`

### ACID
- **Defined in**: Part II — Infrastructure / Storage / `2-databases`
- **Reference link**: `/course-book/2-infrastructure/storage/2-databases/`

### CAP theorem
- **Defined in**: Part II — Infrastructure / Storage / `2-databases`
- **Reference link**: `/course-book/2-infrastructure/storage/2-databases/`

### Object storage (concept)
- **Defined in**: Part II — Infrastructure / Storage / `3-storage`
- **Reference link**: `/course-book/2-infrastructure/storage/3-storage/`

### Three-tier architecture / Data layer / Service layer
- **Defined in**: Part III — Application Development / `4-three-tier-architecture`
- **Reference link**: `/course-book/3-application-development/4-three-tier-architecture/`

### Dependency injection / IServiceCollection / Lifetime (Singleton, Scoped, Transient)
- **Defined in**: Part III — Application Development / `6-dependency-injection`
- **Reference link**: `/course-book/3-application-development/6-dependency-injection/`

### IConfiguration / Connection string handling via configuration
- **Defined in**: Part III — Application Development / `5-configuration-and-environments`
- **Reference link**: `/course-book/3-application-development/5-configuration-and-environments/`
