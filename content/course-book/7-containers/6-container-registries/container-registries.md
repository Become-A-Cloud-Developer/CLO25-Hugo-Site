+++
title = "Container Registries"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 60
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/7-containers/6-container-registries.html)

[Se presentationen på svenska](/presentations/course-book/7-containers/6-container-registries-swe.html)

---

A built image is only useful if the machines that need to run it can get hold of it. Local builds live in the daemon's storage on a single laptop; deployment targets — a teammate's machine, a CI runner, a production cluster — never see them. Distributing an image therefore needs a server that can store images and serve them to anyone with the right credentials. That server is a container registry, and the operations a registry supports — naming images, addressing them by tag or by digest, controlling who can push and who can pull — shape day-to-day container workflows from local development through production deployment.

## What a registry is

A **container registry** is a server that stores and distributes Docker images; Docker Hub is public and free, while Azure Container Registry (ACR) is private and integrated with Azure identity for enterprise teams. Both implement the same underlying protocol — the OCI Distribution Specification — so the Docker CLI, the BuildKit toolchain, and orchestrators like Kubernetes interact with any compliant registry through the same commands.

A registry is best understood as a content-addressable store. Each image layer is an immutable blob identified by a SHA256 hash; an [image manifest](/course-book/7-containers/2-images-and-layers/) lists the layer hashes plus configuration; the image as a whole is identified by the hash of that manifest. The registry exposes a small REST API: clients ask for a manifest, then pull each blob the manifest references, then assemble them locally into a usable image. Pushing reverses the flow — the client uploads any blobs the registry does not already have, then uploads the manifest last. Because blobs are content-addressed, the registry can deduplicate them across images: two images that share a Debian base image store that base image's layers exactly once.

### Repositories and the reference syntax

Within a registry, images are organized into **repositories**. A repository holds many versions of the same logical image — typically the build history of one application. The fully qualified reference to an image takes the form `host/repository:tag`, for example `mycr.azurecr.io/cloudsoft/recruitment:1.0`. The host segment identifies the registry; the repository segment names the application (and optionally a project or team prefix); the tag identifies the version.

When the host is omitted, the Docker client assumes Docker Hub: `docker pull nginx:latest` is shorthand for `docker pull docker.io/library/nginx:latest`. This implicit default makes Docker Hub feel native to the toolchain, but any registry can be addressed by including its hostname in the reference. A reference can also use a digest in place of a tag, written `repository@sha256:...`; the use of digests is covered below.

## Tags versus digests

An **image tag** is a human-readable label for a version of an image (e.g., `latest`, `v1.0`, `main`); tags are mutable, meaning the same tag can be reassigned, unlike image digests which are immutable. A tag is just a pointer that the registry maintains in a small piece of metadata: it can be moved to point at a different image at any time, and rebuilding an image with the same tag and re-pushing it overwrites the old pointer.

An **image digest** is a SHA256 hash of an image's manifest; it uniquely and immutably identifies an image (e.g., `sha256:abc123...`) and remains the same even if the tag is reassigned. Because the digest is computed from the manifest's bytes, two different images can never share a digest, and any change — a single byte in any layer — produces a different digest.

This distinction matters for production deployments. A workflow that pulls `myapp:latest` is implicitly trusting that the tag has not been moved between the moment the image was tested and the moment it is pulled into production. If a teammate pushes a new build to `:latest` between those moments, the deployment runs an untested image. Pinning the deployment to a digest — `myapp@sha256:9f86d0...` — eliminates that race entirely, because the digest can only refer to the exact bytes that were tested. CI pipelines therefore tend to record the digest produced by the build step and pass that digest to the deploy step, while still publishing a friendly tag for humans to read. A common convention is to push both `:1.0` and `:latest` for every release, and to deploy by digest while documenting the corresponding tag for traceability.

## Push and pull

**Push** is the operation to upload a locally built image to a container registry; `docker push username/myimage:latest` sends the image and its layers to the registry, making it available for others to pull. The client first checks which layer blobs the registry already has — if a base image like `mcr.microsoft.com/dotnet/aspnet` is already present, those layers are skipped — and uploads only the new ones, then finally writes the manifest under the requested tag.

**Pull** is the reverse: the operation to download an image from a container registry to the local machine; `docker pull username/myimage:latest` fetches the image manifest and layers from the registry for local use. The client retrieves the manifest, downloads any layers it does not already have cached, and reconstructs the image. Both push and pull are resumable at the layer level: a connection drop in the middle of a 500 MB layer means that one layer must restart, but layers already transferred are not retransmitted.

### Worked example

Pushing a locally built application to a private Azure Container Registry uses the same two-step pattern regardless of registry: tag the image with the registry's hostname, then push.

```bash
docker tag myapp:local mycr.azurecr.io/myapp:1.0
docker push mycr.azurecr.io/myapp:1.0
```

The `docker tag` command does not copy or rebuild the image — it adds a second name to the same image bytes already in the local daemon. The new name happens to include the registry hostname, which tells `docker push` where to send the upload. The push then streams the layers up to `mycr.azurecr.io`, skipping any blobs the registry already has. Once it completes, any other machine authenticated to that registry can run `docker pull mycr.azurecr.io/myapp:1.0` and receive the same bytes. The companion exercise [Build and Push Your First Image](/exercises/20-docker/) walks through this flow against Docker Hub, where the registry hostname is implicit.

## Public and private registries

Registries split broadly into public and private along two axes: who can pull (read access) and who can push (write access).

**Docker Hub** is the default public container registry hosted by Docker; it stores millions of images (official images like nginx and ubuntu, community images, and user-pushed images), and anyone can pull images without authentication. It is the place where a Dockerfile's `FROM nginx:alpine` ultimately resolves. Free accounts can also push their own images, which become publicly readable by default. Docker Hub sits at the centre of the open-source container ecosystem, and many teams use it as their default registry because everything else does.

The pitfall to know about is rate limiting. Anonymous pulls from Docker Hub are throttled per source IP — at the time of writing, 100 pulls per six hours — and authenticated free accounts are throttled at 200 per six hours. A CI pipeline that pulls `node:20` for every build, run from a shared cloud runner pool that shares an outbound IP with thousands of other tenants, can hit the limit unpredictably. The fix is either to authenticate the CI runner against Docker Hub with a paid plan, to mirror the upstream image into a private registry the team controls, or — increasingly common — to consume official images from a registry like Microsoft's MCR (`mcr.microsoft.com/dotnet/aspnet`) which does not throttle.

**Azure Container Registry (ACR)** is Microsoft's managed private container registry; it stores images securely, controls access via Azure identity, integrates with Azure Container Apps and CI/CD pipelines, and supports geo-replication for global availability. An ACR instance has a hostname like `myregistry.azurecr.io` and lives inside an Azure subscription, billed by the storage and bandwidth it uses. Images pushed to ACR are private by default — pulling requires authentication — which makes ACR the natural choice for proprietary application images that should not be world-readable.

A useful default for an Azure-hosted system is to push every application image to ACR, even if the team also uses Docker Hub for shared base images. ACR's rate limits are governed by the registry's chosen pricing tier, not by anonymous-IP throttling, so production pulls remain predictable. Geo-replication can mirror an image automatically across Azure regions, so a workload in a different region pulls from a local replica rather than across the public internet.

### Choosing a registry

| Registry | Visibility | Pull authentication | Common use |
|----------|------------|---------------------|------------|
| Docker Hub (public) | World-readable | None for public images | Open-source distribution, base images |
| Docker Hub (private repo, paid) | Authenticated | PAT or username/password | Small teams, mixed open-source workflow |
| Azure Container Registry | Private | Azure identity | Production deployments on Azure |
| MCR (Microsoft Container Registry) | Public, no rate limit | None | Pulling Microsoft official images (.NET, SQL Server) |

The choice is rarely binary. Most production setups consume base images from Docker Hub or MCR (because that is where the upstream publishers push them) and push application images to a private registry like ACR (because that is where deployment targets pull from). The Dockerfile's `FROM` line and the CI pipeline's `docker push` target are independent decisions.

## Authenticating to a registry

Every push requires authentication, and every private pull does too. The registry never stores plaintext credentials — it accepts a credential, mints a short-lived bearer token, and uses that token for the actual blob and manifest operations — but the entry point that produces the credential is what teams have to configure.

For Docker Hub, the entry point is a username and a Personal Access Token (PAT). The PAT is generated in the Docker Hub web UI with a chosen scope (read-only, read-write, or admin) and an optional expiration. The CLI command `docker login -u <username>` prompts for the PAT and stores a credential reference in `~/.docker/config.json`. Treating a PAT like a password is essential: it is a secret, it should never appear in a repository, and it should be revoked when an engineer leaves the team.

For ACR, several authentication paths exist, and the right one depends on what is doing the authenticating. For interactive use from a developer laptop, `az acr login --name myregistry` exchanges the developer's Azure login for a short-lived ACR token and configures the Docker CLI automatically — no long-lived secret is stored. For an Azure compute resource pulling an image — a Container App, a VM, an AKS node pool — the right path is **managed identity**: the resource has its own Azure identity, the registry grants that identity the `AcrPull` role, and the platform handles credential rotation transparently with no secret involved. Managed identities are explained in detail under [Identity and Security](/course-book/5-identity-and-security/). For a CI pipeline running outside Azure, the preferred path is OIDC federation: the pipeline presents a short-lived OIDC token issued by the CI provider, ACR validates it against a configured trust relationship, and grants access without a stored secret. The Part VIII chapters on CI/CD cover OIDC for registry login in depth.

The pattern across all three is the same: avoid long-lived secrets where possible, prefer identity-based authentication, and when an [API key](/course-book/5-identity-and-security/) or PAT is unavoidable, store it in a secret manager rather than in source control or a `.env` file.

### Registry security scans

Most managed registries run a vulnerability scanner against pushed images, comparing each layer's installed packages against public CVE databases. ACR integrates with Microsoft Defender for Cloud to surface findings; Docker Hub offers Snyk-powered scanning on paid plans; standalone scanners like Trivy can run against any registry as part of a CI pipeline. The output is a list of CVEs by severity, attached to a specific image digest. Security scanning does not replace careful base-image selection — a vulnerability discovered after the scan still ships — but it raises the floor by catching obviously stale images before they reach production. A common policy is to fail a deployment if the image carries unfixed Critical CVEs and to warn but proceed for High or below, with the rules tuned to the team's risk tolerance.

## Summary

A container registry is a content-addressable server that stores image blobs and manifests, organizing them into repositories addressed as `host/repo:tag`. Tags are mutable pointers that humans use to refer to versions; digests are immutable SHA256 hashes that uniquely identify exact image bytes, and production deployments should pin to digests to avoid running an image other than the one that was tested. Docker Hub is the default public registry and the home of most upstream open-source images, but its anonymous rate limits make it a poor primary registry for CI pipelines; Azure Container Registry is the private counterpart for Azure-hosted systems, authenticated via Azure identity rather than a stored secret. Push and pull move layers in and out of the registry, deduplicated by content hash so that shared base layers transfer only once. Authentication ranges from PATs (Docker Hub) through managed identities (ACR from Azure compute) to OIDC federation (ACR from external CI), with the trend across all paths being to eliminate long-lived stored secrets.
