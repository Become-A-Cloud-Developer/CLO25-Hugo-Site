+++
title = "Dockerfiles och multi-stage builds"
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

## Dockerfiles och multi-stage builds
Del VII — Containrar

---

## Imagen är driftsättningens enhet
- En **Dockerfile** är det versionshanterade receptet som producerar en image
- Samma indata, samma image — på vilken maskin som helst med en Docker-daemon
- Källkod, beroenden, runtime och entrypoint följs åt
- Reproducerbarhet är hela poängen med container-tekniken

---

## Dockerfile-instruktioner
- **FROM** — välj basimage; varje Dockerfile börjar här
- **WORKDIR** — sätt arbetskatalog för COPY och RUN
- **COPY** — hämta filer från build-kontexten in i imagen
- **RUN** — kör ett skalkommando under build (skapar ett lager)
- **EXPOSE / ENV / ARG** — dokumentera portar, sätt runtime- / build-variabler

---

## Build-kontexten
- Den katalog som skickas till `docker build` — allt COPY kan nå
- CLI:n skickar hela kontexten till daemonen över ett socket
- En uppsvälld kontext ger långsamma builds och läckta hemligheter
- **.dockerignore** utesluter mönster från kontexten (som `.gitignore`)
- Uteslut alltid `bin/`, `obj/`, `node_modules/`, `.git/`, `.env`

---

## Lagercachen och instruktionsordning
- Varje instruktion fingeravtrycks; oförändrade indata återanvänder lagret
- Cachen invalideras vid första ändrade steg — alla följande steg körs om
- Kopiera beroendefiler *före* källkod: `package.json` sedan `npm install`
- Källkodsändringar triggar inte längre en full ominstallation
- Samma trick för `*.csproj` + `dotnet restore`, `requirements.txt` + `pip install`

---

## ENTRYPOINT mot CMD
- **ENTRYPOINT** — den binär som alltid körs när en container startar
- **CMD** — standardargument, kan skrivas över på `docker run`
- App-imager: lås entrypoint till binären — imagen är självdokumenterande
- Verktygs-imager: lämna öppen, använd CMD för flexibilitet
- Använd alltid JSON-array-formen så att `SIGTERM` når applikationen

---

## Multi-stage-mönstret
- Byggverktyg (SDK, kompilator, pakethanterare) behövs inte vid körning
- Att packa med dem ökar imagestorlek och attackytan
- En **multi-stage build** använder flera `FROM`-instruktioner
- Tidiga stadier kompilerar och publicerar; sista stadiet håller bara runtime
- `COPY --from=build` flyttar artefakten över stadiegränsen

---

## .NET multi-stage-exempel
- Stadie 1: `FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build`
- `dotnet restore`, sedan `dotnet publish -c Release -o /app/publish`
- Stadie 2: `FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final`
- `COPY --from=build /app/publish .` — bara runtime + publicerade DLL:er
- Slutlig image krymper från ~1 GB till ~200 MB

---

## Praktik
- Övning: [/exercises/20-docker/](/exercises/20-docker/)
- Delövning 3 går igenom .NET single-stage till multi-stage-refaktoreringen
- Jämför imagestorlekar med `docker images` före och efter
- Ändra en källkodsfil och se vilka lager som byggs om

---

## Frågor?
