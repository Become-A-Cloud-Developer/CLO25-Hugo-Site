+++
title = "Docker Compose"
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

## Docker Compose
Del VII — Containers

---

## Varför Compose finns
- En riktig applikation är sällan **en enda container** — webb + databas + kö är tre
- `docker run` för varje del blir en lång, ömtålig kommandosekvens
- Ordning, flaggor och hostnamn måste stämma på varje utvecklarmaskin
- En ny utvecklare som ansluter till projektet ska inte behöva den ramsan
- **Compose** samlar allt i en fil som checkas in i repot

---

## Vad Compose är
- **Docker Compose** = ett verktyg som läser `docker-compose.yml` och orkestrerar fler-container-applikationer
- Riktar sig mot **lokal utveckling**, inte produktion
- Automatiserar nätverk, volymhantering och miljökonfiguration
- Ett kommando — `docker compose up` — startar hela stacken
- Samma fil driver integrationstester i CI

---

## Compose-filen
- Ett YAML-dokument, vanligtvis `compose.yaml` i repots rot
- Tre topp-nivå-nycklar som spelar roll: `services`, `volumes`, `networks`
- `services` är obligatorisk; övriga är valfria
- Compose skapar ett **standardnätverk** för projektet automatiskt
- Modern Compose ignorerar den gamla `version:`-nyckeln — utelämna den

---

## Tjänster
- En **tjänst** = en namngiven, containeriserad applikation
- Varje post under `services:` blir en container vid körning
- Tjänstnamnet fungerar samtidigt som **DNS-hostnamn** i projektnätverket
- Antingen `build: .` (bygg från lokal Dockerfile) eller `image: mongo:7` (hämta från registry)
- `ports: ["8080:8080"]` publicerar till hosten; tjänst-till-tjänst-trafik behöver inte det

---

## Named networks och DNS
- Ett **named network** skapas och hanteras av Compose
- Tjänster på samma nätverk hittar varandra via **tjänstnamnet**
- Compose kör en inbäddad DNS-server inne i projektnätverket
- Webb-containern når databasen via `mongodb://db:27017` — ingen `localhost`, ingen IP
- Ny container, ny IP — nästa DNS-uppslagning returnerar den nya adressen

---

## Volymer
- Containers är **flyktiga** — `docker compose down` slänger deras skrivbara lager
- En **namngiven volym** är en katalog som Docker-daemonen hanterar på hosten
- Överlever container-borttagning; ansluts igen vid nästa `docker compose up`
- `volumes: { db-data: {} }` deklarerar den; `volumes: [db-data:/data/db]` monterar den
- Bind mounts är till för källkod, inte för databastillstånd

---

## Miljövariabler och .env
- Containeriserade applikationer läser konfiguration från **miljövariabler**
- `environment:` sätter variabler inne i en tjänst
- `.env`-filen läses automatiskt; värden interpoleras som `${VAR}` i compose-filen
- `.env` är gitignored — hemligheter stannar utanför repot
- `MongoDB__ConnectionString=mongodb://db:27017` är .NET-mönstret

---

## Beroendeordning
- **`depends_on`** anger att en tjänst ska starta efter en annan
- Garanterar inte att beroendet är **redo** — bara att det har startat
- Kombinera med en `healthcheck` och `condition: service_healthy` för readiness
- Healthcheck kör en probe inne i beroende-containern
- Produktionslösningen är **retry-logik** i applikationen

---

## Exempel: web + db
- `web`-tjänsten byggs från lokal Dockerfile, port 8080 publiceras
- `db`-tjänsten är `mongo:7`, volymen `db-data` monteras på `/data/db`
- Båda tjänsterna i namngivna nätverket `appnet`
- `web` väntar på att `db`-healthcheck (`mongosh --eval ping`) ska passera
- `MongoDB__ConnectionString=mongodb://db:27017` löses upp via Compose-DNS

---

## Compose vs produktion
- Compose kör på **en enda host** — ingen horisontell skalning, ingen självläkning
- **Azure Container Apps** för hanterad fler-host-produktion
- **Kubernetes** för storskaliga eller komplexa topologier
- Tjänst / volym / miljövariabel-koncepten översätts framåt — Del VIII utvecklar dem
- Compose är lokal utveckling; produktion är en annan runtime

---

## Frågor?
