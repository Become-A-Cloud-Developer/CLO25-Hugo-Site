+++
title = "Hemlighetshantering"
program = "CLO"
cohort = "25"
courses = ["BCD", "ACD"]
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

## Hemlighetshantering
Del V — Identitet och säkerhet

---

## Varför secrets behöver en egen lagring
- Secrets i versionshantering är vanligaste vägen till produktionsintrång
- Miljövariabler ligger i **klartext** på värden
- Rotation via omdriftsättning lämnar lång giltig tidsperiod efter läcka
- Inget revisionsspår — ingen vet vem som läst vad, när

---

## Azure Key Vault
- **Azure Key Vault** är en hanterad tjänst för secrets, nycklar och certifikat
- Secrets hämtas över HTTPS med en bearer token från **Entra ID**
- Varje secret är **versionerad** — sätt nytt värde, gammalt värde bevaras
- Varje läsning loggas med anropande identitet, tidsstämpel och käll-IP

---

## Bootstrapping-problemet
- Att läsa en Key Vault-secret kräver autentisering mot Entra ID
- Att lagra en autentiseringsuppgift omintetgör hela poängen
- **Managed Identity** — en Entra ID-identitet kopplad till compute-resursen
- Plattformen roterar bakomliggande credentials automatiskt — ingen secret i kod

---

## DefaultAzureCredential
- En credential-klass provar en kedja av källor i ordning
- I Azure: hittar värdens **Managed Identity**
- På en laptop: faller tillbaka till `az login`-sessionen
- Samma kod körs i produktion och utveckling utan ändring

---

## RBAC rolltilldelningar
- **RBAC rolltilldelning** = säkerhetsprincipal + roll + omfång
- Applikationsidentiteten får `Key Vault Secrets User` (läsbehörighet)
- Omfång på **valvet**, inte prenumerationen — least privilege
- Operatörer får `Key Vault Secrets Officer` för rotation, åtskilt från appen

---

## Konfigurationsproviderkedjan
- `AddAzureKeyVault(uri, new DefaultAzureCredential())` lägger till valvet i kedjan
- Secret `MongoDB--ConnectionString` blir nyckeln `MongoDB:ConnectionString`
- Controllern läser `configuration["MongoDB:ConnectionString"]` — oförändrat
- Källan flyttar från miljövariabel till valv utan kodändring

---

## Applikations-secrets vs pipeline-secrets
- **Applikations-secrets** — connection strings, API-nycklar → Key Vault
- **Pipeline-secrets** — registercredentials, prenumerationer → GitHub Actions secrets
- **OIDC-federation** eliminerar långlivade deploy-credentials helt
- Pipeline-sidan behandlas i Del VIII (DevOps och leverans)

---

## Operativ checklista
- **Aldrig** committa en secret till versionshantering — använd pre-commit-skanners
- **Aldrig** logga en secret — redigera bort i strukturerad loggning
- **Aldrig** mejla eller chatta en secret — använd engångsverktyg
- **Rotera vid misstanke** — billig omkonfigurering slår dyrt intrång

---

## Frågor?
