+++
title = "Paginering, idempotens och rate limiting"
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

## Paginering, idempotens och rate limiting
Del VI — Tjänster och API:er

---

## Varför "returnera allt" inte fungerar
- Ett svar med en miljon rader **slösar bandbredd, minne och trådar**
- Klienten kan inte återuppta en avbruten överföring
- Kontraktet sätts **dag ett** — att backa bryter klienter
- Paginering begränsar svarsstorleken från första versionen

---

## Tre pagineringsstrategier
- **Offset / limit** — `?offset=20&limit=10`, enkel men djupa sidor långsamma
- **Page / size** — `?page=3&size=10`, UI-vänligt, samma avvägningar
- **Cursor** — opak server-utfärdad token, **stabil vid skrivningar**
- Cursor kan inte hoppa till godtycklig sida; offset kan

---

## Svarsformatet
- JSON-envelope med fälten `data` och `pagination`, eller
- **`Link`-header** i GitHub-stil med `rel="next"`
- Envelopen mappar rakt mot en typad **DTO**
- Välj en variant och håll dig konsekvent i hela API:et

---

## Idempotens
- En förfrågan är **idempotent** när två anrop ger samma effekt som ett
- `GET`, `PUT`, `DELETE` är idempotenta enligt HTTP-definitionen
- `POST` är **inte** idempotent — två POST skapar två resurser
- Idempotens låter klientbibliotek göra säkra retries vid tillfälliga fel

---

## Headern Idempotency-Key
- Klienten genererar en UUID **före första försöket**
- Samma nyckel vid varje retry av samma logiska operation
- Servern cachar svaret per nyckel (24 timmars TTL är vanligt)
- Senare träffar returnerar det **cachade svaret**, ingen ny körning

---

## Rate limiting
- Begränsar antal förfrågningar per identitet per tidsfönster
- Skyddar backend mot **missbruk och oavsiktliga hot-loops**
- Returnerar `429 Too Many Requests` med **`Retry-After`**-header
- ASP.NET Core levereras med rate-limiter-middleware

---

## Tre rate-limit-algoritmer
- **Fixed window** — billig, tillåter 2× burst vid gränsen
- **Sliding window** — jämn, dyrare att underhålla
- **Token bucket** — burst upp till kapacitet, stadig påfyllning
- Token bucket passar **verklig burstig klienttrafik** bäst

---

## Exempel
- `GET /api/quotes?cursor=...&limit=20`
- Returnerar `{ data: [...], pagination: { nextCursor: "..." } }`
- `Program.cs` registrerar en **token-bucket**-policy: 100 tokens, 10/s
- `[EnableRateLimiting("quotes")]` aktiverar controllern

---

## Frågor?
