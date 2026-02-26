+++
title = "5. Provisioning Azure Blob Storage"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 5
date = 2026-02-25
lastmod = 2026-02-25
draft = false
+++

# Provisioning Azure Blob Storage

## Goal

Create an Azure Storage Account, configure a Blob container for public image hosting, upload a hero image, and retrieve the container URL for use in your application's configuration.

> **What you'll learn:**
>
> - How to create an Azure Storage Account through the Azure Portal
> - How to create a Blob container with anonymous read access for public assets
> - How to upload files to Azure Blob Storage
> - How to construct the container URL for application configuration
> - How to verify blob access from a browser
> - Key concepts of Azure Blob Storage: accounts, containers, and blobs

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Active Azure subscription with resource creation permissions
> - ✓ Familiarity with the Azure Portal
> - ✓ A hero image file (any `.jpg` or `.png` image, ideally 1920x1080 or similar for a hero section)
> - ✓ A web browser for verification

## Exercise Steps

### Overview

1. **Create a Storage Account**
2. **Create a Blob Container**
3. **Upload a Hero Image**
4. **Verify Public Access**
5. **Configure the Application**
6. **Test and Verify**

### **Step 1:** Create a Storage Account

Set up an Azure Storage Account to host your application's static assets. A Storage Account is the top-level resource for all Azure Storage services — it provides a unique namespace in Azure for your blobs, files, queues, and tables. For this exercise, you will use the Blob service to host images that your web application serves to users.

1. **Navigate to** the Azure Portal at <https://portal.azure.com>

2. **Search for** "Storage accounts" using the search bar at the top

3. **Select** Storage accounts from the search results

4. **Click** the **+ Create** button

5. **Configure** the Basics tab with the following settings:

   - **Subscription**: Select your subscription
   - **Resource Group**: Use your existing resource group (e.g., `CloudDatabasesRG`) or create a new one
   - **Storage account name**: Enter a globally unique name (e.g., `cloudsoftyourname`, lowercase letters and numbers only, 3–24 characters)
   - **Region**: Select `North Europe` (or a region close to you)
   - **Performance**: Select `Standard`
   - **Redundancy**: Select `Locally-redundant storage (LRS)`

6. **Click** Review + Create, then **click** Create

7. **Wait** for the deployment to complete (this typically takes 30–60 seconds)

> ℹ **Concept Deep Dive**
>
> Azure Storage Accounts provide a unified namespace for multiple storage services. The Blob service (Binary Large Object) is optimized for storing unstructured data such as images, videos, documents, and backups. Each storage account supports up to 5 PiB of data and is accessible via HTTP/HTTPS from anywhere in the world.
>
> **Performance tiers:** Standard uses magnetic drives (HDD) and is cost-effective for most workloads. Premium uses solid-state drives (SSD) and is designed for low-latency scenarios like disk storage for virtual machines. For serving static images, Standard is the appropriate choice.
>
> **Redundancy options:** LRS (Locally-redundant storage) maintains three copies of your data within a single data center. This is the most affordable option and provides sufficient durability for development and non-critical production workloads. For business-critical applications, you would choose GRS (Geo-redundant storage) which replicates data to a secondary region.
>
> ⚠ **Common Mistakes**
>
> - The storage account name must be globally unique across all of Azure, contain only lowercase letters and numbers, and be 3–24 characters long
> - Choosing Premium performance for image hosting adds unnecessary cost — Standard is sufficient for serving static content
> - Selecting a region far from your users increases latency for image loading
>
> ✓ **Quick check:** Navigate to the Storage Account overview page and verify it shows the correct region and "Standard" performance tier

### **Step 2:** Create a Blob Container

Create a container within your Storage Account to organize and store your image files. A container is similar to a directory — it groups related blobs together and provides access control at the container level. For serving images publicly through your web application, the container needs anonymous read access.

1. **Navigate to** your newly created Storage Account in the Azure Portal

2. **Click** "Containers" in the left menu under the "Data storage" section

3. **Click** the **+ Container** button

4. **Enter** the container name: `images`

5. **Set** the "Anonymous access level" to **Blob (anonymous read access for blobs only)**

6. **Click** Create

> ℹ **Concept Deep Dive**
>
> Azure Blob Storage has a three-level hierarchy: Storage Account → Container → Blob. Containers serve as the organizational unit — they are like top-level folders that hold blobs (files). Each container has its own access policy that controls whether unauthenticated users can read the blobs inside it.
>
> **Anonymous access levels:**
>
> - **Private (no anonymous access)** — Default. All requests must be authenticated with an account key, SAS token, or Azure AD. Use this for sensitive data.
> - **Blob (anonymous read access for blobs only)** — Anyone with the URL can read individual blobs, but cannot list the blobs in the container. This is ideal for serving public assets like images — users can access `hero.jpg` directly but cannot browse the container to discover other files.
> - **Container (anonymous read access for containers and blobs)** — Anyone can read blobs and list all blobs in the container. Avoid this unless you intentionally want the container contents to be discoverable.
>
> If the "Anonymous access level" dropdown is greyed out or missing, it means that anonymous access is disabled at the storage account level. To enable it: go to Settings → Configuration in your Storage Account, find "Allow Blob anonymous access", set it to **Enabled**, and save. Then return to create the container.
>
> ⚠ **Common Mistakes**
>
> - Leaving the access level as "Private" means your application cannot serve images without authentication tokens — the browser will get a 403 Forbidden error
> - Setting the access level to "Container" instead of "Blob" allows anyone to list all files in the container, which may expose assets you did not intend to make discoverable
> - Container names must be lowercase, 3–63 characters, and can only contain letters, numbers, and hyphens
>
> ✓ **Quick check:** The `images` container appears in the container list with "Blob" shown as the access level

### **Step 3:** Upload a Hero Image

Upload your hero image to the Blob container. Once uploaded, the image will be accessible via a public URL that follows a predictable pattern based on your storage account name, container name, and blob name.

1. **Click** on the `images` container to open it

2. **Click** the **Upload** button in the toolbar

3. **Click** "Browse for files" and **select** your hero image file

4. **Rename** the file to `hero.jpg` in the upload dialog (or ensure your file is already named `hero.jpg`)

5. **Click** Upload

6. **Verify** the upload by checking that `hero.jpg` appears in the container's blob list

7. **Click** on `hero.jpg` to open its properties

8. **Copy** the blob URL — it will follow this pattern:

   ```text
   https://{accountname}.blob.core.windows.net/images/hero.jpg
   ```

> ℹ **Concept Deep Dive**
>
> Azure Blob Storage URLs follow a deterministic pattern: `https://{account}.blob.core.windows.net/{container}/{blob}`. This predictable URL structure means your application can construct image URLs by simply knowing the storage account name, container name, and file name — no API calls needed to resolve URLs at runtime.
>
> When you upload a blob, Azure stores it with metadata including the content type (MIME type). The Portal typically auto-detects the content type based on the file extension (e.g., `image/jpeg` for `.jpg`). Correct content types are important because browsers use them to determine how to handle the response — an incorrect content type could cause the browser to download the image instead of displaying it.
>
> Blob names are case-sensitive in Azure Storage. `hero.jpg` and `Hero.jpg` are treated as two different blobs. Establish a naming convention (lowercase is recommended) and use it consistently across your application code and storage.
>
> ⚠ **Common Mistakes**
>
> - Uploading with a different filename than what the application expects — the CloudSoft application expects `hero.jpg`
> - Very large images (10+ MB) will slow page loading. For a hero section, optimize the image to 500 KB–2 MB for a good balance between quality and performance
> - Forgetting to note the full blob URL — you will need the container base URL (without the filename) for the application configuration
>
> ✓ **Quick check:** The blob URL displayed in the properties panel matches the pattern `https://{accountname}.blob.core.windows.net/images/hero.jpg`

### **Step 4:** Verify Public Access

Confirm that the uploaded image is publicly accessible by opening the blob URL directly in a browser. This verification step ensures that the container access level is configured correctly before integrating with your application.

1. **Copy** the full blob URL from the previous step (e.g., `https://cloudsoftyourname.blob.core.windows.net/images/hero.jpg`)

2. **Open** a new browser tab (or an incognito/private window for a clean test)

3. **Paste** the URL into the browser address bar and **press** Enter

4. **Verify** that the image loads and displays correctly in the browser

> ℹ **Concept Deep Dive**
>
> Testing in an incognito or private browser window is a best practice when verifying public access. Your regular browser session may have cached Azure Portal authentication tokens that could mask access issues — an incognito window has no stored credentials, simulating how an unauthenticated user (or your application's end users) will access the image.
>
> If the image loads successfully, it confirms two things: the container's anonymous access level is set to "Blob" (or "Container"), and the blob was uploaded with the correct content type. If you see an XML error response instead of the image, the access configuration needs to be corrected.
>
> ⚠ **Common Mistakes**
>
> - Getting a `ResourceNotFound` error means the URL is incorrect — double-check the storage account name, container name, and blob name (all case-sensitive)
> - Getting a `PublicAccessNotPermitted` error means anonymous access is disabled at the storage account level — enable it in Settings → Configuration → "Allow Blob anonymous access"
> - Getting an `AuthorizationPermissionMismatch` error means the container access level is set to "Private" — change it to "Blob" in the container's access level settings
>
> ✓ **Quick check:** The hero image displays correctly in an incognito browser window without any authentication

### **Step 5:** Configure the Application

Update your CloudSoft application's configuration to use the Azure Blob Storage container URL. The application uses the `AzureBlob:ContainerUrl` configuration key to construct image URLs at runtime. By updating this value, the application will serve images from Azure Blob Storage instead of the local file system.

1. **Construct** the container base URL (without the filename):

   ```text
   https://{accountname}.blob.core.windows.net/images
   ```

2. **Open** the application's `appsettings.json` file

3. **Locate** the `AzureBlob` configuration section

4. **Update** the `ContainerUrl` value with your actual container URL:

   > `appsettings.json`

   ```json
   {
     "AzureBlob": {
       "ContainerUrl": "https://cloudsoftyourname.blob.core.windows.net/images"
     }
   }
   ```

5. **Set** the `UseAzureStorage` feature flag to `true` to enable Azure Blob Storage:

   > `appsettings.json`

   ```json
   {
     "FeatureFlags": {
       "UseMongoDb": true,
       "UseAzureStorage": true
     }
   }
   ```

> ℹ **Concept Deep Dive**
>
> The application uses the **Options Pattern** to bind the `AzureBlob` configuration section to an `AzureBlobOptions` class. At runtime, the `AzureBlobImageService` reads the `ContainerUrl` and appends the image filename to construct the full URL. For example, when the controller requests `hero.jpg`, the service returns `https://cloudsoftyourname.blob.core.windows.net/images/hero.jpg`.
>
> The **feature flag** (`UseAzureStorage`) controls which `IImageService` implementation is registered in the dependency injection container. When `false`, the application uses `LocalImageService` which serves images from the `wwwroot/images` folder. When `true`, it uses `AzureBlobImageService` which serves images from Azure Blob Storage. This pattern allows you to switch between local and cloud storage without code changes.
>
> The `ContainerUrl` should not include a trailing slash or a filename — just the base URL up to and including the container name. The service appends the image filename at runtime.
>
> ⚠ **Common Mistakes**
>
> - Including a trailing slash in the `ContainerUrl` (e.g., `https://account.blob.core.windows.net/images/`) results in a double slash in the constructed URL
> - Including the blob name in the `ContainerUrl` (e.g., `https://account.blob.core.windows.net/images/hero.jpg`) means the service will append the filename again
> - Forgetting to set `UseAzureStorage` to `true` means the application will continue using local storage even though the URL is configured
> - Using HTTP instead of HTTPS — Azure Blob Storage supports both, but HTTPS should always be used for security
>
> ✓ **Quick check:** The `ContainerUrl` value ends with the container name (`/images`) and does not include a trailing slash or filename

### **Step 6:** Test and Verify

Confirm that the full integration works by running the application and verifying that the hero image loads from Azure Blob Storage. This end-to-end verification ensures that the infrastructure provisioning, configuration, and application code work together correctly.

1. **Run** the application:

   ```bash
   dotnet run
   ```

2. **Check** the console output — you should see:

   ```text
   Using Azure Blob Storage for images
   ```

3. **Navigate to** the About page in your browser

4. **Verify** that the hero section displays with the background image

5. **Inspect** the image source to confirm it loads from Azure Blob Storage:

   - Right-click the hero section and select "Inspect" (or press F12)
   - Look at the `background-image` CSS property on the `.hero-section` element
   - Confirm the URL points to your Azure Blob Storage (e.g., `https://cloudsoftyourname.blob.core.windows.net/images/hero.jpg`)

6. **Test** with Azure Storage disabled — change `UseAzureStorage` back to `false`, restart the application, and verify it falls back to local storage:

   ```text
   Using local storage for images
   ```

> ✓ **Success indicators:**
>
> - Console output shows "Using Azure Blob Storage for images"
> - The About page hero section renders with the background image
> - Browser DevTools confirm the image URL points to Azure Blob Storage
> - Switching the feature flag to `false` correctly falls back to local storage
>
> ✓ **Final verification checklist:**
>
> - ☐ Storage Account created with Standard performance and LRS redundancy
> - ☐ Blob container `images` created with "Blob" anonymous access level
> - ☐ `hero.jpg` uploaded and accessible via public URL
> - ☐ `appsettings.json` updated with correct `ContainerUrl`
> - ☐ Feature flag `UseAzureStorage` set to `true`
> - ☐ Application loads hero image from Azure Blob Storage

## Common Issues

> **If you encounter problems:**
>
> **Image not loading (403 Forbidden):** The container access level is set to "Private". Navigate to the container in the Azure Portal, click "Change access level", and select "Blob (anonymous read access for blobs only)".
>
> **Image not loading (404 Not Found):** The blob name in the URL does not match the uploaded file name. Blob names are case-sensitive — verify that the file was uploaded as `hero.jpg` (lowercase).
>
> **"Allow Blob anonymous access" is disabled:** Navigate to your Storage Account → Settings → Configuration. Set "Allow Blob anonymous access" to **Enabled** and save. Then update the container access level.
>
> **Console shows "Using local storage for images":** The `UseAzureStorage` feature flag is `false`. Update it to `true` in `appsettings.json` (or `appsettings.Development.json` if running in development mode).
>
> **Image URL has double slashes:** The `ContainerUrl` has a trailing slash. Remove it so the URL ends with the container name (e.g., `/images` not `/images/`).
>
> **Still stuck?** Test the blob URL directly in an incognito browser window first. If the image loads there but not in the application, the issue is in the application configuration. If it does not load in the browser, the issue is in the Azure Storage configuration.

## Summary

You've successfully provisioned Azure Blob Storage infrastructure for serving application images which:

- ✓ Provides a globally accessible, high-availability storage service for static assets
- ✓ Uses anonymous read access for public image serving without authentication overhead
- ✓ Integrates with the application through a simple configuration URL
- ✓ Supports feature flag toggling between local and cloud storage

> **Key takeaway:** Azure Blob Storage provides a scalable, cost-effective solution for serving static assets from the cloud. The deterministic URL pattern (`https://{account}.blob.core.windows.net/{container}/{blob}`) means your application can construct image URLs from configuration alone — no SDK or API calls needed at runtime. Combined with a feature flag, this gives you a clean separation between local development (fast iteration with local files) and production deployment (globally distributed cloud storage).

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Create the same Storage Account and container using the Azure CLI (`az storage account create` and `az storage container create`) for scriptable provisioning
> - Configure a custom domain for your Blob Storage endpoint so images are served from your own domain (e.g., `images.cloudsoft.com`)
> - Enable Azure CDN (Content Delivery Network) in front of Blob Storage to cache images at edge locations worldwide for faster loading
> - Explore SAS tokens (Shared Access Signatures) to provide time-limited access to private blobs without making the container public

## Done! 🎉

Great job! You've provisioned Azure Blob Storage, uploaded a hero image, and configured your application to serve images from the cloud. This infrastructure foundation enables your application to scale its static asset delivery independently of the web server, a common pattern in cloud-native application architecture.
