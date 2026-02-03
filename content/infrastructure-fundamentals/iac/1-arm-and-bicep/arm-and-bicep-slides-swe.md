+++
title = "ARM & Bicep – Föreläsning 1 – Bilder"
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

<!-- RevealJS-kompatibel: bilder separeras med --- -->

## Varför IaC
- Reproducerbart, granskningsbart, versionshanterat
- Motverkar konfigurationsdrift
- PR-granskning ersätter klick i portalen

---

## ARM vs Bicep (samma motor)
- **ARM**: JSON, mer omständligt
- **Bicep**: kortfattad DSL, kompilerar till ARM
- Verktyg: VS Code-tillägg, linter, moduler

---

## Mallens anatomi
- **parameters**: location, namePrefix, adminUser, sshKey, myIp
- **resources**: VNet/Subnet, NSG, Public IP, NIC, **Ubuntu VM**
- **outputs**: public IP

---

## Arbetsflöde
```bash
az deployment group what-if -f main.bicep -p dev.bicepparam

az deployment group create -f main.bicep -p dev.bicepparam
```
- Validera → Distribuera → Läs outputs

