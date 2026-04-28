+++
title = "Images och lager"
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

## Images och lager
Del VII — Containrar

---

## Varför images finns
- En container är en **isolerad process** — men en process behöver filer
- En **image** är den oföränderliga filsystem-mallen som containern startar från
- Innehåller både **bitarna** (binärer, bibliotek, konfiguration) och **metadata** (entrypoint, env, portar)
- Två containrar från samma image ser **identiska filsystem** vid start

---

## Vad en image bär med sig
- En **skrivskyddad ögonblicksbild** av applikationen och dess beroenden
- Standard-**ENTRYPOINT** och **CMD**
- **Miljövariabler**, arbetskatalog, exponerade portar, körningsanvändare
- En oföränderlig **digest** som fingeravtrycker varje byte och varje konfigurationsfält

---

## Lagermodellen
- Varje Dockerfile-instruktion (`FROM`, `COPY`, `RUN`) skapar ett **lager**
- Lager är **skrivskyddade filsystems-ögonblicksbilder**, staplade via ett union-filsystem
- Containern lägger till ett tunt **skrivbart lager** ovanpå — slängs vid stopp
- Borttagningar tar inte bort bytes; de bara **maskerar** dem i ett högre lager

---

## Innehållsadressering
- Varje lager identifieras av en **SHA256-hash** av sitt innehåll
- Identiskt innehåll ger identiska hashar — över värdar, över byggen
- Register lagrar varje lager **en gång**, även när det delas av 100 images
- Build-cachen återanvänder ett lager när dess **indata är oförändrade**

---

## Lagerordning spelar roll
- En cache-miss **kaskaderar**: varje lager nedanför måste köras om
- Ordna instruktioner från **minst till mest föränderliga**
- För .NET: kopiera `*.csproj` och kör `restore` **innan** källkoden kopieras in
- Belöningen är byggen som tar **sekunder**, inte minuter

---

## Base image
- Första `FROM`-instruktionen — ger OS, runtime, pakethanterare
- `mcr.microsoft.com/dotnet/aspnet:10.0` — Debian, ~220 MB, full runtime
- `...:10.0-alpine` — Alpine + musl libc, ~110 MB
- Mindre bas: snabbare pull, mindre attackyta, ibland **glibc/musl-friktion**

---

## Image manifest
- Ett litet **JSON-dokument** som listar lager, digests, storlekar, plattform
- En pull hämtar manifestet **först**, sedan bara de saknade lagren
- Manifestets egen SHA256 är imagens **oföränderliga digest**
- En **manifest list** mappar en tag till flera plattformsspecifika manifest

---

## Inspektera en image
- `docker pull mcr.microsoft.com/dotnet/aspnet:10.0`
- `docker history <image>` går igenom lagerstacken nedifrån och upp
- Avslöjar **bas-rootfs**, runtime-installation, .NET-kopia, metadata-lager
- Visar exakt **var bytsen ligger** och vad som är värt att optimera

---

## Frågor?
