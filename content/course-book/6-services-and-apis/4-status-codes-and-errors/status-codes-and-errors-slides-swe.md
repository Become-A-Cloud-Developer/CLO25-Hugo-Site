+++
title = "Statuskoder, versionshantering och felsvar"
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

## Statuskoder, versionshantering och felsvar
Del VI — Tjänster och API:er

---

## Statuskoden är den primära signalen
- Ett REST-anrop talar **två språk** — body för data, status för utfall
- Klienter ska kunna förgrena på **bara koden**, inte på prosan
- En `200` med ett felmeddelande i body förstör alla parsers
- Body fyller på med detaljer; **statusraden talar sanning**

---

## Lyckade-svaren
- **`200 OK`** — standardvalet för `GET` och lyckad `PUT`
- **`201 Created`** — `POST` skapade en ny resurs; inkludera **`Location`**-header
- **`204 No Content`** — lyckat utan body; typiskt för `DELETE`
- Body i en `201` ska vara den **nya resursen**, inte bara ett id

---

## Klientfel-koderna
- **`400 Bad Request`** — felaktig input, saknat fält, parse-fel
- **`401 Unauthorized`** — ingen giltig credential; betyder egentligen *oautentiserad*
- **`403 Forbidden`** — credential är giltig men **inte berättigad**
- **`404 Not Found`** — resursen på den här URI:n finns inte
- **`409 Conflict`** — anropet krockar med resursens **nuvarande tillstånd**

---

## 400 mot 422 och throttling-koden
- **`400`** — JSON parsade inte, fel typer, saknat obligatoriskt fält
- **`422 Unprocessable Entity`** — JSON parsade, **affärsregel** avvisade
- **`429 Too Many Requests`** — rate-limit överskriden; inkludera `Retry-After`
- `[ApiController]` ger `400` för modellvalidering — välj ett spår och håll det

---

## Serverfel och informationsläckage
- **`500 Internal Server Error`** — ohanterat undantag, nedströmsfel
- Body får **aldrig avslöja** stack traces eller interna undantagsmeddelanden
- Anonyma klienter ska inte lära sig systemets interna form
- Mappa undantag till `ProblemDetails` i `app.UseExceptionHandler`

---

## Problem details — RFC 7807
- En standard-form för fel-JSON: `type`, `title`, `status`, `detail`, `instance`
- Skickas med `Content-Type: application/problem+json`
- En parser räcker för **varje** API som följer konventionen
- `type` är den stabila felklass-nyckeln; `instance` korrelerar mot loggar

---

## ProblemDetails i ASP.NET Core
- `ControllerBase.Problem(...)` — bas-hjälpare för vilket fel som helst
- `ControllerBase.ValidationProblem(...)` — lägger till **`errors`**-dictionary
- `[ApiController]` returnerar `ValidationProblemDetails` vid modellfel **automatiskt**
- Kombinera med `app.UseExceptionHandler("/error")` för ohanterade undantag

---

## Strategier för API-versionshantering
- **URL-väg** — `/v1/quotes`; synlig, enkel att routea, det säkra förvalet
- **Header (media-typ)** — `Accept: ...; v=2`; ren URI, svårare att felsöka
- **Query-sträng** — `?api-version=2`; lätt att pinna, skräpar i cache
- Välj **en** och tillämpa den på varje endpoint

---

## Disciplin kring brytande ändringar
- De flesta ändringar bör vara **additiva** — nya valfria fält, nya endpoints
- Ett nytt fält bryter aldrig en befintlig klient; ett omdöpt fält gör alltid det
- Brytande ändringar läggs bakom en **ny version**, med den gamla kvar i drift
- Använd `Sunset: <datum>` för att tala om när den gamla versionen pensioneras

---

## Frågor?
