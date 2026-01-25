+++
title = "Vanliga serverroller"
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

## Serverroller
- Servrar utför ofta en specifik uppgift anpassad till ett affärs- eller applikationsbehov. Detta kallas vanligtvis för en _serverroll_.
---
## Viktiga serverroller
- **Webbserver**: Levererar webbsidor till klienter.
- **Applikationsserver**: Hanterar och kör affärslogik för större applikationer.
- **Proxyserver**: Mellanhand som förbättrar säkerhet och prestanda.
- **Bastion Host**: Säker åtkomstpunkt för hantering av servrar på interna nätverk.
- **Databaseserver**: Lagrar och hanterar strukturerad data - databaser.
- **Fildelningsserver**: Centraliserad lagring och delning av filer.
- **DHCP-server**, **E-postserver**, **DNS-server**, **Utskriftsserver**, **FTP/SFTP-server**, **Domänkontrollant**, **Mediadelningsserver**, etc.
---
## Webbserver
- Bearbetar HTTP/HTTPS-förfrågningar och levererar webbsidor.
- Hanterar statiskt innehåll (t.ex. HTML, JS, CSS) och dynamiskt innehåll genererat av en server.
- Exempel:
    - Apache
    - Nginx
    - Microsoft IIS
---
## Applikationsserver
- Utför affärslogik och integrerar backend-tjänster.
- Perfekt för flerskiktsarkitekturer och komplexa transaktioner.
- Exempel:
    - Apache Tomcat
    - JBoss
    - Microsoft IIS
---
## Proxyserver
- **Forward Proxy**: Maskerar klienter och hanterar _utgående_ förfrågningar.
- **Reverse Proxy**: Dirigerar _inkommande_ trafik till backend-servrar och hanterar säkerhet (ex. SSL).
- Exempel:
    - Squid
    - HAProxy
    - Nginx
---
## Bastion Host
- Ger säker åtkomst till servrar på privata nätverk från externa källor som Internet.
- Fungerar som en gateway för _fjärradministration_.
- Kallas ibland även för _Jump Server_
- Exempel:
    - AWS Bastion Host
    - Azure Bastion
    - Fail2Ban
---
## Sammanfattning
- **Webbserver**: För att leverera _webbinnehåll_.
- **Applikationsserver**: För att köra _affärsapplikationer_.
- **Proxyserver**: För ökad säkerhet och _trafikhantering_.
- **Bastion Host**: För säker åtkomst till _fjärradministration_ av servrar.
