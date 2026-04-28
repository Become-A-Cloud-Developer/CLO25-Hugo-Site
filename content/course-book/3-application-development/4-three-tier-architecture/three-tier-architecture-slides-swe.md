+++
title = "Trelagersarkitektur"
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

## Trelagersarkitektur
Del III — Applikationsutveckling

---

## Varför monolitiska controllers blir ohållbara
- En controller blandar **HTTP-hantering**, affärsregler och dataåtkomst
- Tre skäl att ändra koden krockar i samma fil
- Tester kräver en riktig databas för att köra validering
- Byte av lagring tvingar fram ändringar i varje controller
- Duplicerade regler glider isär mellan funktioner över tid

---

## De tre lagren
- **Presentationslager** — controllers och vyer; HTTP in, HTTP ut
- **Servicelager** — affärslogik, validering, orkestrering
- **Datalager** — repositories som döljer lagringstekniken
- Varje lager har ett skäl att ändras
- Mappar signalerar strukturen; **interface** upprätthåller den

---

## Lagerdiagram
[Diagram: Presentation -> Service -> Data, pilar endast nedåt, interface ritade vid varje gräns]

---

## Beroenderiktning
- Referenser går endast nedåt — aldrig uppåt
- Controllern beror på `INewsletterService`, inte den konkreta klassen
- Servicen beror på `ISubscriberRepository`, inte den konkreta klassen
- Konkreta typer kopplas in i `Program.cs` via DI-containern
- Varje lager kompileras mot en **abstraktion**

---

## Anropsflöde: Prenumerera på nyhetsbrev
- Controllern tar emot POST, skickar e-post och namn till `INewsletterService`
- Servicen validerar, anropar `ISubscriberRepository.ExistsAsync` och `AddAsync`
- Repositoryt skriver till lagret och returnerar
- Servicen returnerar ett `OperationResult`
- Controllern väljer en redirect eller renderar formuläret igen

---

## Praktiskt exempel
- `NewsletterController` — presentationslager, returnerar `IActionResult`
- `NewsletterService` — servicelager, returnerar `OperationResult`
- `InMemorySubscriberRepository` — datalager, skriver till `ConcurrentDictionary`
- Byt repositoryt mot en MongoDB-baserad klass — controller och service orörda
- Övning: `/exercises/10-webapp-development/2-service-layer/`

---

## Avvägningen
- Fler filer per funktion (4–6 istället för 1)
- Återanvändbara affärsregler mellan funktioner
- Utbytbara lagringsbackends
- Enhetstester som kör ett lager i taget
- Värt kostnaden när applikationen växer förbi en enstaka funktion

---

## Frågor?
