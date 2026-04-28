+++
title = "Secret Management"
program = "CLO"
cohort = "25"
courses = ["BCD", "ACD"]
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

## Secret Management
Part V — Identity and Security

---

## Why secrets need a dedicated store
- Secrets in source control are the most common production breach pathway
- Environment variables are **plaintext at rest** on the host
- Rotation by redeployment leaves a long valid window after a leak
- No audit trail — no record of who read what, when

---

## Azure Key Vault
- **Azure Key Vault** is a managed service for secrets, keys, and certificates
- Secrets accessed over HTTPS using a bearer token from **Entra ID**
- Every secret is **versioned** — set a new value, old value retained
- Every read is logged with caller identity, timestamp, and source IP

---

## The bootstrap problem
- Reading a Key Vault secret requires authentication to Entra ID
- Storing a credential to authenticate defeats the entire point
- **Managed Identity** — an Entra ID identity attached to the compute resource
- Platform rotates the underlying credentials automatically — no secret in code

---

## DefaultAzureCredential
- One credential class probes a chain of sources in order
- In Azure: finds the **managed identity** of the host
- On a laptop: falls back to `az login` session
- Same code runs in production and development without modification

---

## RBAC role assignments
- **RBAC role assignment** = principal + role + scope
- Application identity gets `Key Vault Secrets User` (read-only)
- Scope at the **vault**, not the subscription — least privilege
- Operators get `Key Vault Secrets Officer` for rotation, separate from app identity

---

## Configuration provider chain
- `AddAzureKeyVault(uri, new DefaultAzureCredential())` adds vault to the chain
- Secret `MongoDB--ConnectionString` becomes key `MongoDB:ConnectionString`
- Controller reads `configuration["MongoDB:ConnectionString"]` — unchanged
- Source migrates from env var to vault without an application code change

---

## Application secrets vs pipeline secrets
- **Application secrets** — connection strings, API keys → Key Vault
- **Pipeline secrets** — registry credentials, subscriptions → GitHub Actions secrets
- **OIDC federation** eliminates long-lived deploy credentials entirely
- Pipeline-side mechanisms covered in Part VIII (DevOps and Delivery)

---

## Operational checklist
- **Never** commit a secret to source control — use pre-commit scanners
- **Never** log a secret — redact in structured logging
- **Never** email or chat a secret — use one-time-view tools
- **Rotate on suspicion** — cheap reconfiguration beats expensive breach

---

## Questions?
