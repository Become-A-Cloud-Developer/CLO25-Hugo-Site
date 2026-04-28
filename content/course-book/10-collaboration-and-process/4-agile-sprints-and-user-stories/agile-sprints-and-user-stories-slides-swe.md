+++
title = "Agilt, sprintar och användarberättelser"
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

## Agilt, sprintar och användarberättelser
Del X — Samarbete och process

---

## Varför iterativ leverans
- Vattenfallsplaner **kollapsar** när kraven ändras under projektets gång
- Iterativ leverans behandlar **förändring som standard**, inte undantag
- Korta cykler begränsar kostnaden för en felaktig riktning
- Varje iteration är ett verkligt tillfälle att lära och omprioritera

---

## De fyra agila värderingarna
- **Individer och interaktioner** framför processer och verktyg
- **Fungerande programvara** framför omfattande dokumentation
- **Kundsamarbete** framför kontraktsförhandling
- **Anpassning till förändring** framför att följa en plan

---

## Scrum och sprinten
- En **sprint** är en fast tidsbox, oftast 1–2 veckor
- Tidsboxen är icke förhandlingsbar — oavslutat arbete går tillbaka
- Ärlig signal varje iteration om teamets verkliga kapacitet
- Roller: produktägare, Scrum master, utvecklingsteam

---

## Sprintens ceremonier
- **Sprintplanering** — bestäm sprintmål och välj arbete
- **Daglig avstämning** — femton minuters synk, inte en statusrapport
- **Sprintgranskning** — visa fungerande programvara för intressenter
- **Retrospektiv** — förbättra processen, inte produkten

---

## Formatet för en användarberättelse
- Format: "Som **roll** vill jag **förmåga**, så att **utfall**"
- Rollen visar vilken användare som tjänas
- Förmågan håller sig till användarsynligt beteende, inte teknik
- Utfallet kopplar arbetet till användarvärde för prioritering

---

## Acceptanskriterier
- **Berättelsespecifika**, testbara villkor för att en story är klar
- Varje kriterium är observerbart — godkänt eller underkänt
- Dokumenterar utfallet av teamets samtal om berättelsen
- Innehåller inte tvärgående krav som täckning eller driftsättning

---

## Definition of done
- **Tvärgående** kvalitetskontrakt för varje user story
- Typiska poster: tester passerar, PR mergad, driftsatt till staging
- Kombineras med acceptanskriterier — båda måste uppfyllas
- Beskriver vad teamet enats om som professionellt arbete

---

## Exempel: registreringsformulär
- Story: "Som ny kund vill jag skapa ett konto med e-post..."
- AC1: dubblett-e-post avvisas med synligt felmeddelande
- AC2: lösenord under 10 tecken avvisas före formulärinskick
- AC3: verifieringsmejl skickas inom 60 sekunder
- DoD-grind: tester, granskad PR, frisk staging-driftsättning

---

## Scrum jämfört med Kanban
- **Scrum**: fasta sprintar, åtagen batch, fullständiga ceremonier
- **Kanban**: kontinuerligt flöde, WIP-gränser per kolumn
- Scrum passar funktionsarbete; Kanban passar driftsköer
- Många team blandar — välj det som ger användbar återkoppling

---

## Frågor?
