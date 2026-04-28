+++
title = "Part VII — Containers"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Containers as a deployment unit: containers vs VMs, images and layers, Dockerfiles and multi-stage builds, multi-platform builds, Compose, and registries."
weight = 70
chapter = true
head = "<label>Part VII</label>"
+++

# Part VII — Containers

Containers replaced the virtual machine as the standard deployment unit for stateless application code. This Part covers the model from the bottom up: how containers differ from VMs, what an image actually is, how a Dockerfile constructs one, how a single image targets multiple CPU architectures, how Compose runs several containers as one local environment, and how registries distribute images to deployment targets.

The companion exercise is the [Docker chapter](/exercises/20-docker/) — five exercises that build a multi-stage .NET image, push it to Docker Hub, run it in Compose alongside a database, and rebuild it for both amd64 and arm64.

{{< children />}}
