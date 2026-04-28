+++
title = "ORM och repository-mönstret"
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

## ORM och repository-mönstret
Del IV — Dataåtkomst

---

## Varför rå dataåtkomst skadar
- Databasdrivrutinens vokabulär läcker in i controllers och tjänster
- Samma fråga skrivs om på tre ställen med små skillnader
- **Schemaändringar** kräver att varje inbäddad fråga hittas
- Affärsregler ligger bredvid filtersyntax istället för att stå för sig själva
- Ingen söm för att ersätta med fejk — varje test kräver en riktig databas

---

## Vad en ORM gör
- Översätter mellan **databasrader** och **objekt i minnet**
- Genererar SQL från typade uttryck (LINQ, query builders)
- **Ändringsspårning** skriver bara modifierade kolumner vid spara
- Identitetskarta håller en instans per primärnyckel per session
- ODM:er tillämpar samma idé på dokumentlager som MongoDB

---

## Vad repository-mönstret gör
- En enda klass döljer all dataåtkomstkod bakom ett **interface**
- Tjänstelagret anropar `FindByIdAsync`, `AddAsync` — aldrig drivrutinen
- Lagringstekniken blir ett avgränsat, utbytbart val
- Tjänstelagret blir enhetstestbart med en fejkad repository
- Stödjer de fyra **CRUD**-operationerna som grund

---

## Generisk vs domänspecifik
- **Generisk** `IRepository<T>` — en implementation, många entiteter, mest CRUD
- **Domänspecifik** `INewsletterRepository` — frågor som matchar verksamheten
- Generisk ensam läcker domänfrågor tillbaka in i tjänstelagret
- Domänspecifik ensam duplicerar CRUD över varje aggregat
- Vanlig kompromiss: generisk bas + domänspecifik utökning

---

## Hur de kombineras
- ORM ligger längst ner — översättning rad till objekt
- Repository ligger ovanpå — domänformat API till tjänstelagret
- ORM tar bort drivrutinsvokabulär från datalagret
- Repository tar bort datalagervokabulär från tjänstelagret
- Var och en ensam lämnar den andra problemet olöst

---

## Konkret exempel: NewsletterRepository
- `INewsletterRepository` deklarerar `GetPublishedAsync`, `FindBySlugAsync`
- `MongoRepository<T>`-basen levererar CRUD via MongoDB-drivrutinen
- `NewsletterRepository` utökar den med nyhetsbrevsspecifika finders
- Registreras i `Program.cs` via **dependency injection**
- `NewsletterService` beror endast på interfacet — inga MongoDB-typer

---

## När det lönar sig
- Flera konsumenter läser samma data
- Tjänstelagret behöver enhetstester **utan databas**
- Lagringstekniken kan ändras mellan miljöer
- Teamet värdesätter att hålla frågesyntax ute ur affärslogiken

---

## När det inte gör det
- En enskild liten tjänst med frågor som bara används på ett ställe
- Inget behov av att testa tjänsten utan en databas
- Generiskt interface växer till att återskapa drivrutinen metod för metod
- Kostnaden är verklig — inför det bara när det förtjänar sin plats

---

## Frågor?
