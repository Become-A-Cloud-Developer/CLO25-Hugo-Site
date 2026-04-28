+++
title = "Pipelines som kod"
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

## Pipelines som kod
Del VIII — DevOps och leverans

---

## Varför pipelinen hör hemma i repot
- Klick i en portal kan inte **code-reviewas** eller **diffas**
- En ny teammedlem kan inte läsa releaseprocessen genom att läsa källkoden
- När portalen designas om förlorar teamet sin samlade kunskap
- En YAML-fil i repot granskas, revertas och loggas som vilken kod som helst

---

## Vokabulären i GitHub Actions
- **Workflow** — en YAML-fil i `.github/workflows/` som triggas av events
- **Jobb** — en uppsättning steg som körs tillsammans på en runner
- **Steg** — ett enskilt skalkommando eller anrop av en action
- **Runner** — maskinen (Ubuntu, Windows, macOS) som kör jobbet
- **Action** — en återanvändbar, namngiven kodenhet som anropas från ett steg

---

## Hierarkin hänger ihop
- Ett **workflow** innehåller ett eller flera **jobb**
- Ett **jobb** körs på en **runner** och innehåller **steg**
- Ett **steg** är antingen ett `run:`-skal eller ett `uses:`-action-anrop
- Jobb körs parallellt som standard; `needs:` kedjar dem sekventiellt
- Varje jobb startar på en fräsch runner — ingen rester från tidigare körningar

---

## Triggers: när ett workflow körs
- **`push`** — körs på varje commit till en angiven branch
- **`pull_request`** — körs när en PR öppnas eller uppdateras; grinden
- **`workflow_dispatch`** — lägger till en manuell "Run workflow"-knapp
- **`schedule`** — körs på cron-uttryck (nattliga tester, städjobb)
- Ett workflow kan lyssna på flera triggers samtidigt

---

## Code review även för pipelinen
- Pipeline-ändringar går genom samma PR-granskning som applikationskod
- Riskabla ändringar (nytt deploy-steg, roterade hemligheter) granskas
- Commit-historiken i `.github/workflows/` blir releaseprocessens revisionslogg
- En dålig pipeline-ändring är ett `git revert` bort

---

## Artefakter: filer mellan jobb
- En **artefakt** är en fil eller filsamling som ett jobb producerar
- Varje jobb körs på en fräsch runner — disken följer inte med
- Ladda upp i build-jobbet, ladda ner i test-jobbet
- Sparas på workflow run-sidan (90 dagar default) för felsökning
- Artefakter är interna i workflowet; Docker-images är externa leverabler

---

## Konkret exempel: minimalt CI-workflow
- `on: [push, pull_request]` — körs på commits till `main` och PR:er
- Tre jobb: `build`, `test`, `docker-build`
- `needs:` kedjar dem; ett fel stoppar kedjan
- Varje jobb checkar ut källkoden på nytt — runners delar inget tillstånd
- `${{ secrets.DOCKERHUB_TOKEN }}` injiceras, syns aldrig i loggarna

---

## Avvägningen: YAML-drift
- Workflows växer förbi läsbarheten snabbt — 30 rader blir 300
- **Återanvändbara workflows** (`workflow_call`) bryter ut delad logik
- **Composite actions** packar stegsekvenser till en namngiven action
- **Matrix-strategier** samlar "samma jobb per version" i en definition
- Refaktorera workflowet med samma instinkter som applikationskoden

---

## Praktik
- Övning: [/exercises/3-deployment/9-cicd-to-container-apps/](/exercises/3-deployment/9-cicd-to-container-apps/)
- Tre pipelines, var och en lägger till ett koncept ovanpå föregående
- Börjar med build + push, slutar med lösenordsfri OIDC-deploy
- Läs varje workflow-fil från start till slut innan den körs

---

## Frågor?
