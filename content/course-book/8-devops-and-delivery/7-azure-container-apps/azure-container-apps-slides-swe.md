+++
title = "Azure Container Apps som driftsättningsmål"
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

## Azure Container Apps som driftsättningsmål
Del VIII — DevOps och leverans

---

## Varför en hanterad runtime
- Statslösa tjänster behöver **skalning, ingress, certifikat** — men inte ett helt kluster
- Vanliga VM:ar lämnar patchning och lastbalansering till teamet
- Ett fullt **Kubernetes**-kluster lägger på noder, RBAC och ingress-controllers
- **Azure Container Apps** sitter i mellanrummet — hanterad, serverless, opinionsstark

---

## Container Apps-modellen
- En **Container App-miljö** är gränsen för nätverk och observabilitet
- En **container app** är den driftsatta enheten — image, env-vars, ingress, skalning
- En **revision** är en oföränderlig ögonblicksbild av appens image och konfiguration
- Varje meningsfull ändring skapar en **ny revision** — aldrig en ändring på plats

---

## Single- mot multiple-revision-läge
- **Single-revision** ersätter den aktiva revisionen vid varje deploy
- **Multiple-revision** låter gamla revisioner leva kvar för **trafikuppdelning**
- Multiple-revision är grunden för **canary** och **blue-green**-strategier
- Avställda revisioner kostar inget vid noll trafik, men skräpar i listan

---

## Ingress
- **Ingress** kopplar in publik **FQDN**, HTTPS och Layer 7-routing
- Hanterade certifikat — ingen manuell certifikatlivscykel
- **Egna domäner** via CNAME plus plattformsutfärdade eller uppladdade certifikat
- **mTLS** på miljönivå autentiserar anrop mellan tjänster

---

## Skalningsregler
- **HTTP-samtidighet** — skala på antal pågående requests per replika
- **CPU / minne** — skala på observerat resurstryck
- **Egna KEDA-scalers** — Service Bus-ködjup, Kafka-lag, cron
- **Skala till noll** med `--min-replicas 0` — betala inget när tyst

---

## Driftsättningens uppdateringsflöde
- `az containerapp update --image mycr.azurecr.io/cloudci:1.0` skapar en ny revision
- Plattformen patchar aldrig en körande revision — alltid en **ny oföränderlig** version
- `az containerapp revision list` visar revisionshistoriken per app
- Pipelinens smoke-gate verifierar att den nya revisionen är **aktiv** och frisk

---

## Hämta från ACR med managed identity
- Naivt: lagra registrets användarnamn + lösenord som app-konfiguration
- Native: ge appen en **managed identity**, tilldela den `AcrPull` på registryt
- Runtimen mintar en **kortlivad ACR-token** vid varje pull — ingen hemlighet sparas
- Samma primitiv som andra Azure-tjänster använder för att läsa från ACR

---

## Hänger ihop med övriga delen
- **Pipeline** bygger och pushar imagen
- **Smoke-gate** curlar FQDN efter `az containerapp update`
- **Driftsättningsstrategin** väljer single- eller multiple-revision-läge
- **OIDC-federation** låter pipelinen anropa `az containerapp update` utan hemlighet

---

## Frågor?
