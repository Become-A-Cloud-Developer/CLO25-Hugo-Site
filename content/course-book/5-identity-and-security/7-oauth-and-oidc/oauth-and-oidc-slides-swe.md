+++
title = "OAuth 2.0 och OpenID Connect"
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

## OAuth 2.0 och OpenID Connect
Del V — Identitet och säkerhet

---

## Problemet med delegerad åtkomst
- Appar behöver anropa API:er åt en användare
- Att be om ett lösenord till en tredje part är osäkert och opraktiskt
- "Logga in med Google" kräver identitet utan att dela lösenord
- Lösning: en betrodd **authorization server** håller credentialen
- Token bär scopad behörighet och signerad identitet

---

## De fyra aktörerna
- **Resource owner** — den mänskliga användaren
- **Client** — applikationen som begär åtkomst
- **Authorization server** — Entra ID, Google, GitHub, Auth0
- **Resource server** — API:et som hostar användarens data
- Förtroendet mellan dem bärs av signerade token

---

## Grant-typer i översikt
- **Authorization code** — interaktiv inloggning (webbläsarappar)
- **Client credentials** — maskin-till-maskin, ingen användare
- **Device code** — enheter med begränsad inmatning (TV, CLI)
- **Refresh token** — byt långlivad token mot ny access-token
- Implicit och password-grant rekommenderas inte längre

---

## Authorization code med PKCE
- Front channel: webbläsaromdirigering till authorization server
- Användaren loggar in där, samtycker till **scope**
- Servern dirigerar tillbaka med en engångs-`code`
- Back channel: klienten byter `code` + `code_verifier` mot token
- PKCE binder ihop de två halvorna — skyddar publika klienter

---

## Redirect-URI och state
- **Redirect-URI** måste vara förregistrerad, exakt matchning
- Hindrar angripare från att kapa koder till egen callback
- **state**-parametern — slumpvärde per inloggning
- Valideras vid callback för att stoppa CSRF
- Båda kontrollerna är obligatoriska

---

## Vad OIDC lägger till
- OAuth ensam ger en **access-token** — en behörighetssedel
- OIDC ger en **ID-token** — en signerad JWT som intygar *vem*
- ID-token-claims: `sub`, `name`, `email`, `iss`, `aud`
- Klienten verifierar signaturen mot utfärdarens publika nyckel
- ID-token är för klienten; access-token är för API:et

---

## OAuth kontra OIDC
- **OAuth** svarar på auktorisering — "får klienten anropa API:et?"
- **OIDC** svarar på autentisering — "vem är användaren?"
- Samma protokollfamilj, olika frågor
- Workload-federation (GitHub Actions → Azure) återanvänder OIDC för icke-mänskliga anropare
- Täcks i DevOps-delen av boken

---

## Registrering i ASP.NET Core
- `AddOpenIdConnect` registrerar hanteraren
- `Authority` pekar på utfärdarens discovery-URL
- `ClientId` och `ClientSecret` från app-registreringen
- `ResponseType = "code"`, `UsePkce = true`
- Resultat: en `ClaimsPrincipal` fylld från ID-token

---

## Frågor?
