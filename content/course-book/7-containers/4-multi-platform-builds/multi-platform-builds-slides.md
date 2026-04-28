+++
title = "Multi-Platform Builds"
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

## Multi-Platform Builds
Part VII — Containers

---

## The mismatch problem
- Developer on **Apple Silicon** (arm64) builds an image
- Pushes to registry, deploys to **Azure VM** (amd64)
- Runtime fails with `exec format error`
- Single-platform images assume build host = run host

---

## What a platform is
- **Platform** = `os/architecture/variant` (e.g. `linux/amd64`)
- CPUs decode only their own instruction set
- Container images bundle compiled binaries
- `uname -m` reveals the host's architecture

---

## Architecture matters silently
- `docker build` defaults to host architecture
- No warning that the image is non-portable
- Mismatch surfaces only at runtime, on a different host
- Errors: `exec format error`, missing executable

---

## Buildx as the multi-platform builder
- **Buildx** = Docker CLI extension using BuildKit
- `docker buildx build --platform linux/amd64,linux/arm64`
- Builds the Dockerfile once per target platform
- Requires `--push` — local store cannot hold multi-arch

---

## QEMU enables cross-architecture builds
- **QEMU** emulates foreign instruction sets in user space
- Kernel routes foreign binaries to QEMU via `binfmt_misc`
- Builds AMD64 images on an ARM64 host transparently
- Trade-off: emulated builds are 5–10× slower

---

## Manifest list as the indirection
- **Manifest list** = registry metadata mapping one tag to many digests
- One tag → list → per-architecture image manifest
- Client pulls only the layers matching its host
- Same `docker pull` command, different bytes per host

---

## How the registry stores it
- Layers are content-addressed by SHA256
- Identical layers shared across architectures (rare for binaries)
- OCI standard — supported by Docker Hub, ACR, GHCR
- `docker buildx imagetools inspect` shows the structure

---

## Worked example
- `docker buildx create --name multiarch --use`
- `docker buildx build --platform linux/amd64,linux/arm64 -t myimage:1.0 --push .`
- ARM64 builds natively; AMD64 emulated under QEMU
- Result: one tag, two architectures, transparent pulls

---

## When to reach for it
- Mixed-architecture teams (Apple Silicon + amd64 cloud)
- ARM cloud targets (Graviton, Ampere) for cost
- Edge / IoT deployment alongside server deployment
- Otherwise: single-platform stays simpler and faster

---

## Questions?
