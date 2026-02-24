# Content Roadmap - CLO25 Hugo Site

**Created:** 2026-02-23
**Last updated:** 2026-02-23
**Course period:** v.5–v.14 (Jan 26 – Apr 5, 2026)
**Current position:** Course week 5 (v.9)

---

## Block 3 — DevOps & Operations (v.11–13, starts Mar 9)

No content exists for this block. This is the most urgent work.

### CI/CD (Course week 7, v.11 — Mar 9)

- [ ] Theory section: What is DevOps, CI vs CD, pipeline concepts
- [ ] Slide presentation (reveal.js)
- [ ] Exercise 6: GitHub Actions pipeline for deploying .NET app to Azure VM
- [ ] Mermaid diagram: CI/CD pipeline flow

### Secret Management (Course week 8, v.12 — Mar 16)

- [ ] Theory section: Secrets vs identities, Azure Key Vault, Managed Identities, RBAC basics
- [ ] Slide presentation (reveal.js)
- [ ] Exercise 7: Store connection string in Key Vault, assign Managed Identity, access from app
- [ ] Mermaid diagram: Key Vault architecture

### Monitoring (Course week 9, v.13 — Mar 23)

- [ ] Theory section: Observability, logs vs metrics, Azure Monitor, Log Analytics, Application Insights
- [ ] Slide presentation (reveal.js)
- [ ] Exercise 8: Enable Azure Monitor, configure log collection, set up alerts, query logs
- [ ] Mermaid diagram: Monitoring data flow

---

## Block 2 — Minor Gaps (v.8–10, in progress)

### Reverse Proxy

- [ ] Theory content: What is a reverse proxy, why use one, Nginx as reverse proxy
- [ ] Exercise (Exercise 4?): Configure Nginx reverse proxy for .NET app on Azure VM

### Bastion Host

- [ ] Theory content: Jump boxes, bastion host pattern, security implications
- [ ] Addition to existing network exercises or standalone section

---

## Assignments

Three inlämningsuppgifter required per course plan. Verify status:

- [ ] Inlämningsuppgift 1 (v.1–3): Infrastructure & Web Dev — exists on site?
- [ ] Inlämningsuppgift 2 (v.4–6): Network, Architecture, Storage — exists on site?
- [ ] Inlämningsuppgift 3 (v.7–9): DevOps & Operations — exists on site?

---

## Content Enrichment (lower priority)

### Standardize exercise format

- [ ] Audit all exercises for consistent sections (Goal, Prerequisites, Steps, Verification, Cleanup)
- [ ] Add "Common Mistakes" sections where missing
- [ ] Add "Concept Deep Dive" sections where missing

### Visual improvements

- [ ] Add Mermaid diagrams to existing theory pages (network topology, 3-tier architecture)
- [ ] Add Mermaid diagrams to IaC section (ARM/Bicep deployment flow)

### Tutorials

- [ ] Configure Custom Domain with SSL on Azure
- [ ] Dockerize a .NET Application (preview for next course)
- [ ] Troubleshooting SSH Connection Issues
- [ ] Set Up Azure CLI on Your Machine

---

## Open Questions

- Should Block 3 exercises follow Portal → CLI → IaC pattern or single-path?
- Swedish slide variants needed for Block 3?
- Confirm exercise numbering: 4 (Reverse Proxy), 6 (CI/CD), 7 (Secrets), 8 (Monitoring)?
- Should CI/CD exercise build on Exercise 10's .NET app or be standalone?
