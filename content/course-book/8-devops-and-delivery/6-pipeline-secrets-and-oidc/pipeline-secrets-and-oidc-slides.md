+++
title = "Pipeline Secrets and OIDC Federation"
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

## Pipeline Secrets and OIDC Federation
Part VIII — DevOps and Delivery

---

## Why pipeline credentials are dangerous
- A pipeline runs **unattended** on third-party hardware with **production access**
- Stored cloud credentials are the most common cause of cloud breach reports
- A long-lived secret leaked once is usable until **manual rotation** catches it
- Defence: non-human identity, narrow scope, short-lived tokens

---

## The GitHub secret store
- A **GitHub secret** is a confidential value stored encrypted in a repository
- Injected as **environment variables** into authorized workflow runs
- Never returned via the UI after creation — only **overwrite** or **delete**
- Logs are **masked** best-effort; never `echo` a secret on purpose

---

## The service principal model
- A **service principal** is an Entra app + secret + RBAC role assignment
- `az ad sp create-for-rbac` emits **client-id**, **client-secret**, **tenant-id**
- The secret is stored in GitHub and read by `azure/login` on every run
- Functional, but the **long-lived password** is the failure mode

---

## The federated-credential model
- A **federated credential** is a trust between a service principal and an external IdP
- GitHub mints a **JWT** describing the workflow run; Entra trusts the signature
- The JWT is exchanged for a real Azure access token — **no stored secret**
- This is **OIDC federation (workload)** — same flow shape as user OIDC, different principal

---

## The federation subject string
- Format: **`repo:org/repo:ref:refs/heads/branch`**
- Encodes which **repository**, which **kind of ref**, which **specific ref**
- Common typos: `refs/head/main`, wrong case, missing `environment:` claim
- Mismatch fails as a generic **"no matching federated identity"** error

---

## Long-lived vs federated
- Stored credential: **client-secret** (long-lived) vs **none** (per-run JWT)
- Lifetime: **months/years** vs **minutes**
- Blast radius if leaked: **full RBAC scope** vs **already expired**
- Branch scoping: **none** vs **built into the subject string**

---

## Worked example — azure/login with OIDC
- Workflow declares **`permissions: id-token: write`** to mint the JWT
- `azure/login@v2` receives **client-id**, **tenant-id**, **subscription-id**
- These are **public identifiers**, not secrets — leaking them is harmless
- `environment: production` adds an env claim that tightens the subject

---

## Where this lands in the exercise
- The companion exercise progresses **Docker Hub PAT → SP secret → OIDC**
- The third stage configures a **federated credential** per branch/environment
- Pipeline keeps working; **no password** lives in GitHub anywhere
- Same pattern wraps any cloud action: registry, deploy, key vault read

---

## Questions?
