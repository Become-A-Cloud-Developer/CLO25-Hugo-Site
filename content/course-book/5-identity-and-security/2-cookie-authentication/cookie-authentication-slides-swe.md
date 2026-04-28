+++
title = "Cookie-baserad autentisering och sessioner"
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

## Cookie-baserad autentisering och sessioner
Del V — Identitet och säkerhet

---

## HTTP glömmer varje request
- HTTP själv bär **inget minne** mellan requests
- En inloggad användare måste återkännas vid varje request
- Webbläsaren lagrar en **cookie** som servern skickar
- Cookies följer automatiskt med på alla same-origin-requests

---

## Autentiserings-cookien
- En **cookie** är data som servern ber webbläsaren spara och skicka tillbaka
- Värdet är en **krypterad, signerad autentiseringsbiljett**
- Ogenomskinlig för webbläsaren, manipuleringsskyddad för servern
- En ändrad byte loggar ut användaren, ger aldrig högre behörighet

---

## Inloggnings-handskakningen
- POST:a användarnamn och lösenord över HTTPS
- Verifiera lösenordet mot lagrad hash
- Bygg en `ClaimsPrincipal` och anropa `HttpContext.SignInAsync`
- Servern skickar `Set-Cookie`, webbläsaren sparar den, redirect tillbaka

---

## Registrera cookie-autentisering
- `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)`
- `.AddCookie(options => …)` styr livslängd och säkerhet
- `app.UseAuthentication()` körs **före** `app.UseAuthorization()`
- Principalen måste finnas på `HttpContext.User` innan `[Authorize]` utvärderas

---

## Cookie-attribut som spelar roll
- `HttpOnly` — JavaScript kan inte läsa cookien
- `Secure` — skickas endast över HTTPS
- `SameSite=Lax` — utelämnas från de flesta cross-site-POSTs
- `Expires` / `Max-Age` — begränsar cookiens livslängd

---

## Sessioner och glidande utgång
- En **session** är perioden då samma användare återkänns
- Fast utgång loggar ut användaren vid en bestämd klockslag
- **Glidande utgång** skickar ny cookie medan användaren arbetar
- Utloggning skickar en utgången cookie; passiva sessioner dör av sig själva

---

## CSRF — det cookie-specifika hotet
- Webbläsaren bifogar cookien till **varje** request, även cross-site-POSTs
- Ett illvilligt formulär kan agera som användaren utan att läsa cookien
- Bearer-token-API:er drabbas inte — tokens skickas inte automatiskt
- Försvaret ligger i **applikationslagret**

---

## Anti-förfalskningstoken (CSRF-skydd)
- En **anti-förfalskningstoken** är en sessionsbunden hemlighet i ett dolt formulärfält
- Paras med en separat cookie som bär den matchande halvan
- `[ValidateAntiForgeryToken]` avvisar varje POST där halvorna inte stämmer
- `SameSite=Lax` plus token är hängslen och livrem

---

## Frågor?
