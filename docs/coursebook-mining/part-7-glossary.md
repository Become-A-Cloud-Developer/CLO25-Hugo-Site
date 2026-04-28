# Part VII — Glossary

Terminology contract for the six chapters of Part VII — Containers.

## Terms owned by this Part

### Container
- **Owner chapter**: `1-containers-vs-vms`
- **Canonical definition**: A **container** is a lightweight, isolated process running on a shared kernel; it bundles an application and its dependencies into a standardized unit that starts in milliseconds, provides process and network isolation, and runs identically across development, testing, and production environments.
- **Used by chapters**: 1 (owner), 2, 3, 4, 5, 6

### Container runtime
- **Owner chapter**: `1-containers-vs-vms`
- **Canonical definition**: A **container runtime** is the software that executes containers on a host machine; Docker is the most common runtime, using the Linux kernel's containerization features (namespaces and cgroups) to enforce isolation without the overhead of a full virtual machine.
- **Used by chapters**: 1 (owner)

### Isolation
- **Owner chapter**: `1-containers-vs-vms`
- **Canonical definition**: **Isolation** in containers is the kernel-enforced separation of processes, filesystems, and networks from the host and other containers; process namespaces hide other processes, network namespaces provide separate network stacks, and cgroups limit CPU and memory to prevent one container from starving others.
- **Used by chapters**: 1 (owner), 5

### Kernel sharing
- **Owner chapter**: `1-containers-vs-vms`
- **Canonical definition**: **Kernel sharing** is the architectural difference between containers and virtual machines: containers all use the same host operating system kernel (but isolated via namespaces), whereas virtual machines each run their own full kernel, trading isolation for size and startup overhead.
- **Used by chapters**: 1 (owner)

### Image
- **Owner chapter**: `2-images-and-layers`
- **Canonical definition**: A **Docker image** is an immutable, layered template that defines an application and its runtime environment; it specifies a base OS, dependencies, files, and a default command, and is stored in a registry for distribution; containers are running instances of images.
- **Used by chapters**: 2 (owner), 3, 4, 5, 6

### Layer
- **Owner chapter**: `2-images-and-layers`
- **Canonical definition**: A **layer** is a read-only filesystem snapshot created by one or more Dockerfile instructions; layers are stacked (like a union filesystem) to form the complete image filesystem, and Docker caches unchanged layers during rebuilds to avoid redundant work.
- **Used by chapters**: 2 (owner), 3, 4

### Base image
- **Owner chapter**: `2-images-and-layers`
- **Canonical definition**: A **base image** is the initial image specified in a Dockerfile's `FROM` instruction; it provides the operating system, runtime, and package manager for the rest of the image (e.g., `FROM mcr.microsoft.com/dotnet/aspnet:latest` provides the .NET runtime).
- **Used by chapters**: 2 (owner), 3

### Image manifest
- **Owner chapter**: `2-images-and-layers`
- **Canonical definition**: An **image manifest** is the JSON metadata that describes an image: it lists layers, their digests and sizes, the image configuration, and platform architecture; a **manifest list** extends this to map multiple platform-specific manifests to a single tag, enabling multi-platform support.
- **Used by chapters**: 2 (owner), 4, 6

### Image cache
- **Owner chapter**: `2-images-and-layers`
- **Canonical definition**: **Image cache** is Docker's optimization that reuses layers from previous builds; when a Dockerfile instruction has not changed and its context is identical, Docker skips re-execution and uses the cached layer, reducing build time significantly.
- **Used by chapters**: 2 (owner), 3

### Dockerfile
- **Owner chapter**: `3-dockerfiles-and-multi-stage-builds`
- **Canonical definition**: A **Dockerfile** is a plain-text file containing a sequence of instructions (`FROM`, `COPY`, `RUN`, `ENTRYPOINT`, etc.) that define how to build a Docker image; each instruction creates a new layer.
- **Used by chapters**: 3 (owner), 4, 5

### Instruction
- **Owner chapter**: `3-dockerfiles-and-multi-stage-builds`
- **Canonical definition**: An **instruction** in a Dockerfile is a command such as `FROM`, `COPY`, `RUN`, `ENV`, `EXPOSE`, or `ENTRYPOINT` that performs an operation (e.g., adding a file, running a shell command, setting an environment variable) and typically creates a new image layer.
- **Used by chapters**: 3 (owner)

### Multi-stage build
- **Owner chapter**: `3-dockerfiles-and-multi-stage-builds`
- **Canonical definition**: A **multi-stage build** is a Dockerfile pattern using multiple `FROM` instructions; earlier stages can execute build-time tools (compilers, package managers) and later stages copy only the compiled artifacts, producing smaller final images without build tools.
- **Used by chapters**: 3 (owner), 4, 5

### Build context
- **Owner chapter**: `3-dockerfiles-and-multi-stage-builds`
- **Canonical definition**: A **build context** is the directory (and its contents) passed to `docker build` from which the Dockerfile can `COPY` files into the image; it excludes files listed in `.dockerignore`, similar to `.gitignore`.
- **Used by chapters**: 3 (owner)

### .dockerignore
- **Owner chapter**: `3-dockerfiles-and-multi-stage-builds`
- **Canonical definition**: A **.dockerignore** file lists patterns of files to exclude from the build context, preventing unnecessary files (node_modules, .git, build artifacts) from being sent to the Docker daemon and reducing image size.
- **Used by chapters**: 3 (owner)

### ENTRYPOINT vs CMD
- **Owner chapter**: `3-dockerfiles-and-multi-stage-builds`
- **Canonical definition**: **ENTRYPOINT** defines the main executable that always runs when a container starts, while **CMD** provides default arguments; `ENTRYPOINT ["app"]` and `CMD ["--help"]` combine so `docker run myimage` runs `app --help`, and `docker run myimage --version` runs `app --version`.
- **Used by chapters**: 3 (owner)

### Platform
- **Owner chapter**: `4-multi-platform-builds`
- **Canonical definition**: A **platform** in container terms specifies the CPU architecture (amd64, arm64, arm/v7) for which an image is compiled; images compiled for one platform cannot run natively on a different architecture without emulation.
- **Used by chapters**: 4 (owner), 6

### Buildx
- **Owner chapter**: `4-multi-platform-builds`
- **Canonical definition**: **Buildx** is a Docker CLI extension that enables building images for multiple platforms and architectures; it uses BuildKit and QEMU emulation to cross-compile and push multi-platform manifests to registries.
- **Used by chapters**: 4 (owner)

### Manifest list
- **Owner chapter**: `4-multi-platform-builds`
- **Canonical definition**: A **manifest list** is registry metadata that maps a single image tag to multiple platform-specific image digests; when a user pulls an image, the Docker client consults the manifest list and downloads the correct image for their architecture.
- **Used by chapters**: 4 (owner), 6

### Cross-platform build
- **Owner chapter**: `4-multi-platform-builds`
- **Canonical definition**: A **cross-platform build** uses tools like `docker buildx` to compile an image for architectures other than the host machine's native architecture; it enables a team with mixed AMD64 and ARM64 machines to pull and run the same image tag.
- **Used by chapters**: 4 (owner)

### Docker Compose
- **Owner chapter**: `5-docker-compose`
- **Canonical definition**: **Docker Compose** is a tool that reads a `docker-compose.yml` file and orchestrates multi-container applications; it automates networking, volume management, and environment setup for local development and testing.
- **Used by chapters**: 5 (owner), 6

### Compose file
- **Owner chapter**: `5-docker-compose`
- **Canonical definition**: A **compose file** (docker-compose.yml) is a YAML document that declares services, volumes, networks, and environment variables for a multi-container application; it serves as the source of truth for the application's topology.
- **Used by chapters**: 5 (owner)

### Service
- **Owner chapter**: `5-docker-compose`
- **Canonical definition**: A **service** (in Compose context) is a named, containerized application; multiple services communicate via a Docker Compose-managed network, each addressable by its service name as a hostname (e.g., the `web` service reaches `mongodb` at hostname `mongodb`).
- **Used by chapters**: 5 (owner)

### Volume
- **Owner chapter**: `5-docker-compose`
- **Canonical definition**: A **volume** in Docker is a directory managed by the Docker daemon (or host-mounted) that persists data beyond a container's lifetime; in Compose, `volumes: { data: {} }` declares a named volume and `service: volumes: [data:/path]` mounts it into the container.
- **Used by chapters**: 5 (owner)

### Named network
- **Owner chapter**: `5-docker-compose`
- **Canonical definition**: A **named network** is a Docker network created and managed by Compose; services connected to the same named network can resolve each other by hostname via embedded DNS, enabling service-to-service communication without hardcoded IPs.
- **Used by chapters**: 5 (owner)

### Dependency
- **Owner chapter**: `5-docker-compose`
- **Canonical definition**: A **dependency** in Compose (specified via `depends_on`) declares that one service should start after another; however, it does not guarantee the dependency is ready, so robust services use retry logic or readiness checks instead.
- **Used by chapters**: 5 (owner)

### Container registry
- **Owner chapter**: `6-container-registries`
- **Canonical definition**: A **container registry** is a server that stores and distributes Docker images; Docker Hub is public and free; Azure Container Registry (ACR) is private and integrated with Azure identity for enterprise teams.
- **Used by chapters**: 6 (owner)

### Image tag
- **Owner chapter**: `6-container-registries`
- **Canonical definition**: An **image tag** is a human-readable label for a version of an image (e.g., `latest`, `v1.0`, `main`); tags are mutable (the same tag can be reassigned), unlike image digests which are immutable.
- **Used by chapters**: 6 (owner)

### Image digest
- **Owner chapter**: `6-container-registries`
- **Canonical definition**: An **image digest** is a SHA256 hash of an image's manifest; it uniquely and immutably identifies an image (e.g., `sha256:abc123...`) and remains the same even if the tag is reassigned.
- **Used by chapters**: 6 (owner)

### Push
- **Owner chapter**: `6-container-registries`
- **Canonical definition**: **Push** is the operation to upload a locally built image to a container registry; `docker push username/myimage:latest` sends the image and its layers to the registry, making it available for others to pull.
- **Used by chapters**: 6 (owner)

### Pull
- **Owner chapter**: `6-container-registries`
- **Canonical definition**: **Pull** is the operation to download an image from a container registry to the local machine; `docker pull username/myimage:latest` fetches the image manifest and layers from the registry for local use.
- **Used by chapters**: 6 (owner)

### Docker Hub
- **Owner chapter**: `6-container-registries`
- **Canonical definition**: **Docker Hub** is the default public container registry hosted by Docker; it stores millions of images (official images like nginx and ubuntu, community images, and user-pushed images), and anyone can pull images without authentication.
- **Used by chapters**: 6 (owner)

### Azure Container Registry
- **Owner chapter**: `6-container-registries`
- **Canonical definition**: **Azure Container Registry (ACR)** is Microsoft's managed private container registry; it stores images securely, controls access via Azure identity, integrates with Azure Container Apps and CI/CD pipelines, and supports geo-replication for global availability.
- **Used by chapters**: 6 (owner)

## Terms borrowed from earlier Parts

### Virtual machine
- **Defined in**: Part II — Infrastructure / `4-inside-a-virtual-server`
- **Reference link**: `/course-book/2-infrastructure/compute/4-inside-a-virtual-server/`

### ASP.NET Core
- **Defined in**: Part III — Application Development / `2-the-dotnet-platform`
- **Reference link**: `/course-book/3-application-development/2-the-dotnet-platform/`

### Configuration / Environment variable
- **Defined in**: Part III — Application Development / `5-configuration-and-environments`
- **Reference link**: `/course-book/3-application-development/5-configuration-and-environments/`

### Connection string
- **Defined in**: Part IV — Data Access / `3-` (placeholder for specific chapter)
- **Reference link**: `/course-book/4-data-access/` (adjust with actual chapter path)

### Secret management
- **Defined in**: Part V — Identity & Security / `8-` (placeholder for specific chapter)
- **Reference link**: `/course-book/5-identity-and-security/` (adjust with actual chapter path)
