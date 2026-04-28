+++
title = "Build, test och smoke-gates"
program = "CLO"
cohort = "25"
courses = ["ACD"]
type = "slide"
date = 2026-04-28
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls = true
progress = true
history = true
center = true
+++

## Build, test och smoke-gates
Del VIII — DevOps och leverans

---

## En pipeline utan gates är ett build-script
- Continuous integration ger värde först när varje commit **verifieras**
- Automation som skickar vad som än pushas är inte CI
- En ändring är "integrerad" när pipelinen mekaniskt visar att den fortfarande fungerar
- Tre gates återkommer i varje container-pipeline: **build, test, smoke**

---

## Build-steget
- Ett **build-steg** kompilerar källkoden till körbara artefakter
- Pipelinens billigaste fråga: "Är detta ett giltigt program?"
- Linting följer med — stilfel och enkla buggmönster fångas för nästan inget
- Output är artefakten senare steg använder (binär, image, publicerad katalog)
- **Fail fast** — stoppar innan något test eller deploy körs

---

## Enhetstester (unit tests)
- Ett **unit test** verifierar en enskild funktion eller metod i **isolation**
- Test-dubbletter ersätter databaser, HTTP-servrar och meddelandeköer
- Karaktäristisk egenskap är **fart** — hundratals tester på sekunder
- Fångar logiska fel inuti en metod: fel grenar, off-by-one, null-hantering
- Fångar inte fel som uppstår när enheter kombineras

---

## Integrationstester
- Ett **integrationstest** kör flera enheter tillsammans, på riktigt
- Riktig PostgreSQL-container, riktigt HTTP-lager, riktig konfigbindning
- Fångar kontraktsglapp som unit-tester missar — fel SQL-kolumner, JSON-skillnader, trasiga migrationer
- Kostnaden är körtid — minuter, inte sekunder
- Körs efter unit-testerna så en billig miss inte slösar dyra cykler

---

## Smoke-tester
- Ett **smoke-test** verifierar att den **driftsatta** applikationen lever och svarar
- Riktas mot publik FQDN — körs mot artefakten, inte mot lokal kod
- Lättviktigt — vanligen `curl /healthz` och statuskoll
- Lovar inte korrekthet; lovar att driftsättningen landade
- **Driftsättnings-gaten** — sista kontrollen innan pipelinen säger "klart"

---

## Ordning på gates — fail fast
- Sortera gates efter kostnad: billigaste först, dyrast sist
- Build → unit → integration → image-bygge → deploy → smoke
- Varje steg `needs:` föregående; pipelinen stannar vid första felet
- En trasig build ska inte spendera 20 minuter på att bygga containrar
- Sparar runner-minuter och kortar feedback-loopen

---

## Test-resultat-publicering
- En **test-result publisher** renderar pass/fail och fel direkt på PR:en
- `dotnet test --logger trx` + `dorny/test-reporter@v1` för .NET
- JUnit XML för TypeScript / Python
- Trasiga tester syns utan att expandera en 5000 raders logg
- En gate är bara så användbar som dess synlighet

---

## Exempel — `dotnet test` följt av smoke
- Job 1: `dotnet test` med TRX-logger; publicerar resultat
- Job 2: `needs: test`, deployar med `az containerapp update`
- Job 3: `needs: deploy`, kör `curl --fail https://${FQDN}/healthz`
- `--fail` ger non-zero på allt utom 2xx → workflow blir röd
- Bonus: kontrollera att svaret innehåller nya build-SHA:n
- Se [/exercises/3-deployment/9-cicd-to-container-apps/](/exercises/3-deployment/9-cicd-to-container-apps/)

---

## Frågor?
