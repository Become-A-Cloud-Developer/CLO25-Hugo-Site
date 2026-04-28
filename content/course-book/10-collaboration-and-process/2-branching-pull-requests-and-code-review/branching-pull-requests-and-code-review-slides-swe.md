+++
title = "Branching, Pull requests och kodgranskning"
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

## Branching, Pull requests och kodgranskning
Del X — Samarbete och process

---

## Varför inte pusha till main
- Inget **andra par ögon** innan koden landar
- Ingen CI-kontroll mellan commit och delad historik
- Kunskapen stannar hos den ursprungliga författaren
- Återställning kräver att publik historik skrivs om

---

## Flödet med topic-branch
- **Branch** från en uppdaterad `main`
- **Push** topic-branchen till GitHub
- **Öppna** en pull request mot `main`
- **Granska** diffen, **merge** och ta bort branchen

---

## Vad en pull request är
- Ett **förslag** att merge:a en branch in i en annan
- En diff med inline-kontroller för granskning
- En **diskussionstråd** kopplad till förslaget
- En **CI-statuspanel** med resultat från automatiska kontroller
- En bestående artefakt långt efter att branchen är borta

---

## Branch protection rules
- Kräv en **pull request** innan merge
- Kräv **N godkända granskningar**
- Kräv att **statuskontroller** (CI) går grönt
- Kräv **linjär historik**
- Kräv att branchen är **uppdaterad** mot `main`

---

## Tre merge-strategier
- **Merge commit** — bevarar topic-commits och branch-form
- **Squash** — slår ihop hela PR:en till en ren commit
- **Rebase** — spelar upp commits linjärt, ingen merge commit
- Välj en, upprätthåll den, sluta diskutera den

---

## Konflikter och draft PR
- Git stannar vid konfliktmarkörerna; **utvecklaren avgör avsikt**
- Korta branches ger färre konflikter än långa
- En **draft PR** signalerar pågående arbete
- Samma diff, samma CI, men ingen merge-knapp och inga pings

---

## Kodgranskning som hantverk
- **Små PR:er** får riktig genomläsning; stora får gummistämpel
- **Fråga, hävda inte** — bjud in till dialog
- Skilj **blockerande** feedback från `nit:`-stilkommentarer
- Vänlig i ton, rigorös i standard

---

## Frågor?
