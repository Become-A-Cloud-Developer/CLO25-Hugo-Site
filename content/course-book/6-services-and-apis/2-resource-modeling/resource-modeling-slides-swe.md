+++
title = "Resursmodellering och URI:er"
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

## Resursmodellering och URI:er
Del VI — Tjänster och API:er

---

## Designens minsta enhet
- REST-API:er exponerar **resurser**, inte funktioner
- Första frågan är substantivet, inte verbet
- HTTP-metoderna står för verben
- Förutsägbarheten kommer ur ett konsekvent rutnät
- Klienter kan gissa nästa URI utan att läsa dokumentationen

---

## Samling kontra objekt
- **Samling** — `/quotes` adresserar listan
- **Objekt** — `/quotes/{id}` adresserar ett enda
- Metoder på en samling: `GET`, `POST`, sällan `DELETE`
- Metoder på ett objekt: `GET`, `PUT`, `PATCH`, `DELETE`
- Två former bär nästan varje endpoint

---

## Namnkonventioner
- **Plural** för samlingar — `/quotes`, aldrig `/quote`
- **Inga verb** i URI:er — HTTP-metoden är verbet
- **Gemener**, bindestreck mellan ord — `/customer-orders`
- Identifierare i objekt-URI — int, GUID eller slug
- Det du väljer blir en del av kontraktet

---

## Hierarkiska relationer
- `/customers/{id}/orders` — order som tillhör en kund
- Underresurs säger "barnet hör till föräldern"
- Platt form `/orders?customerId=42` säger "barnet är fristående"
- Båda kan returnera samma JSON
- URI:n betonar *vad API:et anser att datan är*

---

## Beslutsram
- **Underresurs** när barnet inte kan finnas utan föräldern
- **Underresurs** när auktoriseringen följer föräldern
- **Platt med filter** när cross-parent-frågor är vanliga
- **Platt med filter** när barnet har en global identitet
- Vissa API:er exponerar båda — scopad lista, platt kanonadress

---

## Filtrering och paginering
- Query-parametrar smalnar av en samling — `?status=active`
- Filtrering, sortering, sökning — alla `?` inte `/`
- Paginering är samma idé — `?page=3&size=20`
- `/quotes/page/3` är ett URI-misstag
- Detaljerade pagineringsmönster kommer i slutet av Del VI

---

## URI-stabilitet — kontraktet
- En publicerad URI är ett kontrakt — klienter beror av den
- Att döpa om `/quotes` till `/cool-quotes` bryter varje klient
- Lösningen när formen måste ändras är **API-versionering**
- `/v1/quotes` förblir fryst; `/v2/quotes` är den nya formen
- Tio minuter vid tavlan sparar den brytande förändringen senare

---

## CloudCiApi — konkret exempel
- `GET /api/quotes` — lista, returnerar array av `QuoteDto`
- `GET /api/quotes/{id}` — ett objekt, eller `404`
- `POST /api/quotes` — `201 Created` med `Location`-header
- `[HttpGet("{id:int}")]` låser segmentet till heltal
- `CreatedAtAction(...)` bygger URL:en från det namngivna actionet

---

## Vad bra resursmodellering ger
- Gissbara URI:er — nya endpoints känns som de gamla
- Stabila URI:er — säkerhetsändringar rör middleware, inte URL
- Tydligt ägarskap — underresurs eller platt är ett uttalat val
- Förutsägbart rutnät — klienter mappar operationer till metoder
- Resten av Del VI bygger ovanpå denna form

---

## Frågor?
