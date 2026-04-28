+++
title = "Multi-Plattformsbyggen"
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

## Multi-Plattformsbyggen
Del VII — Containrar

---

## Problemet med felmatchning
- Utvecklare på **Apple Silicon** (arm64) bygger en image
- Pushar till register, driftsätter till **Azure VM** (amd64)
- Körningen misslyckas med `exec format error`
- Single-plattform-images antar att byggvärd = körvärd

---

## Vad en plattform är
- **Plattform** = `os/arkitektur/variant` (t.ex. `linux/amd64`)
- CPU:er avkodar endast sin egen instruktionsuppsättning
- Container-images paketerar kompilerade binärer
- `uname -m` avslöjar värdens arkitektur

---

## Arkitektur spelar roll i tysthet
- `docker build` defaultar till värdens arkitektur
- Ingen varning för att image:n är icke-portabel
- Felmatchning syns först vid körning på annan värd
- Fel: `exec format error`, saknad körbar fil

---

## Buildx som multi-plattformsbyggare
- **Buildx** = Docker CLI-tillägg som använder BuildKit
- `docker buildx build --platform linux/amd64,linux/arm64`
- Bygger Dockerfile en gång per målplattform
- Kräver `--push` — lokala lagret hanterar inte multi-arkitektur

---

## QEMU möjliggör korsbyggen
- **QEMU** emulerar främmande instruktionsuppsättningar i userspace
- Kärnan routar främmande binärer till QEMU via `binfmt_misc`
- Bygger AMD64-images på en ARM64-värd transparent
- Avvägning: emulerade byggen är 5–10× långsammare

---

## Manifest-listan som indirektion
- **Manifest-lista** = register-metadata som mappar en tagg till flera digests
- En tagg → lista → per-arkitektur image-manifest
- Klienten hämtar endast lagren som matchar dess värd
- Samma `docker pull`, olika bytes per värd

---

## Hur registret lagrar det
- Lager adresseras innehållsmässigt via SHA256
- Identiska lager delas mellan arkitekturer (ovanligt för binärer)
- OCI-standard — stöds av Docker Hub, ACR, GHCR
- `docker buildx imagetools inspect` visar strukturen

---

## Genomarbetat exempel
- `docker buildx create --name multiarch --use`
- `docker buildx build --platform linux/amd64,linux/arm64 -t myimage:1.0 --push .`
- ARM64 byggs nativt; AMD64 emuleras under QEMU
- Resultat: en tagg, två arkitekturer, transparenta pulls

---

## När det är värt det
- Team med blandade arkitekturer (Apple Silicon + amd64 i molnet)
- ARM-mål i molnet (Graviton, Ampere) för kostnadseffektivitet
- Edge / IoT-driftsättning parallellt med serverdriftsättning
- Annars: single-plattform förblir enklare och snabbare

---

## Frågor?
