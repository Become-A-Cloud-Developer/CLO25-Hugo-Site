+++
title = "Konfiguration och miljöer"
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

## Konfiguration och miljöer
Del III — Applikationsutveckling

---

## Varför externalisera konfiguration
- Samma binär ska köra i **dev, staging och produktion** oförändrad.
- **Hemligheter** får inte hamna i versionshantering — git-historik är permanent.
- Driftpersonal ändrar inställningar utan att bygga om applikationen.
- Flera driftsättningar av samma binär lever med olika inställningar.

---

## Leverantörskedjan
- `appsettings.json` — grundvärden som följer med källkoden.
- `appsettings.{Environment}.json` — miljöspecifik **override**.
- **User-secrets** — endast lokala utvecklingsuppgifter.
- **Miljövariabler** — driftsättningsspecifika värden och produktionshemligheter.
- Kommandoradsargument — sista ordet vid diagnostiska körningar.

---

## IConfiguration som läsgränssnitt
- `IConfiguration` slår ihop kedjan till ett **nyckel-värde-träd**.
- Hierarkiska nycklar använder kolon: `MongoDB:ConnectionString`.
- Injiceras via DI; konsumenten vet inte vilken leverantör värdet kom från.
- Returnerar `null` för nycklar som ingen leverantör tillhandahåller.

---

## Val av miljö
- `ASPNETCORE_ENVIRONMENT` styr aktiv miljö.
- Standard är `Production` när variabeln saknas — säkrare än dev.
- Läser in `appsettings.<env>.json` och aktiverar utvecklarfunktioner.
- `app.Environment.IsDevelopment()` styr körtidsbeteende.

---

## User-secrets-arbetsflödet
- Lagras utanför projektmappen, i användarprofilen.
- Kopplas till projektet via `UserSecretsId`-GUID i `.csproj`.
- CLI: `dotnet user-secrets init`, `set`, `list`.
- Aktiv **endast** när miljön är `Development`.

---

## Override via miljövariabler
- `MongoDB:ConnectionString` blir `MongoDB__ConnectionString`.
- Dubbelt understreck ersätter kolon — de flesta OS förbjuder kolon i variabelnamn.
- Leverantören ligger senare i kedjan och åsidosätter JSON-filerna.
- Standardmetod för produktionshemligheter och driftsättningsvärden.

---

## Starkt typade IOptions
- `IOptions<T>` binder en konfigurationssektion till en typad C#-klass.
- Tar bort magiska strängar — felstavningar blir kompileringsfel.
- Sektionsvägen lever på ett ställe, inte hos varje konsument.
- `IOptionsSnapshot<T>` och `IOptionsMonitor<T>` för förändringskänsliga fall.

---

## Dev vs produktions-hemlighetslager
- User-secrets och miljövariabler lagrar hemligheter i **klartext på disk**.
- Produktion kräver kryptering, åtkomstlogg och rotation.
- Hanterade hemlighetslager (Azure Key Vault) ansluts till samma kedja.
- Detaljeras i Del V — Managed Identities och Key Vault.

---

## Frågor?
