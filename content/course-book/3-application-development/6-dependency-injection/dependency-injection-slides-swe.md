+++
title = "Dependency Injection"
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

## Dependency Injection
Del III — Applikationsutveckling

---

## Problemet
- Controllers som **`new`:ar sina samarbetspartners** blandar arbete med montering
- Ett konkret val blir **hårdkodat** i konsumenten
- Tester kan inte byta in fejk-objekt utan att skriva om klassen
- Object-graph-koppling läcker ut i **varje lager**

---

## Inversionen
- En klass **deklarerar** vad den behöver via sin constructor
- En **container** i runtime levererar implementationerna
- Konsumenten beror på ett **interface**, inte en konkret typ
- Konstruktionsordningen flyttas till **ett ställe** vid uppstart

---

## Constructor Injection
- Beroenden kommer in via **constructor-parametrar**
- Lagras i **`readonly`**-fält — oföränderliga under objektets livstid
- Gör kraven **explicita** i typsignaturen
- Passar naturligt med **interface-baserad** registrering

---

## Service-registrering
- `IServiceCollection` är **startup-byggaren** i `Program.cs`
- `AddScoped<INewsletterService, NewsletterService>()` binder **interface till implementation**
- Registrering på konkret typ fungerar för **interna** hjälpklasser
- Containern stängs när `builder.Build()` körs

---

## De tre livstiderna
- **Singleton** — en instans för hela applikationen
- **Scoped** — en instans per HTTP-request
- **Transient** — en ny instans vid varje upplösning
- Standardval för applikationstjänster är oftast **Scoped**

---

## Captive Dependencies
- En **Singleton** som håller ett **Scoped** beroende fångar in det
- Den kortlivade tjänsten **uppgraderas** till den längre livstiden
- Tillstånd per request **läcker** då mellan användare
- Validera vid uppstart; omvänd riktning (Scoped beror på Singleton) är säker

---

## Genomgånget exempel — Newsletter
- `INewsletterService` → `NewsletterService` registreras som **Scoped**
- `ISubscriberRepository` → `InMemorySubscriberRepository` registreras som **Singleton**
- `NewsletterController` tar emot **`INewsletterService`** via sin constructor
- Byte av repository ändrar **en rad** i `Program.cs`

---

## Testbarhet
- Bygg controllern manuellt med en **fejk**-service
- Ingen DI-container, ingen databas, ingen HTTP-server behövs
- Tester blir **snabba, deterministiska, isolerade**
- Vinsten av **interface + constructor injection**

---

## Frågor?
