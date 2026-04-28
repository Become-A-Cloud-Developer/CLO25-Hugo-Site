+++
title = "Images and Layers"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 20
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/7-containers/2-images-and-layers.html)

[Se presentationen på svenska](/presentations/course-book/7-containers/2-images-and-layers-swe.html)

---

A [container](/course-book/7-containers/1-containers-vs-vms/) is an isolated process, but a process needs files to execute: a binary, shared libraries, configuration, and a directory layout the application expects to find. Containers do not conjure that filesystem on demand. They start from a pre-built, immutable template that defines the entire environment down to the last shared object. That template is the image, and understanding how images are constructed, distributed, and cached is the difference between a container build that completes in seconds and one that drags through every dependency on every push.

## What an image is

A **Docker image** is an immutable, layered template that defines an application and its runtime environment; it specifies a base OS, dependencies, files, and a default command, and is stored in a registry for distribution; containers are running instances of images. The image itself never executes — it is a passive artifact, a snapshot of a filesystem plus a small bundle of metadata. The container runtime takes that snapshot, wraps it in process and network isolation, and starts the configured entrypoint. Two containers launched from the same image see identical filesystems at the moment of start.

An image is more than the files it carries. The metadata records the default command (`ENTRYPOINT` and `CMD`), the working directory, environment variables, exposed ports, and the user the process runs as. Pull an `nginx` image and the metadata says: run `/docker-entrypoint.sh nginx -g 'daemon off;'`, exposing port 80, as the `root` user. Pull `mcr.microsoft.com/dotnet/aspnet:10.0` and the metadata configures `DOTNET_RUNNING_IN_CONTAINER=true`, sets the working directory, and prepares the runtime to execute a published .NET assembly. The image bundles both the bits and the instructions for how to run them.

Images are immutable. Once built, the bytes do not change, and a fingerprint (the manifest digest) covers every file, every metadata field, and every layer. Modifying anything — a dependency, a configuration line, a single byte — produces a new image with a new digest. The previous image still exists, unchanged, addressable by its old digest. This immutability is what makes container deployments reproducible: a digest pinned in production today refers to the same bits that ran in staging last week.

## The layer model

An image is not a single tarball of files. It is a stack of **layers**, where a layer is a read-only filesystem snapshot created by one or more Dockerfile instructions; layers are stacked (like a union filesystem) to form the complete image filesystem, and Docker caches unchanged layers during rebuilds to avoid redundant work. Each instruction in a Dockerfile — `FROM`, `COPY`, `RUN`, `ADD` — produces one layer. Metadata-only instructions like `ENV`, `EXPOSE`, and `WORKDIR` mutate the image configuration without adding filesystem content, but they still participate in the cache key.

When a container starts, the runtime stacks the image's layers in order using a union filesystem (`overlay2` on most Linux hosts). The lowest layer is the base; each subsequent layer overlays the one below, with later files masking earlier ones at the same path. The container then receives a thin, writable layer on top — a scratch space that absorbs any modifications the running process makes. When the container stops, that writable layer can be discarded; the underlying image layers are untouched.

This stacking has a subtle consequence: deletion does not actually remove bytes. A `RUN rm -rf /var/cache/apt/*` produces a new layer that masks the cache directory, but the bytes still sit in the layer below, traveling with the image to every host that pulls it. This is why image-size optimization usually means avoiding the bytes in the first place — running the install and the cleanup as a single `RUN` command so both happen in the same layer — rather than removing them after the fact.

### Content addressing

Each layer is identified by a SHA256 hash of its contents. Two builds that produce byte-identical layer content produce identical hashes, regardless of who built them or when. This is content addressing, and it is the mechanism that makes layer caching and sharing possible. A registry storing one hundred images that all use `mcr.microsoft.com/dotnet/aspnet:10.0` as their base stores the runtime layer exactly once. A host pulling its hundredth ASP.NET image downloads only the application-specific layers; the runtime is already on disk from the first pull.

The same logic powers the build cache. **Image cache** is Docker's optimization that reuses layers from previous builds; when a Dockerfile instruction has not changed and its context is identical, Docker skips re-execution and uses the cached layer, reducing build time significantly. The cache key for a layer combines the parent layer's digest, the instruction text, and (for `COPY` and `ADD`) the hashes of the files being copied. If any input changes, the cache misses and Docker re-executes the instruction. If nothing changed, the existing layer is reused untouched.

## Why layer ordering matters

Cache hits cascade top-down: as soon as one instruction misses the cache, every instruction below it must also re-execute, even if its inputs are identical. The cached `dotnet publish` from yesterday is useless if the layer feeding into it is new. This is why Dockerfile authors order instructions from least to most volatile — stable, slow-to-rebuild layers first, fast-changing application code last.

Consider an ASP.NET project. The .NET SDK base image and the NuGet dependencies change weekly at most. The application source code changes on every commit. A naive Dockerfile that copies the entire source tree before restoring packages forces a `dotnet restore` on every build, even when only a controller method changed. A well-ordered Dockerfile copies the `.csproj` files first, runs `dotnet restore` in its own layer, then copies the rest of the source. The restore layer caches across hundreds of source-only commits; a fresh build with no dependency changes finishes in seconds rather than minutes.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app
```

The discipline is to ask, for each instruction: how often does its input change? Put the rare changes near the top of the Dockerfile; put the constant changes near the bottom. This is not a stylistic preference — it is the difference between a CI pipeline that finishes in 30 seconds and one that takes 5 minutes on every push.

## The base image

Every image chain starts with a **base image**, the initial image specified in a Dockerfile's `FROM` instruction; it provides the operating system, runtime, and package manager for the rest of the image (e.g., `FROM mcr.microsoft.com/dotnet/aspnet:latest` provides the .NET runtime). The base image determines the kernel-compatibility expectations, the available shell, the libc flavor (glibc vs. musl), the package manager, and the security posture of everything built on top.

The choice of base image is the single most consequential decision in a Dockerfile. An ASP.NET application can be built `FROM mcr.microsoft.com/dotnet/aspnet:10.0` (Debian-based, ~220 MB, includes a full .NET runtime and the dependencies the runtime expects), or `FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine` (Alpine-based, ~110 MB, uses musl libc), or `FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine` paired with a self-contained publish (smallest possible, but the application carries its own runtime).

Smaller images pull faster, attack surfaces are smaller, and start-up is quicker. The trade-off is operational: Alpine's musl libc occasionally surfaces compatibility issues with native dependencies compiled against glibc; some performance counters and globalization features in .NET behave differently under Alpine. Distroless images go even further — no shell, no package manager — at the cost of debugging convenience. The rule of thumb is to pick the lightest base that does not introduce more friction than it saves, and to pin a specific tag (`10.0` rather than `latest`) so the base does not silently shift under the build.

## The image manifest

When an image lands in a registry, its identity is described by a manifest. An **image manifest** is the JSON metadata that describes an image: it lists layers, their digests and sizes, the image configuration, and platform architecture; a **manifest list** extends this to map multiple platform-specific manifests to a single tag, enabling multi-platform support. The manifest is small — a few kilobytes of JSON — but it is the linchpin of the distribution model.

A pull operation begins by fetching the manifest, not the image. The client reads the layer digests, checks which ones are already on disk, and downloads only the missing layers. The manifest itself has a digest, the SHA256 of its JSON bytes, and that digest is what `docker pull mcr.microsoft.com/dotnet/aspnet@sha256:abc123...` resolves to. Tags like `10.0` are mutable pointers into the registry's manifest store; digests are immutable.

This separation matters in production. A deployment pipeline that pulls `aspnet:10.0` today and `aspnet:10.0` tomorrow may receive two different images if Microsoft pushed a patch release in between. A pipeline that resolves the tag once, captures the digest, and deploys by digest receives the same bits every time, regardless of subsequent tag movements.

## Storage and pull efficiency

Layer sharing is not just a build optimization; it is the mechanism that keeps registries and hosts from drowning in duplicated bytes. A team that builds twenty microservices, all from the same `mcr.microsoft.com/dotnet/aspnet:10.0` base, stores the runtime layer once on the registry and once on each host. The only per-service cost is the application-specific layers — typically tens of megabytes — even though each service's image, taken in isolation, would appear to be 250 MB.

The same effect speeds up incremental deployments. A new build that only touches the application binary produces a new top layer; every layer below — base image, dependency restore, framework configuration — is unchanged and already present on the target host. The host pulls the manifest, sees that it is missing only the new top layer, downloads a few megabytes, and starts the new container. Hosts that have never seen the image at all pay the full cost the first time and the marginal cost every time after.

## Worked example: pulling and inspecting an image

The companion exercise [Run Your First Container](/exercises/20-docker/) starts with a single command:

```bash
docker pull mcr.microsoft.com/dotnet/aspnet:10.0
```

The output streams a list of layer pulls, one per line, each identified by a 12-character hash prefix. Some layers download in parallel; others marked `Already exists` are skipped because they were pulled by an earlier image. Once the manifest reconciliation completes, the local Docker daemon has the full image and prints the resolved digest.

To see the structure of the pulled image, `docker history` walks the layer stack:

```bash
docker history mcr.microsoft.com/dotnet/aspnet:10.0
```

```text
IMAGE          CREATED        CREATED BY                                      SIZE
sha256:abc...  3 days ago     ENTRYPOINT ["dotnet"]                           0B
<missing>      3 days ago     ENV ASPNETCORE_HTTP_PORTS=8080                  0B
<missing>      3 days ago     COPY /dotnet /usr/share/dotnet                  175MB
<missing>      3 days ago     RUN apt-get update && apt-get install -y ...    32MB
<missing>      3 weeks ago    /bin/sh -c #(nop)  CMD ["bash"]                 0B
<missing>      3 weeks ago    /bin/sh -c #(nop) ADD file:... in /             80MB
```

Reading from the bottom: the `ADD` of the Debian rootfs is the base layer, contributing 80 MB. The next `RUN` installs runtime dependencies (libicu, libssl) for another 32 MB. The `COPY` lays down the .NET runtime itself at 175 MB. Above that, several metadata-only entries (`ENV`, `ENTRYPOINT`) carry zero bytes but still appear in the history — they are configuration changes baked into the manifest. The `<missing>` IMAGE column is expected; intermediate layer IDs are not stored locally for pulled images, only the final digest. The bottom-up reading reveals exactly what the image is made of and where the size comes from, which is precisely the information needed to decide whether a smaller base image or a multi-stage build would shrink the artifact.

## Summary

A Docker image is the immutable, layered filesystem template a container starts from, packaging both the bits the application needs and the metadata that describes how to run them. The image is built as a stack of read-only layers, one per Dockerfile instruction, each identified by a content hash that lets Docker cache unchanged layers and registries share them across images. The base image fixes the OS, libc flavor, and runtime that everything else builds on, making it the single most consequential choice in a Dockerfile. Layer ordering — stable inputs first, volatile inputs last — determines whether the build cache delivers seconds or minutes. The image manifest is the JSON record that names the layers and their digests; pulls fetch only the layers a host is missing, which is what makes container distribution efficient enough to be practical at scale.
