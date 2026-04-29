+++
title = "Week 6 (v.10)"
program = "CLO"
cohort = "25"
courses = ["BCD"]
description = "Cloud storage: object storage, databases, and how to choose between them"
weight = 6
+++

# Week 6 (v.10) — Storage

Provision cloud databases and object storage. Connect from code, then choose the right tool for the job: blob storage vs database, SQL vs NoSQL.

## Theory

- [Part II — Infrastructure: Storage](/course-book/2-infrastructure/storage/)
  - [What Is Persistence](/course-book/2-infrastructure/storage/1-what-is-persistence/what-is-persistence/)
  - [Databases](/course-book/2-infrastructure/storage/2-databases/databases/)
  - [Storage](/course-book/2-infrastructure/storage/3-storage/storage/) — block, file, and object storage
- [Part IV — Data Access](/course-book/4-data-access/)
  - [Object Storage](/course-book/4-data-access/4-object-storage/object-storage/)

## Practice

- [Cloud Databases — Portal Interface](/exercises/5-cloud-databases/1-portal-interface/) — provision Cosmos DB via the Azure Portal
- [Cloud Databases — Command Line](/exercises/5-cloud-databases/2-command-line-interface/) — provision via `az` CLI
- [Cloud Databases — ARM and Bicep](/exercises/5-cloud-databases/3-arm-and-bicep/) — provision declaratively
- [Cloud Databases — Connecting from Code](/exercises/5-cloud-databases/4-connecting-from-code/) — connection strings, MongoDB driver, Cosmos DB SDK
- [Cloud Databases — Azure Blob Storage](/exercises/5-cloud-databases/5-azure-blob-storage/) — store and serve files from blob storage

## Preparation

- Read up on storage and databases in the cloud

## Reflection Questions

- How do different storage options compare?
- What are the advantages of blob storage?
- When do you use a database vs blob storage?

## Links

- [Azure Cosmos DB](https://learn.microsoft.com/azure/cosmos-db/)
- [Azure Blob Storage](https://learn.microsoft.com/azure/storage/blobs/)
