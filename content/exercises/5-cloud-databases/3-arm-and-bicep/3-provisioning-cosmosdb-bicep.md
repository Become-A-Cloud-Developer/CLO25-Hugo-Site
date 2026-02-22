+++
title = "3. Provisioning CosmosDB with Bicep"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 3
date = 2026-02-22
lastmod = 2026-02-22
draft = false
+++

# Provisioning CosmosDB with Bicep

## Goal

Define and deploy Azure Cosmos DB infrastructure declaratively using a Bicep template. Create a complete template that provisions a Cosmos DB account with the MongoDB API, a database, and a collection — then deploy it using `az deployment group create` and verify the resources in the Azure Portal.

> **What you'll learn:**
>
> - How to write a Bicep template for Cosmos DB with the MongoDB API
> - How to use the `Microsoft.DocumentDB/databaseAccounts` resource type
> - How to configure serverless capability declaratively
> - How to define child resources for databases and collections
> - How to deploy Bicep templates and retrieve outputs
> - Key advantages of Infrastructure as Code over imperative CLI scripts

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Active Azure subscription with resource creation permissions
> - ✓ Azure CLI installed and authenticated (`az login`)
> - ✓ Bicep CLI installed (bundled with Azure CLI 2.20+, verify with `az bicep version`)
> - ✓ A text editor (VS Code with the Bicep extension recommended)
> - ✓ Familiarity with Azure CLI basics

## Exercise Steps

### Overview

1. **Create the Bicep Template with Parameters**
2. **Define the Cosmos DB Account Resource**
3. **Define the Database and Collection**
4. **Add Outputs**
5. **Deploy the Bicep Template**
6. **Verify in the Azure Portal**

### **Step 1:** Create the Bicep Template with Parameters

Create a new Bicep file and define the parameters that make the template configurable and reusable. Parameters allow the same template to be deployed across multiple environments — development, staging, and production — by changing only the parameter values.

1. **Create** a new file named `cosmosdb.bicep` in your working directory

2. **Add** the following parameter definitions:

   > `cosmosdb.bicep`

   ```bicep
   @description('The name of the Cosmos DB account. Must be globally unique.')
   param accountName string

   @description('The Azure region for the Cosmos DB account.')
   param location string = resourceGroup().location

   @description('The name of the MongoDB database.')
   param databaseName string = 'bookmarks_db'

   @description('The name of the MongoDB collection.')
   param collectionName string = 'bookmarks'

   @description('The shard key for the collection.')
   param shardKey string = 'category'
   ```

> ℹ **Concept Deep Dive**
>
> Bicep parameters with default values allow flexible reuse — you only need to provide the account name (which must be globally unique), while other values have sensible defaults. The `@description` decorator provides documentation that appears in editor tooltips and ARM template exports. Using `resourceGroup().location` as the default location ensures the Cosmos DB account is created in the same region as its resource group, reducing latency and simplifying deployment.
>
> Bicep is a domain-specific language (DSL) that compiles to ARM JSON templates. It provides a cleaner, more readable syntax while producing the same deployment artifacts. Every Bicep file compiles 1:1 to an equivalent ARM template, so there is no loss of capability.
>
> ⚠ **Common Mistakes**
>
> - Forgetting that `accountName` has no default — it must be provided at deployment time
> - Using a `.json` extension instead of `.bicep` — the Azure CLI expects Bicep files for Bicep deployments
> - Parameter names are case-sensitive in Bicep — `accountName` and `AccountName` are different parameters
>
> ✓ **Quick check:** The file is saved with a `.bicep` extension, and VS Code shows no syntax errors if the Bicep extension is installed

### **Step 2:** Define the Cosmos DB Account Resource

Add the Cosmos DB account resource definition to the template. This resource represents the top-level account that hosts all databases and collections. The resource type, API version, and property structure follow the Azure Resource Manager schema for `Microsoft.DocumentDB/databaseAccounts`.

1. **Add** the following resource definition below the parameters in `cosmosdb.bicep`:

   > `cosmosdb.bicep`

   ```bicep
   resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
     name: accountName
     location: location
     kind: 'MongoDB'
     properties: {
       databaseAccountOfferType: 'Standard'
       capabilities: [
         {
           name: 'EnableServerless'
         }
         {
           name: 'EnableMongo'
         }
       ]
       locations: [
         {
           locationName: location
           failoverPriority: 0
           isZoneRedundant: false
         }
       ]
       apiProperties: {
         serverVersion: '4.2'
       }
     }
   }
   ```

> ℹ **Concept Deep Dive**
>
> The resource declaration follows the pattern: `resource <symbolic-name> '<type>@<api-version>' = { ... }`. The symbolic name (`cosmosAccount`) is used only within the Bicep file to reference this resource — it does not appear in Azure.
>
> Key properties explained:
>
> - `kind: 'MongoDB'` — Selects the MongoDB API for this account
> - `databaseAccountOfferType: 'Standard'` — Required property with only one valid value
> - `capabilities` — An array enabling `EnableServerless` (pay-per-use pricing) and `EnableMongo` (MongoDB wire protocol support)
> - `locations` — Configures the primary region with `failoverPriority: 0` (single-region for serverless)
> - `apiProperties.serverVersion` — Sets the MongoDB wire protocol version
>
> Unlike CLI commands that execute immediately, Bicep describes the desired end state. Azure Resource Manager compares the template against the current state and makes only the necessary changes — this is declarative infrastructure management.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `EnableMongo` in the capabilities array alongside `EnableServerless` can cause deployment failures
> - The `kind` value is case-sensitive — use `'MongoDB'`, not `'MongoDb'` or `'mongo'`
> - Using an API version older than `2021-10-15` may not support serverless capabilities
> - Omitting `databaseAccountOfferType` causes a validation error even though `'Standard'` is the only option
>
> ✓ **Quick check:** No red squiggly lines in VS Code, and the resource block is properly nested within the file

### **Step 3:** Define the Database and Collection

Add the database and collection as child resources of the Cosmos DB account. Bicep's `parent` property establishes the resource hierarchy, which is cleaner and more readable than the slash-separated naming convention used in raw ARM JSON templates.

1. **Add** the database resource below the account resource in `cosmosdb.bicep`:

   > `cosmosdb.bicep`

   ```bicep
   resource database 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases@2024-05-15' = {
     parent: cosmosAccount
     name: databaseName
     properties: {
       resource: {
         id: databaseName
       }
     }
   }
   ```

2. **Add** the collection resource below the database resource:

   > `cosmosdb.bicep`

   ```bicep
   resource collection 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases/collections@2024-05-15' = {
     parent: database
     name: collectionName
     properties: {
       resource: {
         id: collectionName
         shardKey: {
           '${shardKey}': 'Hash'
         }
       }
     }
   }
   ```

> ℹ **Concept Deep Dive**
>
> The `parent` property in Bicep establishes the resource hierarchy: account → database → collection. This tells Azure Resource Manager about the dependency chain — the database cannot be created before the account, and the collection cannot be created before the database. Bicep handles this ordering automatically.
>
> The `resource.id` property inside `properties` is required by the ARM API and must match the resource name. This duplication is an ARM API design pattern — the outer `name` is the resource identifier for Azure Resource Manager, while the inner `resource.id` is the identifier for the Cosmos DB service itself.
>
> The `shardKey` object maps the partition key path to its partitioning strategy. The syntax `'${shardKey}': 'Hash'` uses Bicep string interpolation to set the parameter value as the object key, with `'Hash'` specifying hash-based partitioning (the standard strategy for most workloads).
>
> ⚠ **Common Mistakes**
>
> - Forgetting the `parent` property and instead using slash-separated names results in implicit dependencies that can cause race conditions
> - The `resource.id` must exactly match the `name` — mismatches cause confusing deployment errors
> - Missing the `resource` wrapper inside `properties` causes validation failures — ARM requires this nested structure
>
> ✓ **Quick check:** The Bicep file now contains three resources: `cosmosAccount`, `database`, and `collection`, each referencing its parent

### **Step 4:** Add Outputs

Add output declarations to make key values easily retrievable after deployment. Outputs are displayed in the terminal when the deployment completes and can be programmatically consumed by scripts or CI/CD pipelines.

1. **Add** the following output declarations at the bottom of `cosmosdb.bicep`:

   > `cosmosdb.bicep`

   ```bicep
   @description('The name of the deployed Cosmos DB account.')
   output accountNameOutput string = cosmosAccount.name

   @description('The primary connection string for the Cosmos DB account.')
   output connectionString string = cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString
   ```

> ℹ **Concept Deep Dive**
>
> The `listConnectionStrings()` function is a Bicep runtime function that calls the Azure Resource Manager API at deployment time to retrieve the connection string from the deployed account. This is different from a property reference — it makes an API call to get a value that is only available after the resource is created.
>
> Outputs are stored in the deployment metadata and can be retrieved later using `az deployment group show`. In production environments, you would typically avoid outputting sensitive values like connection strings and instead store them directly in Azure Key Vault using a Bicep module. For learning purposes, the output makes it easy to verify the deployment.
>
> ⚠ **Common Mistakes**
>
> - The `listConnectionStrings()` function is only available at deployment time — it cannot be used in parameter defaults or variables
> - Output values containing secrets will be visible in deployment logs and the Azure Portal's deployment history
> - Referencing `cosmosAccount.properties.connectionStrings` does not work — you must use the `listConnectionStrings()` function
>
> ✓ **Quick check:** The complete file should now have five sections: parameters, account resource, database resource, collection resource, and outputs

### **Step 5:** Deploy the Bicep Template

Deploy the template to Azure using the Azure CLI. The `az deployment group create` command sends your Bicep template to Azure Resource Manager, which compiles it to ARM JSON and orchestrates the resource creation in the correct dependency order.

1. **Ensure** the resource group exists:

   ```bash
   az group create --name CloudDatabasesRG --location northeurope
   ```

2. **Deploy** the Bicep template (replace `yourname` with your actual name):

   ```bash
   az deployment group create \
     --resource-group CloudDatabasesRG \
     --template-file cosmosdb.bicep \
     --parameters accountName="cosmosdb-yourname-bcd"
   ```

3. **Wait** for the deployment to complete (typically 3–8 minutes)

4. **Retrieve** the deployment outputs after completion:

   ```bash
   az deployment group show \
     --resource-group CloudDatabasesRG \
     --name cosmosdb \
     --query "properties.outputs" \
     --output json
   ```

5. **Extract** just the connection string for use in applications:

   ```bash
   az deployment group show \
     --resource-group CloudDatabasesRG \
     --name cosmosdb \
     --query "properties.outputs.connectionString.value" \
     --output tsv
   ```

> ℹ **Concept Deep Dive**
>
> Bicep deployments are idempotent — running the same template multiple times with the same parameters produces the same result without errors. Azure Resource Manager compares the template's desired state against the current state and makes only the necessary changes. If all resources already exist with the correct configuration, the deployment completes quickly with no modifications. This is a significant advantage over imperative CLI scripts, which would fail on the second run because the resources already exist.
>
> The deployment name defaults to the template filename without the extension (`cosmosdb` from `cosmosdb.bicep`). You can override this with the `--name` parameter if you want a custom deployment name.
>
> ⚠ **Common Mistakes**
>
> - Forgetting to create the resource group first results in a "ResourceGroupNotFound" error
> - Providing an account name that already exists globally (even in other subscriptions) causes a conflict error
> - Running the command from a directory that does not contain `cosmosdb.bicep` causes a "file not found" error
> - The `--parameters` flag uses `key=value` syntax (no spaces around `=`)
>
> ✓ **Quick check:** The deployment completes with `"provisioningState": "Succeeded"` and the outputs section shows the account name and connection string

### **Step 6:** Verify in the Azure Portal

Confirm that the Bicep deployment created all resources correctly by inspecting them in the Azure Portal. Verifying infrastructure as code deployments visually helps build confidence in the automation and catches any configuration mismatches.

1. **Navigate to** the Azure Portal at <https://portal.azure.com>

2. **Open** the Cosmos DB account by searching for the account name or navigating through the `CloudDatabasesRG` resource group

3. **Confirm** the following on the account overview page:

   - API shows "MongoDB"
   - Capacity mode shows "Serverless"
   - Region matches your selected location

4. **Click** "Data Explorer" in the left menu

5. **Verify** that `bookmarks_db` database and `bookmarks` collection appear in the tree view

6. **Insert** a test document to verify the collection is functional:

   ```json
   {
       "title": "Bicep Documentation",
       "url": "https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/",
       "category": "cloud",
       "tags": ["azure", "iac", "bicep"],
       "createdAt": "2026-02-22T10:00:00Z"
   }
   ```

7. **Test idempotency** by re-running the deployment command from Step 5 — it should succeed without errors or changes

> ℹ **Concept Deep Dive**
>
> Idempotency is a core property of declarative infrastructure management. When you re-run the same Bicep template, Azure Resource Manager detects that the resources already exist with the correct configuration and reports "no changes needed." This makes Bicep templates safe to run in CI/CD pipelines on every commit — they converge toward the desired state without causing errors or duplicating resources.
>
> Compare this to the CLI script from the previous approach: running `az cosmosdb create` twice with the same account name would either fail or require additional existence checks. Bicep eliminates this entire category of problems by design.
>
> ✓ **Quick check:** The Portal shows all resources matching the template configuration, the test document saves successfully, and re-running the deployment produces no errors

## Common Issues

> **If you encounter problems:**
>
> **"InvalidTemplateDeployment" error:** Check the Bicep file for syntax errors. Run `az bicep build --file cosmosdb.bicep` to compile locally and see detailed error messages.
>
> **"AccountNameAlreadyExists":** Cosmos DB account names must be globally unique. Change the `accountName` parameter to include a random suffix.
>
> **"AuthorizationFailed":** Your Azure account may lack the required permissions. Ensure you have at least Contributor role on the resource group.
>
> **Deployment takes longer than 15 minutes:** Check the deployment status in the Azure Portal under your resource group → Deployments. Long deployments may indicate a region capacity issue — try a different region.
>
> **"BicepNotInstalled" or version errors:** Run `az bicep install` or `az bicep upgrade` to get the latest Bicep CLI version.
>
> **Still stuck?** Run `az bicep build --file cosmosdb.bicep` to validate the template locally before deploying. This catches most syntax and schema errors without waiting for a deployment round-trip.

## Summary

You've successfully defined and deployed Cosmos DB infrastructure using Bicep which:

- ✓ Declaratively defines the complete database infrastructure in a single file
- ✓ Provisions a Cosmos DB account, database, and collection with one command
- ✓ Outputs the connection string for application consumption
- ✓ Supports idempotent deployments — safe to run repeatedly

> **Key takeaway:** Infrastructure as Code with Bicep transforms infrastructure from manual configuration into version-controlled, reviewable, and repeatable artifacts. The same template can provision identical environments for development, staging, and production. Bicep's declarative approach means you describe what you want, not how to create it — Azure Resource Manager handles the orchestration, dependency ordering, and idempotency automatically.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Add a Key Vault resource to the template and store the connection string as a secret instead of outputting it
> - Create a parameters file (`cosmosdb.parameters.json`) for different environments (dev, staging, prod)
> - Add diagnostic settings to send Cosmos DB metrics to a Log Analytics workspace
> - Extract the Cosmos DB resources into a Bicep module for reuse across projects

## Done! 🎉

Great job! You've defined your entire Cosmos DB infrastructure as code using Bicep. This template can be version-controlled in Git, reviewed in pull requests, and deployed automatically through CI/CD pipelines — bringing software engineering practices to infrastructure management.
