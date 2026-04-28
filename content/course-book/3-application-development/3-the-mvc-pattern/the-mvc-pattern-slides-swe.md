+++
title = "MVC-mönstret"
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

## MVC-mönstret
Del III — Applikationsutveckling

---

## Varför MVC finns
- Indatahantering, affärstillstånd och rendering ändras av **olika skäl**
- Att blanda dem ger kod som är svår att testa eller ändra
- MVC tilldelar varje ansvar en **egen roll**
- Rollerna kommunicerar via smala gränssnitt, inte delat tillstånd

---

## De tre rollerna
- **Controller** — tar emot indata, koordinerar arbete, returnerar ett resultat
- **Vy** — renderar tillstånd till HTML via en Razor-mall
- **Modell** — det typade dataobjekt som skickas från controller till vy
- Rika domänmodeller hör hemma i tjänstelagret, inte här

---

## Controllers
- En klass vars namn slutar på `Controller`, placerad i `Controllers/`
- Publika metoder är **actions** som hanterar förfrågningar
- Varje action returnerar ett `IActionResult`
- Attribut som `[HttpGet]`, `[HttpPost]`, `[Authorize]` styr vilka anrop som matchar

---

## Routing
- Matchar inkommande **URI mot en controller-action**
- Konventionell mall: `{controller=Home}/{action=Index}/{id?}`
- Attribut-routing: `[Route]` och `[HttpGet("{id:int}")]` på själva metoden
- Route-värden extraheras från sökvägen innan action körs

---

## Model binding
- Konverterar requestdata till **typade action-parametrar**
- Källor: route-värden, query string, formulärfält, JSON-body
- Valideringsattribut (`[Required]`, `[StringLength]`) fyller `ModelState`
- Action-kroppen kontrollerar `ModelState.IsValid` innan den fortsätter

---

## Action-resultat
- `ViewResult` — rendera en Razor-vy
- `RedirectToActionResult` — 302-redirect efter ett lyckat POST
- `JsonResult` — serialisera ett objekt för ett API-svar
- `NotFoundResult`, `BadRequestResult` — uttryckliga felsvar

---

## Razor-syntax
- `.cshtml`-filer blandar HTML och C#-uttryck inledda med `@`
- `@model Product` deklarerar den starkt typade modellen
- `@if`, `@foreach` för flödeskontroll i mallen
- Tag helpers (`asp-for`, `asp-action`) binder inputfält till modellens egenskaper

---

## Request-flödet
- Request → middleware → **routing** → controller → action → resultat → response
- Dependency injection förser controllerns konstruktor med dess beroenden
- Varje steg kan testas eller bytas ut oberoende av de övriga
- Ramverket sköter serialisering, statuskoder och headers

---

## Frågor?
