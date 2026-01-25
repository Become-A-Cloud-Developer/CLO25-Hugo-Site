+++
title = "Inuti en fysisk server"
program = "CLO"
cohort = "25"
courses = ["BCD"]
type = "slide"
date = 2024-11-17
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls= true
progress= true
history= true
center= true
+++

## Inuti en fysisk server
- Fysiska servrar är utformade för tillförlitlighet
---
## Kärnkomponenter
- **CPU**: Utför instruktioner och hanterar uppgifter.
- **RAM**: Tillfällig lagring för aktiva processer och data.
- **Lagringsenheter**: Permanent datalagring.
- **Moderkort**: Central kommunikationshub som förbinder de övriga komponenterna
- **PSU**: Omvandlar ström.
- **Kylsystem**: Upprätthåller optimala temperaturer.
- **NICs**: Hanterar nätverksanslutning.
- **Expansionsplatser**: Ger möjlighet att ansluta fler komponenter.
---
## CPU
- Serverns "hjärna".
- Flerkärniga processorer hanterar parallella uppgifter.
- Exempel: Intel Xeon, AMD EPYC.
---
## RAM
- Lagrar temporärt data för snabbare bearbetning.
- Påverkar serverns hastighet och multitasking.
- Använder ofta ECC (Error-Correcting Code) för tillförlitlighet.
---
## Lagringsenheter
- Typer: HDD (kapacitet) och SSD (hastighet).
- Stöder RAID för redundans och prestanda.
- Lagrar permanent data.
---
## Moderkort
- Huvudkretskortet som ansluter alla komponenter.
- Innehåller CPU-sockel, RAM-platser och lagringsgränssnitt.
---
## Strömförsörjning (PSU)
- Omvandlar ström till serverkompatibel spänning.
- Ofta redundant för tillförlitlighet.
- Effektivitetsbetyg (t.ex. 80 PLUS) visar energieffektivitet.
---
## Kylsystem
- Förhindrar överhettning av komponenter.
- Inkluderar fläktar, kylflänsar eller vätskekylning.
- Justerar dynamiskt med inbyggda sensorer.
---
## Nätverkskort (NIC)
- Ansluter servrar till nätverk.
- Hastigheter från 1 Gbps till 40 Gbps.
- Integrerade eller tillagda via expansionskort.
---
## Expansionsplatser
- PCIe-platser för GPU:er, ytterligare nätverkskort, etc.
- USB och andra portar för underhåll.
- Ökar serverns funktionalitet.
---
## Fysisk serverdesign
- Byggd för drift dygnet runt.
- **Hot-Swappable-komponenter**: Byte utan driftstopp.
- **Rackmonterat chassi**: Passar serverrack.
- **Hanteringsgränssnitt**: Verktyg som IPMI och iLO för fjärrövervakning.
