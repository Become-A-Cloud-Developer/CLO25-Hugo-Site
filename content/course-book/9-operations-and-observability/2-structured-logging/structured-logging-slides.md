+++
title = "Structured Logging with ILogger"
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

## Structured Logging with ILogger
Part IX — Operations and Observability

---

## Why structure matters
- Free-text log lines are **searchable**, not queryable
- Concatenated strings dissolve their values into prose
- At scale, an operator needs **fields**, not regex extraction
- Structured logs keep both the human-readable message **and** the named fields

---

## What structured logging is
- A constant **message template** with named placeholders
- Values passed as separate arguments — never interpolated
- Framework retains the rendered text **and** the field map
- Downstream stores index the fields as **columns**

---

## ILogger&lt;T&gt; in ASP.NET Core
- Host registers an open generic `ILogger<>` mapping in **DI**
- Constructors ask for `ILogger<NewsletterController>` — get it
- The type parameter `T` becomes the **log category**
- Categories are matched **hierarchically** by namespace prefix

---

## Message templates
- First argument is a **constant string** with `{PascalCasePlaceholders}`
- Trailing arguments fill the placeholders by **position**
- Field names come from the template, not the argument variables
- Never build the template at runtime — it is the **event identity**

---

## Log levels
- **Trace / Debug** — diagnostic detail, off in production
- **Information** — normal flow: request started, job completed
- **Warning** — unexpected but recovered (retry, fallback, missing config)
- **Error / Critical** — failed operation / system-level failure

---

## Logging scopes
- `using (logger.BeginScope(new Dictionary<string, object> { ... }))`
- Fields attach to **every log call inside the block**
- Common use: per-request **correlation ID** added by middleware
- Scopes nest — request, tenant, and operation scopes compose

---

## Enrichment
- Some context belongs on **every** log line, not at each call site
- Global: machine name, build SHA, environment — added at host setup
- Per-request: correlation ID, user ID, tenant ID — added by middleware
- Keeps the call site focused on the **event**, not the metadata

---

## Worked example
- `ILogger<NewsletterController>` injected via constructor
- Per-request scope opened with `CorrelationId = HttpContext.TraceIdentifier`
- `LogInformation("Subscribe requested for {Email}", email)`
- A query can now group by template, filter by correlation ID

---

## Operational practices
- **No PII in logs** — replace email/name with surrogate IDs
- **No secrets in logs** — scrub config values, tokens, headers
- **Sample at high volume** — drop Debug, sample chatty events
- **Keep templates stable** — dashboards key on the constant text

---

## Questions?
