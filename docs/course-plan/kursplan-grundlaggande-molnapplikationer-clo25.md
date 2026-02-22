# Kursplan: Grundläggande molnapplikationer

**Poäng:** 50 yhp
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

Denna kurs ger den studerande en grundläggande förståelse för hur moderna molnapplikationer utvecklas och distribueras. Kursen introducerar molnplattformar, deras tjänstemodeller och de centrala byggstenarna i en webbaserad applikation såsom:

- Klient–server-modellen
- Trelagsarkitektur
- Vanliga designmönster

Kursen ger kännedom om:
- **Microsoft Azure**
- **Amazon Web Services (AWS)**
- **Google Cloud Platform (GCP)**

Kursen fokuserar sedan på utveckling och infrastruktur till och i Azure.

### Praktiskt innehåll

Studerande utvecklar en enklare applikation och får praktisk erfarenhet av att driftsätta den i molnet. Kursen behandlar:

- Grunderna i hur applikationer konfigureras för olika miljöer
- Versionshantering och CI/CD-flöden med GitHub Actions
- Hur secrets och identiteter hanteras på ett säkert sätt

### Kursmål

Målet med kursen är att den studerande ska förstå hur molnbaserade applikationer utvecklas, konfigureras och driftsätts på en grundläggande nivå, och därigenom skapa en stabil bas inför kommande kurser om arkitektur, molndrift och skalbara lösningar.

---

## Lärandemål

### Kunskapsmål

| Mål | Godkänt | Väl Godkänt |
| --- | ------- | ----------- |
| Redogöra för molnplattformars tjänstemodeller (IaaS, PaaS, SaaS) samt grundläggande begrepp inom cloudutveckling och drift. | Har uppnått målet | - |
| Beskriva klient–server-modellen, trelagersarkitektur och vanliga designmönster som MVC, Dependency Injection och Repository Pattern. | Har uppnått målet | - |
| Förklara principerna bakom DevOps samt grundläggande versionshantering och CI/CD-flöden. | Har uppnått målet | - |
| Redogöra för säker hantering av secrets, identities och åtkomstkontroll i molnmiljöer. | Har uppnått målet | - |
| Beskriva grunderna i loggning och monitorering av applikationer i molnet. | Har uppnått målet | - |

### Färdighetsmål

| Mål | Godkänt | Väl Godkänt |
| --- | ------- | ----------- |
| Utveckla och driftsätta en enklare applikation på en molnplattform samt konfigurera dess grundläggande resurser. | Har uppnått målet | Driftsätter och hanterar applikationen självständigt och strukturerat, med renare konfiguration och färre manuella steg så att lösningen blir mer stabil och lättare att återupprepa. |
| Använda Git och skapa ett enklare CI/CD-flöde för automatiserad driftsättning. | Har uppnått målet | Skapar ett CI/CD-flöde som är självständigt framtaget, tydligt strukturerat och minimerar manuella moment vid driftsättning. |
| Genomföra grundläggande systemadministration i portal och CLI samt utföra enkel felsökning av driftsättningsproblem. | Har uppnått målet | Felsöker självständigt och metodiskt och visar det genom tydliga och korrekta kommandon eller konfigurationer som löser felet. |
| Hantera secrets och identiteter på ett säkert sätt vid driftsättning av applikationer. | Har uppnått målet | Hanterar secrets och identiteter självständigt och på ett sätt som följer god säkerhetspraxis. |
| Använda loggning och monitoreringsverktyg för att identifiera orsaker till enklare fel och verifiera att applikationen körs korrekt. | Har uppnått målet | - |

### Kompetensmål

| Mål | Godkänt | Väl Godkänt |
| --- | ------- | ----------- |
| Självständigt utveckla, driftsätta och felsöka enklare molnapplikationer enligt etablerade arbetssätt och DevOps-principer. | Har uppnått målet | Genomför utveckling, driftsättning och felsökning självständigt och med tydlig struktur, så att processen går att återupprepa utan onödiga manuella steg. |
| Använda lämpliga grundläggande molntjänster och verktyg samt tillämpa goda säkerhetsprinciper vid hantering av secrets och identiteter. | Har uppnått målet | Använder molntjänster och säkerhetslösningar på ett sätt som ger en stabil, ren och tydligt organiserad lösning. |

---

## Examination

Kursen examineras genom **inlämningsuppgifter** med tillhörande dokumentation och rapport.
