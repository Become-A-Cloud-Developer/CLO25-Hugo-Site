# Part VII — Containers — Mining Notes

## Studieguide alignment

- Companion course weeks: ACD week 2 (v.16) "Docker och Docker Compose"
- Reflection questions across this week (extract verbatim from studieguide):
  - Vad är skillnaden mellan en Docker-image och en container?
  - Varför använder man multi-stage builds i en Dockerfile?
  - Hur kopplar Docker Compose ihop flera tjänster i ett lokalt nätverk?

## Companion exercise

- Path: `content/exercises/20-docker/`
- 5 sub-exercises:
  - 1-run-your-first-container: Pull and run an nginx container, serve a custom page with bind mounts, manage container lifecycle
  - 2-build-and-push-your-first-image: Write a Dockerfile, build a custom image, push to Docker Hub
  - 3-containerize-cloudsoft-recruitment: Single-stage and multi-stage Dockerfiles for ASP.NET Core app
  - 4-docker-compose-local-development-stack: Orchestrate app + MongoDB with volumes, named networks, and feature flags
  - 5-multi-platform-builds: Build for AMD64 and ARM64 with buildx, manifest lists
- Key code patterns mentioned: `docker run`, `docker ps`, bind mounts, port mapping, `Dockerfile` (FROM, COPY, RUN, ENTRYPOINT), multi-stage builds, `docker-compose.yml`, named volumes, services, Docker Hub push/pull, `buildx`, platform manifests.
- Key file names: Dockerfile, .dockerignore, docker-compose.yml, index.html, appsettings.json.
- Key library / API surface: Docker CLI, docker-compose CLI, Docker Hub API, buildx, QEMU emulation.

## Per-chapter brief

### Chapter 1 — Containers vs Virtual Machines (slug: 1-containers-vs-vms)
- Owns terms: container, container runtime, isolation (process namespace, cgroup at high level), kernel sharing.
- Borrows terms: virtual machine (from Part II Ch 4), hypervisor (from Part II Ch 4).
- Reflection questions to answer: Vad är skillnaden mellan en Docker-image och en container? (partial — containers vs VMs specifically); How do containers differ from VMs in isolation and resource overhead?
- Worked example to mine from exercise: Exercise 1 ("Run Your First Container") demonstrates running a container from a pulled image (`docker run nginx`) — the container is an isolated process running the nginx web server. Contrast this with a VM: the container shares the kernel with the host, starts in milliseconds, and uses minimal overhead. Binding port 8080:80 shows how containers provide network isolation but can expose ports to the host.
- Slide-pair: yes
- Course tag: ACD
- Cross-link target: /exercises/20-docker/1-run-your-first-container/
- Companion section in Part II: /course-book/2-infrastructure/compute/4-inside-a-virtual-server/

### Chapter 2 — Images and Layers (slug: 2-images-and-layers)
- Owns terms: image, layer, base image, image manifest, image cache.
- Borrows terms: container (from Chapter 1), kernel (from Part II), file system (from Part II).
- Reflection questions: What is a Docker image? How do layers work? Why does image caching matter for build speed?
- Worked example: Exercise 1 shows pulling the nginx image (`docker pull nginx`) — this downloads a pre-built image from Docker Hub with multiple layers. Exercise 2 ("Build and Push Your First Image") shows building a custom image with `FROM nginx` (base image) and `COPY index.html` (adds a new layer). Each instruction in a Dockerfile creates a layer; layers are read-only and stacked to form the final image. Docker caches layers during builds, so rebuilding with unchanged earlier steps reuses cached layers.
- Slide-pair: yes
- Course tag: ACD
- Cross-link target: /exercises/20-docker/2-build-and-push-your-first-image/

### Chapter 3 — Dockerfiles and Multi-Stage Builds (slug: 3-dockerfiles-and-multi-stage-builds)
- Owns terms: Dockerfile, instruction, multi-stage build, build context, .dockerignore, ENTRYPOINT vs CMD.
- Borrows terms: image (from Chapter 2), layer (from Chapter 2), ASP.NET Core (from Part III Ch 2).
- Reflection questions: Varför använder man multi-stage builds i en Dockerfile? (from week 2); Why do you specify ENTRYPOINT instead of CMD?
- Worked example: Exercise 3 ("Containerize CloudSoft Recruitment") shows the multi-stage pattern. First, a single-stage build produces a ~1GB image containing .NET SDK, source code, and compiled output. Then, refactoring to multi-stage: stage 1 (`FROM mcr.microsoft.com/dotnet/sdk`) runs `dotnet restore` and `dotnet publish`; stage 2 (`FROM mcr.microsoft.com/dotnet/aspnet`) copies only the published artifacts (the small runtime). The final image is ~200MB. This demonstrates why multi-stage separates build-time tools (compiler, package manager) from runtime (interpreter, runtime libraries). Build context (the directory passed to `docker build`) determines which files are available to COPY. A .dockerignore file (like .gitignore) excludes files from the build context.
- Slide-pair: yes
- Course tag: ACD
- Cross-link target: /exercises/20-docker/3-containerize-cloudsoft-recruitment/

### Chapter 4 — Multi-Platform Builds (slug: 4-multi-platform-builds)
- Owns terms: platform (architecture: amd64/arm64), buildx, manifest list, cross-platform build.
- Borrows terms: image (from Chapter 2), container (from Chapter 1), registry (from Chapter 6).
- Reflection questions: Why do single-platform images break in mixed teams (AMD64 vs ARM64)? How does docker buildx solve this?
- Worked example: Exercise 5 ("Multi-Platform Builds") starts by showing `uname -m` (reveals host architecture: x86_64 = AMD64, arm64 = ARM64). A Dockerfile built on one machine produces an image for that architecture only; a classmate with different architecture cannot run it. The exercise sets up `docker buildx create` (creates a builder using QEMU emulation) and `docker buildx build --platform linux/amd64,linux/arm64 -t user/image:latest --push` (builds for both at once, pushes to registry). The registry stores a **manifest list** (metadata) pointing to each platform-specific image digest. When a user pulls, the Docker client checks the manifest list and downloads the correct image for their architecture.
- Slide-pair: yes
- Course tag: ACD
- Cross-link target: /exercises/20-docker/5-multi-platform-builds/

### Chapter 5 — Docker Compose (slug: 5-docker-compose)
- Owns terms: Docker Compose, compose file, service (compose), volume, named network, dependency.
- Borrows terms: container (from Chapter 1), image (from Chapter 2), environment variable (from Part III Ch 5), connection string (from Part IV Ch 3).
- Reflection questions: Hur kopplar Docker Compose ihop flera tjänster i ett lokalt nätverk? (from week 2); How do named networks enable service-to-service communication?
- Worked example: Exercise 4 ("Docker Compose — Local Development Stack") shows a multi-container development environment. The `docker-compose.yml` defines: service `web` (the ASP.NET Core app, built from Dockerfile), service `mongo` (MongoDB image), service `mongo-express` (web UI for MongoDB). A **named network** (implicit in Compose) connects them: the app connects to MongoDB at hostname `mongo` (DNS resolved by Compose), not `localhost`. **Named volumes** persist data: `volumes: { data: {} }` followed by `mongo: volumes: [data:/data/db]` ensures database files survive container restarts. The **compose file** format specifies version, services, volumes, and networks in YAML. **Dependencies** (`depends_on`) declare ordering, though the best practice is to configure services to retry on connection failure. Environment variables override `appsettings.json`: `environment: { FeatureFlags__UseMongoDB: 'true' }` toggles MongoDB support without code change.
- Slide-pair: yes
- Course tag: ACD
- Cross-link target: /exercises/20-docker/4-docker-compose-local-development-stack/

### Chapter 6 — Container Registries (slug: 6-container-registries)
- Owns terms: container registry, image tag, image digest, push, pull, Docker Hub, Azure Container Registry (ACR).
- Borrows terms: image (from Chapter 2), manifest list (from Chapter 4), secret management (from Part V Ch 8).
- Reflection questions: What is Docker Hub? How do registries differ (Docker Hub vs Azure Container Registry)? Why use a private registry?
- Worked example: Exercise 2 shows pushing an image to Docker Hub: `docker tag myimage:latest username/myimage:latest` (tag locally with registry name) then `docker push username/myimage:latest`. The registry stores the image; anyone can then `docker pull username/myimage:latest`. An **image tag** (e.g., `latest`, `v1.0`, `main`) is human-readable; an **image digest** (SHA256 hash) is immutable and unique. Docker Hub is public and free; it stores millions of community images. **Azure Container Registry (ACR)** is a private registry for enterprise teams: images are not publicly visible, access is controlled by Azure identity, and it integrates with Azure Container Apps for automated deployments. The choice between Docker Hub and ACR depends on whether images need to be public (Hub) or private (ACR). Credentials for private registries must be provided at pull time.
- Slide-pair: yes
- Course tag: ACD
- Cross-link target: /exercises/20-docker/2-build-and-push-your-first-image/

## Cross-Part dependencies (forward references)

- Container orchestration and scaling (Kubernetes, Azure Container Apps) are mentioned but belong to Part VIII or beyond.
- Security scanning and vulnerability management in registries are mentioned but detailed security operations belong to Part VI / Part IX.
- Performance tuning (layer caching optimization, buildx BuildKit features) is mentioned but advanced build optimization belongs to future depth.

## Tonal reference

Use `content/course-book/2-infrastructure/compute/4-inside-a-virtual-server/inside-a-virtual-server.md` as the gold standard. Key features to emulate:
- **Motivation paragraph** opens before definitions — e.g., "Containers solve the 'it works on my machine' problem by packaging your application with its exact dependencies, so it runs the same way everywhere."
- **Bold on first use** of every key term — "A **container** is a lightweight, isolated process..."
- **Worked examples** with interpretation — show the exercise command, then explain what it demonstrates (e.g., `docker run --name my-nginx` shows isolation; `-p 8080:80` shows networking).
- **Closing Summary section** recapping load-bearing claims — tie together images, layers, Dockerfiles, multi-stage, Compose, and registries as an integrated system for reproducible, portable applications.
- **1500–3500 words** per chapter — concise but detailed enough to convey concepts and practical understanding.
