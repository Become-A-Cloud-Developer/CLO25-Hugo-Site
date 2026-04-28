+++
title = "Pipeline-hemligheter och OIDC-federation"
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

## Pipeline-hemligheter och OIDC-federation
Del VIII — DevOps och leverans

---

## Varför pipeline-credentials är farliga
- En pipeline körs **obevakad** på extern hårdvara med **produktionsåtkomst**
- Lagrade molncredentials är den vanligaste orsaken bakom molnincidenter
- En läckt långlivad hemlighet är användbar tills **manuell rotation** hinner i kapp
- Försvar: icke-mänsklig identitet, smalt scope, kortlivade tokens

---

## GitHub-hemlighetslagret
- En **GitHub secret** är ett konfidentiellt värde lagrat krypterat i ett repo
- Injiceras som **miljövariabler** in i auktoriserade workflow-körningar
- Visas aldrig i UI:t efter skapande — bara **skriv över** eller **radera**
- Loggar maskeras **best-effort**; eka aldrig en hemlighet med flit

---

## Service principal-modellen
- En **service principal** är en Entra-app + hemlighet + RBAC-rolltilldelning
- `az ad sp create-for-rbac` ger **client-id**, **client-secret**, **tenant-id**
- Hemligheten lagras i GitHub och läses av `azure/login` vid varje körning
- Fungerar, men det **långlivade lösenordet** är själva felmoden

---

## Den federerade credential-modellen
- En **federated credential** är ett förtroende mellan service principalen och en extern IdP
- GitHub utfärdar en **token** (JWT) om workflow-körningen; Entra litar på signaturen
- Token växlas mot en riktig Azure-token — **ingen lagrad hemlighet**
- Detta är **OIDC-federation (workload)** — samma form som användar-OIDC, annan principal

---

## Federationens subject-sträng
- Format: **`repo:org/repo:ref:refs/heads/branch`**
- Anger vilket **repo**, vilken **typ av ref**, vilken **specifik ref**
- Vanliga typos: `refs/head/main`, fel case, saknad `environment:`-claim
- Felaktig matchning ger ett generiskt **"no matching federated identity"**

---

## Långlivad mot federerad
- Lagrad credential: **client-secret** (långlivad) mot **ingen** (token per körning)
- Livslängd: **månader/år** mot **minuter**
- Blast-radius vid läcka: **fullt RBAC-scope** mot **redan utgången**
- Branch-scoping: **ingen** mot **inbyggd i subject-strängen**

---

## Genomgång — azure/login med OIDC
- Workflow deklarerar **`permissions: id-token: write`** för att mynta token
- `azure/login@v2` tar emot **client-id**, **tenant-id**, **subscription-id**
- Dessa är **publika identifierare**, inte hemligheter — läckage skadar inte
- `environment: production` ger en miljö-claim som drar åt subject-strängen

---

## Var detta landar i övningen
- Övningen progresserar **Docker Hub-PAT → SP-hemlighet → OIDC**
- Tredje steget konfigurerar en **federated credential** per branch/miljö
- Pipelinen fortsätter fungera; **inget lösenord** ligger kvar i GitHub
- Samma mönster täcker alla moln-anrop: registry, deploy, key vault-läsning

---

## Frågor?
