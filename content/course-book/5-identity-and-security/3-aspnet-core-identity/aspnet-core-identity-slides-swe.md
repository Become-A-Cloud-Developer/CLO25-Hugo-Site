+++
title = "ASP.NET Core Identity"
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

## ASP.NET Core Identity
Del V — Identitet och säkerhet

---

## Varför inte bygga själv?
- Lösenordshashning, salt och iterationer är lätta att göra **fel**
- Återställningstokens, e-postbekräftelse och låsning är hela funktioner i sig
- Varje egen variant är en framtida **säkerhetsincident**
- Ramverket har redan en testad implementation

---

## Vad Identity är
- Ett bibliotek för **användarhantering**: registrering, inloggning, återställning, låsning
- Äger användartabellen och **lösenordshashen**
- Komponeras med cookie-autentisering — ersätter den inte
- Två huvudtjänster: **UserManager** och **SignInManager**

---

## IdentityUser-modellen
- Basentitet med `Id`, `UserName`, `Email`, `PasswordHash`, `SecurityStamp`
- Spårar **låsningstillstånd** och räknare för misslyckade försök
- **Utöka** genom att härleda `ApplicationUser : IdentityUser`
- Den härledda typen följer med som generisk parameter

---

## Användarlagringar
- En **användarlagring** är persistensbackenden bakom Identity
- **EF Core** mot SQL är standardvalet
- **In-memory**-lagring endast för tester och demon
- **Egna** lagringar via `IUserStore<TUser>` för äldre databaser

---

## UserManager vs SignInManager
- **UserManager** är data-API:et — skapa, hitta, byta lösenord, tilldela roll
- **SignInManager** orkestrerar hela **inloggningsflödet**
- `PasswordSignInAsync` verifierar hashen och utfärdar cookie-biljetten
- Båda är **DI-registrerade**, request-scopade tjänster

---

## Hashning av lösenord i Identity
- Standardalgoritm: **PBKDF2** med HMAC-SHA-512
- **Salt per användare** plus 310 000 iterationer (nuvarande standard)
- Långsamheten stoppar offline-attacker; saltet stoppar rainbow-tables
- Applikationen **lagrar aldrig klartext** — bara hashen

---

## Låsning och återställning
- **Låsning** aktiveras efter `MaxFailedAccessAttempts` (standard 5)
- Låsta konton misslyckas direkt även med korrekt lösenord
- **Återställningstokens** är signerade och tidsbegränsade; bundna till security stamp
- Lyckad lösenordsändring ogiltigförklarar utestående tokens

---

## Konkret exempel
- `AddDefaultIdentity<IdentityUser>().AddEntityFrameworkStores<ApplicationDbContext>()`
- Register-controllern anropar `_userManager.CreateAsync(user, password)`
- Sedan `_userManager.AddToRoleAsync(user, "Candidate")`
- Slutligen `_signInManager.SignInAsync(user, isPersistent: false)`

---

## När Identity inte ska användas
- Rena JSON-API:er — använd bara **JWT bearer**
- SPA federerad mot extern IdP — använd **OpenID Connect**
- Interna verktyg — använd organisationens katalog (Entra ID)
- Använd Identity när applikationen äger sin **användardatabas**

---

## Frågor?
