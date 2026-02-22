+++
title = "2. Provisioning CosmosDB via AZ CLI"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 2
date = 2026-02-22
lastmod = 2026-02-22
draft = false
+++

# Provisioning CosmosDB via AZ CLI

## Goal

Provision an Azure Cosmos DB account with the MongoDB API, create a database and collection, and retrieve the connection string — all using the Azure CLI. Build a reusable shell script that automates the entire provisioning process.

> **What you'll learn:**
>
> - How to provision a Cosmos DB account with MongoDB API using `az cosmosdb` commands
> - How to create databases and collections from the command line
> - How to query connection strings and keys programmatically
> - How to build a reusable provisioning script for repeatable deployments
> - Key advantages of CLI-based provisioning over portal-based approaches

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Active Azure subscription with resource creation permissions
> - ✓ Azure CLI installed and authenticated (`az login`)
> - ✓ A terminal (bash, zsh, or Azure Cloud Shell)
> - ✓ Familiarity with basic shell scripting concepts

## Exercise Steps

### Overview

1. **Set Up Variables and Resource Group**
2. **Create the Cosmos DB Account**
3. **Create the Database and Collection**
4. **Retrieve the Connection String**
5. **Build the Complete Provisioning Script**
6. **Test and Verify**

### **Step 1:** Set Up Variables and Resource Group

Define reusable shell variables and create a resource group to hold your Cosmos DB resources. Using variables at the top of your workflow makes commands reusable and reduces errors from typos. The resource group acts as a logical container for related Azure resources, making it easy to manage lifecycle, access control, and cost tracking together.

1. **Open** your terminal and **ensure** you are logged in to Azure:

   ```bash
   az login
   ```

2. **Define** the following shell variables (replace `yourname` with your actual name):

   ```bash
   RESOURCE_GROUP="CloudDatabasesRG"
   LOCATION="northeurope"
   ACCOUNT_NAME="cosmosdb-yourname-bcd"
   DATABASE_NAME="bookmarks_db"
   COLLECTION_NAME="bookmarks"
   ```

3. **Create** the resource group:

   ```bash
   az group create \
     --name $RESOURCE_GROUP \
     --location $LOCATION
   ```

4. **Verify** the resource group was created:

   ```bash
   az group show --name $RESOURCE_GROUP --query "{name:name, location:location}" --output table
   ```

> ℹ **Concept Deep Dive**
>
> Shell variables keep configuration values in one place, making scripts easier to maintain and reuse across environments. The `--location` parameter determines the Azure region where the resource group metadata is stored — choose a region close to you for lower latency during development. The resource group itself is free; you only pay for the resources inside it.
>
> ⚠ **Common Mistakes**
>
> - Using uppercase letters or spaces in `ACCOUNT_NAME` — Cosmos DB account names must be lowercase letters, numbers, and hyphens only (3–44 characters)
> - Forgetting to run `az login` first results in authentication errors on every subsequent command
> - Variable names are case-sensitive in bash — `$RESOURCE_GROUP` and `$resource_group` are different
>
> ✓ **Quick check:** The `az group show` command displays your resource group name and location in a table

### **Step 2:** Create the Cosmos DB Account

Create the Cosmos DB account with MongoDB API compatibility and serverless capacity mode. This is the foundational resource — all databases and collections live within this account. The account creation takes several minutes because Azure is provisioning the underlying infrastructure across its data centers.

1. **Run** the following command to create the Cosmos DB account:

   ```bash
   az cosmosdb create \
     --name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --kind MongoDB \
     --server-version "4.2" \
     --capabilities EnableServerless \
     --locations regionName=$LOCATION failoverPriority=0 isZoneRedundant=false
   ```

2. **Wait** for the command to complete (this typically takes 3–8 minutes)

3. **Verify** the account was created successfully:

   ```bash
   az cosmosdb show \
     --name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --query "{name:name, kind:kind, location:location}" \
     --output table
   ```

> ℹ **Concept Deep Dive**
>
> Each flag in the create command serves a specific purpose:
>
> - `--kind MongoDB` selects the MongoDB API, enabling MongoDB wire protocol compatibility
> - `--capabilities EnableServerless` sets the capacity mode to serverless, which charges only for consumed Request Units (RUs) — ideal for development and intermittent workloads with no minimum charge when idle
> - `--server-version "4.2"` sets the MongoDB wire protocol version that the account will support
> - `--locations` configures the primary region — with serverless capacity, you get a single-region deployment
>
> The `--query` and `--output table` flags use JMESPath expressions to extract specific fields from the JSON response and display them in a readable table format. This technique is useful for quickly verifying resource properties without scrolling through verbose JSON output.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `--kind MongoDB` creates a NoSQL (Core) API account instead — the API type cannot be changed after creation
> - Omitting `--capabilities EnableServerless` defaults to provisioned throughput, which incurs continuous charges even when idle
> - If the account name is already taken globally, you will get an error — try adding a random number suffix
>
> ✓ **Quick check:** The `az cosmosdb show` command displays a table with `kind` showing `MongoDB`

### **Step 3:** Create the Database and Collection

Create the database and collection within your Cosmos DB account. These commands mirror the data model hierarchy: account → database → collection → documents. The database is a logical grouping, while the collection is where your actual data will be stored.

1. **Create** the MongoDB database:

   ```bash
   az cosmosdb mongodb database create \
     --account-name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --name $DATABASE_NAME
   ```

2. **Create** the collection with a shard key:

   ```bash
   az cosmosdb mongodb collection create \
     --account-name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --database-name $DATABASE_NAME \
     --name $COLLECTION_NAME \
     --shard "category"
   ```

3. **Verify** the database was created:

   ```bash
   az cosmosdb mongodb database list \
     --account-name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --output table
   ```

4. **Verify** the collection was created:

   ```bash
   az cosmosdb mongodb collection list \
     --account-name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --database-name $DATABASE_NAME \
     --output table
   ```

> ℹ **Concept Deep Dive**
>
> The `az cosmosdb mongodb` subcommand group provides MongoDB-specific operations. The command hierarchy mirrors the data model: `az cosmosdb mongodb database` for database operations and `az cosmosdb mongodb collection` for collection operations. The `--shard` parameter sets the partition key path — this determines how Cosmos DB distributes documents across physical partitions.
>
> Unlike the Portal experience where you click through forms, CLI provisioning can be scripted, version-controlled in Git, and integrated into CI/CD pipelines. This is a key advantage for infrastructure automation — you can reproduce the exact same infrastructure in development, staging, and production environments.
>
> ⚠ **Common Mistakes**
>
> - The `--shard` key path does not include a leading `/` in the CLI command (unlike the Portal which shows `/category`)
> - Running the collection create command before the database create command fails — the database must exist first
> - Misspelling `--account-name` as `--name` will cause the command to fail with a confusing error
>
> ✓ **Quick check:** Both `list` commands display your database and collection in table format

### **Step 4:** Retrieve the Connection String

Retrieve the connection string that applications will use to connect to your Cosmos DB account. The connection string uses the standard MongoDB URI format, which means existing MongoDB drivers can connect to Cosmos DB without code changes — only the connection string needs to change.

1. **Retrieve** the primary connection string:

   ```bash
   az cosmosdb keys list \
     --name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --type connection-strings \
     --query "connectionStrings[0].connectionString" \
     --output tsv
   ```

2. **Store** the connection string in an environment variable for use in applications:

   ```bash
   export COSMOS_CONNECTION_STRING=$(az cosmosdb keys list \
     --name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --type connection-strings \
     --query "connectionStrings[0].connectionString" \
     --output tsv)
   ```

3. **Verify** the variable was set:

   ```bash
   echo "Connection string starts with: ${COSMOS_CONNECTION_STRING:0:30}..."
   ```

> ℹ **Concept Deep Dive**
>
> The `--type connection-strings` flag retrieves the full connection URI instead of raw access keys. The `--query` parameter uses a JMESPath expression to extract just the first connection string from the response array. Using `--output tsv` produces raw text without JSON quotes, which is essential when storing the value in a variable — quoted strings would break application connection attempts.
>
> Storing the connection string in an environment variable keeps it out of shell history files and makes it available to applications that read configuration from environment variables (a twelve-factor app best practice). The variable is only available in the current shell session — for persistence, use a `.env` file (excluded from Git), Azure Key Vault, or your IDE's run configuration.
>
> ⚠ **Common Mistakes**
>
> - Using `--type keys` instead of `--type connection-strings` returns raw access keys, not the complete MongoDB connection URI
> - Forgetting `--output tsv` wraps the value in JSON quotes, which breaks connection attempts
> - The environment variable only persists in the current terminal session — opening a new terminal requires setting it again
> - Never echo the full connection string in shared environments or logs — it contains the account access key
>
> ✓ **Quick check:** The echoed connection string starts with `mongodb://` and contains `.mongo.cosmos.azure.com:10255`

### **Step 5:** Build the Complete Provisioning Script

Combine all the individual commands into a single reusable script. Scripting the full provisioning process means you can reproduce your entire database infrastructure with a single command — essential for setting up new environments, disaster recovery, or onboarding new team members.

1. **Create** a new file named `provision-cosmosdb.sh`

2. **Add** the following script content:

   > `provision-cosmosdb.sh`

   ```bash
   #!/bin/bash
   set -euo pipefail

   # ============================================
   # CosmosDB Provisioning Script
   # Provisions a CosmosDB account with MongoDB API
   # ============================================

   # Configuration — change these for your environment
   RESOURCE_GROUP="CloudDatabasesRG"
   LOCATION="northeurope"
   ACCOUNT_NAME="cosmosdb-yourname-bcd"
   DATABASE_NAME="bookmarks_db"
   COLLECTION_NAME="bookmarks"

   echo "=== CosmosDB Provisioning Script ==="
   echo ""

   # Step 1: Resource Group
   echo "Creating resource group '$RESOURCE_GROUP' in '$LOCATION'..."
   az group create \
     --name $RESOURCE_GROUP \
     --location $LOCATION \
     --output none

   # Step 2: CosmosDB Account
   echo "Creating CosmosDB account '$ACCOUNT_NAME' (this may take several minutes)..."
   az cosmosdb create \
     --name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --kind MongoDB \
     --server-version "4.2" \
     --capabilities EnableServerless \
     --locations regionName=$LOCATION failoverPriority=0 isZoneRedundant=false \
     --output none

   # Step 3: Database
   echo "Creating database '$DATABASE_NAME'..."
   az cosmosdb mongodb database create \
     --account-name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --name $DATABASE_NAME \
     --output none

   # Step 4: Collection
   echo "Creating collection '$COLLECTION_NAME' with shard key 'category'..."
   az cosmosdb mongodb collection create \
     --account-name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --database-name $DATABASE_NAME \
     --name $COLLECTION_NAME \
     --shard "category" \
     --output none

   # Step 5: Retrieve Connection String
   echo "Retrieving connection string..."
   CONNECTION_STRING=$(az cosmosdb keys list \
     --name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --type connection-strings \
     --query "connectionStrings[0].connectionString" \
     --output tsv)

   echo ""
   echo "=== Provisioning Complete ==="
   echo "Account:    $ACCOUNT_NAME"
   echo "Database:   $DATABASE_NAME"
   echo "Collection: $COLLECTION_NAME"
   echo ""
   echo "Connection String:"
   echo "$CONNECTION_STRING"
   ```

3. **Make** the script executable:

   ```bash
   chmod +x provision-cosmosdb.sh
   ```

4. **Run** the script:

   ```bash
   ./provision-cosmosdb.sh
   ```

> ℹ **Concept Deep Dive**
>
> The `set -euo pipefail` line at the top enables strict error handling, which is critical for infrastructure scripts:
>
> - `-e` exits immediately if any command returns a non-zero exit code
> - `-u` treats references to unset variables as errors (catches typos in variable names)
> - `-o pipefail` ensures that errors in piped commands are not silently swallowed
>
> The `--output none` flag suppresses the verbose JSON output that Azure CLI returns by default. This keeps the script output clean with only the progress messages you defined. Errors will still be printed to stderr, so failures are not hidden.
>
> This script is idempotent for the resource group (`az group create` succeeds even if it already exists) but the `az cosmosdb create` command will fail if an account with the same name already exists. For a fully idempotent script, you would add existence checks or use Bicep templates (covered in the next section).
>
> ⚠ **Common Mistakes**
>
> - Forgetting `chmod +x` results in "Permission denied" when trying to run the script
> - Running the script without being logged in (`az login`) causes all commands to fail
> - The script prints the connection string to stdout — be careful not to share terminal output or logs that contain it
>
> ✓ **Quick check:** The script runs without errors and prints the account name, database, collection, and connection string at the end

### **Step 6:** Test and Verify

Confirm that all resources were provisioned correctly by checking them both from the CLI and the Azure Portal. This verification step ensures your automation produced the expected results.

1. **Verify** the Cosmos DB account exists and has the correct configuration:

   ```bash
   az cosmosdb show \
     --name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --query "{name:name, kind:kind, capabilities:capabilities[0].name}" \
     --output table
   ```

2. **Verify** the database and collection exist:

   ```bash
   az cosmosdb mongodb database list \
     --account-name $ACCOUNT_NAME \
     --resource-group $RESOURCE_GROUP \
     --query "[].name" \
     --output tsv
   ```

3. **Open** the Azure Portal at <https://portal.azure.com> and **navigate to** your Cosmos DB account to visually confirm:

   - The account shows "API: MongoDB" and "Capacity mode: Serverless"
   - The `bookmarks_db` database appears in Data Explorer
   - The `bookmarks` collection exists with shard key `/category`

4. **Clean up** resources when you are finished experimenting (optional):

   ```bash
   az group delete --name $RESOURCE_GROUP --yes --no-wait
   ```

> ℹ **Concept Deep Dive**
>
> The `--no-wait` flag on the delete command starts the deletion process and returns immediately without waiting for it to complete. Resource group deletion cascades to all resources inside it — this is both powerful and dangerous, which is why the `--yes` flag is required to skip the confirmation prompt.
>
> Verifying CLI-provisioned resources in the Portal is good practice — it confirms that the automation produced the same result you would get through manual provisioning. In production workflows, this verification step would be replaced by automated integration tests or infrastructure validation scripts.
>
> ⚠ **Common Mistakes**
>
> - Running `az group delete` without `--yes` causes the command to hang waiting for interactive confirmation
> - Deleting the resource group removes everything inside it — make sure you don't have other resources in the same group that you want to keep
> - The delete operation may take several minutes even with `--no-wait` — the flag only makes the CLI return immediately, not the actual deletion
>
> ✓ **Quick check:** The Portal shows your Cosmos DB account with MongoDB API and Serverless capacity, and the database and collection are visible in Data Explorer

## Common Issues

> **If you encounter problems:**
>
> **"The subscription is not registered to use namespace 'Microsoft.DocumentDB'":** Run `az provider register --namespace Microsoft.DocumentDB` and wait a few minutes before retrying.
>
> **"Account name already exists":** Cosmos DB account names are globally unique. Change `ACCOUNT_NAME` to include a random suffix or your initials.
>
> **"AuthorizationFailed":** Your Azure account may not have the required permissions. Ensure you have at least Contributor role on the subscription or resource group.
>
> **Command hangs or takes very long:** The `az cosmosdb create` command can take up to 10 minutes. If it exceeds this, check the deployment status in the Azure Portal under your resource group's "Deployments" section.
>
> **Still stuck?** Verify you are using the correct Azure subscription with `az account show` and switch if needed with `az account set --subscription "Your Subscription Name"`.

## Summary

You've successfully provisioned Azure Cosmos DB infrastructure entirely from the command line which:

- ✓ Creates a Cosmos DB account with MongoDB API and serverless capacity
- ✓ Sets up a database and collection with a partition key
- ✓ Retrieves the connection string programmatically
- ✓ Packages everything into a reusable provisioning script

> **Key takeaway:** CLI-based provisioning transforms manual portal clicks into repeatable, scriptable commands. The provisioning script you built can be version-controlled in Git, shared with team members, and integrated into CI/CD pipelines. This is a fundamental shift from "click-ops" to infrastructure automation — the same approach scales from a single development database to provisioning hundreds of environments.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Add command-line arguments to the script (using `$1`, `$2`, or `getopts`) to make the account name and region configurable at runtime
> - Explore `az cosmosdb mongodb user` commands for creating database users with specific roles
> - Add an existence check (`az cosmosdb show`) before creating to make the script fully idempotent
> - Investigate `az cosmosdb keys regenerate` for rotating access keys as a security practice

## Done! 🎉

Great job! You've automated the provisioning of Azure Cosmos DB using the Azure CLI. You can now create and tear down database infrastructure with a single script, laying the foundation for infrastructure as code practices you will build on with Bicep templates.
