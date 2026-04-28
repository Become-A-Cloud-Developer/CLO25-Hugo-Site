+++
title = "Att arbeta med Jira"
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

## Att arbeta med Jira
Del X — Samarbete och process

---

## Varför ett digitalt verktyg
- En whiteboard fungerar bara när alla sitter i **samma rum**
- Distribuerade och fjärr-team kan inte dela en fysisk tavla
- Verktyget måste kunna läsas och uppdateras varifrån som helst
- Jira är standardsvaret bland professionella mjukvaruteam

---

## Vad Jira är
- En **databas av arbetsobjekt** med en projektstruktur ovanpå
- Projekt → issues → workflow-status (de tre lagren)
- Varje issue har en nyckel som `PROJ-123` — den bärande identifieraren
- Vyer som sprint board och backlog är filter över databasen

---

## Issue-hierarkin
- **Epic** — kvartalsstor temaenhet, ägs av produkt
- **Story** — användarvänd funktion, ryms i en eller två sprints
- **Task** (uppgift) — internt arbete utan direkt användarvärde
- **Sub-task** — dag-eller-två-enheten en utvecklare plockar upp

---

## Workflow som tillståndsmaskin
- **Workflow** — sekvens av statusar en issue rör sig genom
- Standard: `TO DO → IN PROGRESS → IN REVIEW → DONE`
- Team anpassar — QA-grindar, driftsättningsgrindar, etc.
- Varje extra status är extra friktion; oftast fyra till sex stycken

---

## Sprint board
- En visuell tavla med kolumner per workflow-status
- Gör **pågående arbete synligt** under det dagliga standup-mötet
- Lång `IN PROGRESS`-kolumn → för mycket i flykten
- Fast `IN REVIEW`-kolumn → granskningar görs inte

---

## Git-integration via branch-namn
- **Branch-namnkonvention** kodar in issue-ID i branchen
- Mönster: `feature/PROJ-123-add-login`
- Jira ser commits och PR:er med matchande nyckel, länkar automatiskt
- Workflow-regel flyttar issue till `DONE` när PR:n mergas

---

## Genomgånget exempel: epic till mergad PR
- Epic `PROJ-100: User authentication` → 2 stories → 6 tasks
- Utvecklare tar `PROJ-104`, drar kortet till `IN PROGRESS`
- Branch: `feature/PROJ-104-login-endpoint`
- PR-titel `PROJ-104: Implement login endpoint` — Jira länkar allt

---

## Kommentardisciplin och estimering
- En issue är **en konversation** — varaktig logg slår Slack-trådar
- **Story points** för stories — abstrakt insats, kalibrerat mot velocity
- **Timmar** för tasks där varaktigheten redan är känd
- Att blanda de två i samma backlog ger meningslösa aggregat

---

## Den ärliga sanningen om Jira
- Jira fungerar bara så bra som teamets **hygien** runt det
- En board som ljuger är värre än ingen board — falsk trygghet
- Dagliga uppdateringar, branch-ID, PR-länkar = en sann tavla
- Disciplinen är värdet, inte verktyget i sig

---

## Frågor?
