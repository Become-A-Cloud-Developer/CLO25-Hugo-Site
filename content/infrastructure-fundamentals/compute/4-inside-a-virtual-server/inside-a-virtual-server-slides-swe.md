+++
title = "Inuti en virtuell server"
program = "CLO"
cohort = "25"
courses = ["BCD"]
type = "slide"
date = 2024-11-17
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls= true
progress= true
history= true
center= true
+++

## Inuti en virtuell server
- Virtuella servrar erbjuder skalbarhet och flexibilitet bortom fysisk hårdvara.
- Komponenterna emulerar fysiska resurser men hanteras av en hypervisor.
---
## Kärnkomponenter i en virtuell server
- **vCPU**: Delar värdserverns fysiska CPU-resurser.
- **Virtuellt RAM**: Tilldelat minne från värdserverns fysiska RAM.
- **Virtuellt lagringsutrymme**: Virtuella diskar som fungerar som oberoende enheter.
- **vNIC**: Ansluter den virtuella servern till nätverk.
- **Hypervisor**: Programvara som möjliggör resursvirtualisering.
---
## Virtuella CPU:er (vCPU:er)
- Emulerar en fysisk CPU och delar resurser med andra virtuella maskiner.
- Hanteras av hypervisorn
---
## Virtuellt RAM
- En tilldelad del av den fysiska serverns minne.
---
## Virtuellt lagringsutrymme
- Virtuella diskar lagras som filer på värdserverns fysiska enheter.
---
## Virtuellt nätverkskort (vNIC)
- Tillhandahåller nätverksanslutning till den virtuella servern.
- Säkerställer säker och isolerad kommunikation.
---
## Hypervisor
- Hanterar virtualiseringen av hårdvaruresurser.
- **Typ 1**: Körs direkt på fysisk hårdvara.
- **Typ 2**: Körs ovanpå ett operativsystem.
---
## Skillnader: Fysiska vs. virtuella servrar
- **Resursdelning**: Fysiska servrar dedikerar resurser; virtuella servrar delar dem.
- **Skalbarhet**: Virtuella servrar kan dynamiskt skala resurser.
- **Kostnad**: Virtuella servrar minskar kostnader genom delad infrastruktur.
- **Hantering**: Virtuella servrar erbjuds som tjänst (IaaS) av molnleverantörer.
---
## Komponenter i Azure VMs
(Exempel)
- **vCPU och RAM**: Välj konfigurationer baserade på behov (t.ex. B-serien för "burstable" workload).
- **Virtuella diskar**: Azure Managed Disks för lagring.
- **Nätverk**: vNet och NSGs för nätverk.
- **Verktyg**: Använd Azure Monitor och ARM-mallar för automatisering och övervakning
