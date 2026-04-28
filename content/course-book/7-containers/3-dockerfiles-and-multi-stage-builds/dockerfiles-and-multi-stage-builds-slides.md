+++
title = "Dockerfiles and Multi-Stage Builds"
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

## Dockerfiles and Multi-Stage Builds
Part VII — Containers

---

## The image is the unit of deployment
- A **Dockerfile** is the version-controlled recipe that produces an image
- Same input, same image — on any machine with a Docker daemon
- Source code, dependencies, runtime, and entrypoint travel together
- Reproducibility is the entire point of containerization

---

## Dockerfile instructions
- **FROM** — pick the base image; every Dockerfile starts here
- **WORKDIR** — set the working directory for COPY and RUN
- **COPY** — pull files from the build context into the image
- **RUN** — execute a shell command during build (creates a layer)
- **EXPOSE / ENV / ARG** — document ports, set runtime / build-time variables

---

## The build context
- The directory passed to `docker build` — everything COPY can reach
- The CLI ships the entire context to the daemon over a socket
- A bloated context means slow builds and accidental leakage of secrets
- **.dockerignore** excludes patterns from the context (like `.gitignore`)
- Always exclude `bin/`, `obj/`, `node_modules/`, `.git/`, `.env`

---

## The layer cache and instruction order
- Each instruction is fingerprinted; unchanged inputs reuse the cached layer
- Cache invalidates on the first changed step — every later step reruns
- Copy dependency manifests *before* source: `package.json` then `npm install`
- Source edits no longer trigger a full reinstall
- Same trick for `*.csproj` + `dotnet restore`, `requirements.txt` + `pip install`

---

## ENTRYPOINT versus CMD
- **ENTRYPOINT** — the executable that always runs when a container starts
- **CMD** — default arguments, overridable on `docker run`
- App images: pin the entrypoint to the binary — image is self-documenting
- Tooling images: leave it open, use CMD for flexibility
- Always use the JSON array form so `SIGTERM` reaches the application

---

## The multi-stage pattern
- Build tools (SDK, compiler, package manager) are not needed at runtime
- Bundling them inflates image size and widens the attack surface
- A **multi-stage build** uses multiple `FROM` instructions
- Earlier stages compile and publish; later stage holds only the runtime
- `COPY --from=build` pulls the artifact across stage boundaries

---

## .NET multi-stage example
- Stage 1: `FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build`
- `dotnet restore`, then `dotnet publish -c Release -o /app/publish`
- Stage 2: `FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final`
- `COPY --from=build /app/publish .` — runtime + published DLLs only
- Final image drops from ~1 GB to ~200 MB

---

## Practice
- Exercise: [/exercises/20-docker/](/exercises/20-docker/)
- Sub-exercise 3 walks through the .NET single-stage to multi-stage refactor
- Compare image sizes with `docker images` before and after
- Try changing one source file and watch which layers rebuild

---

## Questions?
