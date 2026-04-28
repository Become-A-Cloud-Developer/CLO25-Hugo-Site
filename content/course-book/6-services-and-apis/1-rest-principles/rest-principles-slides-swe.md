+++
title = "REST-principer"
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

## REST-principer
Del VI — Tjänster och API:er

---

## Varför REST finns
- HTTP-API:er är kontrakt mellan program som **aldrig har träffats**
- Utan en gemensam stil hittar varje API på sina egna konventioner
- **REST** är den dominerande arkitekturstilen ovanpå HTTP
- Formulerad av Roy Fielding år 2000 som en beskrivning av hur webben redan fungerade
- Inte ett protokoll — en uppsättning **begränsningar** som en tjänst kan välja att följa

---

## Vad REST är
- **REST** = Representational State Transfer
- Servern exponerar namngivna **resurser** via URI:er
- Klienten manipulerar **representationer** med standardiserade HTTP-metoder
- Varje förfrågan bär med sig allt servern behöver för att behandla den
- En tjänst är mer eller mindre RESTful — det finns inget godkänt/underkänt

---

## De fem bärande begränsningarna
- **Klient-server-separation** — oberoende halvor, bara API-kontraktet mellan dem
- **Stateless** — varje förfrågan självständig; ingen sessionsdata på servern
- **Cachebarhet** — svar deklarerar om de får cachas
- **Enhetligt gränssnitt** — resurser, standardmetoder, representationer
- **Skiktat system** — proxyer och CDN är osynliga för klienten

---

## Stateless i praktiken
- Servern håller **persistent** tillstånd — inget per-konversation-tillstånd
- Vilken replika som helst kan svara på vilken förfrågan som helst
- Auth-kontext, cursors, identifierare följer med **i varje förfrågan**
- Klienten blir större; servern skalar horisontellt utan extra arbete
- Avvägning: större payloads, upprepade headers, ingen implicit kontext

---

## Det enhetliga gränssnittet
- **Resurs** = allt som kan adresseras med en URI (`/quotes/42`)
- **Representation** = byten på tråden (oftast JSON)
- Standardmetoder: `GET`, `POST`, `PUT`, `PATCH`, `DELETE`
- Konventioner låter mellanlager resonera utan att kunna applikationen
- HATEOAS (länkar i svar) är strikt-REST — sällan i produktion

---

## Exempel: GET /quotes/42
- `GET /api/quotes/42` namnger resursen
- `200 OK` med `Content-Type: application/json` är representationen
- `Cache-Control: public, max-age=60` gör svaret cachebart
- Ingen sessionscookie — ren **stateless**
- Byggs i `CloudCiApi` i den åtföljande övningen

---

## Richardson Maturity Model
- **Nivå 0** — en URL, åtgärden i kroppen (SOAP-över-HTTP)
- **Nivå 1** — flera URL:er, men fortfarande POST till allt
- **Nivå 2** — resurser + standardmetoder + statuskoder (de flesta "REST"-API:er)
- **Nivå 3** — Nivå 2 + HATEOAS (sällsynt i produktion)
- Nivå 2 är sweet spot för fungerande API:er

---

## REST är inte det enda valet
- **gRPC** — HTTP/2 + Protocol Buffers, mindre och streambar, opak för HTTP-verktyg
- **GraphQL** — klienten väljer fält från en endpoint, svår att cacha
- **REST** — vinner för publika HTTP-API:er, CDN-caching, förutsägbara resurser
- Välj efter arbetsbelastning, inte efter hype

---

## Frågor?
