+++
title = "De tre pelarna: loggar, mätvärden, spårningar"
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

## De tre pelarna: loggar, mätvärden, spårningar
Del IX — Drift och observerbarhet

---

## En driftsatt app är en svart låda
- Utan **telemetri** ser operatören bara "processen lever"
- En misslyckad förfrågan är omöjlig att skilja från en lyckad
- Ingen debugger, inget brytpunkt, ingen andra chans att återskapa
- Insynen är endast det som applikationen väljer att sända ut

---

## Övervakning vs. observerbarhet
- **Övervakning** besvarar en fast lista frågor ("är CPU > 80 %?")
- **Observerbarhet** låter dig ställa nya frågor till befintliga signaler
- Övervakning fångar fel du har förutsett
- Observerbarhet utreder de fel du inte förutsåg

---

## Loggar — berättelsen
- Tidstämplade, **diskreta händelser** med full kontext
- En loggrad = en händelse för en användare, med stack trace och ID
- Bäst för: detaljer per förfrågan, undantagsanalys
- Brister: aggregerade frågor över hög volym blir dyra

---

## Mätvärden — siffror över tid
- **Numeriska mätningar** aggregerade i tidsintervall
- Räknare, mätare, histogram — föraggregerade i sin natur
- Bäst för: dashboards, larm, SLO:er, långsiktiga trender
- Brister: kan inte beskriva enskilda förfrågningar

---

## Spårningar — flöde mellan tjänster
- Kausala kedjor sammanbundna av ett delat **trace ID** (operation ID)
- En spårning är ett träd av **spans**: rot-span + barn per anrop
- W3C Trace Context propagerar ID:t över HTTP-hopp
- Bäst för: latensattribution, utredning över tjänstegränser

---

## Kostnadsavvägningar
- **Loggar** — dyra i volym, oumbärliga för detaljer
- **Mätvärden** — billigast i skala; kostnaden växer med kardinalitet
- **Spårningar** — medelkostnad; sampling bevarar diagnostiskt värde
- En fungerande stack blandar alla tre med olika samplingsgrader

---

## Exempel: en misslyckad checkout
- **Mätvärde** — `http_requests_failed{route="/checkout"}` toppar 14:03
- **Spårning** — rot-span 4,8s; barn-span timeout mot betalleverantör
- **Logg** — operation ID hämtar undantaget, stack trace, användar-ID
- En incident, tre vyer — ingen är tillräcklig ensam

---

## Hur de tre samverkar
- **Mätvärden** upptäcker incidenten och avgränsar omfattningen
- **Spårningar** lokaliserar det felande beroendet
- **Loggar** förklarar exakt vad som hände för användaren
- Delade korrelations-ID låter utredaren pivotera mellan signalerna

---

## Vad Del IX täcker
- `ILogger<T>` och strukturerad loggning i ASP.NET Core
- Application Insights som telemetrimottagare för .NET
- Log Analytics och KQL för frågbara centraliserade loggar
- Hälsokontroller, larm och Service Level Objectives

---

## Frågor?
