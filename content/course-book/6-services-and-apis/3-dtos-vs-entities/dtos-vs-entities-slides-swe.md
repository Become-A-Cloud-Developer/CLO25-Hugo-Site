+++
title = "DTO:er kontra entiteter"
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

## DTO:er kontra entiteter
Del VI — Tjänster och API:er

---

## Två former, en resurs
- **Entiteten** är domänformen — vad intern kod resonerar kring
- **DTO:n** är wire-formen — vad API:et exponerar
- De ser identiska ut dag ett, divergerar månad tre
- Olika förändringstakt, olika orsaker att gå sönder
- En typ för båda är valet som kostar mest senare

---

## Varför entiteten är en dålig wire-form
- **Överexponerar** interna fält — varje public-property publiceras som standard
- **Läcker persistensdetaljer** — navigationsegenskaper, `RowVersion`, nyckelval
- **Kopplar** API-kontraktet till databasschemat
- Att lägga till en kolumn skickar kolumnen till alla klienter
- Att dölja fält med `[JsonIgnore]` inverterar det säkra standardvärdet

---

## Vad en DTO är
- En klass formad för **wire**, inte för lagring
- Bär endast fälten som API-kontraktet exponerar
- Inga persistensattribut, inget beteende — en platt post
- Frikopplad från entiteten av en avsiktlig typgräns
- Databasändringar bryter inte klienter; API-ändringar tvingar inga migrationer

---

## Request-DTO — indatavalidering
- Beskriver vad klienten **får skicka**
- Utelämnar server-kontrollerade fält (`Id`, `CreatedAt`)
- Valideringsattribut sitter naturligt: `[Required]`, `[StringLength]`
- `[ApiController]` returnerar `400 Bad Request` automatiskt vid fel
- Request-regler och domäninvarianter förblir oberoende

---

## Response-DTO — selektiv exponering
- Beskriver vad servern **returnerar**
- Interna kolumner hålls ute genom konstruktion — ingen property att fylla
- Kan forma om data: platta ut, byta namn, formatera datum
- Bär server-tilldelade värden som `Id` och `CreatedAt`
- Kompilatorn upprätthåller asymmetrin mellan request och response

---

## Var mappningen bor
- **Inline** i controllern — okej för en resurs, duplicering växer
- **Egen mappare-klass** eller extension methods — testbart, ett ställe att hitta
- **Bibliotek** (AutoMapper, Mapster) — noll kod per typ, dolt beteende
- Övningen använder en `private static QuoteDto ToDto(Quote q)`-metod
- Välj den lättaste formen som överlever antalet resurser

---

## Konkret exempel — Quote
- Entiteten bär `Id`, `Author`, `Text`, `CreatedAt`, plus framtida audit-kolumner
- `CreateQuoteRequest` har `Author` och `Text` — servern äger resten
- `QuoteResponse` bär varje publikt fält, inget internt
- Mapparen är en metod, ändras bara när response-formen ändras
- Varje lager utvecklas i sin egen takt

---

## Kostnaden — när det är värt det
- Fler typer betyder mer kod — tre eller fyra filer per resurs
- Värt det när API:et är **publikt** eller har externa klienter
- Värt det när persistensen kommer växa med interna kolumner
- Värt det när valideringsregler skiljer sig från domäninvarianter
- Svårt att motivera enbart för tunna privata endpoints i en driftsättning

---

## Frågor?
