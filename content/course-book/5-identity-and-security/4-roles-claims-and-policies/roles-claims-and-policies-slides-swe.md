+++
title = "Roller, claims och policyer"
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

## Roller, claims och policyer
Del V — Identitet och säkerhet

---

## Auktorisering behöver fakta
- Autentisering bekräftar **vem** som loggat in
- Auktorisering avgör **vad** den identiteten får göra
- Beslutet behöver **fakta om användaren**, inte bara att hen är inloggad
- Fakta lever på principalen som **claims**

---

## ClaimsPrincipal
- `HttpContext.User` exponerar en **`ClaimsPrincipal`**
- Varje **claim** (påstående) är ett namn-värdepar (typ + värde)
- Controllers läser claims via `User.FindFirst(type)`
- Cookie-biljetten bär samma claims tillbaka vid varje request

---

## Roller är ett särskilt claim
- En **roll** är ett claim av typen `ClaimTypes.Role`
- `[Authorize(Roles = "Admin")]` kontrollerar det claimet
- `User.IsInRole("Admin")` är samma kontroll för hand
- Avsiktligt grovkornig — snabb gruppmedlemskontroll

---

## Exempel: AdminController
- `[Authorize(Roles = "Admin")]` på controller-klassen
- Varje action kräver claim med rollen **Admin**
- Anonym request → 401, omdirigeras till login
- Inloggad icke-admin → 403 forbidden

---

## När roller inte räcker
- "Redigera din egen profil" beror på **identitet**, inte grupp
- "EU-tenant" beror på ett **tenant-claim**
- "Minst 18 år" beror på en **beräkning**
- Att uttrycka nyans som roller får rollistan att svälla

---

## Auktoriseringspolicyer
- En **policy** är en namngiven uppsättning krav
- Registreras en gång via `AddAuthorization(o => o.AddPolicy(...))`
- Tillämpas med `[Authorize(Policy = "Name")]`
- Att byta namn eller logik är en **enradig ändring**

---

## Egna krav och handlers
- `IAuthorizationRequirement` bär parametrarna
- `IAuthorizationHandler` utvärderar mot `context.User`
- Handlern anropar `context.Succeed(requirement)` vid träff
- Exempel: policyn **MinimumAge** läser ett `DateOfBirth`-claim

---

## Beslutsguide
- Enbart `[Authorize]` — vilken inloggad användare som helst
- `[Authorize(Roles = "X")]` — regeln är gruppmedlemskap
- `RequireClaim`-policy — regeln är claim-likhet
- Eget krav + handler — regeln behöver beräkning

---

## Frågor?
