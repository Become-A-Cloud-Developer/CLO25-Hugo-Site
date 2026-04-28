+++
title = "Objektlagring och filuppladdning"
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

## Objektlagring och filuppladdning
Del IV — Dataåtkomst

---

## Varför inte databasen
- Databaser är optimerade för **små, indexerade** operationer, inte stora binära payloads
- Stora `BLOB`-kolumner blåser upp **transaktionsloggar** och backup-fönster
- Strukturerad data läses via predikat; binär data läses via **identitet** (URL)
- En foreign-key-liknande referens kopplar databasraden till blob-URL:en

---

## Blobs och containers
- En **blob** är en oföränderlig byte-sekvens med ett sökvägs-liknande namn, content type och metadata
- En **container** är den översta grupperingen av blobs i ett storage-konto
- Blob-namnrymden är **platt per container** — snedstreck är konvention, inte riktiga kataloger
- Åtkomstpolicyer och access tiers konfigureras på container-nivå

---

## Oföränderlighetsmodellen
- Blobs stödjer inte **partiella skrivningar** — uppladdningar ersätter hela bloben
- Ingen `UPDATE` av byte 1024 — minsta skrivning är en fullständig omskrivning
- Samtidiga uppladdningar till samma namn: en vinner helt, den andra skrivs över helt
- Oföränderlighet är vad som låter objektlagring **replikera aggressivt** mellan datacenter

---

## Access tiers (åtkomstnivåer)
- **Hot** — högst lagringskostnad, lägst läskostnad, omedelbar åtkomst
- **Cool** — lägre lagringskostnad, högre läskostnad, omedelbar åtkomst
- **Archive** — billigast lagring, timslång **rehydration** innan läsning är möjlig
- Livscykelpolicyer flyttar blobs mellan nivåer automatiskt baserat på ålder eller åtkomstmönster

---

## SAS-token
- En **SAS-token** är en tidsbegränsad, scope-begränsad URL signerad med konto-nyckeln
- Kodar behörigheter, scope, utgångstid och signatur i query-strängen
- Låter webbläsare ladda upp eller ner **direkt** utan att byten passerar app-servern
- User delegation SAS (signerad via Entra ID) föredras framför service SAS i produktion

---

## Streaming-uppladdning
- SDK:n delar upp källströmmen i **block** (4–100 MB)
- Block laddas upp **parallellt** och försöks om individuellt vid fel
- Ett avslutande `Commit Block List` sätter atomiskt ihop dem till en blob
- En ASP.NET request body är en `Stream` — pipa den direkt in i `BlobClient.UploadAsync`

---

## En service-metod
- `BlobServiceClient` skapas en gång och återanvänds — en tunn wrapper kring HTTP-klienten
- `GetBlobContainerClient` och `GetBlobClient` är **lättviktiga** — inga nätverksanrop
- `UploadAsync(stream, overwrite: true)` utför den faktiska streaming-överföringen
- Att sätta `BlobHttpHeaders.ContentType` säkerställer korrekt rendering vid senare nedladdning

---

## Blob-lagring vs databas-BLOB
- **Blob-lagring** vinner för allt som serveras via HTTP eller är större än några hundra KB
- **Databas-kolumn** vinner för små, transaktionellt kopplade binära data
- Blob-lagring kostar ören per GB; databaslagring kostar kronor per GB
- SAS-baserad direkt åtkomst från webbläsare är **bara möjlig** med blob-lagring

---

## Frågor?
