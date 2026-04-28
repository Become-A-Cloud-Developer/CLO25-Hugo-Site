+++
title = "API-nycklar och maskin-till-maskin"
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

## API-nycklar och maskin-till-maskin
Del V — Identitet och säkerhet

---

## Problemet med maskin-till-maskin
- En backend som anropar en annan backend har **ingen användare**
- Inget inloggningsformulär, ingen webbläsare, ingen cookie
- Anroparen är en process, inte en person
- Behöver ändå en credential som servern kan kontrollera
- API-nycklar är den enklaste grinden för detta fall

---

## Vad en API-nyckel är
- En lång, ogenomskinlig **delad hemlighet** mellan klient och server
- Skickas i en header — vanligen `X-Api-Key`
- Bär **inga claims**, ingen identitet, ingen utgångstid
- Bevisar endast "anroparen känner till hemligheten"
- Samma egenskap för varje klient som har nyckeln

---

## Hur den följer med ett anrop
- Header på varje anrop: `X-Api-Key: kQ8j+7Hf...`
- Måste gå över **TLS** — klartext hamnar i router-loggar
- Saknad header → `401 Unauthorized` + `WWW-Authenticate: ApiKey`
- Fel header → `403 Forbidden`
- Rätt header → begäran går vidare till controllern

---

## ASP.NET Core middleware
- En klass med `RequestDelegate`-konstruktor och `InvokeAsync(HttpContext)`
- Registreras med `app.UseMiddleware<ApiKeyMiddleware>()`
- Placeras **efter** routing, **före** controllers
- Läser headern, jämför med `string.Equals(..., Ordinal)`
- Antingen kortsluter eller anropar `await _next(context)`

---

## Svagare än JWT
- **Ingen utgångstid** — giltig tills konfigurationen ändras
- **Inga claims** — varje anropare ser likadan ut
- **Ingen per-anropare-auktorisering** — samma nyckel, samma åtkomst
- **Rotation är destruktiv** om inte servern accepterar två nycklar
- JWT bär identitet; API-nycklar bär bara "känner hemligheten"

---

## Generering och lagring
- Generera med `openssl rand -base64 48` — 384 bitar entropi
- 256 bitar är golvet; verkliga risken är **läckage**, inte brute force
- **Aldrig** i versionshanterad kod — en commit räcker för läckage
- Lokal utveckling: placeholder i `appsettings.Development.json`
- Produktion: plattformens secret store + `secretref:` env-variabel

---

## Rotation av nycklar
- Rotation med en enda nyckel är destruktiv — alla klienter bryts samtidigt
- Produktionsmönster: acceptera **`Current` och `Previous`** parallellt
- Lyft fram ny nyckel, degradera den gamla; klienter byter i egen takt
- Ta bort `Previous` när telemetri visar att ingen trafik använder den
- Utan rotation blir varje läckage permanent

---

## När en API-nyckel räcker
- En betrodd klient driftsatt tillsammans med servern
- Hotmodellen är "stäng ute anonym trafik", inte per-anropare-revision
- Interna eller lågkänsliga API:er där enkelhet väger tyngre än attribution

---

## När den inte räcker
- Per-anropare-attribution krävs ("vilken klient gjorde detta?")
- Olika anropare behöver olika behörigheter
- Credentials måste löpa ut utan operatörsingripande
- Flera oberoende konsumenter måste kunna återkallas individuellt
- Nästa steg är en **JWT bearer-token**

---

## Frågor?
