+++
title = "Private Registry and Automated Deployment"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Replace Docker Hub with Azure Container Registry, deploy automatically from GitHub Actions using a service principal, and verify with a smoke test."
weight = 2
draft = false
+++

# Private Registry and Automated Deployment

## Goal

In the previous exercise you built a pipeline that pushes images to Docker Hub and an Azure Container App that pulls them. That gets you a build, but every release still needs a human to run `az containerapp update`, and your image is sitting in a public registry where anyone in the world can pull it. In this exercise you'll move the image into a **private** registry (Azure Container Registry), let the pipeline **deploy** the new image for you, and add a **smoke test** so a broken release fails the build instead of silently going live.

The new picture looks like this: GitHub Actions logs into Azure with a **service principal**, pushes the image into ACR, then issues `az containerapp update` to roll the Container App forward. The Container App itself uses its own **managed identity** to pull from the registry. A second job in the workflow then curls the public FQDN and fails the build if the response isn't `200`.

> **What you'll learn:**
>
> - Why a private registry matters for production and how ACR fits into a CI/CD flow
> - The "robot account" pattern: service principals for non-human auth from outside Azure
> - How `azure/login@v2` consumes service principal JSON to authorize `az` commands in GitHub Actions
> - How to wire an existing Container App to pull from a private registry using a system-assigned managed identity
> - How to deploy a new image from the pipeline with `az containerapp update`
> - Why a smoke test is the missing piece between "image pushed" and "deployment succeeded"

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ A working pipeline from the previous exercise that builds and pushes to Docker Hub
> - ✓ A live Azure Container App pulling from that public Docker Hub image
> - ✓ The Azure CLI signed in (`az account show` returns your subscription)
> - ✓ The GitHub CLI signed in (`gh auth status` reports a logged-in user)
> - ✓ Owner or User Access Administrator rights on the resource group `rg-cicd-week4` (you need to create role assignments)
> - ✓ `jq` installed locally (used to read fields out of JSON output; install with `brew install jq` on macOS or `sudo apt install jq` on Ubuntu)

## Exercise Steps

### Overview

1. **Create the Azure Container Registry**
2. **Create a service principal for the pipeline**
3. **Store the service principal JSON as a GitHub secret**
4. **Update the workflow to push to ACR**
5. **Trigger the pipeline and verify the image lands in ACR**
6. **Reconfigure the Container App to pull from ACR**
7. **Confirm the app still serves from the new registry**
8. **Add an automated deploy step to the workflow**
9. **Push a UI change and watch it deploy automatically**
10. **Add a smoke-test job to the workflow**
11. **Break the deploy briefly to see the smoke test fail**
12. **Test Your Implementation**

### **Step 1:** Create the Azure Container Registry

The registry is the private bucket your pipeline pushes images into and your Container App pulls them out of. ACR is a managed service — you don't run it, you just point Azure resources at it. The Basic SKU is enough for coursework: it's the cheapest tier and the feature set is identical for what we need (push, pull, role-based access).

1. **Make sure** the resource group from the previous exercise exists. The command is idempotent — if it's already there, nothing happens:

   ```bash
   az group create -n rg-cicd-week4 -l northeurope
   ```

2. **Choose** a globally unique ACR name. ACR names must be 5–50 characters, lowercase letters and digits only, and unique across all of Azure. A safe pattern is `acr` plus your initials plus four random digits, for example `acrlap4827`:

   ```bash
   ACR_NAME=acrlap4827
   ```

3. **Create** the registry:

   ```bash
   az acr create -n $ACR_NAME -g rg-cicd-week4 --sku Basic
   ```

4. **Capture** its full resource ID — you'll need it for role assignments in the next step:

   ```bash
   ACR_ID=$(az acr show -n $ACR_NAME -g rg-cicd-week4 --query id -o tsv)
   echo $ACR_ID
   ```

> ℹ **Concept Deep Dive**
>
> A registry is just an OCI-compliant API in front of blob storage. Docker Hub is one; GitHub Container Registry is another; ACR is Azure's. The reason to prefer ACR for production is integration: ACR understands Azure RBAC, supports managed identities for pulling, and lives in the same network and billing context as the workload that consumes it. For a public open-source image, Docker Hub is fine. For a closed-source production image, you want the registry to enforce who can read it.
>
> ⚠ **Common Mistakes**
>
> - The registry name must be globally unique. If `az acr create` fails with `already in use`, pick another name.
> - Hyphens and underscores are not allowed — only lowercase letters and digits.
>
> ✓ **Quick check:** `az acr show -n $ACR_NAME -g rg-cicd-week4` returns JSON describing the registry.

### **Step 2:** Create a service principal for the pipeline

A **service principal** is an Azure-managed account that lives outside Azure — exactly what GitHub Actions needs in order to call `az` commands without a human typing a password. You create it once, hand the credentials to GitHub as a secret, and from then on every workflow run authenticates with it. The trick is to scope it tightly: it should be allowed to push images to *your* ACR and update *your* Container App, and nothing else.

1. **Create** the service principal with permission to push to your registry. The `--json-auth` flag formats the output exactly the way the `azure/login@v2` action expects:

   ```bash
   az ad sp create-for-rbac \
     --name "github-cicd-sp" \
     --role AcrPush \
     --scopes $ACR_ID \
     --json-auth > creds.json
   ```

2. **Look at** the file (you'll delete it in the next step). It contains five fields — `clientId`, `clientSecret`, `subscriptionId`, `tenantId`, and a couple of endpoint URLs:

   ```bash
   cat creds.json
   ```

3. **Capture** the `clientId` and the resource ID of your Container App — you need both for the second role assignment:

   ```bash
   APP_ID=$(jq -r .clientId creds.json)
   CA_ID=$(az containerapp show -g rg-cicd-week4 -n <ca-name> --query id -o tsv)
   ```

   Replace `<ca-name>` with the Container App name you used in the previous exercise.

4. **Grant** the service principal permission to update the Container App:

   ```bash
   az role assignment create \
     --assignee "$APP_ID" \
     --role "Container Apps Contributor" \
     --scope $CA_ID
   ```

> ℹ **Concept Deep Dive**
>
> The service principal needs **two** roles because it does two different things: `AcrPush` lets it push images to the registry, and `Container Apps Contributor` lets it issue `az containerapp update`. Granting both at the smallest possible scope (this one ACR, this one Container App) is the principle of least privilege. If you scoped `AcrPush` to the subscription instead, this principal could push to *every* registry in your subscription — not what you want.
>
> ⚠ **Common Mistakes**
>
> - Granting `Contributor` on the resource group is overkill. Stick with the narrow built-in roles.
> - The `clientSecret` in `creds.json` is a long-lived password. Treat it like one.
>
> ✓ **Quick check:** `az role assignment list --assignee $APP_ID -o table` lists two assignments — one on the ACR, one on the Container App.

### **Step 3:** Store the service principal JSON as a GitHub secret

`creds.json` is a credential. It does not belong in your repo, your shell history, or your screenshots. The right place for it is GitHub Actions secrets, where it's encrypted at rest and only injected as an environment variable at workflow runtime.

1. **Set** the secret directly from the file using the GitHub CLI:

   ```bash
   gh secret set AZURE_CREDENTIALS < creds.json
   ```

2. **Delete** the local file immediately:

   ```bash
   rm creds.json
   ```

3. **Verify** the secret is registered:

   ```bash
   gh secret list
   ```

   You should see `AZURE_CREDENTIALS` in the output. (You can never read its value back — that's by design.)

> ℹ **Concept Deep Dive**
>
> GitHub Actions secrets are encrypted with the repository's public key the moment you set them. Workflow runs see them as environment variables only inside steps that explicitly reference `${{ secrets.NAME }}`. They're masked in logs, but a malicious workflow could still print them — secrets are a deterrent against accidents, not against a hostile pull request.
>
> ⚠ **Common Mistakes**
>
> - Pasting the JSON into the GitHub web UI works but is hard to get right (newlines matter). The `gh secret set ... < file` form is reliable.
> - Forgetting to delete the local file is the single most common way these credentials leak. Get used to deleting it as part of the same command: `gh secret set AZURE_CREDENTIALS < creds.json && rm creds.json`.

### **Step 4:** Update the workflow to push to ACR

The build step doesn't change — Docker doesn't care which registry it's pushing to. What changes is the **login** step (you authenticate to Azure, then have Docker authenticate to ACR through it) and the **image tag** (`<acr-name>.azurecr.io/<repo>:<tag>` instead of `docker.io/<user>/<repo>:<tag>`).

1. **Open** the workflow file from the previous exercise:

   > `.github/workflows/ci.yml`

2. **Replace** the Docker Hub login and build steps with the ACR variants. Set `ACR_NAME` and `IMAGE_NAME` at the top of the job so you can reuse them:

   > `.github/workflows/ci.yml`

   ```yaml
   name: Build and deploy

   on:
     push:
       branches: [main]
     workflow_dispatch:

   env:
     ACR_NAME: acrlap4827
     IMAGE_NAME: cloudci
     RESOURCE_GROUP: rg-cicd-week4
     CONTAINER_APP: ca-cicd-week4

   jobs:
     deploy:
       runs-on: ubuntu-latest
       steps:
         - name: Check out source
           uses: actions/checkout@v4

         - name: Log in to Azure
           uses: azure/login@v2
           with:
             creds: ${{ secrets.AZURE_CREDENTIALS }}

         - name: Log in to ACR
           run: az acr login --name $ACR_NAME

         - name: Build and push image
           run: |
             docker build \
               --build-arg BUILD_SHA=${{ github.sha }} \
               -t $ACR_NAME.azurecr.io/$IMAGE_NAME:${{ github.sha }} \
               -t $ACR_NAME.azurecr.io/$IMAGE_NAME:latest \
               .
             docker push $ACR_NAME.azurecr.io/$IMAGE_NAME:${{ github.sha }}
             docker push $ACR_NAME.azurecr.io/$IMAGE_NAME:latest
   ```

3. **Replace** `acrlap4827` and `ca-cicd-week4` with your actual ACR and Container App names.

4. **Remove** the `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` secrets from the repo — you don't need them anymore:

   ```bash
   gh secret delete DOCKERHUB_USERNAME
   gh secret delete DOCKERHUB_TOKEN
   ```

> ℹ **Concept Deep Dive**
>
> Tagging the image with **both** `${{ github.sha }}` and `latest` is the conventional pattern. The SHA tag is immutable: `acrlap4827.azurecr.io/cloudci:a1b2c3d` will always point to the exact image built from commit `a1b2c3d`. The `latest` tag is a moving pointer to whatever was pushed most recently. You'll deploy by SHA below, because that's the only way to roll back without rebuilding.
>
> ⚠ **Common Mistakes**
>
> - `azure/login@v2` will fail with `Login failed` if the secret JSON is malformed. The `--json-auth` flag in step 2 is what produced the right shape.
> - `az acr login` only works *after* `azure/login` — it reuses the Azure session.

### **Step 5:** Trigger the pipeline and verify the image lands in ACR

The first run of the new workflow proves three things at once: the service principal works, ACR is reachable from GitHub's runners, and your image makes it across.

1. **Commit and push** the workflow change:

   ```bash
   git add .github/workflows/ci.yml
   git commit -m "Switch CI from Docker Hub to ACR"
   git push
   ```

2. **Watch** the run from the terminal:

   ```bash
   gh run watch
   ```

   The job should finish green in a couple of minutes.

3. **Confirm** the image is in ACR:

   ```bash
   az acr repository show-tags -n $ACR_NAME --repository cloudci -o table
   ```

   You should see two tags: `latest` and the commit SHA.

> ✓ **Quick check:** Both tags appear in `az acr repository show-tags`. The Container App is still running the old Docker Hub image — that's expected; you fix it in the next step.

### **Step 6:** Reconfigure the Container App to pull from ACR

The pipeline can now push to a private registry, but the Container App still has its `image:` field pointing at Docker Hub. Worse, even if you change the field, the Container App can't pull from a private ACR without credentials. The right way to give it those credentials is **not** to share the service principal — that's GitHub's account, not the Container App's. Each resource gets its own identity.

The cleanest pattern is a **system-assigned managed identity** on the Container App: an Azure-managed account whose lifecycle is tied to the resource itself. When the Container App is deleted, the identity disappears with it. You grant that identity `AcrPull` on the registry and tell the Container App to use it.

1. **Enable** a system-assigned managed identity on the Container App:

   ```bash
   az containerapp identity assign \
     -g rg-cicd-week4 \
     -n $CONTAINER_APP \
     --system-assigned
   ```

   Replace `$CONTAINER_APP` with your Container App name (or set it: `CONTAINER_APP=ca-cicd-week4`).

2. **Capture** the identity's principal ID:

   ```bash
   PRINCIPAL_ID=$(az containerapp show \
     -g rg-cicd-week4 -n $CONTAINER_APP \
     --query identity.principalId -o tsv)
   ```

3. **Grant** the identity `AcrPull` on the registry:

   ```bash
   az role assignment create \
     --assignee "$PRINCIPAL_ID" \
     --role AcrPull \
     --scope $ACR_ID
   ```

4. **Tell** the Container App to use that identity when pulling from ACR:

   ```bash
   az containerapp registry set \
     -g rg-cicd-week4 \
     -n $CONTAINER_APP \
     --server $ACR_NAME.azurecr.io \
     --identity system
   ```

5. **Update** the Container App's image to the one you just pushed:

   ```bash
   az containerapp update \
     -g rg-cicd-week4 \
     -n $CONTAINER_APP \
     --image $ACR_NAME.azurecr.io/cloudci:latest
   ```

> ℹ **Concept Deep Dive**
>
> There are two non-human accounts in this exercise and they do different jobs. The **service principal** lives outside Azure and is used by GitHub to push images and update the Container App. The **managed identity** lives on the Container App resource and is used by the Container App to pull images. Mixing them up — for example, giving the service principal `AcrPull` and using it as the registry identity — works but is conceptually wrong: you'd be sharing GitHub's credential with an Azure resource that has its own.
>
> ⚠ **Common Mistakes**
>
> - Skipping the `AcrPull` role assignment: the Container App will fail to pull and you'll see `ImagePullBackOff` or a perpetual "activating" status.
> - Forgetting `--identity system` on `containerapp registry set`: the Container App will keep using whatever username/password it was using before (often nothing, if you started from a public Docker Hub image).
>
> ✓ **Quick check:** `az containerapp show -g rg-cicd-week4 -n $CONTAINER_APP --query properties.template.containers[0].image` reports the ACR image path.

### **Step 7:** Confirm the app still serves from the new registry

Before adding more automation, verify the manual cutover worked. The user-facing behaviour shouldn't have changed at all.

1. **Get** the FQDN of the Container App:

   ```bash
   FQDN=$(az containerapp show \
     -g rg-cicd-week4 -n $CONTAINER_APP \
     --query properties.configuration.ingress.fqdn -o tsv)
   echo https://$FQDN
   ```

2. **Curl** the home page and confirm a `200`:

   ```bash
   curl -I https://$FQDN/
   ```

3. **Open** the URL in a browser. The app should look identical to before — same SHA badge, same content.

> ✓ **Quick check:** `curl -I` returns `HTTP/2 200`. The app loads in the browser.

### **Step 8:** Add an automated deploy step to the workflow

Right now your pipeline pushes a new image but doesn't tell the Container App to use it. Adding one more step closes the loop. The step issues `az containerapp update` with the SHA-tagged image — the same SHA that was just built — so the version that's live is unambiguous.

1. **Open** the workflow file again:

   > `.github/workflows/ci.yml`

2. **Add** a new step after the build/push step:

   > `.github/workflows/ci.yml`

   ```yaml
         - name: Deploy to Container App
           run: |
             az containerapp update \
               -g $RESOURCE_GROUP \
               -n $CONTAINER_APP \
               --image $ACR_NAME.azurecr.io/$IMAGE_NAME:${{ github.sha }}
   ```

3. **Commit and push:**

   ```bash
   git add .github/workflows/ci.yml
   git commit -m "Auto-deploy to Container App after push"
   git push
   ```

> ℹ **Concept Deep Dive**
>
> Deploying by SHA, not by `:latest`, is what makes this pipeline auditable. If something goes wrong in production at 14:32, you can read the GitHub Actions log, find which commit was built at 14:30, and roll back by issuing `az containerapp update --image ...:<previous-sha>`. With `:latest` you'd have no idea which image is actually running.
>
> ⚠ **Common Mistakes**
>
> - Deploying with `--image ...:latest`: works, but defeats traceability and makes rollback impossible without rebuild.
> - Forgetting that `az containerapp update` is asynchronous: the command returns when Azure accepts the request, not when the new revision is healthy. The smoke test in step 10 covers that gap.

### **Step 9:** Push a UI change and watch the pipeline deploy automatically

This is the moment the pipeline pays off. From now on, every commit on `main` flows all the way to production without you typing `az` once.

1. **Edit** the home page heading. Open `Views/Home/Index.cshtml` and change something visible — for example the welcome heading.

2. **Commit and push:**

   ```bash
   git add Views/Home/Index.cshtml
   git commit -m "Tweak home page heading"
   git push
   ```

3. **Watch** the pipeline:

   ```bash
   gh run watch
   ```

4. **Refresh** the FQDN in your browser. Your change is live.

> ✓ **Quick check:** The new heading appears at `https://$FQDN/` within a minute or two of the workflow finishing.

### **Step 10:** Add a smoke-test job to the workflow

`az containerapp update` returning success doesn't mean the new revision is actually serving traffic. Maybe the image starts but throws on the first request. Maybe a config value is missing. A **smoke test** is the cheapest possible safety net: hit the public URL after deploy, fail the pipeline if it doesn't return `200`. That single check catches a surprising fraction of bad releases.

1. **Open** the workflow file:

   > `.github/workflows/ci.yml`

2. **Add** a second job below the existing `deploy` job. Use `needs: deploy` to make it wait. Replace `<your-fqdn>` with the FQDN you captured in step 7:

   > `.github/workflows/ci.yml`

   ```yaml
     smoke-test:
       runs-on: ubuntu-latest
       needs: deploy
       steps:
         - name: Curl the home page
           run: |
             curl --fail \
               --max-time 30 \
               --retry 5 \
               --retry-delay 5 \
               --retry-all-errors \
               https://<your-fqdn>/
   ```

3. **Commit and push:**

   ```bash
   git add .github/workflows/ci.yml
   git commit -m "Add smoke test after deploy"
   git push
   ```

4. **Watch** the run. Both jobs should turn green:

   ```bash
   gh run watch
   ```

> ℹ **Concept Deep Dive**
>
> `--fail` makes `curl` return a non-zero exit code on any HTTP error (4xx, 5xx) — without it, `curl` happily prints the error page and exits `0`. `--retry-all-errors` plus `--retry-delay 5` gives the new revision a fair chance to come up: Container Apps can take 30–60 seconds to swap revisions in production. `needs: deploy` is the GitHub Actions way to express "don't run this until the deploy job succeeds" — if deploy fails, smoke-test is skipped, and the workflow as a whole is marked failed.
>
> ⚠ **Common Mistakes**
>
> - Calling `curl https://...` without `--fail`: a 500 response will pass.
> - Forgetting `--max-time`: a hung server will spin the runner forever.
> - Hard-coding the FQDN here is fine for one Container App. For multiple environments, pass it via a workflow output or `vars`.

### **Step 11:** Break the deploy briefly to see the smoke test fail

A safety net you've never seen catch anything is one you don't trust. Force a failure on purpose, watch the smoke test fire, then restore.

1. **Edit** the workflow file. In the `Deploy to Container App` step, change the image tag from `${{ github.sha }}` to a tag that doesn't exist, e.g. `does-not-exist`:

   > `.github/workflows/ci.yml`

   ```yaml
         - name: Deploy to Container App
           run: |
             az containerapp update \
               -g $RESOURCE_GROUP \
               -n $CONTAINER_APP \
               --image $ACR_NAME.azurecr.io/$IMAGE_NAME:does-not-exist
   ```

2. **Commit and push.** Watch the run:

   ```bash
   git add .github/workflows/ci.yml
   git commit -m "Intentionally break deploy to test smoke gate"
   git push
   gh run watch
   ```

3. **Observe** what happens. The `deploy` job may itself fail (the image isn't in the registry), or it may succeed but the new revision will fail to start. Either way, the `smoke-test` job either fails (curl gets a non-200) or is skipped (deploy didn't succeed). The workflow as a whole turns red.

4. **Restore** the working tag and push again:

   > `.github/workflows/ci.yml`

   ```yaml
         - name: Deploy to Container App
           run: |
             az containerapp update \
               -g $RESOURCE_GROUP \
               -n $CONTAINER_APP \
               --image $ACR_NAME.azurecr.io/$IMAGE_NAME:${{ github.sha }}
   ```

   ```bash
   git add .github/workflows/ci.yml
   git commit -m "Restore working deploy"
   git push
   ```

> ✓ **Quick check:** The deliberately-broken run is red on GitHub's run list; the recovery run is green. The Container App keeps serving the previous good revision throughout — broken deploys do not bring the site down.

### **Step 12:** Test Your Implementation

Step back and verify the whole pipeline behaves the way you expect.

1. **Run** the workflow once more by pushing a trivial change (for example, edit a comment in `Views/Home/Index.cshtml`):

   ```bash
   git commit --allow-empty -m "Trigger pipeline"
   git push
   gh run watch
   ```

2. **Confirm** both jobs ran:

   - `deploy` is green
   - `smoke-test` is green

3. **Inspect** ACR. The new SHA should be present:

   ```bash
   az acr repository show-tags -n $ACR_NAME --repository cloudci -o table
   ```

4. **Inspect** the running Container App. The image should match the SHA you just pushed:

   ```bash
   az containerapp show \
     -g rg-cicd-week4 -n $CONTAINER_APP \
     --query properties.template.containers[0].image -o tsv
   ```

5. **Visit** `https://$FQDN/` — the app responds and (if your home page shows the SHA badge from the previous exercise) the badge matches the new commit.

6. **Reflect** on the secret you stored. `AZURE_CREDENTIALS` is a JSON blob containing a long-lived `clientSecret` — a password, effectively. Anyone who exfiltrates it (a compromised dependency in a workflow, a copy-pasted log) can push images to your registry and update your Container App until you rotate it. The next exercise removes that risk by replacing the long-lived secret with short-lived OIDC tokens that GitHub mints on every run.

> ✓ **Success indicators:**
>
> - The pipeline pushes to ACR (not Docker Hub) and the registry contains both `latest` and a SHA tag for the latest commit
> - The Container App pulls from ACR using its system-assigned managed identity
> - Pushing to `main` deploys automatically — no manual `az containerapp update`
> - The smoke test fails the pipeline when the deploy is broken
> - The home page reflects the latest commit's UI changes within minutes of `git push`
>
> ✓ **Final verification checklist:**
>
> - ☐ ACR exists and holds at least two tags
> - ☐ Service principal has only `AcrPush` on the registry and `Container Apps Contributor` on the Container App
> - ☐ `AZURE_CREDENTIALS` is set as a GitHub secret; `creds.json` is not on disk
> - ☐ Container App has system-assigned managed identity with `AcrPull` on the registry
> - ☐ Workflow has a deploy step that uses the SHA tag
> - ☐ Workflow has a `smoke-test` job with `needs: deploy`
> - ☐ A green pipeline run exists with both jobs passing

## Common Issues

> **If you encounter problems:**
>
> **`azure/login@v2` fails with "Login failed":** The `AZURE_CREDENTIALS` secret is malformed. Re-run `az ad sp create-for-rbac --json-auth` and re-set the secret with `gh secret set AZURE_CREDENTIALS < creds.json`.
>
> **`az acr login` fails with "unauthorized":** The service principal lacks `AcrPush` on the registry. Confirm with `az role assignment list --assignee $APP_ID --scope $ACR_ID`.
>
> **`az containerapp update` succeeds but the app shows the old content:** Container Apps swap revisions asynchronously. Wait a minute and refresh. If the new revision is unhealthy, the platform keeps the old one serving traffic — check `az containerapp revision list` for one with `Failed` provisioning state.
>
> **Container App shows "ImagePullBackOff" or stays in "Activating":** The Container App's managed identity doesn't have `AcrPull` on the registry, or the `--identity system` flag wasn't used on `az containerapp registry set`. Re-run both commands from step 6.
>
> **Smoke test fails with "curl: (28) Operation timed out":** The Container App's new revision didn't come up in time. Increase `--max-time`, or check the app's logs with `az containerapp logs show -g rg-cicd-week4 -n $CONTAINER_APP --follow`.
>
> **Still stuck?** Re-read the role assignments in step 2 and step 6. Most failures in this exercise are RBAC mismatches: the principal isn't who you think it is, or it doesn't have the role you think it has, or it has it on the wrong scope.

## Summary

You replaced a public registry with a private one, gave GitHub Actions a scoped identity to push and deploy, gave the Container App its own identity to pull, and put a smoke test in front of the live URL. The pipeline now does end-to-end what it should: every push results in a tagged image in ACR, a Container App revision running that exact image, and an automated check that the app is actually serving.

- ✓ Private registry (ACR) instead of public Docker Hub
- ✓ Service principal for non-human auth from outside Azure
- ✓ System-assigned managed identity for the Container App's pull
- ✓ Auto-deploy step using the immutable SHA tag
- ✓ Smoke test gating the pipeline on real HTTP behaviour

> **Key takeaway:** A pipeline that pushes an image is half a pipeline. The other half is deploying that image and proving the deploy worked. Until both halves are automated, "shipping to production" is still a manual ritual — and it stays unreliable as long as humans are in the loop for the routine path.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Add a manual approval gate before the deploy job (GitHub Environments) so production releases require a click
> - Read the Container App revision logs after a deploy and surface them as a workflow artifact
> - Replace `:latest` everywhere with the SHA tag and observe how rollback becomes a one-line command
> - Investigate `az containerapp revision copy` for blue/green deploys without downtime

## Done!

The pipeline now ships code on its own. There's still one weak link: the `AZURE_CREDENTIALS` secret is a long-lived password sitting in your repository's secret store. The next exercise removes it entirely by replacing it with **OpenID Connect federation** — short-lived tokens minted per workflow run, tied to your specific repository and branch.
