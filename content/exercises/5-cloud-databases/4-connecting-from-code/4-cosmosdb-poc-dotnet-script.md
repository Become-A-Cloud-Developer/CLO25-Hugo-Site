+++
title = "4. CosmosDB PoC with .NET Script"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 4
date = 2026-02-22
lastmod = 2026-02-22
draft = false
+++

# CosmosDB PoC with .NET Script

## Goal

Build a proof-of-concept application that connects to Azure Cosmos DB using .NET 10 file-based scripting. Use the MongoDB driver to perform all CRUD operations (Create, Read, Update, Delete) against your cloud-hosted database, reading the connection string from an environment variable.

> **What you'll learn:**
>
> - How to use .NET 10 file-based app execution (`dotnet run file.cs`)
> - How to add NuGet packages with the `#:package` directive
> - How to connect to Cosmos DB using the MongoDB.Driver NuGet package
> - How to perform CRUD operations against a MongoDB-compatible database
> - How to manage connection strings securely via environment variables
> - How to verify application data in the Azure Portal Data Explorer

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ .NET 10 SDK installed (verify with `dotnet --version`)
> - ✓ A provisioned Cosmos DB account with MongoDB API (from a previous exercise in this section)
> - ✓ The Cosmos DB connection string available
> - ✓ A terminal and a text editor

## Exercise Steps

### Overview

1. **Set Up the Connection String**
2. **Create the .NET Script File**
3. **Implement Insert Operations**
4. **Implement Query and List Operations**
5. **Implement Update and Delete Operations**
6. **Test the Complete CRUD Workflow**

### **Step 1:** Set Up the Connection String

Configure the Cosmos DB connection string as an environment variable. Storing connection strings in environment variables keeps secrets out of source code and follows the twelve-factor app methodology for configuration management.

1. **Retrieve** your Cosmos DB connection string from the Azure Portal (Settings → Connection strings) or from your CLI output

2. **Set** the environment variable in your terminal:

   ```bash
   export COSMOS_CONNECTION_STRING="mongodb://your-account:your-key@your-account.mongo.cosmos.azure.com:10255/?ssl=true&retrywrites=false&maxIdleTimeMS=120000"
   ```

3. **Verify** the variable is set:

   ```bash
   echo "${COSMOS_CONNECTION_STRING:0:30}..."
   ```

> ℹ **Concept Deep Dive**
>
> Environment variables are the standard way to inject configuration into applications without hardcoding values. This approach has several benefits: the same code works against different databases (development, staging, production) by changing only the environment variable, secrets never appear in source code or version control, and the operating system provides built-in isolation between processes.
>
> The connection string format `mongodb://...` is the standard MongoDB connection URI. Because Cosmos DB supports the MongoDB wire protocol, the standard MongoDB.Driver NuGet package connects to it without any Cosmos-DB-specific code.
>
> ⚠ **Common Mistakes**
>
> - Forgetting to wrap the connection string in quotes when it contains special characters like `&` or `=`
> - The environment variable only persists in the current terminal session — opening a new terminal requires setting it again
> - Accidentally committing a `.env` file or a hardcoded connection string to Git exposes your database credentials
>
> ✓ **Quick check:** The echo command shows the first 30 characters of your connection string starting with `mongodb://`

### **Step 2:** Create the .NET Script File

Create a .NET 10 file-based application that connects to Cosmos DB using the MongoDB driver. .NET 10 supports running single `.cs` files directly with `dotnet run`, which is ideal for proof-of-concept work — no project file or solution structure needed.

1. **Create** a new file named `explore-cosmos.cs`

2. **Add** the package directive and initial connection setup:

   > `explore-cosmos.cs`

   ```csharp
   #:package MongoDB.Driver@3.*

   using MongoDB.Driver;
   using MongoDB.Bson;
   using MongoDB.Bson.Serialization.Attributes;

   // Read the connection string from environment variable
   var connectionString = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING")
       ?? throw new InvalidOperationException(
           "COSMOS_CONNECTION_STRING environment variable is not set. " +
           "Set it with: export COSMOS_CONNECTION_STRING=\"your-connection-string\"");

   // Connect to Cosmos DB
   var client = new MongoClient(connectionString);
   var database = client.GetDatabase("bookmarks_db");
   var collection = database.GetCollection<Bookmark>("bookmarks");

   Console.WriteLine("Connected to Cosmos DB successfully!");
   Console.WriteLine($"Database: bookmarks_db");
   Console.WriteLine($"Collection: bookmarks");
   Console.WriteLine();
   ```

3. **Add** the `Bookmark` record class at the bottom of the file:

   > `explore-cosmos.cs`

   ```csharp
   // Document model
   record Bookmark
   {
       [BsonId]
       [BsonRepresentation(BsonType.ObjectId)]
       public string? Id { get; init; }

       [BsonElement("title")]
       public required string Title { get; init; }

       [BsonElement("url")]
       public required string Url { get; init; }

       [BsonElement("category")]
       public required string Category { get; init; }

       [BsonElement("tags")]
       public string[] Tags { get; init; } = [];

       [BsonElement("createdAt")]
       public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
   }
   ```

> ℹ **Concept Deep Dive**
>
> The `#:package MongoDB.Driver@3.*` directive is a .NET 10 feature that allows single-file applications to declare NuGet package dependencies inline. The `@3.*` version range syntax specifies any version in the 3.x range. When you run the file with `dotnet run explore-cosmos.cs`, the runtime automatically restores the package before execution.
>
> The `Bookmark` record uses MongoDB.Driver attributes to control serialization: `[BsonId]` marks the primary key field, `[BsonRepresentation(BsonType.ObjectId)]` tells the driver to store it as a MongoDB ObjectId, and `[BsonElement("title")]` maps the C# property to the JSON field name. Using `record` instead of `class` provides value-based equality and immutability by default.
>
> ⚠ **Common Mistakes**
>
> - Forgetting the `#:package` directive causes a compilation error — the MongoDB.Driver namespace will not be found
> - Using `#r` instead of `#:package` — the `#r` directive is for .NET Interactive notebooks, not file-based apps
> - Hardcoding the connection string instead of reading from an environment variable makes the code insecure and non-portable
> - The `required` keyword on properties ensures they must be set during object initialization
>
> ✓ **Quick check:** Run `dotnet run explore-cosmos.cs` — it should print "Connected to Cosmos DB successfully!" without errors

### **Step 3:** Implement Insert Operations

Add code to insert bookmark documents into the collection. This demonstrates the Create operation in CRUD, using both single-document and batch insert methods.

1. **Add** the following insert code after the connection setup (before the `Bookmark` record definition):

   > `explore-cosmos.cs`

   ```csharp
   // === CREATE: Insert bookmarks ===
   Console.WriteLine("--- Inserting Bookmarks ---");

   var bookmarks = new List<Bookmark>
   {
       new()
       {
           Title = "Microsoft Learn",
           Url = "https://learn.microsoft.com",
           Category = "learning",
           Tags = ["microsoft", "documentation", "tutorials"]
       },
       new()
       {
           Title = "GitHub",
           Url = "https://github.com",
           Category = "development",
           Tags = ["git", "code", "collaboration"]
       },
       new()
       {
           Title = "Azure Portal",
           Url = "https://portal.azure.com",
           Category = "cloud",
           Tags = ["azure", "management", "portal"]
       },
       new()
       {
           Title = "Stack Overflow",
           Url = "https://stackoverflow.com",
           Category = "development",
           Tags = ["questions", "community", "programming"]
       }
   };

   await collection.InsertManyAsync(bookmarks);
   Console.WriteLine($"Inserted {bookmarks.Count} bookmarks.");
   Console.WriteLine();
   ```

> ℹ **Concept Deep Dive**
>
> The `InsertManyAsync` method sends all documents to the database in a single network round-trip, which is more efficient than inserting them one at a time with `InsertOneAsync`. The MongoDB driver automatically generates an `_id` (ObjectId) for each document if one is not provided. After insertion, the `Id` property on each `Bookmark` object is populated with the generated value.
>
> Because the `Bookmark` record uses `[BsonElement]` attributes, the C# property names (PascalCase) are mapped to lowercase JSON field names in the database. This follows MongoDB naming conventions while keeping C# code idiomatic.
>
> ⚠ **Common Mistakes**
>
> - Inserting documents with duplicate `_id` values causes a `MongoBulkWriteException` — each document must have a unique identifier
> - Running the script multiple times inserts duplicate data because `InsertManyAsync` always creates new documents
> - Forgetting `await` on async methods causes the program to exit before the operation completes
>
> ✓ **Quick check:** The console output shows "Inserted 4 bookmarks." and no exceptions are thrown

### **Step 4:** Implement Query and List Operations

Add code to retrieve and filter documents from the collection. This demonstrates the Read operations in CRUD, including listing all documents, filtering by field values, and querying by array contents.

1. **Add** the following query code after the insert section:

   > `explore-cosmos.cs`

   ```csharp
   // === READ: Query bookmarks ===
   Console.WriteLine("--- All Bookmarks ---");

   var allBookmarks = await collection.Find(_ => true).ToListAsync();
   foreach (var b in allBookmarks)
   {
       Console.WriteLine($"  [{b.Category}] {b.Title} - {b.Url}");
   }
   Console.WriteLine($"Total: {allBookmarks.Count} bookmarks");
   Console.WriteLine();

   // Filter by category
   Console.WriteLine("--- Development Bookmarks ---");

   var devFilter = Builders<Bookmark>.Filter.Eq(b => b.Category, "development");
   var devBookmarks = await collection.Find(devFilter).ToListAsync();
   foreach (var b in devBookmarks)
   {
       Console.WriteLine($"  {b.Title} ({string.Join(", ", b.Tags)})");
   }
   Console.WriteLine($"Found: {devBookmarks.Count} development bookmarks");
   Console.WriteLine();

   // Filter by tag
   Console.WriteLine("--- Bookmarks tagged 'azure' ---");

   var tagFilter = Builders<Bookmark>.Filter.AnyEq(b => b.Tags, "azure");
   var azureBookmarks = await collection.Find(tagFilter).ToListAsync();
   foreach (var b in azureBookmarks)
   {
       Console.WriteLine($"  {b.Title} - {b.Url}");
   }
   Console.WriteLine();
   ```

> ℹ **Concept Deep Dive**
>
> The MongoDB.Driver provides a strongly-typed `Builders<T>.Filter` API for constructing queries. `Filter.Eq` creates an equality filter, while `Filter.AnyEq` matches documents where any element in an array field equals the specified value. These filter builders generate MongoDB query documents (like `{ "category": "development" }`) but with compile-time type safety — renaming a property in the `Bookmark` class will cause a build error in the filter, catching bugs early.
>
> The `Find(_ => true)` expression is a shorthand for "match all documents" — the `_` parameter is a discarded lambda that always returns true. For production code with large collections, you would add pagination using `.Skip()` and `.Limit()` to avoid loading all documents into memory.
>
> Queries that filter on the shard key (`category`) are efficient single-partition queries in Cosmos DB. The `tagFilter` query on `tags` requires scanning across partitions because `tags` is not the shard key — this consumes more Request Units at scale.
>
> ⚠ **Common Mistakes**
>
> - Using string-based field names (e.g., `Filter.Eq("category", "development")`) bypasses type safety and is prone to typos
> - Forgetting `await` on `ToListAsync()` returns a `Task` object instead of the actual results
> - Loading all documents with `Find(_ => true)` on a large collection can consume excessive memory and Request Units
>
> ✓ **Quick check:** The output lists all 4 bookmarks, then filters to show only development bookmarks (2) and azure-tagged bookmarks

### **Step 5:** Implement Update and Delete Operations

Add code to update existing documents and delete documents from the collection. This completes the CRUD operations, demonstrating both targeted single-document updates and filtered deletions.

1. **Add** the following update and delete code after the query section:

   > `explore-cosmos.cs`

   ```csharp
   // === UPDATE: Modify a bookmark ===
   Console.WriteLine("--- Updating a Bookmark ---");

   var updateFilter = Builders<Bookmark>.Filter.Eq(b => b.Title, "GitHub");
   var update = Builders<Bookmark>.Update
       .Set(b => b.Title, "GitHub - Where the world builds software")
       .Set(b => b.Tags, new[] { "git", "code", "collaboration", "open-source" });

   var updateResult = await collection.UpdateOneAsync(updateFilter, update);
   Console.WriteLine($"Matched: {updateResult.MatchedCount}, Modified: {updateResult.ModifiedCount}");

   // Verify the update
   var updatedBookmark = await collection.Find(updateFilter).FirstOrDefaultAsync();
   if (updatedBookmark == null)
   {
       // The title changed, so search by URL instead
       var urlFilter = Builders<Bookmark>.Filter.Eq(b => b.Url, "https://github.com");
       updatedBookmark = await collection.Find(urlFilter).FirstOrDefaultAsync();
   }
   Console.WriteLine($"Updated title: {updatedBookmark?.Title}");
   Console.WriteLine($"Updated tags: {string.Join(", ", updatedBookmark?.Tags ?? [])}");
   Console.WriteLine();

   // === DELETE: Remove a bookmark ===
   Console.WriteLine("--- Deleting a Bookmark ---");

   var deleteFilter = Builders<Bookmark>.Filter.Eq(b => b.Title, "Stack Overflow");
   var deleteResult = await collection.DeleteOneAsync(deleteFilter);
   Console.WriteLine($"Deleted: {deleteResult.DeletedCount} bookmark(s)");
   Console.WriteLine();

   // Final count
   var finalCount = await collection.CountDocumentsAsync(_ => true);
   Console.WriteLine($"--- Final bookmark count: {finalCount} ---");
   ```

> ℹ **Concept Deep Dive**
>
> The `Builders<T>.Update` API provides a fluent interface for constructing update operations. The `.Set()` method updates specific fields without replacing the entire document — this is an atomic, partial update that is both efficient and safe for concurrent access. `UpdateOneAsync` modifies only the first matching document, while `UpdateManyAsync` would modify all matches.
>
> `DeleteOneAsync` removes a single document matching the filter. The `DeletedCount` property confirms how many documents were actually removed — this is useful for verifying that the filter matched the intended document. If no document matches, `DeletedCount` is 0 and no error is thrown.
>
> Both update and delete operations consume Request Units in Cosmos DB. Operations that include the shard key in the filter are more efficient because they target a single partition.
>
> ⚠ **Common Mistakes**
>
> - Using `ReplaceOneAsync` instead of `UpdateOneAsync` replaces the entire document, which can unintentionally remove fields
> - After updating the `Title` field, any subsequent filter based on the old title value will not find the document
> - `DeleteOneAsync` with a filter that matches multiple documents only deletes the first match — use `DeleteManyAsync` to remove all matches
>
> ✓ **Quick check:** The update shows "Matched: 1, Modified: 1", the delete shows "Deleted: 1", and the final count is 3

### **Step 6:** Test the Complete CRUD Workflow

Run the complete script, verify the output in the terminal, and cross-reference the results in the Azure Portal Data Explorer. This end-to-end verification confirms that your code interacts correctly with the cloud database.

1. **Run** the script:

   ```bash
   dotnet run explore-cosmos.cs
   ```

2. **Verify** the console output shows:

   - Successful connection message
   - 4 bookmarks inserted
   - All bookmarks listed with categories
   - Filtered results for "development" and "azure" tags
   - Update confirmation (Matched: 1, Modified: 1)
   - Delete confirmation (Deleted: 1)
   - Final count of 3

3. **Open** the Azure Portal at <https://portal.azure.com>

4. **Navigate to** your Cosmos DB account → Data Explorer

5. **Expand** `bookmarks_db` → `bookmarks` → Documents

6. **Verify** that:

   - Three documents remain in the collection
   - The GitHub bookmark has the updated title and tags
   - The Stack Overflow bookmark has been removed

7. **Clean up** the test data (optional) — add this at the end of your script or run it separately:

   ```csharp
   // Optional: Clean up all inserted documents
   // await collection.DeleteManyAsync(_ => true);
   // Console.WriteLine("All bookmarks deleted.");
   ```

> ✓ **Success indicators:**
>
> - Script runs without exceptions from start to finish
> - All CRUD operations produce the expected output counts
> - Data Explorer in the Azure Portal reflects the final state (3 documents)
> - The updated GitHub bookmark shows the new title and tags array
> - The Stack Overflow bookmark no longer exists in the collection
>
> ✓ **Final verification checklist:**
>
> - ☐ Connection string read from environment variable (not hardcoded)
> - ☐ All four CRUD operations execute successfully
> - ☐ Console output matches expected results
> - ☐ Azure Portal Data Explorer confirms the data state
> - ☐ The `Bookmark` record maps correctly to MongoDB documents

## Common Issues

> **If you encounter problems:**
>
> **"COSMOS_CONNECTION_STRING environment variable is not set":** Set the environment variable with `export COSMOS_CONNECTION_STRING="your-connection-string"` in the same terminal where you run the script.
>
> **"Authentication failed":** Verify the connection string is correct and complete. Copy it again from the Azure Portal (Settings → Connection strings). Ensure the string is wrapped in quotes when setting the environment variable.
>
> **"MongoCommandException: Command insert failed":** This often indicates a shard key issue. Ensure the `category` field is present in all inserted documents, as it matches the collection's partition key.
>
> **"The type or namespace name 'MongoDB' could not be found":** Ensure the `#:package MongoDB.Driver@3.*` directive is the very first line of the file (before any `using` statements).
>
> **"dotnet run: command not found" or version error:** Verify .NET 10 is installed with `dotnet --version`. File-based execution requires .NET 10 or later.
>
> **Still stuck?** Try connecting with a minimal script that only creates the `MongoClient` and lists database names to isolate whether the issue is connectivity or code logic.

## Summary

You've successfully built a proof-of-concept that connects to Cosmos DB from .NET code which:

- ✓ Uses .NET 10 file-based execution for rapid prototyping
- ✓ Connects to Cosmos DB using the standard MongoDB.Driver package
- ✓ Performs all CRUD operations with strongly-typed models
- ✓ Manages connection strings securely via environment variables

> **Key takeaway:** Because Cosmos DB supports the MongoDB wire protocol, you can use the standard MongoDB.Driver NuGet package — the same driver you would use with a native MongoDB instance. The only difference is the connection string. This means migrating between MongoDB and Cosmos DB is primarily a configuration change, not a code change. You have validated this concept hands-on with a working proof-of-concept.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Add a `FindOneAndUpdateAsync` operation to atomically update and return a document in a single round-trip
> - Implement pagination using `.Skip()` and `.Limit()` to handle large collections efficiently
> - Add an index on the `tags` field using `collection.Indexes.CreateOneAsync()` and observe the query performance difference
> - Explore the MongoDB.Driver LINQ provider to write queries using C# LINQ syntax instead of filter builders

## Done! 🎉

Great job! You've connected to Azure Cosmos DB from application code and performed all CRUD operations using the MongoDB driver. This proof-of-concept validates that existing MongoDB driver code works seamlessly with Cosmos DB — a key insight that you will apply when integrating Cosmos DB into a full web application.
