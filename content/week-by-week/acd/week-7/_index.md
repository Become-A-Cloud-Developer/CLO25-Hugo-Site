+++
title = "Week 7 (v.21)"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Blob Storage and health checks: file uploads, PDF validation, IBlobService, deep health probes, Google OAuth"
weight = 7
+++

# Week 7 (v.21) — Blob Storage and Health Checks

Accept file uploads, validate them by content (not filename), store them in Azure Blob Storage via a managed identity, and add deep health probes that report dependency status to Azure Container Apps.

## Theory

- [Part IV — Data Access: Object Storage](/course-book/4-data-access/4-object-storage/object-storage/) — blob storage, containers, and access patterns
- [Part IX — Operations and Observability: Health Checks](/course-book/9-operations-and-observability/5-health-checks/health-checks/) — liveness, readiness, and deep probes
- [Part V — Identity and Security: OAuth and OIDC](/course-book/5-identity-and-security/7-oauth-and-oidc/oauth-and-oidc/) — Google as an external identity provider

## Practice

- [Storage and Resilience — Uploads and Deep Probes](/exercises/6-storage-and-resilience/1-uploads-and-deep-probes/) — three progressive exercises
  - [MVC uploads and PDF validation](/exercises/6-storage-and-resilience/1-uploads-and-deep-probes/1-mvc-uploads-and-pdf-validation/)
  - [Cosmos and Blob via managed identity](/exercises/6-storage-and-resilience/1-uploads-and-deep-probes/2-cosmos-and-blob-via-managed-identity/)
  - [Deep health probes and cleanup](/exercises/6-storage-and-resilience/1-uploads-and-deep-probes/3-deep-health-probes-and-cleanup/)

## Preparation

- Read up on Azure Blob Storage
- Familiarize yourself with the health check pattern in ASP.NET Core

## Reflection Questions

- Why validate file content (magic bytes) and not just the file extension?
- How does the feature flag that toggles between local and cloud storage work?
- What is the purpose of health checks in a production environment?
- What is the difference between a liveness probe and a readiness probe?

## Links

- [Azure Blob Storage](https://learn.microsoft.com/azure/storage/blobs)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
