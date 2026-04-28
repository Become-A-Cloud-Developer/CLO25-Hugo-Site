+++
title = "Vad är en server?"
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

## Vad är en server?
- Servrar tillhandahåller datorkraften bakom applikationer, webbplatser, databaser och mycket mer.
- En server behandlar klientförfrågningar och levererar tjänster via ett nätverk.
---
## Viktiga egenskaper
- **Beräkningskraft**: Utrustad med CPU, minne och lagring för att utföra olika uppgifter.
- **Nätverksuppkopplad**: Möjliggör kommunikation med klienter och andra tjänster.
- **Tjänstebaserad**: Tillhandahåller applikationer, databaser, fillagring, etc. till användare.
---
## Typer av servrar
- Fysiska servrar
- Virtualla servrar
- Containrar
- Serverlösa funktioner (Serverless)
---
## Fysiska servrar
(Bare-Metal)
- Dedikerad hårdvara som kör applikationer och lagrar data.
- Ger full kontroll och prestanda men kräver mer hantering.
- Exempel:
    - AWS Bare Metal Instances
    - Azure BareMetal Servers.
---
## Virtuella servrar
(VMs)
- Mjukvarubaserad emulering som körs på en fysisk värd (host)
- Tillåter flera isolerade miljöer på delad hårdvara, vilket ökar effektiviteten.
- Exempel:
    - AWS EC2 Instances
    - Azure VMs
    - Google Cloud Compute Engine.
---
## Containrar
(Docker)
- Lätta, portabla miljöer för att köra applikationer.
- Delar värdserverns operativsystem men håller processer isolerade.
- Perfekta för mikroservicearkitekturer.
- Exempel:
    - AWS ECS Tasks
    - Azure Container Instances
    - GKE Pods.
---
## Serverlösa funktioner
(Serverless)
- Tar bort behovet av att hantera infrastruktur.
- Kod körs direkt, utan att man känner till den underliggande infrastrukturen, som hanteras av molnleverantörer.
- Lämpligt för skalbara lösningar med minimalt underhåll.
- Exempel:
    - AWS Lambda
    - Azure Functions
    - Google Cloud Functions.
---
## Välja rätt typ
- **Fysiska servrar**: Ger maximal _kontroll_, lämpliga för specialiserade behov.
- **Virtuella maskiner**: Erbjuder _flexibilitet_ och isolering utan behov av fysisk hårdvara.
- **Containrar**: Effektiva, _skalbara_ och perfekta för moderna, distribuerade applikationer.
- **Serverlöst**: Eliminerar infrastruktur och _fokuserar på kod_ och snabb skalning.
---
## Faktorer att överväga
- **Prestanda**: Uppgifter med hög belastning gynnas av _fysiska servrar_ eller _VMs_.
- **Skalbarhet**: _Containrar och serverlösa funktioner_ är utmärkta för att hantera varierande arbetsbelastningar.
- **Kostnad**: _Serverlösa alternativ_ erbjuder betala-för-användning-modeller; VMs ger förutsägbara priser.
- **Kontroll**: _Fysiska servrar och VMs_ erbjuder mer kontroll över miljön, vilket är kritiskt för vissa applikationer.
