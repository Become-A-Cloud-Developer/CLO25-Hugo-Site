+++
title = "Images and Layers"
program = "CLO"
cohort = "25"
courses = ["ACD"]
type = "slide"
date = 2026-04-28
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls = true
progress = true
history = true
center = true
+++

## Images and Layers
Part VII — Containers

---

## Why images exist
- A container is an **isolated process** — but a process needs files
- The **image** is the immutable filesystem template the container starts from
- Bundles both the **bits** (binaries, libraries, config) and the **metadata** (entrypoint, env, ports)
- Two containers from one image see **identical filesystems** at start

---

## What an image carries
- A **read-only filesystem snapshot** of the application and its dependencies
- The default **ENTRYPOINT** and **CMD**
- **Environment variables**, working directory, exposed ports, runtime user
- An immutable **digest** that fingerprints every byte and every config field

---

## The layer model
- Each Dockerfile instruction (`FROM`, `COPY`, `RUN`) creates one **layer**
- Layers are **read-only filesystem snapshots**, stacked via a union filesystem
- The container adds a thin **writable layer** on top — discarded on stop
- Deletes do not remove bytes; they just **mask** them in a higher layer

---

## Content addressing
- Each layer is named by a **SHA256 hash** of its contents
- Identical content produces identical hashes — across hosts, across builds
- Registries store each layer **once**, even when shared by 100 images
- The build cache reuses a layer when its **inputs are unchanged**

---

## Layer ordering matters
- A cache miss **cascades**: every layer below must re-execute
- Order instructions from **least to most volatile**
- For .NET: copy `*.csproj` and `restore` **before** copying source code
- The reward is builds that finish in **seconds**, not minutes

---

## The base image
- The first `FROM` instruction — provides OS, runtime, package manager
- `mcr.microsoft.com/dotnet/aspnet:10.0` — Debian, ~220 MB, full runtime
- `...:10.0-alpine` — Alpine + musl libc, ~110 MB
- Smaller bases: faster pulls, smaller attack surface, occasional **glibc/musl friction**

---

## The image manifest
- A small **JSON document** listing layers, digests, sizes, platform
- A pull fetches the manifest **first**, then only the missing layers
- The manifest's own SHA256 is the **immutable digest** of the image
- A **manifest list** maps one tag to multiple platform-specific manifests

---

## Inspecting an image
- `docker pull mcr.microsoft.com/dotnet/aspnet:10.0`
- `docker history <image>` walks the layer stack bottom-up
- Reveals **base rootfs**, runtime install, .NET copy, metadata layers
- Tells you exactly **where the bytes live** and what to optimize

---

## Questions?
