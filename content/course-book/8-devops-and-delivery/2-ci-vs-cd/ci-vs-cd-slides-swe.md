+++
title = "Continuous Integration vs Continuous Deployment"
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

## CI vs CD
Del VIII — DevOps och leverans

---

## CI/CD är tre praktiker, inte en
- **CI** — dagliga sammanslagningar till en gemensam trunk
- **Kontinuerlig leverans** — varje grön commit är redo att släppas, mänsklig grind
- **Kontinuerlig driftsättning** — varje grön commit driftsätts, ingen mänsklig grind
- Att klumpa ihop dem döljer vilket steg ett team bör ta härnäst

---

## Smärtan som motiverade CI
- Två-veckors feature-branchar driver hårt från trunken
- Namnbyten, beroendeuppgraderingar och refaktoreringar krockar vid merge
- Integrationsrisken växer **icke-linjärt** med branchens ålder
- Tre små dagliga merges slår en merge av tre dagars arbete

---

## Vad CI faktiskt kräver
- Varje commit triggar bygge och test, ovillkorligt
- Varje commit når en gemensam trunk inom timmar
- Ett rött bygge blockerar **alla** tills det är grönt igen
- Disciplinen kring det röda bygget är det som gör CI till en praktik

---

## Trunk-baserad utveckling
- Kortlivade branchar: timmar till två dagar, aldrig veckor
- Vertikal skivning: släpp migrationen först, sedan endpointen, sedan UI:t
- Feature flags döljer ofärdigt arbete bakom en runtime-switch
- Trunken har alltid produktionsform

---

## Pull request-grinden
- Trunkens kvalitetströskel flyttas till merge-tillfället
- Bygge grönt, tester gröna, linter ren, en peer review
- Skild från en driftsättningsgrind — PR-grinden skyddar själva trunken
- Snabb pipeline (~minuter) är en förutsättning, inte en lyx

---

## Spektrumet
- **CI** — trunken går alltid att bygga; driftsättning är en separat fråga
- **Kontinuerlig leverans** — grön commit kan driftsättas inom minuter, människa väljer när
- **Kontinuerlig driftsättning** — grön commit driftsätts automatiskt, ingen knapp att trycka på
- Varje steg förutsätter strikt att föregående redan är på plats

---

## Genomgånget exempel: feature-branch vs flagga-skyddad trunk
- Team A: 9-dagars branch → smärtsam merge, två integrationsbuggar, 14 dagar till prod
- Team B: 5 små PR:s under 4 dagar, varje grön och driftsatt bakom en flagga
- Dag 4: flaggan slås på för 5% kanarie → 100% i slutet av dagen
- Samma feature, mindre integrationsrisk, driftsättning frikopplad från release

---

## Välja CD eller kontinuerlig driftsättning
- Reglerad regim → mänsklig godkännande är obligatoriskt; välj **leverans**
- Återställning snabbare än diagnos → välj **driftsättning**
- Ingen kanarie eller flaggor ännu → inte redo för **driftsättning**
- Beslutet handlar om operativ mognad, inte teknisk kapacitet

---

## Var detta kopplar in
- Ex 3.9.1 — CI + manuell driftsättning (klicka "Create new revision")
- Ex 3.9.2 — Kontinuerlig driftsättning med smoke-test-grind
- Ex 3.9.3 — Samma leveransform, OIDC-federation ersätter den lagrade hemligheten
- Korslänk: `/exercises/3-deployment/9-cicd-to-container-apps/`

---

## Frågor?
