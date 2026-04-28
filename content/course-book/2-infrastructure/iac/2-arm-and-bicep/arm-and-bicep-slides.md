+++
title = "ARM & Bicep – Lecture 1 – Slides"
program = "CLO"
cohort = "25"
courses = ["BCD"]
type = "slide"
date = 2025-08-10
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls= true
progress= true
history= true
center= true
+++

<!-- RevealJS-compatible: slides separated by --- -->

## Why IaC
- Repeatable, auditable, versioned
- Prevent config drift
- Review with PRs instead of clicks

---

## ARM vs Bicep (same engine)
- **ARM**: JSON, verbose
- **Bicep**: concise DSL, compiles to ARM
- Tools: VS Code ext, linter, modules

---

## Template anatomy
- **parameters**: location, namePrefix, adminUser, sshKey, myIp
- **resources**: VNet/Subnet, NSG, Public IP, NIC, **Ubuntu VM**
- **outputs**: public IP

---

## Workflow
```bash
az deployment group what-if -f main.bicep -p dev.bicepparam

az deployment group create -f main.bicep -p dev.bicepparam
```
- Validate → Deploy → Inspect outputs
