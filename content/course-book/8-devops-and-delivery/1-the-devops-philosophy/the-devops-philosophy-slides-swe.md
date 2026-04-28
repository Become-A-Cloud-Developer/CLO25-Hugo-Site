+++
title = "DevOps-filosofin"
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

## DevOps-filosofin
Del VIII — DevOps och leverans

---

## Den historiska klyftan
- Utvecklare skrev kod; **drift** körde den
- Kvartalsvisa releaser, helgnatt-driftsättningar, skuldbeläggning vid fel
- Två team med **motsatta mål**: förändring vs. drifttid
- Kostnaden gömdes i **batch-storlek** och köer mellan team

---

## Vad DevOps förändrade
- **Delat ägarskap**: bygg det, kör det, ta jouren
- **Skuldfria post-mortems** söker saknade skyddsräcken, inte personer
- **Automation som standard**: allt som görs två gånger skriptas
- Kulturen först; verktyg och mått därefter

---

## De fyra DORA-måtten
- **Ledtid** — från commit till produktion
- **Driftsättningsfrekvens** — hur ofta kod når användarna
- **MTTR** — från incident till återställd tjänst
- **Change-failure rate** — andel driftsättningar som orsakar incident
- Två mäter genomflöde; två mäter stabilitet

---

## Genomflöde vs. stabilitet
- Genomflöde utan stabilitet = kaospipeline
- Stabilitet utan genomflöde = fryst releasecykel
- Elit-team scorar högt på **alla fyra** samtidigt
- Frekvent övning av återställning sänker MTTR

---

## Värdeströmmen
- Stegen från idé till körande kod
- Mest tid är **väntan**, inte arbete
- Slöseri gömmer sig i stora batchar och manuella överlämningar
- Kartlägg först, automatisera värsta kön sedan

---

## Konkret exempel: en tvåårig resa
- Före: kvartalsvisa driftsättningar, 8 h MTTR, 30% change-failure
- Efter: dagliga driftsättningar, 12 min MTTR, 5% change-failure
- Kulturskifte: delad jour + skuldfria genomgångar
- Verktygsskifte: GitHub Actions + Container Apps + smoke tests

---

## Vad Del VIII täcker
- CI vs. CD, pipelines som kod, gates, driftsättningsstrategier
- Hemligheter, OIDC-federation, Azure Container Apps som mål
- Varje kapitel svarar: vilket DORA-mått påverkar detta?

---

## Frågor?
