# Inlämningsuppgift 1: Containerbaserad webbapplikation — från inner loop till Azure Container Apps

I den här uppgiften ska du visa att du självständigt kan utveckla, containerisera, säkra och driftsätta en containerbaserad .NET-webbapplikation på Azure Container Apps med en CI/CD-pipeline — utan att följa en steg-för-steg-guide. Du har övat på alla underliggande moment i kursens laborationer. Nu ska du sätta ihop dem på egen hand och förklara dina beslut.

Den tänkta applikationen är **CloudSoft Recruitment Portal** — en webbapplikation där kandidater kan bläddra bland och söka jobbannonser och där administratörer hanterar annonser och ansökningar. Du kan utgå från den version du byggt under laborationerna, utveckla din egen variant av den, eller välja en helt annan applikationsidé — så länge din applikation följer samma struktur och funktionsuppsättning: en .NET MVC-webbapplikation med minst två användarroller, persistent domändata och tydlig åtskillnad mellan lokal utveckling och produktion.

Skriv rapporten som ett sammanhängande resonemang som förklarar både *vad* du byggt och *varför*. Använd diagram, kodavsnitt och skärmdumpar för att stödja resonemanget. Rapporten är inte en steg-för-steg-tutorial — fokus ligger på beslut, motiveringar och struktur.

## Delmoment 1: Agilt arbetssätt och inner loop

Applikationen kan göra mycket. Bestäm vad din version ska göra och fånga den avgränsningen i ett litet antal user stories.

- Skriv **3–5 user stories** på formatet *"Som \<roll\> vill jag \<förmåga\> så att \<värde\>"*.
- Beskriv din **inner loop** — cykeln av att skriva kod, köra den och få feedback under utvecklingen. Vilka verktyg och kommandon använder du? Hur förändras loopen när applikationen också beror på en databas?

## Delmoment 2: Containerisering och lokal utvecklingsmiljö

Paketera applikationen som en container och bygg upp en lokal utvecklingsmiljö som kör allt du behöver.

Resonera kring:

- Hur din **Dockerfile** är strukturerad och varför
- Hur du kör **hela stacken lokalt** med alla beroende tjänster
- Var din image hamnar och hur den tar sig dit
- Vilka avvägningar du gjort längs vägen

## Delmoment 3: Autentisering, auktorisering och datalager

Implementera användarhantering och persistent lagring för din applikation.

Resonera kring:

- **Vilka användarna är** och vad varje roll får göra
- Hur applikationen **autentiserar** användare och **skyddar sig** mot vanliga webbrelaterade hot (t.ex. CSRF, svaga lösenord, läckta sessionskakor)
- Hur den **första administratören** kommer in i en nyinstallerad lösning
- Hur **datalagret** är uppbyggt — vad som körs i lokal utveckling jämfört med produktion, och hur applikationen ansluter till respektive miljö

## Delmoment 4: CI/CD och driftsättning på Azure

Automatisera vägen från `git push` till en körande revision i Azure Container Apps.

Resonera kring:

- Hur din **pipeline är strukturerad** — vilka steg den har och vad varje steg producerar
- Hur **hemligheter och identiteter** hanteras (hemligheter ska inte committas i repositoryt)
- Hur **Azure-resurser** (Container Registry, Container Apps-miljö, Container App) skapas och uppdateras
- Hur du **verifierar** att en driftsättning faktiskt lyckats

## Delmoment 5: Verifiering av den driftsatta lösningen

Visa att den driftsatta applikationen fungerar från start till mål från det publika internet — inte bara att deployen blev klar.

## Genomgående aspekter

I hela rapporten ska du behandla nedanstående områden. Du kan väva in dem i delmomenten eller behandla dem i egna avsnitt.

### Säkerhet

Säkerhet är inte ett enskilt kapitel — det påverkar hur du bygger, paketerar, konfigurerar, driftsätter och driver applikationen. Reflektera över säkerhetsaspekterna i varje steg. Exempel att tänka på: cookie-inställningar, rollkontroll, hemlighetshantering, åtkomst till registry, image-ursprung, nätverksexponering av Container App.

### Infrastructure as Code

Beskriv hur någon annan kan **återskapa** din miljö. Är stegen dokumenterade som skript, mallar eller workflow-steg? Vad är automatiserat och vad är manuellt?

### Användning av AI-assistenter

I kursplanens lärandemål ingår användning av AI-assistenter. Reflektera över hur du använt AI-assistenter i uppgiften — i vilka uppgifter de hjälpte dig, var de hade fel, och hur du verifierade att förslagen stämde.

## Inlämningskrav

- **Format:** PDF, ett enda dokument
- **Första sidan:** ska innehålla:
  - Ditt namn
  - En skärmdump på den driftsatta applikationens landningssida där den publika URL:en syns tydligt i adressfältet
  - En länk till ditt publika GitHub-repository
- **GitHub-repository:** ska innehålla applikationskoden, Dockerfile, Docker Compose-fil, infrastrukturskript eller mallar samt GitHub Actions-workflowet
- **Diagram:** eventuella diagram ska vara dina egna — inte skärmdumpar från andra
- **Kodavsnitt:** ska vara i monospace-typsnitt och gå att kopiera — inte skärmdumpar av kod

> ## En kommentar om avgränsning
>
> Gör en tydlig avgränsning. Allt vi gör i kursen går att lösa på ett mer avancerat sätt än vi övat. Poängen med uppgiften är att **visa att du förstår och kan tillämpa det vi gått igenom**, inte att lägga till verktyg och mönster vi inte använt. Beskriv vad som ingår, vad som inte ingår, och motivera gränsdragningen.
