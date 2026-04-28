+++
title = "Strukturerad loggning med ILogger"
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

## Strukturerad loggning med ILogger
Del IX — Drift och observerbarhet

---

## Varför struktur spelar roll
- Loggrader i fri text går att **söka i**, inte att fråga mot
- Sammanslagna strängar löser upp värdena i prosa
- I skala behöver en operatör **fält**, inte regex-extraktion
- Strukturerade loggar bevarar både meddelandet **och** de namngivna fälten

---

## Vad strukturerad loggning är
- En konstant **meddelandemall** med namngivna platshållare
- Värden skickas som separata argument — aldrig interpolerade
- Ramverket behåller den renderade texten **och** fältmängden
- Nedströmslager indexerar fälten som **kolumner**

---

## ILogger&lt;T&gt; i ASP.NET Core
- Host:en registrerar en öppen generisk `ILogger<>`-mappning i **DI**
- Konstruktorer ber om `ILogger<NewsletterController>` — får den
- Typparametern `T` blir **loggkategorin**
- Kategorier matchas **hierarkiskt** efter namnrymdsprefix

---

## Meddelandemallar
- Första argumentet är en **konstant sträng** med `{PascalCasePlatshållare}`
- Efterföljande argument fyller platshållarna efter **position**
- Fältnamnen kommer från mallen, inte från argumentens variabelnamn
- Bygg aldrig mallen vid körning — den är **händelsens identitet**

---

## Loggnivåer
- **Trace / Debug** — diagnostiska detaljer, av i produktion
- **Information** — normalt flöde: begäran startad, jobb klart
- **Warning** — oväntat men återhämtat (retry, fallback, saknad config)
- **Error / Critical** — misslyckad operation / systemfel

---

## Loggningsscope
- `using (logger.BeginScope(new Dictionary<string, object> { ... }))`
- Fälten fästs på **varje loggkall inuti blocket**
- Vanlig användning: **korrelations-ID** per begäran, satt av middleware
- Scope kan nästlas — begäran, tenant och operation komponeras

---

## Berikning
- Visst sammanhang hör till **varje** loggrad, inte till varje anropsplats
- Globalt: maskinnamn, build SHA, miljö — sätts vid host-uppstart
- Per begäran: korrelations-ID, användar-ID, tenant-ID — sätts av middleware
- Anropsplatsen kan fokusera på **händelsen**, inte på metadatan

---

## Genomgånget exempel
- `ILogger<NewsletterController>` injiceras via konstruktorn
- Scope per begäran öppnas med `CorrelationId = HttpContext.TraceIdentifier`
- `LogInformation("Subscribe requested for {Email}", email)`
- En fråga kan nu gruppera per mall och filtrera på korrelations-ID

---

## Driftspraxis
- **Inga personuppgifter i loggar** — ersätt e-post/namn med surrogat-ID
- **Inga hemligheter i loggar** — sanera config-värden, tokens, headers
- **Sampla vid hög volym** — släng Debug, sampla pratiga händelser
- **Håll mallarna stabila** — dashboards lutar sig mot den konstanta texten

---

## Frågor?
