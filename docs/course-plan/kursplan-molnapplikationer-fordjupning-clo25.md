# Kursplan: Molnapplikationer fördjupning

**Poäng:** 40 yhp
**Program:** Cloudutvecklare (CLO25-27)
**Skola:** Yrkeshögskolan Campus Mölndal
**Version:** 2025-12-03

---

## Introduktion till kurspaketet i molnutveckling

Kursen ingår i ett kurspaket bestående av:

| Kurs | Poäng |
| ---- | ----- |
| Grundläggande molnapplikationer | 50 yhp |
| Molnapplikationer – fördjupning | 40 yhp |
| Skalbara molnapplikationer | 40 yhp |
| Kubernetes | 20 yhp |

Kurspaketet utgår från etablerade mognadstrappor inom modern molnutveckling. Trapporna beskriver hur drift, infrastruktur och arbetssätt stegvis rör sig från manuella processer till hög grad av automation, robusthet, säkerhet och skalbarhet.

---

## Mognadstrappor

### Infrastrukturtjänster (IaaS → PaaS → Serverless)

| Nivå | Beskrivning |
| ---- | ----------- |
| **IaaS** (Infrastructure as a Service) | Virtuella maskiner, nätverk och grundläggande drift hanteras manuellt. Motsvarar traditionell IT placerad i molnet. |
| **PaaS** (Platform as a Service) | Betydande delar av infrastrukturen hanteras som tjänster, vilket möjliggör större fokus på applikationslogik. |
| **Serverless** | Drift, skalning och resurshantering sker automatiskt. Utvecklarens arbete koncentreras till funktioner och händelsestyrd logik. |

### Driftsmiljöer (VM → Containers → Serverless)

| Nivå | Beskrivning |
| ---- | ----------- |
| **Virtuella maskiner** | Isolerade, tunga miljöer där varje applikation har dedikerade resurser. |
| **Containers** | Portabla och lätta paket som ger en enhetlig och reproducerbar driftsmiljö. Från enkel container till Kubernetes-orkestrering. |
| **Serverless** | Automatiserad distribution, skalning och livscykelhantering av funktionsbaserade applikationer. |

### Arbetssätt (Dev + Ops → DevOps → Cloud Native → Orchestration)

| Nivå | Beskrivning |
| ---- | ----------- |
| **Dev + Ops** | Ren applikationsutveckling (Dev) och ren provisionering/konfigurering av infrastruktur (Ops). |
| **DevOps** | Integrerade processer där utveckling, drift och automation samverkar. |
| **Cloud Native** | Applikationer byggda för molnmiljöer med standardisering, skalbarhet och automation som grund. |
| **Orchestration** | Full automation av driftmiljöer med hjälp av orkestreringsplattformar som Kubernetes. |

---

## Kurspaketets progression

### Grundläggande molnapplikationer (50 yhp)
**Tyngdpunkt:** IaaS/PaaS, VM och Dev + Ops

Den studerande bygger sin första webbapplikation, versionshanterar den och driftsätter den i en grundläggande molnmiljö. DevOps och Cloud Native introduceras.

### Molnapplikationer – fördjupning (40 yhp)
**Tyngdpunkt:** PaaS, Containers och DevOps

Applikationer utvecklas enligt mjukvaruarkitektur för större lösningar och containeriseras. Infrastruktur definieras i kod.

### Skalbara molnapplikationer (40 yhp)
**Tyngdpunkt:** Serverless och Cloud Native

Fokus ligger på autoskalning och eventdrivna lösningar. Orchestration introduceras.

### Kubernetes (20 yhp)
**Tyngdpunkt:** Orchestration

Kursen knyter ihop: avancerad drift, applikationsdesign för klustermiljöer, nätverk, distribuerade system och automatiserade deploymentstrategier.

**Progressionen:**
> Första molnapplikation → Containerbaserade arbetssätt → Skalbara cloud-native lösningar → Orkestrerad drift med Kubernetes

---

## Kursbeskrivning

Denna kurs bygger vidare på grunderna i utveckling för molnmiljöer och fördjupar den studerandes förståelse för hur moderna molnapplikationer designas, paketeras och deployas. Kursen behandlar containerization och hur applikationer körs i en container-based architecture, samt hur dessa distribueras med hjälp av molntjänster och avancerade CI/CD-flöden. Detta görs i molnplattformen Azure.

Studerande vidareutvecklar applikationer och anpassar dem för containerbaserad drift. Kursen introducerar Infrastructure as Code (IaC), miljöhantering och automatiserade deployer, samt hur secrets och identiteter hanteras i mer komplexa miljöer. Logging, monitorering och troubleshooting kopplas direkt till applikationskod så att den studerande förstår hur utveckling och drift stöder varandra i praktiken. Fokus ligger på helhetsförståelse för hur kod skrivs, paketeras, körs och övervakas i molnet, samt att ge ett grundläggande tänk för hur applikationer kan förberedas för framtida skalbarhet.

### Kursmål

Målet med kursen är att den studerande ska kunna paketera, deploya och övervaka applikationer i molnet på ett professionellt sätt, samt skapa en stabil grund inför kurserna om skalbara lösningar och Kubernetes.

---

## Lärandemål

### Kunskapsmål

| Mål | Godkänt | Väl Godkänt |
| --- | ------- | ----------- |
| Redogöra för hur containerbaserade applikationer utvecklas, paketeras och deployas i moderna molnmiljöer. | Har uppnått målet | - |
| Beskriva grunderna i Infrastructure as Code (IaC) samt hur logging, monitorering och felsökning stödjer utveckling och drift av molnbaserade applikationer. | Har uppnått målet | - |
| Redogöra för livscykelhantering och säkerhetsaspekter för applikationer i moderna molnmiljöer. | Har uppnått målet | - |
| Redogöra för hur AI-assistenter/agenter används som stöd för att ta fram lösningen. | Har uppnått målet | - |

### Färdighetsmål

| Mål | Godkänt | Väl Godkänt |
| --- | ------- | ----------- |
| Paketera, vidareutveckla och driftsätta en containerbaserad applikation på en molnplattform. | Har uppnått målet | Paketerar och driftsätter applikationen med tydlig struktur och minimala manuella steg, så att lösningen är lätt att förstå och reproducera. |
| Använda Infrastructure as Code-verktyg för att skapa, uppdatera och rulla ut miljöer som applikationen behöver. | Har uppnått målet | Skapar IaC-konfigurationer som är välorganiserade, återanvändbara och körs utan manuella justeringar. |
| Implementera ett automatiserat deploymentflöde via CI/CD och dokumentera hur flödet fungerar. | Har uppnått målet | Implementerar en pipeline med tydlig struktur och hög grad av automatisering, så att deployment är reproducerbar och fri från manuella steg. |
| Konfigurera och använda logging och monitoreringsverktyg för felsökning, analys och verifiering av applikationens funktion. | Har uppnått målet | Använder logging och monitorering systematiskt för felsökning, med tydlig struktur i hur information tas fram och används. |

### Kompetensmål

| Mål | Godkänt | Väl Godkänt |
| --- | ------- | ----------- |
| Planera och genomföra hela deployment-processen för en containerbaserad applikation – från kod till drift – på ett sätt som visar förståelse för hur utveckling och drift samverkar. | Har uppnått målet | Genomför hela deployment-processen med stabil struktur och utan onödiga manuella moment. |
| Välja och motivera molntjänster, deploymentstrategier och verktyg utifrån krav på utveckling, drift, säkerhet och livscykelhantering. | Har uppnått målet | Tillämpa molntjänster och verktyg så att lösningen är stabil, säker samt fungerande och tydligt strukturerad. |
| Instruera AI-assistenter/agenter för att nå värdemålet med lösningen utifrån User Stories, arkitektur och testning. | Har uppnått målet | - |

---

## Examination

Kursen examineras genom **inlämningsuppgifter** med tillhörande dokumentation och rapport.
