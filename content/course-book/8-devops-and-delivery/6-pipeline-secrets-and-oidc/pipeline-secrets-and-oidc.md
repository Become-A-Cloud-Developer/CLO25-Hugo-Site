+++
title = "Pipeline Secrets and OIDC Federation"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 60
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/8-devops-and-delivery/6-pipeline-secrets-and-oidc.html)

[Se presentationen på svenska](/presentations/course-book/8-devops-and-delivery/6-pipeline-secrets-and-oidc-swe.html)

---

A pipeline that deploys to a cloud account needs an identity. The build step compiles the code; the deploy step has to call Azure, AWS, or GCP and tell it to update something — and the cloud provider will only honour that call if the request carries proof that the caller is allowed to make it. One approach is to store a long-lived password in the CI provider's secret store and let the pipeline use it on every run. The stronger approach is to let the CI provider mint a short-lived token for each individual workflow run and have the cloud provider trust that token directly, with no shared password anywhere. This chapter covers both approaches, the failure mode that drove the shift from one to the other, and the configuration mechanics that connect a GitHub Actions workflow to an Azure tenant without a stored credential.

## Why pipeline credentials are dangerous

A continuous integration pipeline is, by design, a script that runs unattended on someone else's hardware with the permissions needed to change production. That combination — automation, third-party infrastructure, production access — is exactly the threat model that made stored cloud credentials the most common cause of cloud breach reports for years. A credential that lives in a secret store is a credential that can be extracted. A credential that is rotated annually is a credential that an attacker can use for up to a year. A credential that grants broad permissions to "deploy" almost always grants enough permissions to exfiltrate data or destroy resources at the same time.

The defence is layered. The pipeline should authenticate as a non-human identity scoped to exactly what the deploy needs, not as a developer's personal account. The credential should be stored encrypted at rest, masked in logs, and never readable through the UI after creation. And — the focus of the second half of this chapter — the credential should ideally not exist at all as a long-lived value, replaced instead by short-lived tokens that the CI provider mints on demand for a single workflow run.

## The GitHub secret store

A **GitHub secret** is a confidential value (API key, access token, password) stored encrypted in a GitHub repository; secrets are injected into workflows as environment variables and are masked in logs to prevent accidental exposure. Secrets can be defined at three scopes — repository, environment (e.g., a `production` environment), and organization — and a workflow step references them through the `${{ secrets.NAME }}` expression syntax. Once a secret is created, the value is never shown again in the GitHub UI; the only operations are overwrite and delete. The plaintext is encrypted with a public key whose private half lives in the GitHub Actions runner control plane, and only an actively running job authorized to use that secret can decrypt it.

Two practical constraints follow from how the store works. First, secrets are exposed only to workflow runs that GitHub considers authorized — typically runs originating from branches in the repository, but explicitly not from forks of public repositories opening pull requests. This default prevents an outside contributor from writing a workflow change that exfiltrates the secret on a CI run. Second, GitHub automatically masks secret values in workflow logs; a secret that accidentally appears in `echo` output is rendered as `***` rather than the plaintext. Masking is best-effort — a base64-encoded value is masked, but a value passed through `tr` or `cut` may slip through — so the safe pattern is to never echo a secret at all.

The store is the right primitive for any credential that the pipeline cannot avoid carrying. Examples include a Docker Hub Personal Access Token (PAT) for pushing images to a public registry, a third-party API key for a service the pipeline integrates with, or — the case this chapter shrinks — an Azure service principal client secret. Storing one of these is necessary; making it unnecessary is better.

## The service principal model

A **service principal** is an Azure-managed identity that represents a non-human principal (a CI pipeline, a scheduled job) and can be granted Azure RBAC roles; it enables automation without storing human passwords in code. Mechanically, a service principal is an instance of an Entra ID **application** plus a credential, with one or more **role assignments** that grant the application permissions on Azure resources. Creating one with the Azure CLI looks like this:

```bash
az ad sp create-for-rbac \
  --name "github-deploy-recruitment" \
  --role contributor \
  --scopes /subscriptions/<sub-id>/resourceGroups/recruitment-rg \
  --json-auth
```

The command emits a JSON document containing four fields the pipeline needs: `clientId` (the application's identifier), `clientSecret` (the long-lived password), `tenantId` (the Entra tenant), and `subscriptionId` (the Azure subscription the role assignment lives in). The `--json-auth` flag formats the output for direct consumption by the `azure/login` action in GitHub Actions.

The pipeline then stores the entire JSON blob — or just the four fields individually — as repository secrets, and an `azure/login` step reads them at runtime to obtain an Azure access token. The role assignment is `contributor` scoped to a single resource group, which is wide enough to update a Container App and narrow enough to keep the blast radius small if the secret leaks.

This model is workable. It is also the failure mode the next section eliminates: the `clientSecret` is a long-lived password sitting in GitHub. If GitHub is compromised, if a maintainer's account is phished, if a workflow change accidentally prints the secret to a log that escapes masking, if the secret is rotated lazily — any of these turns into an attacker with `contributor` rights on a real resource group.

## The federated-credential model

A **federated credential** is a trust relationship between a service principal in Azure and an external identity provider (e.g., GitHub); it allows the external provider to mint short-lived tokens that Azure validates, eliminating the need for a stored password. Instead of GitHub holding a copy of the secret, GitHub holds a private signing key and Entra holds the corresponding public key. When a workflow runs, GitHub mints a [JWT](/course-book/5-identity-and-security/5-bearer-tokens-and-jwt/) signed with that private key — describing exactly which workflow run is asking for credentials — and the workflow exchanges that token with Entra for a real Azure access token.

**OIDC federation (workload)** is the practice of using OpenID Connect tokens issued by a CI/CD provider (GitHub Actions, GitLab CI) to authenticate to cloud providers (Azure, AWS, GCP); it replaces long-lived stored credentials with short-lived, automatically rotated tokens. The "workload" qualifier matters: the OIDC flow used here is structurally similar to the user-facing OIDC flow described in [OAuth and OIDC](/course-book/5-identity-and-security/7-oauth-and-oidc/), but the principal being authenticated is a CI pipeline, not a human at a browser. The user-flavour flow exists to log a person in; the workload-flavour flow exists to log a workflow run in. Both rely on JWTs, but they solve different problems.

The exchange happens in three steps inside the pipeline:

1. The GitHub Actions runtime, on request from the workflow, generates a **short-lived token** — a bearer token issued by a CI/CD provider for a specific workflow run; it expires after a short duration (minutes to hours) and is automatically rotated on each run, reducing the blast radius of a compromise. The token's payload describes the run: the repository, the branch or tag, the workflow file, and the environment name.
2. The workflow presents this JWT to Entra ID's token endpoint, asking for an Azure access token in exchange.
3. Entra validates the JWT against the federated credential's trust configuration. If the JWT's claims match the configured **subject** string and the signature verifies against GitHub's published public keys, Entra issues a normal Azure access token, and the workflow uses it to call Azure as the configured service principal.

No password is ever stored. The pipeline carries no Azure credential between runs; the credential is reborn on every run from a JWT that itself only lives for a few minutes.

### The federation subject string

The subject string is the load-bearing part of the trust configuration, because it controls which workflow runs Entra will accept tokens from. It encodes which repository, which kind of ref, and which specific ref the run must be on:

```text
repo:cloud-developers/recruitment:ref:refs/heads/main
```

That subject says: trust GitHub-issued JWTs that come from the `cloud-developers/recruitment` repository, where the workflow ran on a push to the `main` branch. A run from any other branch presents a different subject and is rejected. The same applies to tags (`refs/tags/v1.0`), pull requests (`pull_request`), and named environments (`environment:production`). Each variant is its own federated credential entry — a project that deploys from `main` and from version tags needs two credentials configured, one per subject.

The subject is also where almost all OIDC misconfigurations live, because the format is unforgiving and the failure mode is silent. Common typos:

- `ref:refs/head/main` instead of `ref:refs/heads/main` (the trailing `s` is part of the standard Git ref path).
- `repo:cloud-developers/Recruitment:...` when the actual repo name is lowercased — the match is case-sensitive.
- `ref:refs/heads/main` when the workflow actually runs from a feature branch — the deploy is gated by the subject, so a workflow running from a `dev/...` branch will fail its login even if everything else is correct.
- `environment:production` configured but the workflow's job does not declare `environment: production` — the JWT will not include the environment claim, and Entra will reject the exchange.

The error from `azure/login` in any of these cases is a generic "AADSTS70021: No matching federated identity record found", which sends the operator hunting through the wrong end of the configuration. The fix is almost always at the federated-credential level: the subject string in Azure must literally match the JWT GitHub will issue, and any drift between the two is a deploy failure.

## Comparison: long-lived vs federated

The two models are not equivalent. Federated credentials require more setup but eliminate the largest single category of CI/CD security incident.

| Property | Long-lived service principal secret | OIDC-federated service principal |
|----------|-------------------------------------|----------------------------------|
| Stored credential | `clientSecret` in GitHub secret | None |
| Credential lifetime | Until rotated (months to years) | Single workflow run (minutes) |
| Blast radius if leaked | Full RBAC scope, until detected | None — the JWT is already expired |
| Rotation effort | Manual; must update GitHub secret | Automatic; GitHub mints fresh JWT each run |
| Setup complexity | One CLI command, one secret | Service principal + federated credential per subject |
| Branch/tag scoping | None — any workflow run can use the secret | Built in via subject string |
| Audit trail | "Service principal X was used" | "Workflow run R used service principal X" |

The federated model is the default the [exercise on CI/CD to Azure Container Apps](/exercises/3-deployment/9-cicd-to-container-apps/) lands on by its third stage, and it is the recommended pattern for any new CI integration with Azure. The long-lived secret model is still valid for pipelines whose CI provider does not support OIDC federation, but for GitHub Actions deploying to Azure there is no reason left to use it.

## Worked example: azure/login with OIDC

The pipeline-side configuration to use a federated credential is small. The workflow declares `id-token: write` permission so the runner is allowed to mint OIDC tokens, and the `azure/login@v2` step receives only the public identifiers — no client secret.

```yaml
permissions:
  id-token: write
  contents: read

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v4

      - name: Azure login (OIDC)
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Deploy revision
        run: az containerapp update --name recruitment --resource-group recruitment-rg --image mycr.azurecr.io/recruitment@sha256:...
```

Three things deserve note. First, the three values passed to `azure/login` are public identifiers, not secrets — putting them in GitHub secrets is convention, not security. Leaking a `client-id` is harmless without a corresponding federated credential to back it. Second, the `permissions: id-token: write` line is mandatory and is the most common omission; without it, the runner refuses to mint the JWT and `azure/login` fails before it even reaches Entra. Third, the `environment: production` declaration does double duty: it gates the job behind whatever protection rules the environment has (manual approvers, branch restrictions), and it adds an `environment` claim to the OIDC token, which lets the federated credential's subject string be scoped tighter than just the branch.

The same workflow could equally well authenticate to a [container registry](/course-book/7-containers/6-container-registries/) using OIDC, push an image, and only then run the deploy step. The pattern generalizes: any cloud action that can be wrapped in an Entra-issued access token can be performed without a stored secret if the federation is configured.

## A note on managed identity

Inside Azure, the equivalent of OIDC federation for non-Azure workloads is the [managed identity](/course-book/5-identity-and-security/8-secret-management/) attached directly to a VM, Container App, or function. A pipeline running on a self-hosted GitHub runner inside Azure can use the runner's managed identity directly and skip OIDC federation entirely. For pipelines running on GitHub-hosted runners (the common case), OIDC federation plays the equivalent role: an identity for a workload that lives outside Azure but needs to act inside it.

## Summary

A CI pipeline that deploys to Azure needs an identity, and the choice of how that identity is provided governs the pipeline's security posture. The traditional approach — a service principal whose client secret lives as a GitHub secret — is functional but exposes a long-lived password to the largest attack surface in the system. The federated-credential approach configures Entra to trust short-lived JWTs minted by GitHub Actions for individual workflow runs; the pipeline carries no password, the token expires within minutes of issue, and the trust relationship is scoped tight to a specific repository, branch, and environment via the federation subject string. The setup mechanics are small — a service principal, a federated credential per subject, and an `azure/login@v2` step with `id-token: write` permission — and the failure modes cluster around typos in the subject string, which presents as a generic "no matching federated identity" error. For new GitHub Actions integrations with Azure, OIDC federation is the recommended default; the long-lived secret model remains a fallback only for CI providers that do not support it.
