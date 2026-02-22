+++
title = "14. Migrating to CosmosDB"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 3
date = 2026-02-22
lastmod = 2026-02-22
draft = false
+++

# Migrating to CosmosDB

## Goal

Migrate the Newsletter application's data layer from a local Docker-hosted MongoDB instance to Azure Cosmos DB. Leverage the repository pattern abstraction to make this migration primarily a configuration change — updating the connection string and adjusting compatibility settings — without modifying application code.

> **What you'll learn:**
>
> - How repository pattern abstraction enables painless database migration
> - How to configure Cosmos DB connection strings in ASP.NET Core
> - How to handle Cosmos DB MongoDB API compatibility differences
> - How to use feature flags to toggle between database backends
> - How to verify data operations against a cloud-hosted database

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Completed the Newsletter app with repository pattern and MongoDB repository implementation
> - ✓ Completed the Cloud Databases section (a provisioned Cosmos DB account with MongoDB API)
> - ✓ The Cosmos DB connection string available
> - ✓ The Newsletter application running locally with Docker MongoDB

## Exercise Steps

### Overview

1. **Prepare the Cosmos DB Database**
2. **Update the Connection String Configuration**
3. **Handle Cosmos DB Compatibility Settings**
4. **Configure the Feature Flag**
5. **Test the Migration**

### **Step 1:** Prepare the Cosmos DB Database

Ensure your Cosmos DB account has a database and collection ready to receive the Newsletter application's subscriber data. The collection needs a shard key that aligns with the application's query patterns.

1. **Open** the Azure Portal at <https://portal.azure.com>

2. **Navigate to** your Cosmos DB account

3. **Open** Data Explorer from the left menu

4. **Create** a new database named `newsletter_db` (if it does not already exist)

5. **Create** a new collection with the following settings:

   - **Collection id**: `subscribers`
   - **Shard key**: `/email`

6. **Verify** the empty collection appears in the Data Explorer tree

> ℹ **Concept Deep Dive**
>
> The shard key `/email` is chosen because email addresses have high cardinality (every subscriber has a unique email) and the application frequently queries by email (for duplicate checking and unsubscribe operations). This ensures efficient single-partition queries for the most common operations.
>
> The database and collection names should match what the application expects in its MongoDB configuration. If your existing `appsettings.json` uses different names, either update the settings or create the Cosmos DB resources with matching names.
>
> ⚠ **Common Mistakes**
>
> - Creating a collection with a shard key that does not match a field in your documents causes all documents to land in a single null-key partition
> - The shard key path is case-sensitive — `/email` and `/Email` are different. Check your C# model's `[BsonElement]` attribute to confirm the exact field name
>
> ✓ **Quick check:** Data Explorer shows the `newsletter_db` database with an empty `subscribers` collection

### **Step 2:** Update the Connection String Configuration

Replace the local MongoDB connection string with the Cosmos DB connection string in the application configuration. ASP.NET Core's configuration system supports environment-specific overrides, so you can maintain separate settings for development (Docker MongoDB) and cloud (Cosmos DB).

1. **Open** the `appsettings.json` file in the project root

2. **Locate** the MongoDB configuration section

3. **Update** the connection string and database name to point to Cosmos DB:

   > `appsettings.json`

   ```json
   {
     "MongoDb": {
       "ConnectionString": "mongodb://your-account:your-key@your-account.mongo.cosmos.azure.com:10255/?ssl=true&retrywrites=false&maxIdleTimeMS=120000",
       "DatabaseName": "newsletter_db"
     }
   }
   ```

4. **Alternatively**, use User Secrets to keep the connection string out of source code:

   ```bash
   dotnet user-secrets set "MongoDb:ConnectionString" "mongodb://your-account:your-key@your-account.mongo.cosmos.azure.com:10255/?ssl=true&retrywrites=false&maxIdleTimeMS=120000"
   ```

> ℹ **Concept Deep Dive**
>
> The connection string is the only infrastructure-level change needed to switch from MongoDB to Cosmos DB. This is possible because Cosmos DB implements the MongoDB wire protocol — the MongoDB.Driver NuGet package communicates with Cosmos DB using the same protocol it uses with native MongoDB.
>
> Using `dotnet user-secrets` stores the connection string outside the project directory (in `~/.microsoft/usersecrets/`), preventing accidental commits to Git. User Secrets are automatically loaded in the Development environment by the ASP.NET Core configuration system. For production, you would use Azure App Service configuration or Key Vault references.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `retrywrites=false` in the connection string causes write operations to fail — Cosmos DB's MongoDB API does not support retryable writes
> - Leaving the old Docker MongoDB connection string active while Docker is stopped causes connection timeout errors
> - Committing the Cosmos DB connection string (which contains the access key) to Git exposes your database credentials
>
> ✓ **Quick check:** The `MongoDb:ConnectionString` value starts with `mongodb://` and contains `.mongo.cosmos.azure.com:10255`

### **Step 3:** Handle Cosmos DB Compatibility Settings

Adjust the MongoDB client configuration to handle differences between native MongoDB and Cosmos DB's MongoDB API. The most important setting is disabling retryable writes, which Cosmos DB does not support in the same way as native MongoDB.

1. **Open** the file where the `MongoClient` is configured (typically the MongoDB repository or service registration)

2. **Verify** that the `MongoClient` is created using the connection string from configuration:

   > `MongoDbSubscriberRepository.cs` or equivalent

   ```csharp
   var settings = MongoClientSettings.FromConnectionString(connectionString);
   settings.RetryWrites = false;
   var client = new MongoClient(settings);
   ```

3. **Alternatively**, if your existing code creates the client directly from the connection string, **ensure** the connection string includes `retrywrites=false`:

   ```text
   mongodb://...?ssl=true&retrywrites=false&maxIdleTimeMS=120000
   ```

> ℹ **Concept Deep Dive**
>
> Cosmos DB's MongoDB API supports most MongoDB operations but has some differences in behavior. The most impactful for this migration is retryable writes — MongoDB 4.2+ enables retryable writes by default, but Cosmos DB does not support the server-side session mechanics that retryable writes require. Setting `RetryWrites = false` in the client settings (or `retrywrites=false` in the connection string) prevents the driver from attempting this unsupported feature.
>
> The `maxIdleTimeMS=120000` parameter sets the maximum idle time for connections in the connection pool. Cosmos DB may close idle connections after a period, so this setting helps the driver manage the pool proactively.
>
> If your existing code already creates the `MongoClient` with just the connection string (`new MongoClient(connectionString)`), and the connection string includes `retrywrites=false`, no code changes are needed. This is the power of the repository pattern — the migration is purely configuration.
>
> ⚠ **Common Mistakes**
>
> - The MongoDB.Driver defaults to `RetryWrites = true` in newer versions — you must explicitly disable it for Cosmos DB
> - Setting `ssl=false` causes connection failures — Cosmos DB requires TLS/SSL on all connections
> - If using `MongoClientSettings`, forgetting to call `FromConnectionString` first means SSL and other connection-string parameters are not applied
>
> ✓ **Quick check:** The application starts without MongoDB connection errors in the console output

### **Step 4:** Configure the Feature Flag

If your application uses feature flags to toggle between data layer implementations, update the configuration to activate the MongoDB (Cosmos DB) repository. Because the `MongoDbSubscriberRepository` uses the standard MongoDB.Driver, it works with both native MongoDB and Cosmos DB without modification.

1. **Open** `appsettings.json`

2. **Locate** the feature flags section

3. **Ensure** the MongoDB feature flag is enabled:

   > `appsettings.json`

   ```json
   {
     "FeatureFlags": {
       "UseMongoDb": true
     }
   }
   ```

4. **Verify** that the service registration in `Program.cs` uses this flag to select the repository implementation:

   > `Program.cs`

   ```csharp
   if (builder.Configuration.GetValue<bool>("FeatureFlags:UseMongoDb"))
   {
       builder.Services.AddSingleton<ISubscriberRepository, MongoDbSubscriberRepository>();
   }
   else
   {
       builder.Services.AddSingleton<ISubscriberRepository, InMemorySubscriberRepository>();
   }
   ```

> ℹ **Concept Deep Dive**
>
> The feature flag pattern allows you to switch between repository implementations without changing code or redeploying. With `UseMongoDb` set to `true`, the dependency injection container provides the `MongoDbSubscriberRepository` — which now connects to Cosmos DB instead of Docker MongoDB. Setting it to `false` falls back to the in-memory implementation for local development without any database dependency.
>
> This is the key benefit of the repository pattern in action: the controller and service layer code has no knowledge of whether subscribers are stored in memory, in Docker MongoDB, or in Azure Cosmos DB. The `ISubscriberRepository` interface abstracts away the storage mechanism entirely.
>
> ✓ **Quick check:** The application starts with the MongoDB repository active, and the console output shows no errors about missing services or unresolvable dependencies

### **Step 5:** Test the Migration

Verify that all CRUD operations work correctly against the Cosmos DB backend. Test the same user workflows you would test with the local MongoDB — subscribing, listing, and unsubscribing — and confirm the data appears in the Azure Portal.

1. **Start** the application:

   ```bash
   dotnet run
   ```

2. **Navigate to** the application in your browser (typically `http://localhost:5000` or the URL shown in the console)

3. **Test Subscribe** — add a new subscriber:

   - Enter an email address and name in the subscription form
   - Submit the form
   - Verify the subscriber appears in the subscriber list

4. **Test List** — view all subscribers:

   - Navigate to the subscribers page
   - Verify the newly added subscriber is displayed

5. **Test Unsubscribe** — remove a subscriber:

   - Click the unsubscribe button for the test subscriber
   - Verify the subscriber is removed from the list

6. **Verify in the Azure Portal:**

   - **Open** Data Explorer in your Cosmos DB account
   - **Expand** `newsletter_db` → `subscribers`
   - **Click** on Documents to view stored subscriber data
   - **Confirm** the documents contain the expected fields (email, name, etc.)
   - **Confirm** that unsubscribed entries have been removed (or marked as unsubscribed, depending on your implementation)

7. **Test adding multiple subscribers** and verify they appear in both the application and Data Explorer

> ✓ **Success indicators:**
>
> - Subscribe operation creates a document visible in both the app and Data Explorer
> - List operation displays all subscribers stored in Cosmos DB
> - Unsubscribe operation removes (or marks) the subscriber in Cosmos DB
> - No errors appear in the application console or browser developer tools
> - Data Explorer shows documents with the correct structure and field names
>
> ✓ **Final verification checklist:**
>
> - ☐ Application connects to Cosmos DB without errors
> - ☐ All CRUD operations (subscribe, list, unsubscribe) work correctly
> - ☐ Data is visible in Azure Portal Data Explorer
> - ☐ No code changes were needed in the repository or controller (only configuration)
> - ☐ The feature flag correctly toggles between in-memory and Cosmos DB repositories

## Common Issues

> **If you encounter problems:**
>
> **"Connection refused" or timeout errors:** Verify the connection string is correct and your network allows outbound connections on port 10255. Some corporate networks or VPNs block non-standard ports.
>
> **"Command insert failed" or write errors:** Ensure `retrywrites=false` is in the connection string or `RetryWrites = false` is set in `MongoClientSettings`.
>
> **"Collection doesn't exist" errors:** The MongoDB.Driver may auto-create collections in native MongoDB but may not in Cosmos DB depending on the API version. Create the collection manually in Data Explorer first.
>
> **Data appears in Cosmos DB but not in the app (or vice versa):** Check that the database name and collection name in `appsettings.json` exactly match the names in Cosmos DB (case-sensitive).
>
> **Application still connects to Docker MongoDB:** Verify the feature flag is enabled and the connection string points to Cosmos DB. Check if Docker is still running and whether the application might be reading from an environment variable that overrides `appsettings.json`.
>
> **Still stuck?** Start Docker MongoDB and switch back to the local configuration (change the connection string back) to confirm the issue is Cosmos DB-specific rather than a general application problem.

## Summary

You've successfully migrated the Newsletter application from Docker MongoDB to Azure Cosmos DB which:

- ✓ Demonstrates the power of the repository pattern — migration was a configuration change, not a code change
- ✓ Connects to a fully managed cloud database with automatic scaling and high availability
- ✓ Uses the same MongoDB.Driver code for both local MongoDB and cloud Cosmos DB
- ✓ Maintains the feature flag toggle for flexible development workflows

> **Key takeaway:** The repository pattern abstraction you built earlier paid off here — switching the data backend from a local Docker MongoDB container to a globally distributed Azure Cosmos DB instance required only a connection string change and a compatibility setting. No controller code, no service code, and no repository code needed modification. This is the concrete benefit of programming against interfaces and following the Dependency Inversion Principle: your application's core logic is decoupled from its infrastructure dependencies.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Configure separate `appsettings.Development.json` (Docker MongoDB) and `appsettings.Production.json` (Cosmos DB) for automatic environment-based switching
> - Explore Cosmos DB's Metrics blade to understand Request Unit consumption for each operation
> - Add a health check endpoint that verifies database connectivity using `IHealthCheck`
> - Investigate Cosmos DB's global distribution by adding a read replica in a second Azure region

## Done! 🎉

Great job! You've migrated your Newsletter application to Azure Cosmos DB with zero code changes. This exercise demonstrates why architectural patterns like the repository pattern and dependency injection matter — they make significant infrastructure changes into routine configuration updates.
