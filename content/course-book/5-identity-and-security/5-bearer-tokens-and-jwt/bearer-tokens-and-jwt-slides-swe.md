+++
title = "Bearer tokens och JWT"
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

## Bearer tokens och JWT
Del V — Identitet och säkerhet

---

## Cookies passar inte alla anropare
- En **mobilapp** delar ingen cookie jar med en webbläsare
- En **SPA** korsar origins där cookies krånglar
- Ett **tjänst-till-tjänst-anrop** har ingen webbläsare alls
- Alla kan ändå sätta en HTTP-**header**

---

## Vad "bearer" betyder
- Skickas i varje request som `Authorization: Bearer <token>`
- Servern validerar tokenen, inte vem som presenterar den
- **Den som bär tokenen** behandlas som autentiserad
- Därför: **TLS är obligatoriskt**, livslängder hålls korta

---

## JWT-formatet
- Tre base64-kodade segment sammanfogade med punkter
- **Header** — algoritm och nyckel-ID (`alg`, `kid`)
- **Payload** — JSON med claims om identiteten
- **Signatur** — bevisar att de två första segmenten inte ändrats

---

## Standard-claims i payloaden
- `iss` — **utfärdare**, tjänsten som signerade tokenen
- `sub` — **subject**, identiteten tokenen gäller
- `aud` — **mottagare** (audience), tjänsten som ska acceptera den
- `exp`, `nbf`, `iat` — när tokenen är giltig

---

## Validering av en JWT
- Läs `kid` ur headern, slå upp **verifieringsnyckeln**
- **Symmetrisk** (HS256) — delad hemlighet hos utfärdare och validerare
- **Asymmetrisk** (RS256) — utfärdaren signerar privat, validerare använder publik
- Kontrollera `iss`, `aud` och `exp` efter att signaturen godkänts

---

## ASP.NET Core JwtBearer
- `AddJwtBearer` med **Authority** och **Audience**
- Authority styr nyckelhämtningen från `/.well-known/...`
- `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`
- Controllers ser samma **`ClaimsPrincipal`** som vid cookie-auth

---

## Korta access tokens, refresh tokens
- Access tokens lever **minuter**, inte dagar
- En läckt token är en användbar credential tills den går ut
- En separat **refresh token** byts mot en ny access token
- Refresh tokens kan **återkallas** serverside; access tokens kan inte

---

## När JWT är fel val
- En session kan **dödas** i databasen — nästa anrop misslyckas
- En JWT är **giltig tills den går ut**, utan central kontroll
- Block-listor och nyckelrotation undergräver formatets vinst
- När återkallning måste vara **omedelbar**, välj serversessioner

---

## Frågor?
