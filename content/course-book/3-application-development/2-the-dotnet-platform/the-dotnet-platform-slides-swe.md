+++
title = "Plattformen .NET"
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

## Plattformen .NET
Del III — Applikationsutveckling

---

## Varför .NET finns
- **Plattformsoberoende** runtime för Windows, Linux och macOS.
- En verktygskedja för desktop, mobil, konsol och webb.
- Öppen källkod under MIT-licens; utvecklas öppet på GitHub.
- Den enade versionen (5, 6, 7, 8, 9, 10) ersätter det äldre Windows-bundna .NET Framework.

---

## Kompileringspipelinen
- **C#-källkod** i `.cs`-filer skriven av utvecklaren.
- C#-kompilatorn producerar **Intermediate Language** (IL) — CPU-oberoende bytekod.
- **Just-in-time** (JIT) översätter IL till maskinkod vid körning.
- Samma IL körs på x86-64, ARM64, Windows, Linux och macOS.

---

## Common Language Runtime
- **CLR** laddar assemblies, JIT-kompilerar IL och driver processen.
- **Garbage collector** frigör oanvänd minnesallokering automatiskt.
- **Exception handling** rullar tillbaka stacken och skyddar processens tillstånd.
- Hanterad runtime eliminerar use-after-free och dubbel frigörning.

---

## Assemblies och NuGet
- En **assembly** är en `.dll` med IL, typmetadata och resurser.
- Metadatan möjliggör reflektion — DI, model binding och routing bygger på det.
- **NuGet** är pakethanteraren; beroenden deklareras i `.csproj`.
- Publika paket på `nuget.org`; privata feeds för interna bibliotek.

---

## Verktyget dotnet CLI
- `dotnet new <mall>` — skapa ett nytt projekt (`mvc`, `webapi`, `console`).
- `dotnet restore` — hämtar deklarerade NuGet-paket.
- `dotnet build` — kompilerar till IL, lägger resultat i `bin/Debug/`.
- `dotnet run` — bygger och startar applikationen lokalt.
- `dotnet add package <namn>` — lägger till en NuGet-referens.

---

## ASP.NET Core
- .NET-**ramverket** för webb, byggt ovanpå CLR.
- **Kestrel** är HTTP-servern som tar emot anslutningar och tolkar förfrågningar.
- Konfigurerbar middleware-**pipeline** i `Program.cs`.
- Inbyggt stöd för **MVC**, dependency injection och konfiguration.

---

## Exempel: dotnet new mvc
- `dotnet new mvc -n CloudSoft` skapar ett MVC-projekt.
- `Controllers/`, `Views/`, `Models/` utgör presentationslagret.
- `Program.cs` bygger värden och startar Kestrel.
- `appsettings.json` levererar konfiguration; `wwwroot/` håller statiska filer.

---

## Frågor?
