+++
title = "Driftsättningsstrategier"
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

## Driftsättningsstrategier
Del VIII — DevOps och Leverans

---

## Varför en strategi behövs
- En driftsättning är det **mest riskfyllda ögonblicket** — den nya koden har aldrig burit riktig trafik
- Strategier skiljer sig på **omkopplingstid**, **blast radius** och **rollback-hastighet**
- Snabbare omkoppling betyder oftast **större blast radius**
- Säkrare utrullning betyder oftast **mer infrastruktur** att underhålla

---

## Manuell gate
- En **manuell gate** stoppar pipelinen tills en människa klickar **Godkänn**
- Continuous **delivery** utan continuous **deployment**
- Fångar "fel ögonblick"-fel — fredagsdeploy, kunddemo, misstänkt build
- Avvägning: **lead time** när godkännaren inte är tillgänglig

---

## Rolling deployment
- Byt replikor **en batch i taget** bakom lastbalanseraren
- Applikationen är tillgänglig under hela utrullningen
- `maxUnavailable` och `maxSurge` formar kapaciteten under fönstret
- Avvägning: ett **fönster med blandade versioner** kan visa kompatibilitetsbuggar

---

## Blue-green deployment
- Två identiska miljöer — en **aktiv**, en **vilande**
- Driftsätt till den vilande, röktesta, **växla trafik atomärt**
- Rollback är en enda växel tillbaka — mäts i sekunder
- Avvägning: ungefär **2× infrastruktur** under driftsättningsfönstret

---

## Canary deployment
- Routa **1–5%** av trafiken till den nya versionen först
- Bevaka felfrekvens, latens och KPI:er mot tröskelvärden
- Ramp upp **1% → 10% → 50% → 100%** om mätvärdena håller; backa om inte
- Kräver trafikuppdelning + metric-styrd promotion

---

## Feature-flaggor
- En **feature-flagga** slår på eller av en funktion vid körning, utan redeploy
- Frikopplar **driftsättning** (installera kod) från **release** (exponera för användare)
- Kombineras med vilken strategi som helst — flagga inuti en canary-build
- Rollback via flaggväxling är snabbare än redeploy

---

## Välja en strategi
- **Manuell gate** — sällan-releaser; människan tillför signalen som saknas
- **Rolling** — tillståndslösa tjänster; bakåtkompatibla ändringar
- **Blue-green** — omkopplingskänsligt; inkompatibla ändringar med tidigare migration
- **Canary** — rika mätvärden; behöver verifiering mot riktig trafik före full exponering

---

## Genomgång — Container Apps
- Övningens manuella gate: pusha image, klicka sedan **Create new revision**
- Automatiserad form: `az containerapp update` + `curl --fail https://$FQDN/health`
- Ett misslyckat röktest avslutar workflowet med non-zero
- Multi-revision-läge + `traffic`-vikter ger en riktig **canary**-primitiv

---

## Frågor?
