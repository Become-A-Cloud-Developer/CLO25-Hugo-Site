+++
title = "Inner Loop vs Outer Loop"
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

## Inner loop kontra yttre loop
Del X — Samarbete och process

---

## Produktivitet är återkopplingsfördröjning
- Varje kodändring väntar på ett svar — väntan styr arbetsdagen
- Två distinkta cykler producerar svaret i mycket olika hastigheter
- Den snabba körs på laptopen; den långsamma på delad infrastruktur
- Båda behövs; var och en fångar problem som den andra inte ser
- Att veta vilken loop som äger vilken kontroll är en ingenjörsbedömning

---

## Inner loop
- Cykeln **edit-build-run-test** på utvecklarens egen maskin
- Körs på **sekunder till några få minuter**, helst osynligt
- Svarar på smala frågor: kompilerar det, är JSON-formen rätt, går testet
- Körs hundratals gånger per dag och utvecklare
- Inget lämnar laptopen förrän utvecklaren är nöjd

---

## Yttre loop
- Cykeln **commit-push-CI-deploy** på delad infrastruktur
- Körs på **minuter**, triggas av `git push` eller en pull request
- Svarar på integrativa frågor: ren build, riktig databas, lyckad driftsättning
- Definierad som kod i repot; styr om merge till `main` får ske
- Teamets gemensamma skyddsnät, inte en ersättning för lokalt arbete

---

## Varför inner loop är värd att optimera
- Sparade sekunder ackumuleras över **tusentals iterationer per vecka**
- Långsamma cykler bryter koncentrationen — kostnaden är omorientering
- Snabb feedback bär test-först-disciplin; långsam feedback urholkar den tyst
- Billig iteration uppmuntrar utforskning — tre ansatser istället för en
- Inner loop-snabbhet är, indirekt, en investering i kodkvalitet

---

## Hot reload
- `dotnet watch run` övervakar källträdet och laddar om vid spara
- Slår samman **spara → stoppa → kör** till ett enda steg
- Webbläsaren blir en levande spegel av källfilen
- Faller tillbaka på full rebuild för ändringar som inte kan patchas
- Bästa avkastningen för UI-tunga iterationer

---

## Snabba enhetstester
- Tusen snabba tester kan köras på **under fem sekunder**
- Hålls snabba genom att undvika I/O — ingen disk, inget nätverk, ingen riktig DB
- Kör hela sviten vid spara; behandla varje rött test som omedelbar signal
- Kan inte validera integration — det är yttre loopens jobb
- Inner loopen äger korrektheten hos enskilda enheter

---

## In-memory-databaser för tester
- EF Core `InMemoryDatabase` eller SQLite `:memory:` — snabbt nog för spara-trigger
- Fångar **query-formsbuggar**: fel join, saknad `Include`, dålig LINQ
- Inte produktionsdatabasen — JSON-kolumner och leverantörs-SQL beter sig olika
- En användbar mellansignal, inte ersättare för riktig-DB-test i yttre loopen
- Lyfter en del integrationstäckning in i inner loopen

---

## Dev container
- Deklarerar hela verktygskedjan — runtime, DB, linters — i en repo-fil
- Eliminerar "fungerar på min maskin" genom att containern *är* maskinen
- Onboarding krymper från en dags installation till ett Reopen-in-Container-klick
- För inner loop-verktyg till valfri IDE som talar protokollet
- Avvägning: indirektionen kan lägga till latens på långsamma maskiner

---

## Gränssamtalet
- Standard: tryck varje kontroll mot inner loop tills något tvingar ut den
- Yttre loop förtjänar sin plats när en kontroll kräver **delad infrastruktur**, är **för långsam** för spara, eller validerar en **produktionslik egenskap**
- Ompröva gränsen när kodbasen växer — långsamma tester smyger sig inåt
- Övning: [/exercises/15-code-collaboration/](/exercises/15-code-collaboration/)
- Teamets gemensamma bedömning är i sig ett stycke ingenjörspraktik

---

## Frågor?
