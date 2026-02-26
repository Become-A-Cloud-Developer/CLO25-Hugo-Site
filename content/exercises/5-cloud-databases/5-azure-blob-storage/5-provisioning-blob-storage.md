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

Create an Azure Storage Account, configure a Blob container for public image hosting, upload a hero image, and verify that the blob is publicly accessible.

> **What you'll learn:**
>
> - How to create an Azure Storage Account through the Azure Portal
> - How to enable anonymous access at the account level
> - How to create a Blob container with anonymous read access for public assets
> - How to upload files to Azure Blob Storage
> - How to verify blob access from a browser
> - Key concepts of Azure Blob Storage: accounts, containers, and blobs

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Active Azure subscription with resource creation permissions
> - ✓ Familiarity with the Azure Portal
> - ✓ A hero image file (any `.jpg` or `.png` image, ideally 1920×1080 or similar for a hero section)

## Exercise Steps

### Overview

1. **Create a Storage Account**
2. **Create a Blob Container**
3. **Upload a Hero Image**
4. **Verify Public Access**

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

6. **Click** the **Advanced** tab and configure the following:

   - Under **Security**, **check** "Allow enabling anonymous access on individual containers"
   - Leave all other Advanced settings at their defaults

   > This setting is required for Step 2, where you will configure public read access on the Blob container. If this is not enabled at the account level, the anonymous access dropdown will be greyed out when creating the container.

7. **Click** Review + Create, then **click** Create

8. **Wait** for the deployment to complete (this typically takes 30–60 seconds)

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
> - **Blob (anonymous read access for blobs only)** — Anyone with the URL can read individual blobs, but cannot list the blobs in the container. This is ideal for serving public assets like images — users can access `hero.png` directly but cannot browse the container to discover other files.
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

4. **Rename** the file to `hero.png` in the upload dialog (or ensure your file is already named `hero.png`)

5. **Click** Upload

6. **Verify** the upload by checking that `hero.png` appears in the container's blob list

7. **Click** on `hero.png` to open its properties

8. **Copy** the blob URL — it will follow this pattern:

   ```text
   https://{accountname}.blob.core.windows.net/images/hero.png
   ```

> ℹ **Concept Deep Dive**
>
> Azure Blob Storage URLs follow a deterministic pattern: `https://{account}.blob.core.windows.net/{container}/{blob}`. This predictable URL structure means your application can construct image URLs by simply knowing the storage account name, container name, and file name — no API calls needed to resolve URLs at runtime.
>
> When you upload a blob, Azure stores it with metadata including the content type (MIME type). The Portal typically auto-detects the content type based on the file extension (e.g., `image/png` for `.png`). Correct content types are important because browsers use them to determine how to handle the response — an incorrect content type could cause the browser to download the image instead of displaying it.
>
> Blob names are case-sensitive in Azure Storage. `hero.png` and `Hero.jpg` are treated as two different blobs. Establish a naming convention (lowercase is recommended) and use it consistently across your application code and storage.
>
> ⚠ **Common Mistakes**
>
> - Uploading with a different filename than what the application expects — the CloudSoft application expects `hero.png`
> - Very large images (10+ MB) will slow page loading. For a hero section, optimize the image to 500 KB–2 MB for a good balance between quality and performance
> - Forgetting to note the full blob URL — you will need the container base URL (without the filename) for the application configuration
>
> ✓ **Quick check:** The blob URL displayed in the properties panel matches the pattern `https://{accountname}.blob.core.windows.net/images/hero.png`

### **Step 4:** Verify Public Access

Confirm that the uploaded image is publicly accessible by opening the blob URL directly in a browser. This verification step ensures that the container access level is configured correctly before integrating with your application.

1. **Copy** the full blob URL from the previous step (e.g., `https://cloudsoftyourname.blob.core.windows.net/images/hero.png`)

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

## Common Issues

> **If you encounter problems:**
>
> **Image not loading (403 Forbidden):** The container access level is set to "Private". Navigate to the container in the Azure Portal, click "Change access level", and select "Blob (anonymous read access for blobs only)".
>
> **Image not loading (404 Not Found):** The blob name in the URL does not match the uploaded file name. Blob names are case-sensitive — verify that the file was uploaded as `hero.png` (lowercase).
>
> **"Allow Blob anonymous access" is disabled:** Navigate to your Storage Account → Settings → Configuration. Set "Allow Blob anonymous access" to **Enabled** and save. Then update the container access level.
>
> **Still stuck?** Test the blob URL directly in an incognito browser window first. If the image does not load in the browser, the issue is in the Azure Storage configuration.

## Summary

You've successfully provisioned Azure Blob Storage infrastructure for serving public images which:

- ✓ Provides a globally accessible, high-availability storage service for static assets
- ✓ Uses anonymous read access for public image serving without authentication overhead
- ✓ Follows the deterministic URL pattern `https://{account}.blob.core.windows.net/{container}/{blob}`

> **Key takeaway:** Azure Blob Storage provides a scalable, cost-effective solution for serving static assets from the cloud. The deterministic URL pattern means your application can construct image URLs from configuration alone — no SDK or API calls needed at runtime. Your container URL (`https://{accountname}.blob.core.windows.net/images`) is what you will use when configuring your application to serve images from Azure Blob Storage.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Create the same Storage Account and container using the Azure CLI (`az storage account create` and `az storage container create`) for scriptable provisioning
> - Configure a custom domain for your Blob Storage endpoint so images are served from your own domain (e.g., `images.cloudsoft.com`)
> - Enable Azure CDN (Content Delivery Network) in front of Blob Storage to cache images at edge locations worldwide for faster loading
> - Explore SAS tokens (Shared Access Signatures) to provide time-limited access to private blobs without making the container public

## Done! 🎉

Great job! You've provisioned Azure Blob Storage, created a public container, and verified that your hero image is accessible via a public URL. This infrastructure is ready for your application to use — you will configure the application to point to your container URL in the next exercise.
