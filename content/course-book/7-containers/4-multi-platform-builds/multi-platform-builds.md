+++
title = "Multi-Platform Builds"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 40
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/7-containers/4-multi-platform-builds.html)

[Se presentationen på svenska](/presentations/course-book/7-containers/4-multi-platform-builds-swe.html)

---

A developer on an Apple Silicon laptop builds a container image, pushes it to a registry, and deploys it to an Azure VM running on Intel hardware. The deployment fails with `exec format error`. The image was compiled for ARM64; the VM runs AMD64. Single-platform images quietly assume the build host and the runtime host share an architecture, and that assumption breaks the moment a team mixes Apple Silicon with x86 cloud VMs, or a CI runner with a developer workstation. The sections below cover how container images encode CPU architecture, why a single image cannot serve both architectures, and how `docker buildx` produces a single tag that resolves to the correct binary for whichever host pulls it.

## What a platform means for an image

A [Docker image](/course-book/7-containers/2-images-and-layers/) is not a portable bundle of source code. It contains compiled binaries — system libraries, the application's runtime, and the application itself — that target a specific instruction set. A **platform** in container terms specifies the CPU architecture (amd64, arm64, arm/v7) for which an image is compiled; images compiled for one platform cannot run natively on a different architecture without emulation.

Two facts make platform a first-class concern:

- The CPU executes only the instruction set it was designed for. An ARM CPU cannot decode AMD64 opcodes, and an AMD64 CPU cannot decode ARM opcodes. There is no runtime translation layer in the kernel that papers over the difference.
- Container images bundle every binary the application depends on. Even if the source code is portable, the binaries inside the image are not. An nginx binary compiled for ARM64 is a different file from the nginx binary compiled for AMD64, with different machine code.

A platform identifier in Docker's notation has three components: `os/architecture/variant`. For server containers, the OS is almost always `linux`. The architecture is the load-bearing field — `amd64` (also called `x86_64`) and `arm64` (also called `aarch64`) cover the overwhelming majority of cases. The optional variant distinguishes sub-versions of an architecture, most commonly for 32-bit ARM (`arm/v6`, `arm/v7`).

### Why architecture mismatches surface late

A developer rarely sees the architecture of their host explicitly. The shell command `uname -m` reveals it: `x86_64` on most Intel and AMD machines, `arm64` on Apple Silicon Macs and Raspberry Pi 4. A `docker build` on either host produces an image targeted at the host's architecture by default, with no warning that the result is non-portable.

The mismatch surfaces only at runtime, on a machine with a different architecture, when the kernel rejects the binary with `exec format error` or `standard_init_linux.go: ... no such file or directory`. Both error messages mean the same thing: the kernel found the image's executable but cannot decode its instructions.

## Buildx and the multi-architecture pipeline

The standard `docker build` command produces one image for one platform — the host's. **Buildx** is a Docker CLI extension that enables building images for multiple platforms and architectures; it uses BuildKit and QEMU emulation to cross-compile and push multi-platform manifests to registries.

Buildx ships with recent Docker Desktop installations and adds a parallel `docker buildx build` command. The relevant differences from plain `docker build`:

- `--platform linux/amd64,linux/arm64` accepts a comma-separated list of target platforms. Buildx executes the [Dockerfile](/course-book/7-containers/3-dockerfiles-and-multi-stage-builds/) once per platform and produces one image per entry.
- `--push` uploads results directly to a registry. Multi-platform builds cannot terminate in the local image store, because that store has no way to represent multiple architectures under a single tag — only a registry can.
- The build runs inside a dedicated builder instance (created with `docker buildx create`), separate from the default Docker engine, and can use BuildKit features that the legacy builder cannot.

### How QEMU enables cross-architecture builds

A laptop with an ARM64 CPU has no native way to execute AMD64 instructions. **QEMU** is a user-space emulator that can decode and execute foreign instruction sets, translating each AMD64 opcode to an equivalent sequence on the host CPU. Docker Desktop registers QEMU with the Linux kernel through the `binfmt_misc` mechanism, so the kernel transparently routes foreign binaries to QEMU when they are invoked.

The practical effect: a `RUN apt-get install nginx` instruction in a Dockerfile being built for `linux/amd64` on an ARM64 host runs the AMD64 `apt-get` binary under QEMU, which in turn invokes AMD64 versions of every helper it needs. The result is a fully AMD64 image filesystem, produced entirely on an ARM64 host.

The trade-off is speed. QEMU adds a translation cost to every instruction the foreign binary executes. A build that takes 30 seconds natively can take 5 to 10 minutes under emulation, and CPU-heavy steps (compiling C extensions, running test suites) suffer the most. For occasional builds this is acceptable. For frequent builds — every commit on a CI pipeline, for example — teams typically use **native builders**: a CI matrix with one runner per architecture, each building its native image, with a final step that combines the results into a multi-platform tag. GitHub Actions and most other CI systems support this pattern, and it is the standard approach for production pipelines.

## The manifest list as the indirection layer

If a single tag in a registry can refer to multiple architecture-specific images, the registry needs a way to record that mapping. The mechanism is the **manifest list** — registry metadata that maps a single image tag to multiple platform-specific image digests; when a user pulls an image, the Docker client consults the manifest list and downloads the correct image for their architecture.

A normal image in a registry has an [image manifest](/course-book/7-containers/2-images-and-layers/) — a JSON document listing the layers, their digests, and a configuration object describing the image. A multi-platform image has one manifest per architecture, plus an additional document — the manifest list — that points to each per-architecture manifest by digest and labels it with its platform.

Conceptually, the structure looks like this:

```text
Tag: myimage:1.0
  -> Manifest list (digest sha256:aaaa...)
       - linux/amd64 -> Image manifest sha256:bbbb...
            - layers, config (AMD64 binaries)
       - linux/arm64 -> Image manifest sha256:cccc...
            - layers, config (ARM64 binaries)
```

When `docker pull myimage:1.0` runs on an ARM64 host, the Docker client first fetches the manifest list, scans it for an entry matching its own platform (`linux/arm64`), and then pulls the layers referenced by that entry's manifest. The AMD64 manifest and its layers are never downloaded on an ARM64 host. The same `myimage:1.0` command on an AMD64 host pulls the AMD64 layers instead. The tag is identical; the bytes that arrive on disk differ.

### How the registry stores the bytes

Registries store layers content-addressably: each layer is a blob identified by the SHA256 of its contents. If the AMD64 and ARM64 images share an identical layer — for example, a layer containing only platform-neutral configuration files — the registry stores it once and both manifests reference the same blob. Most layers, however, contain compiled binaries and therefore differ between architectures. A multi-platform image typically occupies close to the sum of its per-architecture sizes, with only modest deduplication.

Manifest lists are part of the OCI Image Specification (and the older Docker Image Manifest V2 Schema 2). Docker Hub, Azure Container Registry, GitHub Container Registry, and every other compliant registry support them without special configuration.

## A worked example

The companion exercise [Multi-Platform Builds](/exercises/20-docker/) walks through the full sequence; the core build command is short:

```bash
docker buildx create --name multiarch --use
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t username/myimage:1.0 \
  --push .
```

The first command creates a builder named `multiarch` and selects it as the active builder. This builder runs in a separate container and brings BuildKit and QEMU into the build path.

The second command builds the Dockerfile in the current directory (`.`) for both `linux/amd64` and `linux/arm64`, tags the result `username/myimage:1.0`, and pushes the resulting manifest list and per-architecture layers to the registry. On an ARM64 laptop, the AMD64 build runs under QEMU emulation and takes noticeably longer than the native ARM64 build; the two builds proceed in parallel where possible.

After the push, `docker buildx imagetools inspect username/myimage:1.0` prints the contents of the manifest list, including the digest and platform of each entry. A pull from any host — `docker pull username/myimage:1.0` — then downloads only the layers for that host's architecture, transparently.

## Trade-offs and when multi-platform matters

Multi-platform builds add cost. Build time grows roughly linearly with the number of platforms when emulating, so a two-platform build takes roughly twice as long as a single-platform one. Image storage in the registry grows proportionally — two platforms occupy roughly twice the storage of one. Pulls remain single-architecture, so users do not pay a runtime cost.

| Approach | Build cost | Cache hit rate | When to use |
|----------|------------|----------------|-------------|
| Single-platform (default) | Native speed | High | All teammates and all targets share one architecture |
| Buildx with QEMU emulation | Slow on emulated platforms | Medium | Occasional builds; small teams; first-time setup |
| Native CI matrix builders | Native speed per arch | High | Frequent builds; production pipelines |

Multi-platform builds matter when the build host's architecture differs from any target host's architecture. The most common drivers:

- A team mixes Apple Silicon (arm64) developer laptops with cloud VMs that default to amd64.
- An application targets edge or IoT devices on ARM as well as servers on AMD64.
- A production deployment uses ARM-based cloud instances (such as AWS Graviton or Azure Ampere) for cost reasons while CI runs on amd64.

When every build host and every deployment target share an architecture, single-platform builds remain the simpler choice. Multi-platform tooling is the answer to architectural diversity, not a default to apply universally.

## Summary

Container images embed compiled binaries that target a specific CPU architecture, so an image built for AMD64 will not run on ARM64 and vice versa. The platform identifier `os/architecture/variant` makes this explicit, and `uname -m` on the host reveals which architecture local builds target by default. `docker buildx` is the multi-platform builder: it accepts a `--platform` list, executes the Dockerfile once per target, and uses QEMU to emulate non-native architectures during the build. The registry side of the story is the manifest list — a single tag points to a list of per-architecture image manifests, and the Docker client picks the right one at pull time. Buildx with QEMU is convenient but slow; production pipelines typically run native builders in a CI matrix and combine the results. Reach for multi-platform builds when the team's architectures diverge from the deployment target's, and stick with single-platform builds when they do not.
