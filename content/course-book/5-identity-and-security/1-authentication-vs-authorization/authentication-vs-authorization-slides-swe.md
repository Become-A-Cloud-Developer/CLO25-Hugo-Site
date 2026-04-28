+++
title = "Autentisering kontra auktorisering"
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

## Autentisering kontra auktorisering
Del V — Identitet och säkerhet

---

## Två frågor varje skyddad begäran besvarar
- **Vem** gör detta anrop?
- **Får de** göra det de begär?
- Autentisering besvarar den första
- Auktorisering besvarar den andra
- Samma begäran, olika sanningskällor

---

## Autentisering
- Validerar en **credential** mot en känd identitet
- Indata: lösenord, bearer-token, certifikat
- Utdata: en identitet kopplad till begäran
- Fel → `401 Unauthorized`
- Säger ingenting om vad anroparen får göra

---

## Auktorisering
- Avgör om en **autentiserad identitet** får fortsätta
- Indata: principalen och den begärda resursen
- Läser roller, claims eller policyresultat
- Fel → `403 Forbidden`
- Rör aldrig credentialen

---

## Principalen
- En `ClaimsPrincipal` kopplad till `HttpContext.User`
- Bär ett namn och en lista av **claims** (påståenden)
- Byggs av autentiseringshanteraren
- Läses av varje auktoriseringskontroll
- Samma form oavsett credentialformat

---

## Ordning i pipelinen
- **Autentiseringsmiddleware** körs tidigt — fyller `HttpContext.User`
- **Routing** matchar begäran mot en controller-action
- **Auktoriseringsfilter** utvärderar `[Authorize]` mot principalen
- Action körs bara om filtret godkänner

---

## Auktoriseringsbeslut
- **Rollkontroll** — `[Authorize(Roles = "Admin")]`
- **Claim-kontroll** — `User.HasClaim("tenant", id)`
- **Policyutvärdering** — `[Authorize(Policy = "ManagerOnly")]`
- Från grovt till fint, från enkelt till centraliserat

---

## Vad denna del utvecklar
- **Cookies** — sessioner i webbläsaren
- **ASP.NET Core Identity** — hanterat användarregister
- **Claims och policies** — auktoriseringsregler
- **JWT** — bearer-token för API:er
- **OAuth / OIDC** — federerad identitet
- **Key Vault** — driftshemligheter

---

## Frågor?
