+++
title = "9. CI/CD to Azure Container Apps"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Build a GitHub Actions pipeline that ships a Dockerized .NET app to Azure Container Apps. Three exercises that progress from public registry + manual deploy, to private registry + automated deploy, to passwordless deployment with OIDC federation."
weight = 9
+++

# CI/CD to Azure Container Apps

The goal of this chapter is a working CI/CD pipeline that builds a containerized .NET MVC app and deploys it to Azure Container Apps. The same simple app travels through all three exercises — each one keeps the previous pipeline working and layers exactly one new concept on top.

The arc moves from "almost CI/CD" to the real thing:

- **First**, GitHub Actions builds the image and pushes it to **Docker Hub** as a public image. The Container App pulls the new tag, but deployment is still manual — you click *Create new revision* in the Portal. The gap between "image is built" and "users see it" is something you should feel before we close it.
- **Second**, Docker Hub is replaced with **Azure Container Registry**. The pipeline now logs in to Azure with a service principal and runs `az containerapp update` itself. A smoke test gates the workflow so a broken build cannot reach production. This is real CI/CD.
- **Third**, the long-lived service principal secret is replaced with **OIDC federation**. The pipeline still works, but no password exists anywhere in GitHub.

> ℹ **Where this fits**
>
> This subsection sits inside the broader **Deployment** chapter. An earlier exercise in the chapter covers the older VM-based approach to GitHub Actions deployment (SCP via a self-hosted runner). The exercises here are the modern container-native equivalent — same idea, different deployment target. Together they show the two main ways teams ship code to Azure today.

{{< children />}}
