+++
title = "OpenAPI och Swagger"
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

## OpenAPI och Swagger
Del VI — Tjänster och API:er

---

## Kontraktsproblemet
- Ett API utan maskinläsbar dokumentation tvingar klienter att **läsa källkod eller gissa**
- Handskriven dokumentation **driver isär** så fort en controller ändras
- Källkoden är auktoritativ men oläsbar för icke-utvecklare
- Lösningen: ett kontrakt härlett från koden, i ett format som **verktyg** kan läsa

---

## OpenAPI-specifikationen
- **OpenAPI** är standardformatet (JSON eller YAML) som beskriver ett HTTP-API
- Deklarerar paths, operationer, parametrar, scheman och säkerhetsscheman
- Donerades till Linux Foundation 2015; OpenAPI 3.0 är vanligast
- Ett dokument — läses av människor, kodgeneratorer och kontrakttest

---

## Paths, operationer, scheman
- En **path** är en URI-mall som `/api/Quotes/{id}`
- En **operation** är en HTTP-metod på en path — `GET /api/Quotes/{id}`
- Ett **schema** beskriver en JSON-form, oftast en DTO
- Återanvändbara scheman ligger under `components/schemas` och refereras med `$ref`

---

## Säkerhetsscheman i dokumentet
- Bearer-token: `"type": "http", "scheme": "bearer"`
- API-nyckel: `"type": "apiKey", "in": "header", "name": "X-Api-Key"`
- Verktyg behandlar säkerhet som en del av kontraktet
- Kodgeneratorer skapar en parameter för credential; tester larmar om auth saknas

---

## Swagger UI som webbläsarutforskare
- **Swagger UI** renderar ett OpenAPI-dokument som en navigerbar, körbar sida
- En tagg per controller; expandera för att se operationer och scheman
- **Try it out** gör varje operation till ett formulär som skickar en riktig request
- Serveras på `/swagger` av Swashbuckle i ASP.NET Core

---

## Swashbuckle genererar dokumentet
- Inspekterar controllers, route-attribut, DTO:er och returtyper
- `AddEndpointsApiExplorer()` + `AddSwaggerGen()` registrerar generatorn
- `UseSwagger()` serverar JSON; `UseSwaggerUI()` serverar utforskaren
- Båda måste registreras **före** `MapControllers()`

---

## Annoteringar gör dokumentet rikare
- `[ProducesResponseType(typeof(QuoteDto), 200)]` dokumenterar success-bodyn
- `[ProducesResponseType(404)]` dokumenterar fel-vägen
- XML-doc-kommentarer flödar in som beskrivningar i operationer och properties
- Standarddokumentet är korrekt men tunt — annoteringar fyller luckorna

---

## Exponera auth så Authorize fungerar
- `AddSecurityDefinition("Bearer", ...)` deklarerar bearer-schemat
- `AddSecurityRequirement(...)` knyter det till operationerna
- Swagger UI får en **Authorize**-knapp; en inklistrad JWT följer med varje "Try it out"
- Utan det svarar säkrade routes med `401` inifrån utforskaren

---

## Verktygsvinsten
- **Kodgeneratorer** (NSwag, Kiota) skapar typade klient-SDK:er
- **Kontrakttester** (Schemathesis) verifierar att svaren matchar scheman
- **Postman / Insomnia** importerar dokumentet som en request-samling
- **Mock-servrar** (Prism) returnerar exempelresponser för parallell utveckling

---

## Frågor?
