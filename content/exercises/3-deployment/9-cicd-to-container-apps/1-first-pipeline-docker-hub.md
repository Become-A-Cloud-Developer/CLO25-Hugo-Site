+++
title = "First Pipeline: Build and Push to Docker Hub"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Build a .NET MVC app, containerize it, push the image to Docker Hub from GitHub Actions, then run it on Azure Container Apps."
weight = 1
draft = false
+++

# First Pipeline: Build and Push to Docker Hub

## Goal

Stand up the smallest possible CI/CD pipeline that takes a fresh ASP.NET Core MVC app, packages it into a Docker image, pushes that image to Docker Hub on every commit to `main`, and runs it on Azure Container Apps. The deploy step is intentionally manual today — you'll wire up the missing automation in the next exercise.

> **What you'll learn:**
>
> - How a GitHub Actions workflow file describes a build pipeline as a YAML recipe
> - How GitHub Secrets let the runner authenticate to external services without leaking credentials
> - Why a Personal Access Token replaces your Docker Hub password for automated logins
> - How a multi-stage Dockerfile produces a small, secure runtime image
> - How Azure Container Apps pulls a public image and serves it at a stable FQDN

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ .NET 10 SDK installed (`dotnet --version` reports a `10.*` version)
> - ✓ Docker Desktop running locally (`docker --version` works)
> - ✓ A free Docker Hub account (<https://hub.docker.com>)
> - ✓ The GitHub CLI installed and authenticated (`gh auth status` reports logged in)
> - ✓ An Azure subscription where you can create resource groups (the free tier is enough)
> - ✓ Git installed and a working terminal in an empty folder for this exercise

## Exercise Steps

### Overview

1. **Scaffold** a fresh ASP.NET Core MVC project
2. **Add a multi-stage Dockerfile** and a `.dockerignore`
3. **Surface the build SHA** on the homepage so you can see the new revision land
4. **Build and run** the container locally to confirm it works
5. **Initialize Git** and push to a new GitHub repository
6. **Generate a Docker Hub access token**
7. **Add the token** as a GitHub Actions secret
8. **Write the GitHub Actions workflow**
9. **Trigger the workflow** and confirm the image appears on Docker Hub
10. **Create the Azure Container App** from the Docker Hub image
11. **Open the FQDN** and verify the live page
12. **Push a change** and manually update the Container App revision
13. **Test Your Implementation**

### **Step 1:** Scaffold a fresh ASP.NET Core MVC project

Start from the default MVC template. The scaffold gives you a working app — a home controller, a shared layout, and Bootstrap pre-wired — without distractions. The CI/CD work in this exercise is independent of the application itself; using the default template means the only thing that can break is the pipeline.

1. **Create** the project from the MVC template:

   ```bash
   dotnet new mvc -o CloudCi --framework net10.0
   cd CloudCi
   ```

2. **Verify** it builds and runs locally:

   ```bash
   dotnet run --launch-profile http
   ```

   Opening `http://localhost:5000` (or whatever port the launch profile reports) should show the default MVC welcome page. Stop the server with `Ctrl+C`.

> ✓ **Quick check:** The project builds and the welcome page renders.

### **Step 2:** Add a multi-stage Dockerfile and a .dockerignore

The container is the unit of deployment for the rest of this chapter. A multi-stage Dockerfile uses one image to compile the app and a different, much smaller image to run it. The build stage pulls in the .NET SDK (well over a gigabyte); the runtime stage carries only the ASP.NET runtime, so the final image you ship is small and contains no compilers or build tools.

1. **Create** a new file at the project root:

   > `CloudCi/Dockerfile`

   ```dockerfile
   # Build stage — has the SDK, compilers, NuGet, etc.
   FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
   WORKDIR /src

   # Copy csproj first so layer caching kicks in on dependency changes.
   COPY *.csproj ./
   RUN dotnet restore

   # Copy the rest of the source and publish a release build.
   COPY . ./
   RUN dotnet publish -c Release -o /app/publish --no-restore

   # Runtime stage — only the ASP.NET runtime, no build tools.
   FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
   WORKDIR /app
   COPY --from=build /app/publish ./

   # Bake the build commit SHA into the image at build time.
   ARG BUILD_SHA=local
   ENV BUILD_SHA=$BUILD_SHA

   # Listen on :8080 — Container Apps maps this to the public ingress.
   ENV ASPNETCORE_URLS=http://+:8080
   EXPOSE 8080

   ENTRYPOINT ["dotnet", "CloudCi.dll"]
   ```

2. **Create** a `.dockerignore` at the project root so the build context stays small:

   > `CloudCi/.dockerignore`

   ```text
   bin/
   obj/
   .vs/
   .vscode/
   .idea/
   .git/
   .gitignore
   *.user
   Dockerfile
   .dockerignore
   ```

> ℹ **Concept Deep Dive**
>
> Why a multi-stage Dockerfile? The .NET SDK image is over a gigabyte and ships with compilers, NuGet caches, and build tools. None of that is useful — or safe — at runtime. The `aspnet` image weighs in at a few hundred megabytes and contains only the runtime. Multi-stage builds let you do the heavy lifting in the first stage and then `COPY --from=build` only the published output into a clean second stage, so the final image is small and has a much smaller attack surface.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `EXPOSE 8080` and `ASPNETCORE_URLS=http://+:8080` means the app listens on `localhost:5000` inside the container and the port mapping silently does nothing.
> - Without a `.dockerignore`, the build context includes `bin/` and `obj/` from your local machine, which can confuse layer caching and bloat the image.
>
> ✓ **Quick check:** `Dockerfile` and `.dockerignore` exist in the project root.

### **Step 3:** Surface the build SHA on the homepage

You need a visible cue that proves the running container is the one your pipeline just built. The simplest cue is to display the Git commit SHA on the homepage. The Dockerfile already declared `BUILD_SHA` as a build argument that becomes a runtime environment variable; now read that variable in the view.

1. **Open** the existing `Views/Home/Index.cshtml`

2. **Replace** the file contents with:

   > `CloudCi/Views/Home/Index.cshtml`

   ```html
   @{
       ViewData["Title"] = "Home";
       var buildSha = Environment.GetEnvironmentVariable("BUILD_SHA") ?? "local";
       var hostName = Environment.MachineName;
   }

   <div class="text-center">
       <h1 class="display-4">Welcome</h1>
       <p>Hello from CloudCi.</p>

       <div class="mt-4">
           <span class="badge bg-secondary" data-testid="build-sha">
               build: @buildSha
           </span>
           <span class="badge bg-secondary" data-testid="host-name">
               host: @hostName
           </span>
       </div>
   </div>
   ```

> ℹ **Concept Deep Dive**
>
> What does `--build-arg` do? Arguments declared with `ARG` in the Dockerfile are available only during `docker build`; they are not part of the running image unless you copy them into an `ENV`. The line `ENV BUILD_SHA=$BUILD_SHA` pins the build-time value into the runtime environment, so the running container can read it like any other environment variable. The workflow will pass `--build-arg BUILD_SHA=${{ github.sha }}` so each image carries the commit it was built from.
>
> ✓ **Quick check:** Running `dotnet run` and visiting the homepage shows two badges — `build: local` and `host: <your machine>`.

### **Step 4:** Build and run the container locally

Before you push anything to the cloud, prove the image actually works on your laptop. This is a tight feedback loop: a broken Dockerfile here is much cheaper to debug than a broken image discovered in Container Apps.

1. **Build** the image, passing a placeholder commit SHA:

   ```bash
   docker build --build-arg BUILD_SHA=dev-local -t cloudci:local .
   ```

2. **Run** the container, mapping port 8080 on your host to port 8080 inside the container:

   ```bash
   docker run --rm -p 8080:8080 cloudci:local
   ```

3. **Browse to** `http://localhost:8080` and confirm the homepage renders with the badge `build: dev-local`.

4. **Stop** the container with `Ctrl+C`.

> ⚠ **Common Mistakes**
>
> - If `docker run` exits immediately, check the build output for compilation errors and confirm `EXPOSE 8080` and `ASPNETCORE_URLS` are set in the Dockerfile.
> - If the page loads but the badge says `build: local`, you forgot the `--build-arg`. Rebuild.
>
> ✓ **Quick check:** `http://localhost:8080` renders the homepage with the `build: dev-local` badge.

### **Step 5:** Initialize Git and push to a new GitHub repository

The pipeline triggers on `push` to the `main` branch, so you need a remote first. Use the GitHub CLI to create the repository and push your initial commit in one shot.

1. **Initialize** Git and make the first commit:

   ```bash
   git init -b main
   git add .
   git commit -m "Initial scaffold with Dockerfile"
   ```

2. **Create** a new public GitHub repository from the current folder and push:

   ```bash
   gh repo create --public --source=. --remote=origin --push
   ```

3. **Confirm** the push by visiting the repository URL the CLI prints, or with:

   ```bash
   gh repo view --web
   ```

> ✓ **Quick check:** The repository exists on GitHub with your initial commit visible.

### **Step 6:** Generate a Docker Hub access token

A Personal Access Token (PAT) is what your GitHub Actions runner will use to authenticate to Docker Hub. Tokens are scoped — you can grant just the permissions the pipeline needs — and revocable, so leaking one is not the same as leaking your account password.

1. **Sign in** at <https://hub.docker.com/> with your account.

2. **Open** Account Settings: click your avatar (top right) → **Account settings** → **Personal access tokens**.

3. **Click** **Generate new token**.

4. **Fill in** the form:

   - **Description:** `cloudci-github-actions`
   - **Access permissions:** **Read, Write, Delete**
   - **Expiration:** the shortest period that covers this course (typically 60 or 90 days)

5. **Click** **Generate** and **copy the token**. You will not see it again — paste it into a temporary scratch file or directly into the next step.

> ℹ **Concept Deep Dive**
>
> Why a PAT instead of your Docker Hub password? Tokens carry their own audit trail and can be revoked individually, so you can rotate the GitHub Actions credential without touching anything else. They are also scoped — a leaked Read/Write/Delete token can push to your registries but cannot, for example, change your account email or delete your account. Treat tokens as you would a SSH key: one per consumer, never reused across services.
>
> ⚠ **Common Mistakes**
>
> - Confusing your Docker Hub **username** with your **email address**. Docker login takes the username (the one in your profile URL), not the email.
> - Closing the dialog before copying the token. If that happens, delete the token and generate a new one.

### **Step 7:** Add the token as a GitHub Actions secret

GitHub Secrets are encrypted environment variables that are only injected into workflow runs. The runner can read them, but they are masked in logs and unavailable to anyone reading the repository or pull requests from forks. Add both your Docker Hub username and the token you just generated.

1. **Set** the username secret. Replace the placeholder with your real Docker Hub username:

   ```bash
   gh secret set DOCKERHUB_USERNAME --body "<your-dockerhub-username>"
   ```

2. **Set** the token secret. The CLI will prompt you to paste it:

   ```bash
   gh secret set DOCKERHUB_TOKEN
   ```

   Paste the PAT and press Enter. The CLI does not echo it back.

3. **Verify** both secrets exist:

   ```bash
   gh secret list
   ```

   You should see `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN`.

> ⚠ **Common Mistakes**
>
> - Pushing the workflow file before adding the secrets — the run will fire and fail authentication. Add the secrets first, then write the workflow.
> - Pasting the PAT into the workflow YAML directly. Anything in YAML is in Git history forever.

### **Step 8:** Write the GitHub Actions workflow

The workflow is a YAML recipe stored in `.github/workflows/`. GitHub Actions watches that folder and runs whatever it finds there in response to the configured trigger. This first version does the bare minimum: check out the code, log in to Docker Hub, build the image, and push it under two tags — the immutable commit SHA and the mutable `latest`.

1. **Create** the workflow folder and file:

   ```bash
   mkdir -p .github/workflows
   ```

2. **Add** the workflow file:

   > `CloudCi/.github/workflows/ci.yml`

   ```yaml
   name: ci

   on:
     push:
       branches: [main]

   jobs:
     build-and-push:
       runs-on: ubuntu-latest
       steps:
         - name: Check out the repository
           uses: actions/checkout@v4

         - name: Log in to Docker Hub
           uses: docker/login-action@v3
           with:
             username: ${{ secrets.DOCKERHUB_USERNAME }}
             password: ${{ secrets.DOCKERHUB_TOKEN }}

         - name: Build and push image
           uses: docker/build-push-action@v6
           with:
             context: .
             push: true
             build-args: |
               BUILD_SHA=${{ github.sha }}
             tags: |
               ${{ secrets.DOCKERHUB_USERNAME }}/cloudci:latest
               ${{ secrets.DOCKERHUB_USERNAME }}/cloudci:${{ github.sha }}
   ```

3. **Commit and push** the workflow:

   ```bash
   git add .github/workflows/ci.yml
   git commit -m "Add CI workflow that pushes to Docker Hub"
   git push
   ```

> ℹ **Concept Deep Dive**
>
> Why two tags? The `:latest` tag is mutable — it always points at the most recent build. That is convenient for the Container App's "always pull the newest" mode, but useless for forensics: there is no record of which commit produced the image currently running in production. The immutable `:${{ github.sha }}` tag pins each image to the exact commit it was built from, so rolling back is just a question of pointing the deployment at an older SHA tag.
>
> ⚠ **Common Mistakes**
>
> - Misindented YAML. Use a real editor with YAML support; one stray space breaks the file silently and the workflow run will refuse to start.
> - Forgetting `push: true` on `docker/build-push-action`. The default is to build only — the image will be made and immediately discarded.

### **Step 9:** Trigger the workflow and confirm the image on Docker Hub

The push you just did already triggered a run. Watch it complete and verify the resulting image lands in your Docker Hub repository.

1. **Watch** the most recent run from the terminal:

   ```bash
   gh run watch
   ```

   Or open the **Actions** tab on the repository in a browser and click into the latest run.

2. **Confirm** the run finished green. The Build and push image step should report two tags pushed.

3. **Visit** your Docker Hub repository in a browser:

   ```text
   https://hub.docker.com/r/<your-dockerhub-username>/cloudci/tags
   ```

   You should see two tags: `latest` and a long hex tag matching your commit SHA.

> ⚠ **Common Mistakes**
>
> - The image does not appear and the run reports `denied: requested access to the resource is denied`. Check that the `DOCKERHUB_USERNAME` secret matches the namespace in the tag (it must — the workflow uses one to build the other).
> - The repository on Docker Hub is created on first push and defaults to **public** for free accounts. If you set the namespace to one you do not own, the push fails. Always use your own username.

### **Step 10:** Create the Azure Container App from the Docker Hub image

Container Apps is Azure's serverless container platform. Give it a public image reference, an ingress port, and a scale rule, and it produces a stable FQDN that serves the image. There are several ways to create one — this exercise uses the Portal so you can see every knob the platform exposes.

1. **Sign in** at <https://portal.azure.com>.

2. **Search** for **Container Apps** in the top bar and click **+ Create**.

3. **Basics tab — Project details:**

   - **Subscription:** your subscription
   - **Resource group:** click **Create new**, name it `rg-cicd-week4`
   - **Container app name:** `ca-cicd-week4`
   - **Region:** `North Europe`

4. **Basics tab — Container Apps Environment:**

   - Click **Create new** next to the Environment dropdown
   - **Environment name:** `cae-cicd-week4`
   - Leave the rest at defaults and click **Create**

5. **Click** **Next: Container >**.

6. **Container tab:**

   - **Uncheck** **Use quickstart image**
   - **Name:** `cloudci`
   - **Image source:** **Docker Hub or other registries**
   - **Image type:** **Public**
   - **Registry login server:** `docker.io`
   - **Image and tag:** `<your-dockerhub-username>/cloudci:latest`
   - Leave CPU and memory at defaults

7. **Click** **Next: Ingress >**.

8. **Ingress tab:**

   - **Ingress:** **Enabled**
   - **Ingress traffic:** **Accepting traffic from anywhere**
   - **Ingress type:** **HTTP**
   - **Target port:** `8080`

9. **Click** **Review + create**, then **Create**. Provisioning takes a couple of minutes.

10. **When deployment finishes**, click **Go to resource** and copy the **Application Url** from the Overview page. That is the FQDN your Container App is serving on.

> ⚠ **Common Mistakes**
>
> - Leaving **Target port** at the default 80. The container listens on 8080, so any other value sends traffic to a closed socket and produces a 502 in the browser.
> - Picking a region that does not yet have Container Apps capacity. North Europe and West Europe are reliable choices for this course.
> - Forgetting to switch **Image source** away from the quickstart image. The Microsoft sample image will provision and serve happily, but it is not your image.

### **Step 11:** Open the FQDN in a browser

This is the moment that turns CI/CD from theory into something tangible.

1. **Browse to** the Application Url you copied in the previous step.

2. **Confirm** the homepage renders.

3. **Confirm** the `build:` badge shows the commit SHA from your most recent push (a long hex string), not `local` or `dev-local`. That proves the image running in Azure is the one your pipeline built.

4. **Confirm** the `host:` badge shows a generated container name (something like `ca-cicd-week4--xxxxxxx`). That is the replica's hostname inside the Container Apps environment.

> ✓ **Quick check:** The page loads and the `build:` badge matches the SHA of the commit shown at `gh repo view --web` → latest commit.

### **Step 12:** Push a change and manually update the Container App

You now have a working build pipeline and a running app, but no automated deploy. Make a code change, push it, watch the new image appear on Docker Hub, and then — by hand — point the Container App at the new tag. This is the step that should feel awkward; the next exercise removes it.

1. **Edit** the homepage greeting:

   > `CloudCi/Views/Home/Index.cshtml`

   ```html
   <h1 class="display-4">Welcome (v2)</h1>
   <p>Hello again from CloudCi.</p>
   ```

2. **Commit and push:**

   ```bash
   git add Views/Home/Index.cshtml
   git commit -m "Update homepage greeting to v2"
   git push
   ```

3. **Watch** the new workflow run with `gh run watch` and confirm it ends green.

4. **Confirm** a new SHA tag appeared at `https://hub.docker.com/r/<your-dockerhub-username>/cloudci/tags`.

5. **Refresh** the Container App's Application Url. You will most likely still see the old greeting — Container Apps caches the resolved image digest of `:latest` and does not re-pull on its own.

6. **Force** the new image to roll out via the Portal:

   - Open the Container App **ca-cicd-week4** in the Portal
   - In the left menu, click **Revisions and replicas**
   - Click **Create new revision**
   - Click into the **cloudci** container row, set the **Image and tag** to the new SHA tag (or keep `:latest` and Azure will re-resolve the digest)
   - Click **Save** at the container level, then **Create** at the revision level

7. **Refresh** the Application Url again. The new greeting **Welcome (v2)** appears.

> ℹ **Concept Deep Dive**
>
> Why is this not real CD yet? The build half is automated — every commit produces an image. The deploy half is a Portal click. Until something tells Container Apps to roll out a new revision, the production environment keeps serving the old image regardless of what is on Docker Hub. The next exercise replaces that click with another GitHub Actions step that calls the Azure CLI; it also switches from the public Docker Hub registry to a private Azure Container Registry, since most production images are not something you want the whole internet to be able to pull.
>
> ⚠ **Common Mistakes**
>
> - Refreshing the FQDN immediately after the workflow finishes and concluding the deploy "worked." The page still shows the old greeting; Container Apps did not re-pull. The point of this step is to feel that gap, not paper over it.
> - Creating a new revision but leaving the image tag pointing at the old SHA. Always confirm the tag in the revision editor matches the latest run.

### **Step 13:** Test Your Implementation

Walk through the full pipeline once more from a clean slate to make sure every piece works end to end.

1. **Make** a third visible code change in `Views/Home/Index.cshtml` — for example change the `<p>` text to `Pipeline run #3.`

2. **Commit and push:**

   ```bash
   git add Views/Home/Index.cshtml
   git commit -m "Pipeline run 3"
   git push
   ```

3. **Watch** the workflow:

   ```bash
   gh run watch
   ```

4. **Verify** the Docker Hub repository at `https://hub.docker.com/r/<your-dockerhub-username>/cloudci/tags` lists a fresh SHA tag matching `git rev-parse HEAD`.

5. **Create** a new revision in the Container App pointing at the new SHA (or `:latest`).

6. **Browse** to the Application Url and verify the new copy is live.

7. **Stop** any local containers you no longer need:

   ```bash
   docker ps
   docker stop <container-id>
   ```

> ✓ **Success indicators:**
>
> - Every `git push` to `main` produces a green Actions run that ends in two new Docker Hub tags
> - The build SHA shown in the page badge matches the Git commit you pushed
> - Updating the Container App revision rolls the running app forward
> - No credentials appear in the workflow file, the repository, or the logs
>
> ✓ **Final verification checklist:**
>
> - ☐ `Dockerfile` and `.dockerignore` exist at the project root
> - ☐ `Views/Home/Index.cshtml` displays the `BUILD_SHA` badge
> - ☐ GitHub repository contains `.github/workflows/ci.yml`
> - ☐ `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` exist in `gh secret list`
> - ☐ Docker Hub repository `<your-dockerhub-username>/cloudci` shows `latest` plus one tag per commit
> - ☐ Azure Container App is running and serving the latest image at its FQDN
> - ☐ Manually creating a new revision in the Portal rolls the homepage forward

## Common Issues

> **If you encounter problems:**
>
> **`denied: requested access to the resource is denied` in the Actions log:** Either the `DOCKERHUB_USERNAME` does not match the image namespace in the tag, or the PAT was revoked or has expired. Regenerate the token, update the secret, and re-run.
>
> **Workflow run never starts:** YAML indentation is almost always the culprit. Open the file in an editor with YAML support and look for tabs (use spaces only) or misaligned `with:` blocks.
>
> **Container App returns 502 Bad Gateway:** The target port is wrong. Open the Container App → Ingress and confirm **Target port** is `8080`, not the default `80`.
>
> **Container App returns 404 on the FQDN:** Ingress is disabled, or the revision is unhealthy. Check Revisions and replicas — if a revision is showing 0 replicas, click into it and read the system logs.
>
> **The page renders but the badge still says `build: local`:** The `--build-arg` is not being passed. Check the `build-args` block in the workflow has `BUILD_SHA=${{ github.sha }}` on its own line and that the indentation matches.
>
> **Still stuck?** Re-run the workflow from the Actions tab with **Re-run jobs → Enable debug logging**. The verbose output usually points straight at the problem.

## Summary

You've built the smallest CI/CD pipeline that does something useful:

- ✓ A multi-stage Dockerfile produces a small runtime image with the build commit baked in
- ✓ A GitHub Actions workflow logs in to Docker Hub with a scoped token and pushes the image on every commit
- ✓ Azure Container Apps serves that image at a stable public FQDN
- ✓ The build SHA badge on the homepage gives you a one-glance answer to "is the new code live yet?"

> **Key takeaway:** A pipeline is just a script that runs on someone else's computer with credentials you trusted it with. The interesting design decisions are about which credentials to hand over, how to scope them, and where the manual steps remain. You found one of those manual steps in the deploy half — that is the gap the next exercise closes.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Add a second job to the workflow that runs `dotnet test` before the image is built, so a failing test blocks the push
> - Replace the `:latest` tag with a Git tag-driven release scheme (`v1.0.0`, `v1.0.1`) and configure the Container App to follow a specific tag
> - Read the Docker Hub rate-limit policy for anonymous pulls — Container Apps will hit it eventually if you scale out
> - Inspect the final image's contents with `docker run --rm -it cloudci:local sh` and see what is actually shipped

## Done!

The pipeline builds and the app runs. Next exercise: switch to a private Azure Container Registry and let the workflow trigger the deploy itself, so a `git push` to `main` is all it takes to roll a new revision into production.
