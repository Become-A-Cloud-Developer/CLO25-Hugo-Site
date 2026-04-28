+++
title = "Larm och SLO:er"
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

## Larm och SLO:er
Del IX — Drift och observerbarhet

---

## Varför larm spelar roll
- En dashboard som ingen tittar på **misslyckas tyst**
- Telemetri i lagring är inte detsamma som att någon blir informerad
- Larm gör en sökbar signal till en telefon som vibrerar
- Den svårare frågan — vad är värt att väcka någon för

---

## Vad en larmregel är
- **Larmregel** = villkor + utvärderingskälla + destination
- Utvärderingskälla: en metrik-ström eller en **KQL**-fråga
- Destination: en **åtgärdsgrupp** som levererar notifieringen
- Konfigureras i Azure Monitor, Application Insights eller Log Analytics

---

## Metrik-baserade vs logg-baserade larm
- **Metrik-baserade** — föraggregerade, billiga, låg fördröjning, fasta metriker
- **Logg-baserade** — kör en KQL-fråga schemalagt, full flexibilitet
- Logg-baserade betalar för intagningsfördröjning (1–3 min) och frågekostnad
- Välj metrik-baserat när metriken finns, logg-baserat när den saknas

---

## Statiska trösklar vs dynamiska baslinjer
- En **tröskel** är den numeriska gräns som utlöser larmet
- Statiska trösklar fungerar när normalt beteende har stabilt intervall
- Dynamiska baslinjer lär sig historiska mönster, hanterar säsongsvariation
- Dynamiska baslinjer behöver ~10 dagars historik för att lära sig

---

## Åtgärdsgrupper
- **Åtgärdsgrupp** = återanvändbar samling av notifieringskanaler
- En grupp, många larmregler — byt jouren på ett ställe
- Kanaler: e-post, SMS, webhook, PagerDuty, Slack, Teams
- Kanalvalet är en del av signalen — e-post vs SMS vs personsökning

---

## Larmtrötthetsfällan
- För många regler → joursvarsteamet ignorerar notifieringar
- Övergående tillstånd (driftsättnings-spikar) utlöser trösklar utan orsak
- "Under N minuter"-duration hjälper på regelnivå
- Den djupare lösningen: sluta larma på tillstånd användarna inte känner

---

## SLI, SLO, felbudget
- **SLI** — mätningen (t.ex. % av förfrågningar som returnerar 2xx)
- **SLO** — målet (t.ex. 99,5% över 30 dagar)
- **Felbudget** — det som är kvar av "tillåten otillgänglighet"
- 99,9% SLO över 30 dagar → ~43 min tolerabel nedtid

---

## Att sätta en realistisk SLO
- Mät SLI:n i **flera veckor innan** ett tal lovas
- Ouppnåelig SLO lär teamet att talet är en fiktion
- SLO:n måste vara både meningsfull för användare och nåbar för teamet
- 99,99% på en tjänst som levererar 99,5% garanterar konstant larmande

---

## Genomgånget exempel: 5xx burn-rate-larm
- SLO: 99,5% lyckade svar över 30 dagar
- Regel: avg `requests_failed_5xx` > 5% över ett 5-minutersfönster
- Enskilda 5xx-blippar utlöser inte; långvariga fel gör det
- Kopplad till åtgärdsgrupp `ag-oncall-payments` — Teams + SMS + e-post

---

## Varför SLO:er motverkar trötthet
- Inom budget = systemet fungerar som avtalat → inget larm
- Budget hotad = verkligt hot mot kontraktet → larm värt att lita på
- Larm blir sällsynta men meningsfulla — jouren lär sig att lita på dem
- Budgeten samlar utveckling och produkt kring en gemensam mätning

---

## Frågor?
