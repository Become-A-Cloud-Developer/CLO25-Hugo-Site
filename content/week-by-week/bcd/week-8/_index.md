+++
title = "Week 8 (v.12)"
program = "CLO"
cohort = "25"
courses = ["BCD"]
description = "Secret management: Azure Key Vault, managed identities, and pipeline secrets"
weight = 8
+++

# Week 8 (v.12) — Secret Management

Stop committing secrets. Use Azure Key Vault for application secrets, managed identities for passwordless service-to-service auth, and OIDC for pipelines.

## Theory

- [Part V — Identity and Security](/course-book/5-identity-and-security/)
  - [Secret Management](/course-book/5-identity-and-security/8-secret-management/secret-management/) — Key Vault, managed identities, RBAC
  - [API Keys](/course-book/5-identity-and-security/6-api-keys/api-keys/)
- [Part VIII — DevOps and Delivery](/course-book/8-devops-and-delivery/)
  - [Pipeline Secrets and OIDC](/course-book/8-devops-and-delivery/6-pipeline-secrets-and-oidc/pipeline-secrets-and-oidc/) — short-lived federated credentials in CI/CD

## Practice

- [Deployment — Implementing Azure Key Vault with MongoDB on Azure VM](/exercises/3-deployment/4-implementing-azure-key-vault-with-mongodb-on-azure-vm/) — store and retrieve a connection string via Key Vault and a managed identity
- [Deployment — Passwordless Deployment with OIDC](/exercises/3-deployment/9-cicd-to-container-apps/3-passwordless-deployment-oidc/) — replace long-lived publish credentials with OIDC

## Preparation

- Read up on Azure Key Vault and Managed Identities

## Reflection Questions

- How do you manage secrets securely in cloud environments?
- What is the difference between secrets and identities?
- How does Azure Key Vault work?
- Why prefer a managed identity over a connection string in app settings?

## Links

- [Azure Key Vault](https://learn.microsoft.com/azure/key-vault/)
- [Managed identities for Azure resources](https://learn.microsoft.com/entra/identity/managed-identities-azure-resources/)
