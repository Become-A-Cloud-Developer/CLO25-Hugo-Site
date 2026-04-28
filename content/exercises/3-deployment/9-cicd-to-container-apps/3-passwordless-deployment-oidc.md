+++
title = "Passwordless Deployment with OIDC Federation"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Replace the AZURE_CREDENTIALS secret with OpenID Connect federation between GitHub Actions and Microsoft Entra ID. The pipeline still deploys, but no long-lived password exists."
weight = 3
draft = false
+++

# Passwordless Deployment with OIDC Federation

## Goal

In the previous exercise you stored a service principal's client secret as `AZURE_CREDENTIALS` in GitHub. The pipeline worked, but the secret was a long-lived password sitting in two places (Azure and GitHub) with no automatic rotation and no scoping beyond "whoever holds it can deploy." In this exercise you'll replace that password with a *trust relationship* — and the pipeline keeps working.

You will configure **OpenID Connect (OIDC) federation** between GitHub Actions and Microsoft Entra ID. On every workflow run, GitHub mints a short-lived token describing the run (which repo, which branch, which event). Azure trusts that token because of a federated credential you registered up front. No password ever leaves Azure; no password ever sits in GitHub.

> **What you'll learn:**
>
> - How OIDC federation replaces a long-lived secret with a short-lived token exchange
> - The difference between an Entra **app registration**, a **service principal**, and a **federated credential**
> - How the `subject` claim scopes who is allowed to authenticate as your service principal
> - Why GitHub workflows need `permissions: id-token: write` to participate
> - How `azure/login@v2` switches from secret-based to federated authentication

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Completed the previous exercise: a working pipeline that uses `AZURE_CREDENTIALS` to push to ACR and update a Container App on every push to `main`
> - ✓ Owner (or User Access Administrator) on the subscription, **and** permission to create app registrations in Entra ID
> - ✓ `az` CLI logged in (`az login`) and `gh` CLI logged in (`gh auth status`)
> - ✓ Your existing ACR name, Container App name, and resource group name handy
> - ✓ Your GitHub repository slug in the form `<your-username>/<your-repo>`

## Exercise Steps

### Overview

1. **Confirm your Azure permissions**
2. **Create the Entra app registration**
3. **Create a service principal for the app**
4. **Assign the deployment roles to the new identity**
5. **Create the federated credential for `main`**
6. **Add the three new GitHub secrets (no client secret)**
7. **Grant the workflow `id-token: write` permission**
8. **Switch `azure/login@v2` to federated authentication**
9. **Push and watch the pipeline deploy with no password**
10. **Delete the old `AZURE_CREDENTIALS` secret**
11. **Inspect the trust relationship**
12. **Reflect on subject-claim scope**
13. **Test Your Implementation**

### **Step 1:** Confirm your Azure permissions

Federation requires you to create an Entra app registration and assign roles on Azure resources. Both privileges are tightly held in real organisations — verifying you have them now saves you a confusing error halfway through.

1. **List your role assignments:**

    ```bash
    az role assignment list \
      --assignee "$(az ad signed-in-user show --query id -o tsv)" \
      --output table
    ```

2. **Confirm** at least one of the following appears:

    - `Owner` on the subscription (or on your resource group)
    - `User Access Administrator` on the subscription (or on your resource group)

3. **Verify** you can create app registrations. In the Entra admin centre this is governed by the **Application Developer** role or by the tenant-wide "Users can register applications" toggle. If `az ad app create` later fails with `Insufficient privileges`, that's the toggle you're missing.

> ℹ **Concept Deep Dive**
>
> Two privilege boundaries are at play. Creating app registrations is a tenant-level Entra ID permission. Assigning roles to the resulting identity is a subscription-level Azure RBAC permission. These are separate systems and a user often has one without the other. In a corporate tenant, ask a directory admin to grant Application Developer; ask a subscription owner to grant you a scoped role assignment, ideally only on the resource group you'll deploy into.
>
> ✓ **Quick check:** The role list includes Owner or User Access Administrator on the scope you intend to deploy to.

### **Step 2:** Create the Entra app registration

An **app registration** is the *definition* of an identity in Entra ID — a globally unique application object describing "an application that wants to authenticate." It does not, by itself, have any permissions in any Azure subscription. Think of it as the blueprint.

1. **Create** the app registration:

    ```bash
    az ad app create \
      --display-name "github-cicd-oidc"
    ```

2. **Capture** the `appId` value from the output. You will use it many times. Store it in a shell variable:

    ```bash
    export APP_ID="<paste appId here>"
    echo "$APP_ID"
    ```

> ℹ **Concept Deep Dive**
>
> The naming convention `<purpose>-<auth-type>` (here, `github-cicd-oidc`) makes it obvious in the Entra portal why the registration exists and how it authenticates. Production setups typically split per environment — `github-cicd-oidc-dev`, `-staging`, `-prod` — each with its own role assignments and federated credentials. For this lab, one registration is enough.
>
> ⚠ **Common Mistakes**
>
> - The `appId` returned here is **not** the same as the registration's `id` (object ID). The `appId` is the *client ID* used by tokens; the `id` is the directory object identifier. The federated-credential commands and the GitHub workflow both use `appId`.
>
> ✓ **Quick check:** `az ad app show --id "$APP_ID" --query displayName -o tsv` prints `github-cicd-oidc`.

### **Step 3:** Create a service principal for the app

The app registration is the blueprint; the **service principal** is the instance you actually grant permissions to in your subscription. Without a service principal, role assignments have nothing to attach to. One app registration produces one service principal per tenant.

1. **Create** the service principal:

    ```bash
    az ad sp create --id "$APP_ID"
    ```

2. **Note** that the output contains both `id` (the SP's object ID, used for some directory operations) and `appId` (the same client ID you already have). Role assignments below use `appId`.

> ℹ **Concept Deep Dive**
>
> The split between app registration and service principal is one of Entra ID's most confusing details. The mental model: the app registration is published once in your home tenant; if a multi-tenant app is consented to in a partner tenant, *that* tenant gets its own service principal pointing at your app registration. For single-tenant CI/CD the distinction is mostly bookkeeping, but you can't skip the `az ad sp create` step — without it, role assignments fail with "principal not found."
>
> ✓ **Quick check:** `az ad sp show --id "$APP_ID" --query appId -o tsv` returns the same value as `$APP_ID`.

### **Step 4:** Assign the deployment roles to the new identity

The service principal needs the same two roles your old `AZURE_CREDENTIALS` SP had: `AcrPush` to push images to ACR, and `Container Apps Contributor` to update the Container App. Same scopes, same roles — different identity.

1. **Capture** the resource IDs you'll need:

    ```bash
    export ACR_ID=$(az acr show \
      --name "<your-acr-name>" \
      --query id -o tsv)

    export CA_ID=$(az containerapp show \
      --name "<your-container-app-name>" \
      --resource-group "<your-resource-group>" \
      --query id -o tsv)
    ```

2. **Grant** push access to the registry:

    ```bash
    az role assignment create \
      --assignee "$APP_ID" \
      --role AcrPush \
      --scope "$ACR_ID"
    ```

3. **Grant** Container App update access:

    ```bash
    az role assignment create \
      --assignee "$APP_ID" \
      --role "Container Apps Contributor" \
      --scope "$CA_ID"
    ```

    Role assignments take a few seconds to propagate. If a subsequent step fails with `Principal does not exist in the directory`, wait ~10 seconds and retry — Entra ID is still replicating the new principal.

> ℹ **Concept Deep Dive**
>
> Note how `--scope` is a specific resource ID, not a subscription. This is **least privilege** in practice: the SP can push to *this* registry and update *this* Container App and nothing else. If you ever need to add a second app or registry, add another role assignment with that resource's scope rather than widening this one.
>
> ⚠ **Common Mistakes**
>
> - Assigning `Contributor` at subscription scope works but gives the SP authority to delete every resource in your subscription. The pipeline only needs the two roles above.
> - Role assignments take **a few seconds to propagate**. If the next step fails immediately with an authorization error, retry once after 10 seconds.
>
> ✓ **Quick check:** `az role assignment list --assignee "$APP_ID" --all -o table` lists both `AcrPush` and `Container Apps Contributor` with the expected scopes.

### **Step 5:** Create the federated credential for `main`

So far you have an identity and you have permissions. What you don't have is a way to authenticate as that identity. With a service principal secret you'd add a password and ship it to GitHub. With OIDC, you add a **federated credential** — a trust rule that says "if a token from issuer X has claims matching pattern Y, treat the bearer as this app."

1. **Create** the federated credential. Replace `<your-username>/<your-repo>` with your repository slug:

    ```bash
    az ad app federated-credential create \
      --id "$APP_ID" \
      --parameters '{
        "name": "main-branch",
        "issuer": "https://token.actions.githubusercontent.com",
        "subject": "repo:<your-username>/<your-repo>:ref:refs/heads/main",
        "audiences": ["api://AzureADTokenExchange"],
        "description": "GitHub Actions, main branch only"
      }'
    ```

2. **Verify** it exists:

    ```bash
    az ad app federated-credential list \
      --id "$APP_ID" \
      -o table
    ```

> ℹ **Concept Deep Dive**
>
> Each field of the federated credential pins down one half of the trust handshake.
>
> - `issuer` — only tokens signed by GitHub Actions' OIDC issuer are accepted. Azure validates the JWT signature against GitHub's published JWKS, so you cannot forge one.
> - `audiences` — the token must declare `api://AzureADTokenExchange` as its intended audience. The `azure/login` action sets this for you.
> - `subject` — an exact-match string GitHub embeds in every token it issues from your workflow. The format `repo:org/name:ref:refs/heads/main` means "from the repo `org/name`, on a workflow run for the ref `refs/heads/main`." Anything else — a different branch, a pull request, a tag — produces a different subject and is silently rejected.
>
> ⚠ **Common Mistakes**
>
> - Typing `head` instead of `heads` (it is `refs/heads/main`, the Git ref form) — silent auth failure.
> - Wrapping `<your-username>/<your-repo>` in `<>` literally — the angle brackets are placeholders, remove them.
> - Creating the federated credential on the wrong app registration. The roles you assigned in Step 4 are tied to `$APP_ID`; the federated credential must be on the same `$APP_ID`.
>
> ✓ **Quick check:** The list shows one entry named `main-branch` with the subject `repo:<your-username>/<your-repo>:ref:refs/heads/main`.

### **Step 6:** Add the three new GitHub secrets (no client secret)

`azure/login@v2` in federated mode needs three pieces of information to ask Azure for a token: which app to authenticate as (`client-id`), which directory (`tenant-id`), and which subscription to operate against (`subscription-id`). None of these are secrets in the cryptographic sense — they're identifiers — but storing them as GitHub secrets keeps your repo's `.yaml` reusable across forks and clones.

1. **Read** the three values:

    ```bash
    echo "CLIENT_ID = $APP_ID"
    echo "TENANT_ID = $(az account show --query tenantId -o tsv)"
    echo "SUBSCRIPTION_ID = $(az account show --query id -o tsv)"
    ```

2. **Set** them as repository secrets:

    ```bash
    gh secret set AZURE_CLIENT_ID \
      --body "$APP_ID"

    gh secret set AZURE_TENANT_ID \
      --body "$(az account show --query tenantId -o tsv)"

    gh secret set AZURE_SUBSCRIPTION_ID \
      --body "$(az account show --query id -o tsv)"
    ```

3. **Confirm** they exist:

    ```bash
    gh secret list
    ```

   You should see all three new names alongside the old `AZURE_CREDENTIALS` (which you'll delete in Step 10).

> ℹ **Concept Deep Dive**
>
> Notice what is *not* in this list: a client secret. The whole point of OIDC federation is that the secret material — the cryptographic key proving "I am this app" — never leaves Azure. GitHub holds only the app's *name* (its client ID). The proof of identity is freshly minted by GitHub on every run as a short-lived JWT.
>
> ✓ **Quick check:** `gh secret list` shows `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID`.

### **Step 7:** Grant the workflow `id-token: write` permission

GitHub treats issuing OIDC tokens as a security-sensitive operation. By default, no workflow can request one — you must opt in explicitly with a `permissions:` block. This prevents a compromised dependency or a careless `pull_request` workflow from exfiltrating your cloud identity.

1. **Open** your workflow file at `.github/workflows/ci.yml` (or whatever you named it in the previous exercise).

2. **Add** a `permissions` block at the job level. The block goes immediately under the job's `runs-on:` line:

    > `.github/workflows/ci.yml`

    ```yaml
    jobs:
      deploy:
        runs-on: ubuntu-latest
        permissions:
          id-token: write
          contents: read
        steps:
          - uses: actions/checkout@v4
          # ...remaining steps unchanged for now
    ```

> ℹ **Concept Deep Dive**
>
> The two permissions do different things.
>
> - `id-token: write` lets the runner request a fresh OIDC JWT from GitHub's token service. Without it, the `azure/login` step fails with `Error: Failed to retrieve OIDC token`.
> - `contents: read` is the default for most workflows but is dropped to nothing once you specify *any* `permissions:` block. Re-adding it keeps `actions/checkout@v4` working.
>
> You can also place `permissions:` at the top of the file (workflow level) instead of inside `jobs.deploy:`. Job-level scoping is the safer default — only this job, not other jobs in the same workflow, can mint tokens.
>
> ⚠ **Common Mistakes**
>
> - Adding `permissions:` only at the workflow level but no `id-token: write` — `azure/login` will fail.
> - Forgetting `contents: read` after introducing `permissions:` — `actions/checkout@v4` fails with "Resource not accessible by integration."
>
> ✓ **Quick check:** YAML parses without error. The block is indented inside `jobs.deploy:` at the same level as `steps:`.

### **Step 8:** Switch `azure/login@v2` to federated authentication

This is the only line of code in the workflow that actually changes. The `azure/login@v2` action accepts two mutually exclusive modes: secret-based (`creds:`) and federated (`client-id` / `tenant-id` / `subscription-id`). You're swapping one for the other.

1. **Locate** the existing `azure/login@v2` step. It currently looks like this:

    > `.github/workflows/ci.yml` *(before)*

    ```yaml
          - name: Sign in to Azure
            uses: azure/login@v2
            with:
              creds: ${{ secrets.AZURE_CREDENTIALS }}
    ```

2. **Replace** it with the federated form:

    > `.github/workflows/ci.yml` *(after)*

    ```yaml
          - name: Sign in to Azure
            uses: azure/login@v2
            with:
              client-id: ${{ secrets.AZURE_CLIENT_ID }}
              tenant-id: ${{ secrets.AZURE_TENANT_ID }}
              subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
    ```

   Note: no `creds:` line. The action infers federated mode from the *absence* of `creds:` and the presence of the three explicit IDs.

> ℹ **Concept Deep Dive**
>
> Internally, `azure/login` now does this on every run: it asks the GitHub runner to mint an OIDC JWT (using the `id-token: write` permission you granted in Step 7), then sends that JWT to Entra ID's `/oauth2/token` endpoint with grant type `urn:ietf:params:oauth:grant-type:token-exchange`. Entra ID validates the JWT against the federated credential you created in Step 5, finds a match, and returns a *different* short-lived access token scoped to the service principal. The rest of the workflow uses that access token. The whole exchange takes milliseconds and leaves no persistent secret behind.
>
> ⚠ **Common Mistakes**
>
> - Leaving the `creds:` line *and* adding the three IDs — `azure/login` complains about ambiguous configuration.
> - Using the wrong secret name (`AZURE_CLIENT_SECRET` from a half-finished tutorial) — there is no client secret in this flow.
>
> ✓ **Quick check:** The step references three secrets and no longer mentions `creds` or `AZURE_CREDENTIALS`.

### **Step 9:** Push and watch the pipeline deploy with no password

The behaviour change is invisible — a successful run looks just like the previous exercise's run. What's *different* is what's in motion: the runner mints a JWT, Azure exchanges it for a token, the token authorises the push to ACR and the update to the Container App.

1. **Commit and push** the workflow change:

    ```bash
    git add .github/workflows/ci.yml
    git commit -m "Switch to OIDC federation for Azure auth"
    git push
    ```

2. **Watch** the run:

    ```bash
    gh run watch
    ```

3. **Open** the run logs and expand the **Sign in to Azure** step. You should see a line like:

    ```text
    Federated identity credential resolved.
    Login successful.
    ```

    No `creds` value is logged because there isn't one.

4. **Verify** the deployment landed: hit your Container App's FQDN. The new image you just pushed should be serving requests.

> ✓ **Quick check:** The workflow run is green, the **Sign in to Azure** step shows the federated-resolution log line, and your app responds at its FQDN.

### **Step 10:** Delete the old `AZURE_CREDENTIALS` secret

Until you delete it, the old long-lived password is still sitting in GitHub. The whole point of this exercise is to have *no* password — so prove that the new flow doesn't depend on the old secret by removing it and pushing one more change.

1. **Delete** the secret:

    ```bash
    gh secret delete AZURE_CREDENTIALS
    ```

2. **Confirm** it is gone:

    ```bash
    gh secret list
    ```

   Only `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID` should remain.

3. **Trigger** another pipeline run by making any small change (for example, edit the homepage text in your MVC app) and pushing:

    ```bash
    git commit -am "Confirm pipeline runs without AZURE_CREDENTIALS"
    git push
    gh run watch
    ```

   The run should pass. The pipeline now has no long-lived secret backing it.

> ⚠ **Common Mistakes**
>
> - Deleting the secret *before* pushing the workflow change in Step 8 — the old `creds: ${{ secrets.AZURE_CREDENTIALS }}` line would resolve to an empty string and fail confusingly. Always change the workflow first, verify it works, then remove the old secret.

### **Step 11:** Inspect the trust relationship

The federated credential is the *only* thing standing between an arbitrary GitHub workflow and your Azure subscription. Worth looking at it directly so you understand what changing it would change.

1. **List** the credentials on your app registration:

    ```bash
    az ad app federated-credential list \
      --id "$APP_ID" \
      -o table
    ```

2. **Show** the full subject of the one credential you have:

    ```bash
    az ad app federated-credential list \
      --id "$APP_ID" \
      --query "[].{name:name, subject:subject, issuer:issuer}" \
      -o json
    ```

3. **Demonstrate** the subject's exact-match nature. Try this:

    ```bash
    git checkout -b experiment-branch
    git commit --allow-empty -m "Try to deploy from a feature branch"
    git push -u origin experiment-branch
    ```

   The push triggers no deployment because your workflow is configured for `on: push: branches: [main]` — that's the first layer of isolation. To see the auth layer also reject the branch, force a run from `experiment-branch` using the `workflow_dispatch` trigger you added in the previous exercise:

    ```bash
    gh workflow run ci.yml --ref experiment-branch
    gh run watch
    ```

   The run starts, but it fails at the **Sign in to Azure** step:

    ```text
    AADSTS70021: No matching federated identity record found for presented assertion.
    ```

   The token GitHub mints for `experiment-branch` carries the subject `repo:org/name:ref:refs/heads/experiment-branch`, which does not match the federated credential's `repo:org/name:ref:refs/heads/main`. Azure refuses to issue a token.

4. **Clean up** the experiment branch:

    ```bash
    git checkout main
    git push origin --delete experiment-branch
    git branch -D experiment-branch
    ```

> ℹ **Concept Deep Dive**
>
> Real production setups exploit this exact-match behaviour deliberately. A common pattern: one app registration per environment, each with its own federated credential, each scoped to a different subject pattern.
>
> - `repo:org/name:environment:prod` — only workflow jobs that target the GitHub `prod` environment can deploy
> - `repo:org/name:pull_request` — a separate, lower-privilege identity used by PR validation jobs
> - `repo:org/name:ref:refs/tags/v*` — a release identity that only authenticates for tag pushes
>
> The principal of least privilege is no longer "give the SP the smallest possible role" — that's still true — but also "give the SP the smallest possible *trigger surface* in GitHub." A leaked role assignment is bad; a leaked secret is worse; a federated credential bound to a single subject is the best of the three.
>
> ✓ **Quick check:** Pushing to `main` deploys; pushing to any other branch does not authenticate.

### **Step 12:** Reflect on subject-claim scope

The OIDC concepts you saw in the authentication chapter apply here too: a federated identity, a `subject` claim, a token-exchange flow. The difference is just *who* is authenticating to *whom*. In a user-to-app login, a person proves to your app that they own a Google account. In machine-to-cloud OIDC, GitHub proves to Azure that this workflow run came from this repo on this branch. Same OAuth 2.0 family, same trust-by-prearrangement, different problem.

The brittleness of the `subject` claim is a feature, not a bug. Walk through these scenarios and predict the behaviour:

1. **You add** an `on: pull_request:` trigger to the workflow so the build runs on PRs as well.

   *Result:* the build runs, but the **Sign in to Azure** step fails for every PR — the subject is `repo:org/name:pull_request`, which has no matching federated credential. Tests can run; deployment cannot. This is the design.

2. **A teammate** opens a PR from a fork that modifies `.github/workflows/ci.yml` to print `${{ secrets.AZURE_CLIENT_ID }}` to the logs.

   *Result:* even if a maintainer accidentally approves and merges, the leaked client ID alone cannot be used to authenticate. There is no client secret to leak, and the federated credential only trusts tokens *minted by GitHub for runs of this repo on `main`*. The attacker would need to control your `main` branch to make a token Azure trusts.

3. **You move** the repo from `your-username/<repo>` to a GitHub organisation `your-org/<repo>`.

   *Result:* the subject changes from `repo:your-username/<repo>:...` to `repo:your-org/<repo>:...`. Authentication breaks until you update the federated credential's subject. (This is a real footgun during organisational migrations.)

> ℹ **Concept Deep Dive**
>
> Production CI/CD with OIDC is rarely "one credential, one branch." A typical setup uses GitHub *environments* (Settings → Environments → Production) and binds the federated credential to `repo:org/name:environment:Production`. The Production environment is configured with required reviewers, so a deployment can only run after a human approval. The subject in the OIDC token reflects the environment, so Azure won't issue a deployment token unless the workflow run is gated by the right reviewers. Authentication and authorization meet in the same claim. That's a subject worth a whole separate exercise.

### **Step 13:** Test Your Implementation

Validate end-to-end that the pipeline works without any long-lived secret.

1. **Confirm** GitHub no longer holds the old secret:

    ```bash
    gh secret list
    ```

   Expected: only `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`.

2. **Confirm** Azure does not hold a client secret for the app:

    ```bash
    az ad app credential list \
      --id "$APP_ID" \
      -o table
    ```

   Expected: an empty table (no password credentials, no certificate credentials).

3. **Trigger** a deployment by pushing to `main`:

    ```bash
    git commit --allow-empty -m "Trigger deployment to verify OIDC flow"
    git push
    gh run watch
    ```

4. **Verify** the **Sign in to Azure** step prints `Federated identity credential resolved.` (or equivalent) and the workflow finishes green.

5. **Verify** the FQDN serves the deployed image:

    ```bash
    curl -I "https://<your-container-app-fqdn>"
    ```

   Expected: `HTTP/2 200`.

6. **Verify** branch isolation. Push an empty commit on a different branch, then dispatch the workflow against it and confirm it cannot authenticate:

    ```bash
    git checkout -b verify-branch-isolation
    git commit --allow-empty -m "Should not deploy"
    git push -u origin verify-branch-isolation
    gh workflow run ci.yml --ref verify-branch-isolation
    gh run watch
    ```

   Expected: the dispatched run fails at **Sign in to Azure** with `AADSTS70021: No matching federated identity record found for presented assertion.` The trust relationship only honours the `main` ref.

7. **Clean up the experiment branch:**

    ```bash
    git checkout main
    git push origin --delete verify-branch-isolation
    git branch -D verify-branch-isolation
    ```

8. **Tear down the cloud resources** when you are completely done with the chapter. The work splits into two homes — the Azure subscription holds the running resources, and Microsoft Entra ID (a tenant-level service) holds the identity used by the pipeline. Deleting one does **not** delete the other.

    First, capture the app registration's `appId` so you can delete it after the resource group:

    ```bash
    APP_ID=$(az ad app list --display-name "github-cicd-oidc" --query "[0].appId" -o tsv)
    echo "$APP_ID"
    ```

    Then delete the resource group (this removes the Container App, the ACR with all its images, the Container Apps environment, the Log Analytics workspace, and every role assignment scoped under the group):

    ```bash
    az group delete -n rg-cicd-week4 --yes --no-wait
    ```

    Then delete the Entra app registration. This cascades to the service principal and the federated credential — both are children of the app registration:

    ```bash
    az ad app delete --id "$APP_ID"
    ```

    Finally, delete the now-orphaned GitHub secrets:

    ```bash
    gh secret delete AZURE_CLIENT_ID
    gh secret delete AZURE_TENANT_ID
    gh secret delete AZURE_SUBSCRIPTION_ID
    ```

> ⚠ **Common Mistakes**
>
> - Deleting the resource group is **not enough**. The Entra app registration lives in the tenant, not the subscription, and stays alive (along with the SP and the federated credential) until you delete it explicitly.
> - Role assignments scoped to deleted resources become **orphaned** — they linger in the directory until garbage collection, but their scope path no longer resolves. Harmless, but they show up as "Identity not found" entries in `az role assignment list` for a while.
> - Entra ID **soft-deletes** app registrations for 30 days. They sit under **Microsoft Entra ID → App registrations → Deleted applications** and can be restored. To purge immediately, click into the deleted app there and choose **Delete permanently**.
> - Deleting the resource group also takes the auto-created Log Analytics workspace with it. If you want to keep your logs (you usually don't, for coursework), export them first.

> ✓ **Success indicators:**
>
> - The pipeline runs green on every push to `main` with no `AZURE_CREDENTIALS` secret in the repo
> - The **Sign in to Azure** step reports a federated token exchange, not a credential blob
> - `az ad app credential list` is empty for the app registration — the SP has no password
> - Pushes to non-`main` branches either do not deploy or fail authentication
> - The Container App FQDN serves the latest image
>
> ✓ **Final verification checklist:**
>
> - ☐ Entra app registration `github-cicd-oidc` exists
> - ☐ Service principal exists for that app, with `AcrPush` and `Container Apps Contributor` role assignments scoped to your specific resources
> - ☐ One federated credential is configured, with subject `repo:<your-username>/<your-repo>:ref:refs/heads/main`
> - ☐ Three GitHub secrets present (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`); old `AZURE_CREDENTIALS` deleted
> - ☐ Workflow has `permissions: id-token: write` and `contents: read` at the job level
> - ☐ `azure/login@v2` uses `client-id` / `tenant-id` / `subscription-id`, not `creds`
> - ☐ Pipeline deploys successfully on push to `main`

## Common Issues

> **If you encounter problems:**
>
> **`Error: Failed to retrieve OIDC token`:** The workflow is missing `permissions: id-token: write`. Add it at the job (or workflow) level and re-run.
>
> **`AADSTS70021: No matching federated identity record found for presented assertion`:** The `subject` in the GitHub-minted token doesn't match any federated credential on your app registration. Compare exactly: `repo:<exact-org-or-user>/<exact-repo>:ref:refs/heads/main`. Watch out for `head` vs `heads`, missing colons, and angle brackets left in from the placeholder.
>
> **`AADSTS700016: Application with identifier '...' was not found`:** `AZURE_CLIENT_ID` is wrong or refers to a deleted app registration. Re-fetch it: `az ad app show --id "<app-name>" --query appId -o tsv`.
>
> **`Insufficient privileges to complete the operation` on `az ad app create`:** Your tenant does not let regular users register applications. Ask a directory admin for the **Application Developer** role or to register the app on your behalf.
>
> **`AuthorizationFailed` when pushing to ACR:** Role assignment from Step 4 didn't propagate, or `--scope` was wrong. Run `az role assignment list --assignee "$APP_ID" --all -o table` and confirm both `AcrPush` and `Container Apps Contributor` are listed at the expected scopes. Wait 30 seconds and retry.
>
> **Pipeline still uses the old SP:** GitHub may have cached the old run definition. Confirm `gh secret list` no longer shows `AZURE_CREDENTIALS` and that the workflow YAML on `main` is the new version.
>
> **Still stuck?** Re-run the **Sign in to Azure** step in debug mode by adding `ACTIONS_STEP_DEBUG: true` as a repo secret and re-running the workflow. The debug logs will show the exact subject the runner sent and the exact response from Entra ID.

## Summary

You replaced a long-lived service-principal secret with a federated trust relationship between GitHub Actions and Microsoft Entra ID. The pipeline still pushes to ACR and updates the Container App, but no password lives in GitHub and no password lives in Azure.

- ✓ The Entra app registration is the identity definition; the service principal is its instance in your subscription
- ✓ Role assignments (`AcrPush`, `Container Apps Contributor`) are attached to the service principal, scoped to specific resources
- ✓ The federated credential's `subject` claim binds authentication to one repo on one branch
- ✓ `permissions: id-token: write` opts the workflow into requesting a JWT from GitHub
- ✓ `azure/login@v2` exchanges that JWT for an Azure access token using OIDC token exchange

> **Key takeaway:** Long-lived secrets in CI/CD are a deployment problem dressed up as a security problem. Federated identity makes the security problem disappear (no secret to leak, rotate, or audit) and simultaneously makes the deployment problem disappear (no rotation runbook). The cost is a one-time setup and a `subject` claim that you must keep aligned with how your repository is structured.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Add a second federated credential with subject `repo:<your-username>/<your-repo>:environment:Production` and gate deployments behind a GitHub Environment with required reviewers.
> - Split into two app registrations: one for build (`AcrPush` only) and one for deploy (`Container Apps Contributor` only). Use job-scoped `azure/login` steps to authenticate as each in turn.
> - Decode an actual OIDC token from a real workflow run. Add a step that runs `curl -H "Authorization: Bearer $ACTIONS_ID_TOKEN_REQUEST_TOKEN" "$ACTIONS_ID_TOKEN_REQUEST_URL&audience=api://AzureADTokenExchange"` and pipe the JWT through `jwt.io` to inspect every claim.
> - Repeat the federation setup against AWS (IAM OIDC provider for `token.actions.githubusercontent.com`) or GCP (Workload Identity Federation). The vocabulary differs but the mental model is identical.

## Done!

The pipeline runs with no password. The next time someone asks "where is the deploy secret stored?" the honest answer is "nowhere — it doesn't exist." That's the point.
