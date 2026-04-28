+++
title = "Relationella vs NoSQL-datamodeller"
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

## Relationella vs NoSQL-datamodeller
Del IV — Dataåtkomst

---

## Två former av applikationsdata
- Huvudböcker och lager kräver **strikt struktur** och atomära uppdateringar
- Kataloger och telemetri kräver **flexibel struktur** och hög lässkapacitet
- Datamodellen styr migreringar, frågor och konsistens

---

## Hur poster struktureras
- **Relationell**: rader i typade tabeller, sammanfogade via främmande nycklar
- **Dokument**: självbeskrivande JSON/BSON-dokument i collections
- Relationellt normaliserar fakta; dokument samlar allt relaterat

---

## Schema som kontrakt
- Relationellt schema **upprätthålls vid skrivning** av databasen
- Dokumentschema lever i **applikationskoden**
- Strikt schema avvisar dålig data; flexibelt schema skjuter upp kontrollen

---

## Migreringens kostnad
- Relationellt: versionerade migreringsskript i takt med koden
- Dokument: collections innehåller **blandade former**; läsningar hanterar båda
- Migreringen flyttar från databasen till applikationslogiken

---

## Denormalisering löser join-problemet
- Dokumentläsningar undviker joins genom att **duplicera data** mellan dokument
- Byter skrivtidskomplexitet mot lästidsenkelhet
- Dokumentets gräns är det centrala modelleringsbeslutet

---

## Eventuell konsistens
- CAP-teoremet tvingar fram ett val under partition
- **Eventuell konsistens**: repliker konvergerar när skrivningar upphör
- Cosmos DB exponerar Strong / Bounded / Session / Eventual som ett reglage

---

## Exempel: Cosmos DB MongoDB API
- Skapa konto, databas, collection — **inget schema deklareras**
- Drivrutinen sätter in godtyckliga BSON-dokument
- En läsning av ett dokument ersätter en join över fyra tabeller

---

## Beslutsramverk
- Relationellt när invarianter spelar roll och data lever längre än koden
- Dokument när formen utvecklas och skalan väger tyngre än joins
- Många system använder **båda** — en modell per bounded context

---

## Frågor?
