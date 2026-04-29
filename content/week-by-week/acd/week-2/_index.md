+++
title = "Week 2 (v.16)"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Docker and Docker Compose: containers, images, multi-stage builds, and local development environments"
weight = 2
+++

# Week 2 (v.16) — Docker and Docker Compose

Containerize the reference application. Understand images vs containers, write a multi-stage Dockerfile, and stand up a multi-service local environment with Docker Compose.

## Presentation

- [Introduktion till Containers & Docker](/presentations/mini-lectures/introduction-to-containers.html) — theoretical introduction to containers, images, and why they matter for cloud development

## Theory

- [Part VII — Containers](/course-book/7-containers/)
  - [Containers vs VMs](/course-book/7-containers/1-containers-vs-vms/containers-vs-vms/)
  - [Images and Layers](/course-book/7-containers/2-images-and-layers/images-and-layers/)
  - [Dockerfiles and Multi-Stage Builds](/course-book/7-containers/3-dockerfiles-and-multi-stage-builds/dockerfiles-and-multi-stage-builds/)
  - [Multi-Platform Builds](/course-book/7-containers/4-multi-platform-builds/multi-platform-builds/)
  - [Docker Compose](/course-book/7-containers/5-docker-compose/docker-compose/)
  - [Container Registries](/course-book/7-containers/6-container-registries/container-registries/)

## Practice

- [Docker](/exercises/20-docker/) — five progressive exercises
  - [Run your first container](/exercises/20-docker/1-run-your-first-container/)
  - [Build and push your first image](/exercises/20-docker/2-build-and-push-your-first-image/)
  - [Containerize the reference app](/exercises/20-docker/3-containerize-cloudsoft-recruitment/)
  - [Docker Compose local development stack](/exercises/20-docker/4-docker-compose-local-development-stack/) (MongoDB + Azurite)
  - [Multi-platform builds](/exercises/20-docker/5-multi-platform-builds/)

## Preparation

- Install Docker Desktop
- Read up on the difference between containers and virtual machines

## Reflection Questions

- What is the difference between a Docker image and a container?
- Why use multi-stage builds in a Dockerfile?
- How does Docker Compose connect multiple services in a local network?

## Links

- [Docker Documentation](https://docs.docker.com)
- [Docker Hub](https://hub.docker.com)
