+++
title = "HTTP Fundamentals"
program = "CLO"
cohort = "25"
courses = ["BCD"]
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

## HTTP Fundamentals
Del III — Applikationsutveckling

---

## Varför HTTP finns
- Bygger på **TCP/IP** — tillför betydelse till byte-strömmen
- **Textbaserade** meddelanden — läsbara med `curl` och utvecklarverktyg
- **Tillståndslöst** — varje förfrågan står ensam, tillstånd reser i headers
- **Enhetligt gränssnitt** — samma vokabulär för alla applikationer

---

## Förfrågan-svar-cykeln
- Klienten öppnar en TCP-anslutning till värd och port
- Klienten skriver en **förfrågan** — metod, URI, headers, valfri body
- Servern skriver ett **svar** — statuskod, headers, valfri body
- Anslutningen frigörs; nästa förfrågan upprepar samma form

---

## HTTP-metoder
- **GET** — hämta en representation, inga sidoeffekter
- **POST** — skicka data som skapar eller ändrar tillstånd
- **PUT** — ersätt en resurs vid en känd URI
- **PATCH** — delvis uppdatering av en resurs
- **DELETE** — ta bort en resurs

---

## GET kontra POST
- **GET**: parametrar i query-strängen, tom body, bokmärkesbar
- **POST**: parametrar i bodyn, dolda från URL-fältet och historiken
- **GET** är **säker** och **idempotent** — upprepningar är ofarliga
- **POST** är ingetdera — upprepningar skapar dubbletter
- Tillståndsändrande åtgärder använder alltid **POST** (eller `PUT`, `PATCH`, `DELETE`)

---

## Statuskoder
- **1xx** informativa, **2xx** lyckade, **3xx** omdirigeringar
- **4xx** klientfel — förfrågan är fel, försök igen hjälper inte
- **5xx** serverfel — servern misslyckades, försök igen kan lyckas
- Vanliga: `200 OK`, `302 Found`, `400 Bad Request`, `404 Not Found`, `500 Internal Server Error`

---

## URI:er och headers
- **URI** = schema + auktoritet + sökväg + query + fragment
- **Sökvägen** styr routing till en controller-action
- **Query-strängen** bär `GET`-parametrar som `nyckel=värde`-par
- **Headers** bär metadata: `Content-Type`, `Authorization`, `Cookie`, `Cache-Control`

---

## Konkret exempel: curl mot ASP.NET Core
- `curl -v https://localhost:7240/Home/Index`
- Förfrågan: `GET /Home/Index HTTP/1.1` plus `Host`- och `Accept`-headers
- Servern matchar sökvägen mot `HomeController.Index()`
- Svar: `200 OK`, `Content-Type: text/html`, renderad Razor-vy i bodyn
- En `POST`-formulärinlämning bär body-data och omdirigerar oftast med `302`

---

## Idempotens spelar roll vid fel
- Att återförsöka en `GET`, `PUT` eller `DELETE` efter en timeout är säkert
- Att återförsöka en `POST` kan skapa dubbletter — betalnings-API:er använder idempotensnycklar
- Dialogen "Bekräfta att formuläret skickas igen" finns eftersom `POST` inte är idempotent

---

## Frågor?
