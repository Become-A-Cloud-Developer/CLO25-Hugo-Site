+++
title = "Anslutningar, pool och transaktioner"
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

## Anslutningar, pool och transaktioner
Del IV — Dataåtkomst

---

## Kostnaden att öppna en anslutning
- TCP-handskakning, TLS-handskakning, protokollhandskakning, autentisering
- 5–50 ms i lokalt nät, 100+ ms över regioner
- Naiv kod öppnar en ny anslutning per anrop och bränner latens på uppsättning
- Lösningen är **inte** färre anslutningar — utan att **återanvända** dem

---

## Connection pool
- En **cache av öppna anslutningar** i klientbiblioteket
- Lediga anslutningar lämnas ut direkt; annars öppnas en ny upp till `MaxPoolSize`
- Konfigureras via **connection string** — `MaxPoolSize`, `MinPoolSize`, `ConnectionLifetime`
- Dimensioneras så att `poolstorlek × repliker` håller sig under databasens anslutningstak

---

## Var poolen bor
- I **klientobjektet** i applikationsprocessen — inte en separat tjänst
- Varje replika har sin egen pool; databasen ser **summan** av alla pooler
- Att konstruera en ny klient per anrop förstör poolningen helt
- Återanvänd klienten; låt poolen göra sitt jobb

---

## Singleton-klient, Scoped-repository
- Databasklienter (`MongoClient`, `BlobServiceClient`, Npgsql-datakälla) är **trådsäkra** och äger poolen
- Registreras som **Singleton** i DI — en instans per process
- Repositories ovanpå registreras som **Scoped** — en per anrop
- Alla scoped repositories delar samma singleton-klient och därmed samma pool

---

## Transaktioner
- En **transaktion** är en arbetsenhet som databasen behandlar atomärt
- Antingen genomförs alla operationer, eller så rullas allt tillbaka vid fel
- Applikationens uttryck för **ACID**-egenskaperna
- Håll transaktioner **korta** — inga HTTP-anrop, ingen användarinteraktion inuti

---

## Isoleringsnivåer
- Styr vad samtidiga transaktioner ser av varandras pågående ändringar
- **Read Uncommitted** — snabbast, tillåter smutsiga läsningar
- **Read Committed** — vanligt standardval, förhindrar smutsiga läsningar
- **Repeatable Read** — samma rad ger samma värde inom transaktionen
- **Serializable** — starkast, transaktionerna beter sig som om de kördes i tur och ordning

---

## Optimistisk kontra pessimistisk låsning
- **Pessimistisk låsning** — lås raden före läsning eller skrivning; andra väntar
- **Optimistisk låsning** — läs med versionsfält, uppdatera bara om versionen stämmer
- Optimistisk passar **låg konkurrens** (profiländringar)
- Pessimistisk passar **hög konkurrens** (sista platsen, sista varan i lager)

---

## Praktiska regler
- **Återanvänd** klientinstanser — Singleton i DI
- Håll **transaktioner korta** och lokala till en databas
- **Undvik distribuerade transaktioner** — föredra outbox-mönstret med lokala transaktioner
- Connection string ligger i konfiguration, aldrig i källkod

---

## Frågor?
