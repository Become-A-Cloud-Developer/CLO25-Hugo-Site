+++
title = "Hälsokontroller"
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

## Hälsokontroller
Del IX — Drift och observabilitet

---

## Varför en TCP-probe inte räcker
- Orkestratorn bestämmer varje sekund: behåll, starta om eller dränera en replika
- Standardprob är "lyckades **TCP connect**" — nästan alltid sant
- Processen kan leva men vara **deadlockad**, sakna beroenden eller hänga i init
- Applikationen måste själv publicera sin syn på sin hälsa

---

## Vad en hälsokontroll är
- En HTTP-endpoint — typiskt **`/healthz`** — som applikationen själv äger
- Returnerar **`200 OK`** vid frisk, **`503 Service Unavailable`** vid sjuk
- Valfri JSON-body listar delkontroller för felsökning av människor
- Måste vara **snabb och billig** — probens budget är några hundra millisekunder

---

## Tre kanoniska probtyper
- **Liveness probe** — kör processen, används för omstartsbeslut
- **Readiness probe** — kan processen ta emot trafik just nu, gatear lastbalanseraren
- **Dependency probe** — är en nedströms tjänst nåbar, byggsten i readiness
- Varje prob svarar på en annan fråga och triggar olika plattformsåtgärder

---

## Liveness mot readiness
- **Liveness fel** → döda och starta om containern
- **Readiness fel** → ta ur lastbalanseraren, starta inte om
- Att blanda ihop dem ger **kaskaderande omstartsloopar** vid kortvariga beroendefel
- Liveness får inte anropa beroenden; readiness gör det oftast

---

## /healthz-konventionen
- `/healthz/live` för liveness, `/healthz/ready` för readiness
- Sökvägen är konvention — Kubernetes-mallar och ASP.NET Core utgår från den
- Separata sökvägar håller de två ansvaren isär
- Samma kontrakt fungerar för Container Apps, Kubernetes och smoke-gaten

---

## ASP.NET Core IHealthCheck
- `IHealthCheck`-gränssnittet — en async-metod som returnerar Healthy / Degraded / Unhealthy
- Färdiga kontroller för **Mongo, Key Vault, SQL, Redis** levereras som extension-metoder
- Egna kontroller implementerar gränssnittet och får beroenden via DI
- **Taggar** dirigerar varje kontroll till rätt probsökväg

---

## Exempel: registrera Mongo och Key Vault
- `AddHealthChecks().AddCheck("self", ..., tags: ["live"])`
- `.AddMongoDb(..., tags: ["ready"])` och `.AddAzureKeyVault(..., tags: ["ready"])`
- `MapHealthChecks("/healthz/live", Predicate = c => c.Tags.Contains("live"))`
- `MapHealthChecks("/healthz/ready", ...)` filtrerar på ready-taggade kontroller

---

## Hälsokontroller och smoke-gaten
- Pipelinens smoke-gate curlar redan den nya revisionen efter `az containerapp update`
- Naturligt mål är **`/healthz/ready`** för den nya revisionen
- 200 bevisar att imagen drogs, containern bootade och beroenden är nåbara
- 503 stoppar gaten innan användare ser något 5xx — ren rollback

---

## Hänger ihop med resten av Del IX
- **Loggar** svarar på "vad hände"; hälsokontroller driver automatisk återhämtning
- Readiness formar **autoskalningen** — sjuka replikor räknas inte som kapacitet
- Dependency probes synliggör fel som loggar ensamma skulle missa
- En endpoint betjänar både kontinuerlig polling och engångs-gate vid deploy

---

## Frågor?
