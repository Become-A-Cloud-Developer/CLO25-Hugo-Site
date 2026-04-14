# Week 3: Blob Storage and ACA Revisions

## Purpose

Add file upload capability with Azure Blob Storage and learn ACA revision management with traffic splitting. Students implement secure PDF upload with validation, and practice blue-green deployment patterns.

## What Students Build

1. **BlobService** — Single implementation using Azure.Storage.Blobs SDK (works against both Azurite and Azure Blob Storage)
2. **CV upload** — PDF upload on job application with comprehensive validation
3. **CV download** — Admin-only endpoint streaming blobs through the app (no direct public URLs)
4. **ACA revisions** — Deploy new revisions with traffic splitting (e.g., 80/20)

## CV Upload Security

The implementation follows defense-in-depth for file uploads:

1. **Extension check**: Only `.pdf` allowed
2. **Content-Type check**: Must be `application/pdf`
3. **Size limit**: Maximum 5 MB
4. **Magic bytes validation**: First 5 bytes must be `%PDF-`
5. **Server-side filename**: `{Guid}.pdf` — never use user-provided filenames
6. **Private container**: Blob container uses `PublicAccessType.None`

## Azurite Configuration

### Local (dotnet run)

```json
{
  "BlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "cvs"
  }
}
```

### Docker (container-to-container)

`UseDevelopmentStorage=true` does **not** work between containers. Use the explicit connection string:

```
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;
```

## ACA Revisions and Traffic Splitting

### Deploy a New Revision

```bash
cd infra
./deploy-revision.sh
```

This script:
1. Builds and pushes a new Docker image with a unique tag
2. Creates a new ACA revision
3. Configures traffic splitting between revisions

### Traffic Splitting Example

```bash
# 80% to current, 20% to new revision
az containerapp ingress traffic set \
  --name cloudsoft-dev-app \
  --resource-group rg-cloudsoft-dev \
  --revision-weight latest=20 <current-revision>=80
```

## Data Protection Keys

Data Protection keys are persisted to:
- **Local/Docker**: File system volume (`/app/data/keys`)
- **Production**: Azure Blob Storage (configured via `BlobStorage` connection string)

This ensures authentication cookies survive container restarts.

## End State

After Week 3, students have:

- PDF upload with validation and Azure Blob Storage
- Admin-only CV download endpoint
- Understanding of ACA revision management
- Complete monolith ready for Week 4 microservice split

## Key Learning

- **Azure Blob Storage**: BlobContainerClient, upload/download, access policies
- **File upload security**: Magic bytes, server-side filenames, size limits
- **ACA revisions**: Blue-green deployments, traffic splitting, rollback
- **Data Protection**: Key persistence across container restarts
- **Azurite**: Local Azure Storage emulation, container-to-container networking
