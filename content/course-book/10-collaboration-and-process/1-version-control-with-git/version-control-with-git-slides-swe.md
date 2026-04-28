+++
title = "Versionshantering med Git"
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

## Versionshantering med Git
Del X — Samarbete och process

---

## Varför versionshantering
- En kodmapp blir ohanterbar så snart **två personer** redigerar den
- Behov: vem ändrade vad, när och **varför**
- Behov: återgå till valfritt tidigare läge utan att förlora senare arbete
- Behov: parallella ändringar utan **destruktiv överskrivning**
- Lösning: förändring blir ett **förstaklassobjekt**, inte en bieffekt

---

## Centraliserat vs. distribuerat
- **Centraliserat (SVN, CVS)**: en server håller historiken
- **Distribuerat (Git)**: varje utvecklare har en **fullständig kopia**
- Lokala commits är billiga; nätverk behövs bara för att **dela**
- Branching och historikvisning sker lokalt och snabbt
- Git vann för att datamodellen är enkel och hållbar

---

## Vad en repository är
- En **`.git/`-katalog** i projektets rot
- **Objektdatabas**: innehållsadresserade snapshots (SHA-1-hashar)
- **Refs**: korta namn som `main` som pekar på commits
- Branches och taggar är inget annat än **refs**
- Tar du bort `.git/` är projektet inte längre versionshanterat

---

## De tre tillstånden
- **Working tree** — synliga, redigerbara filer på disk
- **Staging area** (index, "köområde") — förberedda ändringar för nästa commit
- **Committed** — permanent registrerat i repositoryt
- `git add` korsar första gränsen
- `git commit` korsar den andra

---

## Commits och branches
- En **commit** är en oföränderlig snapshot identifierad av en **SHA-hash**
- Varje commit pekar på sin **förälder/föräldrar** — en riktad acyklisk graf
- En **branch** är en flyttbar pekare till en commit
- Att skapa en branch skriver en liten ref-fil; att byta är snabbt
- `HEAD` namnger den branch du arbetar på just nu

---

## Remotes och synkronisering
- En **remote** är en annan kopia av repositoryt (oftast `origin`)
- `git fetch` — hämta commits från remote, ändra **inte** lokala branches
- `git pull` — fetch **plus** merge/rebase in i nuvarande branch
- `git push` — ladda upp lokala commits till remote
- Symmetriskt par: inget sker automatiskt — vilket möjliggör **offlinearbete**

---

## Det dagliga flödet
- `git status` — vad är ändrat, staged, ospårat
- `git add <filer>` — staga selektivt
- `git commit -m "..."` — registrera på nuvarande branch
- `git push` — dela med teamet
- Små, frekventa commits med beskrivande meddelanden

---

## Genomgånget exempel
- `git init` — skapa `.git/`, gör mappen till en repo
- `git add .` — staga alla filer
- `git commit -m "Initial commit"` — första snapshot, ingen förälder
- `git remote add origin <url>` — registrera GitHub som `origin`
- `git push -u origin main` — ladda upp och sätt upstream

---

## Var Git passar in
- **Branches** blir grunden för pull requests och kodgranskning
- **Remotes** blir länken mellan lokalt arbete och CI/CD
- **Commit-disciplin** ligger bakom ren historik och reverts
- CLI:t är knöligt; **datamodellen** är enkel och korrekt
- Tillhörande övning: `/exercises/15-code-collaboration/`

---

## Frågor?
