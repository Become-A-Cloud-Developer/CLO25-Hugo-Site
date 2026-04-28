+++
title = "Log Analytics och KQL — grunderna"
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

## Log Analytics och KQL — grunderna
Del IX — Drift och observabilitet

---

## Varför ett frågespråk
- App Insights-dashboards svarar på de **vanliga** frågorna
- Långsvansade incidenter kräver **godtyckliga** frågor mot samma data
- Ett frågespråk gör en engångsfråga till ett enradssvar
- Datan finns redan där — den behöver bara rätt sätt att frågas

---

## Workspace och tabeller
- En **Log Analytics**-workspace är det gemensamma tidsseriearkivet
- Container Apps och Application Insights skriver båda dit
- Datan partitioneras i **tabeller**, en per datakällstyp
- `AppRequests`, `AppExceptions`, `AppDependencies`, `ContainerAppConsoleLogs_CL`

---

## KQL som pipeline av transformer
- **KQL** (Kusto Query Language) kedjar operatorer med tecknet `|`
- Varje operator transformerar tabellen som flödar genom den
- Läs uppifrån och ned — källa, filter, smala av, aggregera, sortera
- Pipelinen är den naturliga enheten för att skriva och granska

---

## Arbetshäst-operatorerna
- `where` filtrerar rader — behåll det som spelar roll, beskär tidigt
- `project` väljer kolumner — smalnar raden före nedströmsarbete
- `summarize` aggregerar — `count()`, `avg()`, `percentile()` per grupp
- `extend` lägger till en beräknad kolumn — `bin(TimeGenerated, 1m)` för diagram

---

## Tidsfönstret först, alltid
- Ett **tidsfönster** begränsar kostnad, latens och partitioner som scannas
- `where TimeGenerated > ago(1h)` hör hemma **först** i varje fråga
- Portalens tidsväljare är bekväm men inte portabel
- Bädda in filtret så att frågan funkar i workbooks och alerts också

---

## Genomgånget exempel: misslyckade requests
- `AppRequests | where TimeGenerated > ago(1h)`
- `| where ResultCode startswith "5"`
- `| summarize FailureCount = count() by OperationName`
- `| order by FailureCount desc | take 10`

---

## Retention som kostnadsratt
- **Retention** styr hur långt bak frågor kan nå — standard **30 dagar**
- Konfigurerbart från 7 dagar till 2 år, per workspace eller per tabell
- Längre retention kostar mer — betala per GB ingesterad **och** per GB lagrad
- Tunna per tabell: behåll `AppRequests` längre, släpp konsolloggar tidigare

---

## Spara nyttiga frågor
- **Sparade frågor** persistar en engångsfråga i workspace eller användarkonto
- **Workbooks** kombinerar frågor, diagram, parametrar i en utredningssida
- En workbook med datumintervall-parameter matar varje fråga inuti den
- Domänspecifika workbooks blir teamets utredningsplaybook

---

## Frågor?
