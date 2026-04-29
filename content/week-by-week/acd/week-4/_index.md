+++
title = "Week 4 (v.18)"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "CI/CD and deployment to Azure: GitHub Actions, ACR, Azure Container Apps, OIDC"
weight = 4
+++

# Week 4 (v.18) — CI/CD and Deployment to Azure

Build a GitHub Actions pipeline that builds, tests, packages a Docker image, pushes it to a registry, and deploys to Azure Container Apps. Replace long-lived publish credentials with passwordless OIDC.

## Theory

- [Part VIII — DevOps and Delivery](/course-book/8-devops-and-delivery/)
  - [The DevOps Philosophy](/course-book/8-devops-and-delivery/1-the-devops-philosophy/the-devops-philosophy/)
  - [CI vs CD](/course-book/8-devops-and-delivery/2-ci-vs-cd/ci-vs-cd/)
  - [Pipelines as Code](/course-book/8-devops-and-delivery/3-pipelines-as-code/pipelines-as-code/)
  - [Build, Test, and Smoke Gates](/course-book/8-devops-and-delivery/4-build-test-and-smoke-gates/build-test-and-smoke-gates/)
  - [Deployment Strategies](/course-book/8-devops-and-delivery/5-deployment-strategies/deployment-strategies/)
  - [Pipeline Secrets and OIDC](/course-book/8-devops-and-delivery/6-pipeline-secrets-and-oidc/pipeline-secrets-and-oidc/)
  - [Azure Container Apps](/course-book/8-devops-and-delivery/7-azure-container-apps/azure-container-apps/)
- [Part VII — Containers: Container Registries](/course-book/7-containers/6-container-registries/container-registries/)

## Practice

- [Deployment — CI/CD to Container Apps](/exercises/3-deployment/9-cicd-to-container-apps/) — three progressive exercises
  - [First pipeline: Docker Hub](/exercises/3-deployment/9-cicd-to-container-apps/1-first-pipeline-docker-hub/)
  - [Private registry (ACR) and deploy](/exercises/3-deployment/9-cicd-to-container-apps/2-private-registry-and-deploy/)
  - [Passwordless deployment with OIDC](/exercises/3-deployment/9-cicd-to-container-apps/3-passwordless-deployment-oidc/)

## Preparation

- Read up on GitHub Actions and YAML workflow syntax
- Make sure you have access to the Azure portal

## Reflection Questions

- What steps belong in a typical CI/CD pipeline?
- What is the difference between Docker Hub and Azure Container Registry?
- How can you automatically verify that a deployment succeeded?
- Why is OIDC preferable to a long-lived service principal secret?

## Links

- [GitHub Actions](https://docs.github.com/actions)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps)
- [Azure Portal](https://portal.azure.com)
