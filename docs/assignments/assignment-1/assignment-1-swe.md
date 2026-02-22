# Inlämningsuppgift 1: Driftsätt en webbapplikation i molnet

I den här uppgiften ska du visa att du självständigt kan provisionera, konfigurera och driftsätta en .NET-webbapplikation i Azure — utan att följa en steg-för-steg-guide. Du har övat på varje moment i kursens laborationer. Nu ska du sätta ihop allt på egen hand och förklara din process.

Skriv din rapport som en sammanhängande tutorial som en annan student i klassen skulle kunna följa för att återskapa din lösning. Använd text, skärmdumpar och kodavsnitt för att beskriva varje delmoment nedan.

## Delmoment 1: Utveckla applikationen

Använd den .NET MVC-applikation du byggde under laborationerna i webbutveckling, eller skapa en ny. Se till att ditt namn syns tydligt på landningssidan.

Beskriv kortfattat:

- Vilken .NET-projektmall du använde
- Vilket arkitekturmönster applikationen använder (t.ex. MVC) och varför detta mönster lämpar sig för webbapplikationer
- Hur klient-server-modellen tillämpas i din lösning — vad som agerar klient och vad som agerar server
- Hur du ändrade landningssidan för att visa ditt namn
- Hur du verifierade att applikationen fungerar lokalt

## Delmoment 2: Provisionera en värdmiljö

Provisionera en Ubuntu-baserad virtuell maskin i Azure för att drifta din applikation.

Välj en av provisioneringsmetoderna som behandlats i laborationerna (Azure Portal, Azure CLI eller ARM/Bicep). Om du använde en skriptbaserad eller mallbaserad metod, förklara fördelarna med Infrastructure as Code (IaC) jämfört med manuell provisionering.

Beskriv:

- Vilken metod du valde och varför
- Vilken molntjänstmodell din lösning använder (IaaS, PaaS eller SaaS) och vad det innebär i praktiken
- De viktigaste konfigurationsvalen du gjorde (VM-storlek, region, nätverk)
- Hur du verifierade att den virtuella maskinen skapades och var nåbar

## Delmoment 3: Konfigurera värdmiljön

Förbered den virtuella maskinen för att köra din .NET-applikation. Det innebär att installera .NET Runtime och skapa en systemd-servicefil.

Beskriv:

- Hur du installerade .NET Runtime
- Innehållet i din servicefil och vad varje direktiv gör
- Hur du verifierade att körmiljön installerades korrekt

## Delmoment 4: Driftsätt applikationen

Driftsätt din applikation på den virtuella maskin du provisionerat och konfigurerat i föregående delmoment.

Beskriv:

- Hur du överförde applikationsfilerna till servern
- Hur du startade applikationen som en tjänst
- Hur du verifierade att tjänsten körs

## Delmoment 5: Verifiera lösningen

Verifiera att din webbapplikation körs i Azure och är nåbar från internet.

Beskriv hur du bekräftade att applikationen körs och är åtkomlig utanför ditt lokala nätverk.

## Säkerhet

Genom hela rapporten, redogör för hur du hanterade säkerheten i varje steg. Ta som minimum upp:

- **Serveråtkomst:** Hur autentiserade du dig mot den virtuella maskinen? Varför föredras denna metod?
- **Nätverkssäkerhet:** Vilka portar öppnade du och varför? Vilka portar förblev stängda?
- **Applikationssäkerhet:** Finns det säkerhetsaspekter att beakta kring hur applikationen exponeras mot internet?

Du kan väva in säkerhet i varje delmoment eller skriva ett separat säkerhetsavsnitt — båda tillvägagångssätten är godtagbara.

## Inlämningskrav

- **Format:** PDF
- **Första sidan:** Ska innehålla:
  - Ditt namn
  - En skärmdump på landningssidan med ditt namn — adressfältet i webbläsaren ska vara tydligt läsbart
  - En länk till ditt publika GitHub-repository som innehåller den fullständiga applikationskoden
- **Git-repository:** Pusha din applikationskod till ett publikt GitHub-repository. Repositoryt ska innehålla all källkod och konfigurationsfiler som behövs för att bygga och driftsätta applikationen (t.ex. projektfiler, servicefil, eventuella provisioneringsskript).
- **Rapportens innehåll:** Fokusera på det intressanta — viktiga beslut, relevanta skärmdumpar och viktiga kodavsnitt. Du behöver inte inkludera varje kodrad i själva rapporten eftersom fullständig kod finns tillgänglig i ditt repository.
