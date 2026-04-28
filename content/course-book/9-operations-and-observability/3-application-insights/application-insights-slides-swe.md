+++
title = "Application Insights och telemetri"
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

## Application Insights och telemetri
Del IX — Drift och observabilitet

---

## Varför en APM-destination
- `ILogger` skriver till stdout — bra, men bara en pelare
- En loggström svarar på **vad** som hände, inte **hur snabbt** eller **hur ofta**
- Request-tider, beroendetider och exceptions kräver en rikare sink
- **Application Insights** är Azures hanterade APM-destination för .NET

---

## Vad Application Insights faktiskt är
- En **APM**-tjänst från Microsoft byggd på en **Log Analytics-workspace**
- **Workspace-baserat läge** lägger app-telemetri bredvid container-stdout
- Ett frågbart lager för `requests`, `exceptions`, `traces`, `ContainerAppConsoleLogs`
- Legacy-läge (separat lagring) finns kvar, men är aldrig rätt val

---

## SDK:ns automatiska instrumentering
- **Requests** — URL, metod, varaktighet, status, operation ID per HTTP-anrop
- **Beroenden** — `HttpClient`, SQL, Azure SDK-anrop tidsmäts automatiskt
- **Exceptions** — typ, meddelande, stack trace, korrelerade till sin request
- **Traces** — varje `ILogger`-rad över den konfigurerade nivån

---

## Connection string och secret-referens
- Modernt format: en **connection string**, inte den gamla instrumentation key
- SDK:n läser `APPLICATIONINSIGHTS_CONNECTION_STRING` från miljön
- I Container Apps: lägg som secret, injicera med `secretref:appinsights-cs`
- En rad räcker: `services.AddApplicationInsightsTelemetry(...)`

---

## De fyra dashboards utvecklare når efter
- **Live Metrics** — senaste 60 sekunderna, ~1 s latens, gratis, hoppar över ingestion
- **Application Map** — noder per tjänst, kanter med p95-latens och felgrad
- **Failures** — exceptions grupperade per typ, drill-down via operation ID
- **Performance** — långsamma operationer rankade per p50/p95/p99-latens

---

## Sampling som kostnadsspak
- Telemetri faktureras per GB ingestion — pratglada tjänster blir dyra
- **Adaptive sampling** är på by default, struper över ~5 items/sekund
- Samplar **hela operationer**, aldrig isolerade exceptions
- Backend reviktar serverside, så antal och medelvärden förblir korrekta

---

## Egna events och metrics
- Injicera `TelemetryClient` från DI, anropa `GetMetric` eller `TrackEvent`
- `GetMetric("home-page-views").TrackValue(1)` — aggregeras klientside, billigt
- `TrackEvent("HomePageViewed", props)` — ett item per anrop, affärssignaler
- Egna items ärver requestens **operation ID** automatiskt

---

## Vad den inte löser av sig själv
- En komponent per miljö, alla tjänster pekar på samma
- Icke-app-data (plattformens audit-loggar, infra-metrics) bor någon annanstans
- Korrelation över tjänster kräver att alla delar samma komponent
- KQL i workspace binder ihop App Insights-tabellerna med container-stdout

---

## Frågor?
