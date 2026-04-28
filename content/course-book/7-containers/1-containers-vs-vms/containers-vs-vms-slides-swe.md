+++
title = "Containrar vs virtuella maskiner"
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

## Containrar vs virtuella maskiner
Del VII — Containrar

---

## Problemet "fungerar på min maskin"
- Samma kod, olika miljö, olika resultat
- Drift göms i **OS-patchar**, systembibliotek och runtime-versioner
- Reproducerbarhet är en teknisk kostnad, inte bara en klyscha
- Containrar och VM angriper problemet — på olika lager

---

## Hur en VM virtualiserar hårdvara
- En **hypervisor** ligger mellan hårdvaran och gästerna
- Varje VM kör sin egen **kärna** och fullständiga operativsystem
- Image-storlek i gigabyte, boot-tid i tiotals sekunder
- Stark isolering: ingen kärna delas mellan VM

---

## Hur en container isolerar en process
- En **container runtime**, en delad värdkärna
- **Namespaces** döljer andra processer, nätverk och mounts
- **cgroups** sätter tak på CPU och minne per container
- Applikationen är det enda som startar

---

## Vad varje modell offrar och vinner
- VM: starkare **isolering**, full OS-flexibilitet, tyngre fotavtryck
- Container: lättare, snabbare, men bunden till värdens kärnafamilj
- VM portabla mellan hypervisorer; containrar mellan värdar med samma kärna-ABI
- En kärna-CVE är ett problem för hela flottan av containrar

---

## Densitet och starttid
- En VM kallstartar på 30 s till minuter; en container på millisekunder
- 10 VM per värd vs 100+ containrar på samma hårdvara
- Varje VM bär permanent sin egen kärna och OS-tjänster
- Varje container bär endast applikationens arbetsuppsättning

---

## Genomgånget exempel: dotnet run
- `dotnet run` redo att svara på ~200 ms
- Samma app i en container: ~200 ms (overhead försumbar)
- Samma app som ny VM: 2–3 minuter klocktid
- Applikationen är identisk — stacken runt omkring är det inte

---

## När passar respektive modell
- VM: legacy Windows-app, reglerad isolering, kärna-tunade databaser
- Container: tillståndslös webbtjänst, kö-worker, schemalagt jobb
- Molnplattformar kombinerar båda: containrar körs **på** VM
- Välj kompromissen, inte trenden
- Korslänk: `/exercises/20-docker/`

---

## Frågor?
