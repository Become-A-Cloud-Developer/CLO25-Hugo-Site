+++
title = "Dockerfiles and Multi-Stage Builds"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 30
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/7-containers/3-dockerfiles-and-multi-stage-builds.html)

[Se presentationen på svenska](/presentations/course-book/7-containers/3-dockerfiles-and-multi-stage-builds-swe.html)

---

Shipping an application to production means shipping a unit of deployment that another machine can run identically to the developer's laptop. The Docker [image](/course-book/7-containers/2-images-and-layers/) fills that role, but an image is only as reproducible as the recipe that builds it. A Dockerfile is that recipe — a plain-text script that turns source code into an immutable image. The sections below develop the Dockerfile format, the build context that feeds it, the layer cache that makes rebuilds fast, and the multi-stage pattern that keeps production images small.

## The Dockerfile as a build recipe

A **Dockerfile** is a plain-text file containing a sequence of instructions (`FROM`, `COPY`, `RUN`, `ENTRYPOINT`, etc.) that define how to build a Docker image; each instruction creates a new layer. Docker reads the file from top to bottom, executes each step against an intermediate container, and snapshots the resulting filesystem as a read-only layer. The final image is the stack of those layers plus a small JSON configuration that records the entrypoint, exposed ports, and environment.

The file is committed alongside source code, which means the build process itself is version-controlled. Anyone with the repository and a Docker daemon can produce the same image, byte for byte, without manually installing compilers or copying configuration. That reproducibility is the whole point: a container is only useful as a unit of deployment if the image behind it is deterministic.

### Dockerfile instructions

An **instruction** in a Dockerfile is a command such as `FROM`, `COPY`, `RUN`, `ENV`, `EXPOSE`, or `ENTRYPOINT` that performs an operation (e.g., adding a file, running a shell command, setting an environment variable) and typically creates a new image layer. Each instruction sits on its own line, written as the instruction name in upper case followed by its arguments.

A working Dockerfile usually combines a small set of instructions:

- `FROM` declares the base image — the starting point for everything that follows. Every Dockerfile begins with `FROM`.
- `WORKDIR` sets the working directory inside the image. Subsequent `COPY` and `RUN` instructions resolve relative paths against this directory, and the container starts in it.
- `COPY` adds files from the build context into the image. `COPY src dst` copies the host path `src` to the image path `dst`.
- `RUN` executes a shell command during the build. `RUN dotnet restore` downloads NuGet packages; `RUN apt-get install -y curl` installs a system package. The result is captured as a new layer.
- `EXPOSE` documents which TCP port the application listens on. It does not actually publish the port — that happens at `docker run` time with `-p` — but it is metadata that tools and humans rely on.
- `ENV` sets an environment variable that persists in every container started from the image. `ENV ASPNETCORE_URLS=http://+:8080` is a common .NET pattern.
- `ARG` declares a build-time variable that can be passed with `docker build --build-arg`. Unlike `ENV`, `ARG` values do not survive into the running container — they exist only during the build.
- `ENTRYPOINT` and `CMD` define what the container runs when it starts. The two interact in a specific way unpacked under [ENTRYPOINT versus CMD](#entrypoint-versus-cmd).

These instructions cover most real Dockerfiles. The full reference contains a few more (`USER`, `LABEL`, `HEALTHCHECK`, `VOLUME`), but the seven above carry almost all the weight in application images.

## The build context and .dockerignore

When `docker build .` is invoked, the trailing `.` is not just a path — it is the **build context**. A **build context** is the directory (and its contents) passed to `docker build` from which the Dockerfile can `COPY` files into the image; it excludes files listed in `.dockerignore`, similar to `.gitignore`. The Docker CLI archives the entire context and ships it to the Docker daemon over a socket. Only files inside the context are visible to `COPY` instructions — a Dockerfile cannot reach above its context to grab a file from a parent directory.

This matters for two reasons. First, the context determines build speed: a 50 MB context uploads in a fraction of a second, while a 5 GB context (with a `node_modules` directory, a `.git` history, and a `bin/` folder full of compiled output) makes every build painful. Second, anything in the context can end up baked into the image by accident — secrets in a `.env` file, local credentials, cached test artifacts.

A **.dockerignore** file lists patterns of files to exclude from the build context, preventing unnecessary files (node_modules, .git, build artifacts) from being sent to the Docker daemon and reducing image size. The syntax mirrors `.gitignore`: one pattern per line, glob wildcards permitted, leading `!` to re-include. A reasonable starting point for a .NET project looks like this:

```text
bin/
obj/
.git/
.vs/
*.user
appsettings.Development.json
**/.env
```

The first three lines exclude build output and version-control metadata that the build does not need. The last two protect against committing local developer secrets into a shared image. Treat `.dockerignore` as a default-deny list when secrets are involved — it is much easier to reason about which files the build *can* see than to chase down which files leaked.

## The layer cache and instruction order

Each instruction produces a layer, and Docker fingerprints that layer based on the instruction text and the files it touched. The [image cache](/course-book/7-containers/2-images-and-layers/) reuses layers from previous builds when the instruction and its inputs have not changed; what matters here is how Dockerfile authoring interacts with it.

The cache invalidates on the first instruction whose inputs differ from the previous build. From that point down, every later instruction reruns. Order therefore decides whether a one-line code change triggers a 30-second rebuild or a 10-minute one.

The canonical example is dependency installation. Consider a Node.js project where `npm install` takes two minutes. A naive Dockerfile copies everything first:

```dockerfile
FROM node:20
WORKDIR /app
COPY . .
RUN npm install
CMD ["node", "server.js"]
```

Every time any source file changes, `COPY . .` invalidates, and `npm install` runs again. The fix is to copy the manifest first, install against it, and only then copy the rest:

```dockerfile
FROM node:20
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm install
COPY . .
CMD ["node", "server.js"]
```

Now `npm install` only reruns when `package.json` or `package-lock.json` change. Source edits invalidate the final `COPY . .` layer, which is fast. The same pattern applies to .NET (`COPY *.csproj` then `RUN dotnet restore`), Python (`COPY requirements.txt` then `RUN pip install`), and Go (`COPY go.mod go.sum` then `RUN go mod download`). The principle is the same: copy what changes rarely, install against it, then copy what changes often.

## ENTRYPOINT versus CMD

The last load-bearing instruction defines what the container does when it runs. Two instructions cooperate here: **ENTRYPOINT** defines the main executable that always runs when a container starts, while **CMD** provides default arguments; `ENTRYPOINT ["app"]` and `CMD ["--help"]` combine so `docker run myimage` runs `app --help`, and `docker run myimage --version` runs `app --version`.

The mechanics matter. `ENTRYPOINT` is fixed: it is the executable that runs no matter what. `CMD` is a default argument list that can be overridden by passing arguments after the image name on `docker run`. If only `CMD` is set, the entire command can be replaced — `docker run myimage bash` drops into a shell. If `ENTRYPOINT` is set, `docker run myimage bash` passes `bash` as an argument to the entrypoint, which is usually not what was intended.

For application images — a web server, a worker, an API — `ENTRYPOINT` is almost always the right choice. The image has a single job, and the container should run that job. Pinning the entrypoint to the application binary prevents the container from accidentally being run as a shell when an operator types something wrong, and it makes the image self-documenting: `docker run myimage` does the obvious thing. Tooling images (a `.NET` SDK image, a `kubectl` image, a `psql` image) are the opposite case — they are meant to be invoked with arbitrary commands, and `CMD` or no entrypoint at all is more flexible.

Both forms accept either a JSON array (`["dotnet", "App.dll"]`) or a shell string (`dotnet App.dll`). Always prefer the array form. The shell form wraps the command in `/bin/sh -c`, which means the application does not receive Unix signals correctly — `SIGTERM` from `docker stop` goes to the shell, not to the application, and the shell ignores it until Docker times out and sends `SIGKILL` ten seconds later. The array form runs the application directly as PID 1, and signals reach it.

## The multi-stage pattern

Building an application requires tools the running application does not need. A .NET service needs the SDK to compile but only the ASP.NET runtime to run. A Go service needs the compiler and its module cache during build but ships as a single static binary. A Java service needs Maven or Gradle and a JDK to build but only a JRE to serve traffic. Bundling the build tools into the production image bloats it (the .NET SDK alone adds about 700 MB) and widens the attack surface — every package manager, compiler, and shell utility in the image is a potential vulnerability.

A **multi-stage build** is a Dockerfile pattern using multiple `FROM` instructions; earlier stages can execute build-time tools (compilers, package managers) and later stages copy only the compiled artifacts, producing smaller final images without build tools. Each `FROM` starts a new stage with a clean filesystem; a later stage uses `COPY --from=<stage>` to pull files out of an earlier one. Only the final stage becomes the published image; the intermediate stages are discarded after the build completes.

### Worked example: .NET multi-stage Dockerfile

The CloudSoft Recruitment exercise (see [/exercises/20-docker/](/exercises/20-docker/), specifically the third sub-exercise) refactors a single-stage .NET image into a two-stage build. The single-stage version produces a roughly 1 GB image carrying the SDK, the source tree, and the compiled output. The multi-stage version produces an image of around 200 MB containing only the runtime and the published artifacts.

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "CloudSoft.Recruitment.dll"]
```

The first stage is named `build` (via `AS build`) and uses the SDK image, which weighs in at over 800 MB but contains the C# compiler, MSBuild, and the NuGet client. It restores dependencies from the project file first — a cacheable step — then copies the source and publishes a release build into `/app/publish`. The second stage named `final` starts fresh from the much smaller `aspnet` runtime image, copies the publish output across with `COPY --from=build`, declares port 8080, and pins the entrypoint to the application DLL. The SDK, the source code, the NuGet cache, and the intermediate `bin` and `obj` directories never reach the final image — they live and die in the discarded `build` stage. The result is a production image that contains exactly what is needed to run the application: the .NET runtime and the published artifacts.

## Summary

A Dockerfile is the version-controlled recipe that turns source code into a Docker image. Each instruction (`FROM`, `WORKDIR`, `COPY`, `RUN`, `EXPOSE`, `ENTRYPOINT`, `CMD`, `ARG`, `ENV`) creates a layer, and the build context is the directory of files those instructions can reach. A `.dockerignore` file keeps the context small and protects against leaking secrets or local artifacts into the image. Instruction order matters because the layer cache invalidates on the first changed step — copying dependency manifests before source code keeps `npm install` or `dotnet restore` cached across edits. `ENTRYPOINT` pins the executable a container runs and is the right default for application images; `CMD` supplies overridable arguments. The multi-stage pattern places build-time tooling in an early stage and copies only the compiled artifacts into a small runtime stage, producing images that are smaller, faster to pull, and carry a smaller attack surface.
