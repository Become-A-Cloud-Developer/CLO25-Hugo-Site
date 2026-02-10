# Studieguide - CLO25

## Grundläggande Molnapplikationer (50p)

- **Program:** Cloudutvecklare (CLO25)
- **Skola:** Yrkeshögskolan Campus Mölndal
- **Tidsperiod:** 26:e januari - 5:e april 2026 (v 5 – v 14)
- **Utbildare:** Lars Appel
- **Utbildningsledare:** Markus Frantz
- **Kunskapskontroll:** Inlämningsuppgifter (3 st)

---

## Kursmaterial

- Föreläsningar och laborationer i klassrum
- Demonstrationer

## Upplägg

Kursen är indelad i tre tematiska områden:

1. **Infrastruktur och webbutveckling (v. 1-3)** - Virtuella servrar, IaC, Web Development
2. **Nätverk, arkitektur och lagring (v. 4-6)** - Virtuella nätverk, 3-tier arkitektur, Storage
3. **DevOps och drift (v. 7-9)** - CI/CD, Secret Management, Monitoring

---

## Inlämningsuppgifter

| Uppgift | Omfattning |
|---------|------------|
| Inlämningsuppgift 1 | v. 1-3 |
| Inlämningsuppgift 2 | v. 4-6 |
| Inlämningsuppgift 3 | v. 7-9 |

Uppgifterna genomförs individuellt och laddas upp som PDF på Google Classroom.

---
<div style="page-break-after: always;"></div>

## Veckoschema

### Kursvecka 1 (v.5) - Virtuella servrar

| Dag | Plats | Tid | Innehåll |
|-----|-------|-----|----------|
| Mån | Campus | Heldag | Kursintroduktion, Azure-konto, Virtuell server |
| Tor | Campus | Heldag | Lab: Virtuell server |
| Fre | Online | Halvdag | Demo |

**Förberedelser:**

- Skapa ett konto för Microsoft Azure

**Reflektionsfrågor:**

- Vad är en virtuell server?
- Vad är Linux och hur skiljer det sig från Windows?
- Hur kan man interagera med en virtuell server i molnet via SSH?

**Videor:**

- [Vad är Linux](https://vimeo.com/457229425/3d0ca84d32)
- [Terminalen](https://vimeo.com/457316312/d8a1057d55)
- [SSH](https://vimeo.com/460605526/5fe8c4e468)

**Länkar:**

- [Azure Portal](https://portal.azure.com)
- [Azure](https://azure.microsoft.com/sv-se)

---

<div style="page-break-after: always;"></div>

### Kursvecka 2 (v.6) - Virtuella servrar och IaC

| Dag | Plats | Tid | Innehåll |
|-----|-------|-----|----------|
| Mån | Campus | Heldag | Infrastructure as Code |
| Tor | Campus | Heldag | Lab: IaC med Azure |
| Fre | Online | Halvdag | Demo |

**Förberedelser:**

- Läs på om Infrastructure as Code

**Reflektionsfrågor:**

- Hur kan man använda IaC för att skapa en virtuell server?
- Vad är Cloud-init?
- Vilka fördelar ger IaC jämfört med manuell konfiguration?


**Länkar:**

- [Azure Portal](https://portal.azure.com)

---

<div style="page-break-after: always;"></div>

### Kursvecka 3 (v.7) - Web Development

| Dag | Plats | Tid | Innehåll |
|-----|-------|-----|----------|
| Mån | Campus | Heldag | .NET Core, MVC-arkitektur |
| Tor | Campus | Heldag | Lab: Webbutveckling med .NET |
| Fre | Online | Halvdag | Demo |

**Förberedelser:**

- Läs på om .NET Core och MVC-mönstret

**Reflektionsfrågor:**

- Vad är MVC-arkitektur och varför används det?
- Hur fungerar HTTP-protokollet?
- Vad är skillnaden mellan GET och POST?

**Länkar:**

- [Azure Portal](https://portal.azure.com)
- [.NET Documentation](https://docs.microsoft.com/dotnet)

---

<div style="page-break-after: always;"></div>

### Kursvecka 4 (v.8) - Virtuella nätverk

| Dag | Plats | Tid | Innehåll |
|-----|-------|-----|----------|
| Mån | Campus | Heldag | Virtuella nätverk, NSG och ASG |
| Tor | Campus | Heldag | Lab: Virtuellt nätverk, Reverse Proxy |
| Fre | Online | Halvdag | Demo |

**Förberedelser:**

- Läs på om virtuella nätverk på Microsoft Azure

**Reflektionsfrågor:**

- Vad är ett virtuellt nätverk?
- Varför har man en bastion host?
- Vad gör en reverse proxy?

**Länkar:**

- [Azure Portal](https://portal.azure.com)

---

<div style="page-break-after: always;"></div>

### Kursvecka 5 (v.9) - Web Development fördjupning

| Dag | Plats | Tid | Innehåll |
|-----|-------|-----|----------|
| Mån | Campus | Heldag | 3-tier Architecture, Databaser |
| Tor | Campus | Heldag | Lab: Applikation med databas |
| Fre | Online | Halvdag | Demo |

**Förberedelser:**

- Läs på om trelagsarkitektur och databaser

**Reflektionsfrågor:**

- Vad är en 3-tier arkitektur?
- Hur hanteras konfiguration i olika miljöer?
- Varför separerar man presentation, logik och data?

**Länkar:**

- [Azure Portal](https://portal.azure.com)

---

<div style="page-break-after: always;"></div>

### Kursvecka 6 (v.10) - Storage

| Dag | Plats | Tid | Innehåll |
|-----|-------|-----|----------|
| Mån | Campus | Heldag | Blob Storage och Databaser |
| Tor | Campus | Heldag | Lab: Storage och databas i Azure |
| Fre | Online | Halvdag | Demo |

**Förberedelser:**

- Läs på om lagring och databaser i molnet

**Reflektionsfrågor:**

- Hur skiljer sig olika lagringsalternativ åt?
- Vad är fördelarna med blob storage?
- När använder man en databas vs blob storage?

**Länkar:**

- [Azure Portal](https://portal.azure.com)

---

<div style="page-break-after: always;"></div>

### Kursvecka 7 (v.11) - CI/CD

| Dag | Plats | Tid | Innehåll |
|-----|-------|-----|----------|
| Mån | Campus | Heldag | DevOps och CI/CD Pipelines |
| Tor | Campus | Heldag | Lab: Pipeline med Github Actions |
| Fre | Online | Halvdag | Demo |

**Förberedelser:**

- Läs på om Github Actions
- Se till att du kan skapa ett repo på Github och att du via VS Code kan köra:
  - `git clone <repo>`
  - `git add .`
  - `git commit -m "My commit"`
  - `git push`

**Reflektionsfrågor:**

- Vad är fördelen med en CI/CD Pipeline?
- Vad är skillnaden mellan Continuous Integration och Continuous Deployment?

**Länkar:**

- [GitHub](https://github.com)

---

<div style="page-break-after: always;"></div>

### Kursvecka 8 (v.12) - Secret Management

| Dag | Plats | Tid | Innehåll |
|-----|-------|-----|----------|
| Mån | Campus | Heldag | Secrets, identiteter |
| Tor | Campus | Heldag | Lab: Secret management i Azure |
| Fre | Online | Halvdag | Demo |

**Förberedelser:**

- Läs på om Azure Key Vault och Managed Identities

**Reflektionsfrågor:**

- Hur hanterar man secrets säkert i molnmiljöer?
- Vad är skillnaden mellan secrets och identiteter?
- Hur fungerar Azure Key Vault?

**Länkar:**

- [Azure Portal](https://portal.azure.com)
- [Azure Key Vault](https://docs.microsoft.com/azure/key-vault)

---

<div style="page-break-after: always;"></div>

### Kursvecka 9 (v.13) - Monitoring

| Dag | Plats | Tid | Innehåll |
|-----|-------|-----|----------|
| Mån | Campus | Heldag | Azure Monitor och loggning |
| Tor | Campus | Heldag | Lab: Azure Monitor |
| Fre | Online | Halvdag | Demo |

**Förberedelser:**

- Läs på om Azure Monitor

**Reflektionsfrågor:**

- Vad är monitoring och varför är det viktigt?
- Hur fungerar Azure Monitor?
- Vad är skillnaden mellan logs och metrics?

**Länkar:**

- [Azure Monitor Documentation](https://docs.microsoft.com/azure/azure-monitor)

---

<div style="page-break-after: always;"></div>

### Kursvecka 10 (v.14) - Avslutning

| Dag | Plats | Tid | Innehåll |
|-----|-------|-----|----------|
| Mån | Campus | Heldag | Sammanfattning och reservtid |
| Tor | Campus | Heldag | Sammanfattning och reservtid |
| Fre | Online | Halvdag | Sammanfattning och reservtid |

**Förberedelser:**

- Färdigställ inlämningsuppgifter

**Reflektionsfrågor:**

- Vad har jag lärt mig på kursen?
- Hur hänger de olika delarna ihop?

**Länkar:**

- [Azure Portal](https://portal.azure.com)

---

<div style="page-break-after: always;"></div>

## Sammanfattning av huvudämnen

| Vecka | Tema | Nyckelbegrepp |
|-------|------|---------------|
| 1 | Virtuella servrar | Azure, IaaS, Cloud-init, SSH, Linux, VM |
| 2 | Virtuella servrar | IaC - Infrastructure as Code |
| 3 | Web Development| .Net Core, MVC, Bash Scripts, HTTP |
| 4 | Virtuella nätverk | NSG, ASG, Bastion Host, Reverse Proxy |
| 5 | Web Development| 3-tier Architecture, Database, Configuration |
| 6 | Storage | Blob Storage, Database |
| 7 | CI/CD | DevOps, Github Actions, Pipelines |
| 8 | Secret Management | Azure Key Vault, Managed Identities, Åtkomstkontroll |
| 9 | Monitoring | Azure Monitor, Loggning, Metrics |
| 10 | Avslutning | Sammanfattning, Reservtid |
