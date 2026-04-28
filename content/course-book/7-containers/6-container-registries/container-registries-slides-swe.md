+++
title = "Container Registries"
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

## Container Registries
Del VII — Containers

---

## Varför ett registry finns
- En byggd image ligger i **en daemons lagring** tills den skickas vidare
- Driftsmål — kollegor, CI, produktion — behöver en **delad plats** att hämta från
- Ett **container registry** är den delade lagringen, nås över OCI Distribution API
- Samma protokoll för Docker Hub, ACR, GHCR och alla andra register

---

## Hur ett registry lagrar images
- Layers är **innehållsadresserade blobs** — nycklade på SHA256
- Ett **image manifest** listar de blobs som utgör en image
- Repositories grupperar versioner av samma logiska image
- Referenssyntax: **`host/repo:tagg`** (t.ex. `mycr.azurecr.io/myapp:1.0`)

---

## Taggar mot digests
- En **tagg** är en **muterbar** pekare — `:latest` kan omplaceras
- En **digest** är en **SHA256-hash** av manifestet — oföränderlig
- Digesten ändras om en enda byte i någon layer ändras
- Produktionsdeploys bör **pinna mot en digest**, inte en tagg

---

## Push och pull
- **`docker push host/repo:tagg`** — laddar upp bara de layers registryt saknar
- **`docker pull host/repo:tagg`** — hämtar manifestet, sedan saknade layers
- Delade bas-layers överförs **en gång** över många images
- Återupptagbart per layer — tappad anslutning gör om bara den layern

---

## Genomgång — tagga och pusha
- `docker tag myapp:local mycr.azurecr.io/myapp:1.0` lägger till ett **andra namn**
- `docker push mycr.azurecr.io/myapp:1.0` strömmar layers till ACR
- Taggen talar om för daemonen **var** den ska pusha — ingen omgygging sker
- Vilken autentiserad maskin som helst kan nu `docker pull` samma bytes

---

## Publikt mot privat
- **Docker Hub** — det publika standard-registryt; hem för nginx, ubuntu, postgres
- Anonyma pulls är **rate-limiterade** — CI-runners slår i taket på delade IP:n
- **Azure Container Registry (ACR)** — privat, åtkomst styrd av Azure-identitet
- Riktiga system blandar båda: bas-images från Hub, app-images till ACR

---

## Autentisera mot registryt
- **Docker Hub** — användarnamn + **PAT** lagrat via `docker login`
- **ACR från laptop** — `az acr login` växlar Azure-login mot en kort token
- **ACR från Azure-compute** — **managed identity** med rollen `AcrPull`
- **ACR från extern CI** — **OIDC-federation**, ingen lagrad hemlighet

---

## Säkerhetsskanningar
- Register skannar pushade images mot publika **CVE**-databaser
- Fynden hängs på en specifik **digest** — samma image, skannad en gång
- En typisk gate faller på **Critical** CVE:er, varnar för High och nedåt
- Skanning höjer golvet; det ersätter inte ett medvetet val av basimage

---

## Frågor?
