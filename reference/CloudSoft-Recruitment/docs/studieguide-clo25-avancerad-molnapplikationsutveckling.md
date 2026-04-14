# Studieguide - CLO25

## Molnapplikationer - Fördjupning (40p)

- **Program:** Cloudutvecklare (CLO25)
- **Skola:** Yrkeshögskolan Campus Mölndal
- **Tidsperiod:** 6:e april - 29:e maj 2026 (v 15 – v 22)
- **Utbildare:** Lars Appel
- **Utbildningsledare:** Markus Frantz
- **Kunskapskontroll:** Inlämningsuppgifter (2 st)

---

## Kursmaterial

- Föreläsningar och laborationer i klassrum
- Demonstrationer
- Referensapplikation: CloudSoft Recruitment Portal (.NET 10 MVC)

## Upplägg

Kursen är indelad i två tematiska områden:

1. **Utvecklingsmetodik, containerisering och driftsättning (v. 1-4)** — Agilt arbetssätt, Docker, autentisering, CI/CD
2. **Övervakning, API-design och molntjänster (v. 5-7)** — Loggning, REST API, filhantering, hälsokontroller

Varje vecka består av två heldagar på campus (onsdag–torsdag) med föreläsningar och laborationer, samt en halvdag online (fredag eftermiddag) där studenterna demonstrerar sin pågående applikationsutveckling.

---

## Inlämningsuppgifter

| Uppgift              | Omfattning |
| -------------------- | ---------- |
| Inlämningsuppgift 1  | v. 1-4     |
| Inlämningsuppgift 2  | v. 5-7     |

Uppgifterna genomförs individuellt och laddas upp som PDF på Google Classroom.

---
<div style="page-break-after: always;"></div>

## Veckoschema

### Kursvecka 1 (v.15) — Agilt arbetssätt och utvecklingsmiljö

| Dag | Plats  | Tid     | Innehåll                                           |
| --- | ------ | ------- | -------------------------------------------------- |
| Ons | Campus | Heldag  | Kursintroduktion, Jira, agil metodik, user stories |
| Tor | Campus | Heldag  | Git, branching, pull requests, inner loop          |
| Fre | Online | Halvdag | Demo: utvecklingsmiljö och `dotnet run`            |

**Förberedelser:**

- Skapa konton för GitHub och Jira
- Installera .NET 10 SDK, Git och VS Code

**Reflektionsfrågor:**

- Vad är en sprint och hur planerar man arbetet i Jira?
- Varför använder man pull requests istället för att pusha direkt till main?
- Vad innebär inner loop i utvecklingsarbete?

**Länkar:**

- [Jira](https://www.atlassian.com/software/jira)
- [GitHub](https://github.com)
- [.NET Documentation](https://docs.microsoft.com/dotnet)

---

<div style="page-break-after: always;"></div>

### Kursvecka 2 (v.16) — Docker och Docker Compose

| Dag | Plats  | Tid     | Innehåll                                                   |
| --- | ------ | ------- | ---------------------------------------------------------- |
| Ons | Campus | Heldag  | Containrar vs VM, images, lager, Docker Hub                |
| Tor | Campus | Heldag  | Multi-stage Dockerfile, multi-plattform, Docker Compose    |
| Fre | Online | Halvdag | Demo: lokal Docker-miljö med MongoDB och Azurite           |

**Förberedelser:**

- Installera Docker Desktop
- Läs på om skillnaden mellan containrar och virtuella maskiner

**Reflektionsfrågor:**

- Vad är skillnaden mellan en Docker-image och en container?
- Varför använder man multi-stage builds i en Dockerfile?
- Hur kopplar Docker Compose ihop flera tjänster i ett lokalt nätverk?

**Länkar:**

- [Docker Documentation](https://docs.docker.com)
- [Docker Hub](https://hub.docker.com)

---

<div style="page-break-after: always;"></div>

### Kursvecka 3 (v.17) — Autentisering och auktorisering

| Dag | Plats  | Tid     | Innehåll                                                |
| --- | ------ | ------- | ------------------------------------------------------- |
| Ons | Campus | Heldag  | ASP.NET Core Identity, cookie-autentisering             |
| Tor | Campus | Heldag  | Roller (Admin, Candidate), `[Authorize]`, CSRF-skydd    |
| Fre | Online | Halvdag | Demo: inloggning, roller och behörighetskontroll        |

**Förberedelser:**

- Läs på om autentisering vs auktorisering
- Bekanta dig med ASP.NET Core Identity

**Reflektionsfrågor:**

- Vad är skillnaden mellan autentisering och auktorisering?
- Hur fungerar cookie-baserad inloggning i en webbapplikation?
- Varför behövs CSRF-skydd och hur implementeras det i ASP.NET Core?

**Länkar:**

- [ASP.NET Core Identity](https://docs.microsoft.com/aspnet/core/security/authentication/identity)

---

<div style="page-break-after: always;"></div>

### Kursvecka 4 (v.18) — CI/CD och driftsättning på Azure

| Dag | Plats  | Tid     | Innehåll                                                     |
| --- | ------ | ------- | ------------------------------------------------------------ |
| Ons | Campus | Heldag  | GitHub Actions: build, test, Docker build, push              |
| Tor | Campus | Heldag  | Azure Container Registry, Azure Container Apps, verifiering  |
| Fre | Online | Halvdag | Demo: pipeline och driftsatt applikation                     |

**Förberedelser:**

- Läs på om GitHub Actions och YAML-syntax för workflows
- Se till att du har tillgång till Azure-portalen

**Reflektionsfrågor:**

- Vilka steg ingår i en typisk CI/CD-pipeline?
- Vad är skillnaden mellan Docker Hub och Azure Container Registry?
- Hur kan man verifiera att en driftsättning lyckades automatiskt?

**Länkar:**

- [GitHub Actions](https://docs.github.com/actions)
- [Azure Container Apps](https://docs.microsoft.com/azure/container-apps)
- [Azure Portal](https://portal.azure.com)

---

<div style="page-break-after: always;"></div>

### Kursvecka 5 (v.19) — Loggning och övervakning

| Dag | Plats  | Tid     | Innehåll                                                   |
| --- | ------ | ------- | ---------------------------------------------------------- |
| Ons | Campus | Heldag  | Strukturerad loggning med `ILogger<T>`, loggnivåer         |
| Tor | Campus | Heldag  | Application Insights, Log Analytics, Azure Monitor         |
| Fre | Online | Halvdag | Demo: loggning och dashboards i Azure                      |

**Förberedelser:**

- Läs på om Azure Monitor och Application Insights

**Reflektionsfrågor:**

- Vad är strukturerad loggning och varför är det bättre än fritextloggar?
- Hur hänger Application Insights och Log Analytics ihop?
- Vad är skillnaden mellan logs och metrics?

**Länkar:**

- [Azure Monitor](https://docs.microsoft.com/azure/azure-monitor)
- [Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)

---

<div style="page-break-after: always;"></div>

### Kursvecka 6 (v.20) — REST API och DTOs

| Dag | Plats  | Tid     | Innehåll                                           |
| --- | ------ | ------- | -------------------------------------------------- |
| Ons | Campus | Heldag  | API-controllers bredvid MVC, DTOs, Swagger         |
| Tor | Campus | Heldag  | JWT-autentisering, API-nyckel-middleware            |
| Fre | Online | Halvdag | Demo: API-anrop och Swagger-dokumentation          |

**Förberedelser:**

- Läs på om REST-principer och JSON
- Bekanta dig med Swagger/OpenAPI

**Reflektionsfrågor:**

- Varför separerar man DTOs från databasmodeller?
- Hur skiljer sig JWT-autentisering från cookie-baserad inloggning?
- Vad är syftet med Swagger i ett API-projekt?

**Länkar:**

- [ASP.NET Core Web API](https://docs.microsoft.com/aspnet/core/web-api)
- [Swagger/OpenAPI](https://swagger.io)

---

<div style="page-break-after: always;"></div>

### Kursvecka 7 (v.21) — Blob Storage och hälsokontroller

| Dag | Plats  | Tid     | Innehåll                                                 |
| --- | ------ | ------- | -------------------------------------------------------- |
| Ons | Campus | Heldag  | Filuppladdning, PDF-validering, `IBlobService`           |
| Tor | Campus | Heldag  | Azure Storage Account, hälsokontroller, Google OAuth     |
| Fre | Online | Halvdag | Demo: filuppladdning och hälsokontroller i produktion    |

**Förberedelser:**

- Läs på om Azure Blob Storage
- Bekanta dig med health check-mönstret i ASP.NET Core

**Reflektionsfrågor:**

- Varför validerar man filinnehåll (magic bytes) och inte bara filändelsen?
- Hur fungerar feature-flaggan som växlar mellan lokal och molnbaserad lagring?
- Vad är syftet med hälsokontroller i en produktionsmiljö?

**Länkar:**

- [Azure Blob Storage](https://docs.microsoft.com/azure/storage/blobs)
- [ASP.NET Core Health Checks](https://docs.microsoft.com/aspnet/core/host-and-deploy/health-checks)

---

<div style="page-break-after: always;"></div>

### Kursvecka 8 (v.22) — Repetition och sammanfattning

| Dag | Plats  | Tid     | Innehåll                                     |
| --- | ------ | ------- | -------------------------------------------- |
| Ons | Campus | Heldag  | Sammanfattning, arkitekturöversikt            |
| Tor | Campus | Heldag  | Genomgång av komplett flöde, reservtid        |
| Fre | Online | Halvdag | Avslutande demo och examination              |

**Förberedelser:**

- Färdigställ inlämningsuppgifter
- Gå igenom reflektionsfrågorna från varje vecka

**Reflektionsfrågor:**

- Hur hänger alla delar ihop: lokal utveckling → Docker → CI/CD → Azure?
- Vilka designmönster har vi använt och varför?

**Länkar:**

- [Azure Portal](https://portal.azure.com)

---

<div style="page-break-after: always;"></div>

## Sammanfattning av huvudämnen

| Vecka | Tema             | Nyckelbegrepp                                                  |
| ----- | ---------------- | -------------------------------------------------------------- |
| 1     | Agilt arbetssätt | Jira, Git, pull requests, inner loop, `dotnet run`             |
| 2     | Docker           | Dockerfile, Docker Hub, multi-plattform, Docker Compose        |
| 3     | Autentisering    | ASP.NET Core Identity, cookies, roller, CSRF                   |
| 4     | CI/CD            | GitHub Actions, ACR, Azure Container Apps, verifiering         |
| 5     | Loggning         | `ILogger<T>`, Application Insights, Azure Monitor              |
| 6     | REST API         | API-controllers, DTOs, JWT, API-nyckel, Swagger                |
| 7     | Blob Storage     | Filuppladdning, Azure Storage, hälsokontroller, OAuth          |
| 8     | Avslutning       | Sammanfattning, repetition, examination                        |
