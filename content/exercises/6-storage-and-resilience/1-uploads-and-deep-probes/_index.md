+++
title = "1. File Uploads and Deep Health Probes"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Build an MVC recruitment portal that accepts PDF CV uploads, then evolve it from in-memory + local files to managed-identity Cosmos DB and Azure Blob Storage, and finish by wiring deep liveness and readiness probes into Container Apps."
weight = 1
+++

# File Uploads and Deep Health Probes

The goal of this chapter is one MVC application — `CloudCiCareers.Web`, an anonymous recruitment portal where applicants upload PDF CVs against hard-coded job postings and recruiters list, edit, and delete the resulting applications — taken through three deliberate transitions. The same `Application` domain (id, job id, applicant name and email, CV blob name, submitted-at timestamp, status, notes) survives all three exercises; what changes is *the persistence backend* — first an in-memory store and a local-file blob, then real **Azure CosmosDB** and a real **Azure Storage Account** — and *the operational surface*, which starts with no probes at all and ends with deep liveness and readiness checks wired into Container Apps.

The arc moves from a working-but-ephemeral upload form, to a real cloud-backed service authenticated by managed identity, to a service whose health Container Apps can actually reason about.

The first exercise scaffolds a fresh ASP.NET Core **MVC scaffold**, builds the Apply form with **PDF magic-bytes validation** (peeking at the first bytes of the upload rather than trusting the filename), wires **antiforgery on multipart uploads**, writes through an `IBlobService` that persists to a local folder, keeps applications in an in-memory store, and ships the whole thing to Azure Container Apps. The OIDC-federated pipeline from the previous deployment chapter is reused as-is, and Application Insights is wired in via the `secretref:` pattern from the logging and monitoring chapter — the chapter opens with an abbreviated infrastructure step that points back to those chapters so you can spend the time on uploads rather than redoing pipeline setup. The second exercise is the contrast moment: the in-memory store is replaced with a `CosmosApplicationStore` against **Azure CosmosDB (serverless)** and the local-file blob with an `AzureBlobService` against an **Azure Storage Account**, but neither uses connection strings — both authenticate via the Container App's **system-assigned managed identity**. A Concept Deep Dive covers Cosmos DB's **data-plane RBAC**, which lives separately from regular Azure RBAC and uses its own built-in role definition. The third exercise gives the service a voice the platform can hear: three endpoints with distinct semantics — **`/health/live` vs `/health/ready`** plus a diagnostic `/health` JSON endpoint — backed by custom `CosmosHealthCheck` and `BlobHealthCheck` implementations and wired up as **Container Apps probes** so the platform restarts unhealthy replicas and stops routing traffic to ones that aren't ready. The chapter closes by tearing down the resource group and the Entra OIDC app, mirroring the cleanup pattern at the end of the previous REST API chapter.

> ℹ **Where this fits**
>
> This subsection sits inside the broader **Storage and Resilience** section. It introduces the two pillars every long-running cloud service needs: durable, identity-authenticated persistence for the data the service owns, and the operational signals — liveness and readiness — that let the platform run the service unattended without papering over real failures. Future chapters in this section will likely tackle queues, async messaging, and scaling concerns; this one stays focused on what it takes to make a single service safe to leave running. The previous **REST API and DTOs** chapter is the immediately preceding context — you finished it with a working OIDC pipeline shipping containers to Azure and an Application Insights wiring you understand, and this chapter builds on that infrastructure rather than re-teaching it.

{{< children />}}
