# Course Book Progress

The persistent progress tracker for the CLO25 Course Book. The orchestrator skill `develop-theory-chapter` reads this at the start of every Part-run and updates it after every gate. Treat it as the single source of truth across runs.

**Status legend:**

- `pending` — chapter brief exists but no files written yet
- `drafting` — B2 worker assigned; files may be partially written
- `review` — B4 review pass in progress
- `fixing` — B5 fix-up in progress
- `voice` — B6 voice rewrite in progress
- `validated` — all 10 gates passed
- `blocked` — at least one gate failed; needs human triage

---

## Migration log

> Filled in during Workstream B. Each row records the source path → destination path of a moved theory section, with the `aliases` rewrite and any link rewrites that were needed.

| From | To | Aliased | Internal link rewrites |
|------|----|---------|------------------------|
| _(empty until Workstream B runs)_ | | | |

---

## Part I — Cloud Foundations

> Migrated from `content/intro-cloud-development/` during Workstream B. Exempt from the new-chapter gates 9 (glossary) and 10 (cross-link to exercise) because the chapters predate those rules.

| Chapter | Slug | Status | Notes |
|---------|------|--------|-------|
| _(populated during migration)_ | | | |

---

## Part II — Infrastructure

> Migrated from `content/infrastructure-fundamentals/` during Workstream B. Exempt from the new-chapter gates 9 and 10.

| Chapter | Slug | Status | Notes |
|---------|------|--------|-------|
| _(populated during migration)_ | | | |

---

## Part III — Application Development

**Course tag:** BCD
**Companion exercise:** `content/exercises/10-webapp-development/`
**Studieguide week:** Course week 3 (v.7) and Course week 5 (v.9)
**Glossary file:** `docs/coursebook-mining/part-3-glossary.md`
**Run completed:** 2026-04-28

| # | Title | Status | Words | Slides EN/SWE | Last gate | Notes |
|---|-------|--------|-------|---------------|-----------|-------|
| 1 | HTTP Fundamentals | validated | 2213 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 2 | The .NET Platform | validated | 1608 | 9 / 9 | 2026-04-28 | All 10 gates pass |
| 3 | The MVC Pattern | validated | 2117 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 4 | Three-Tier Architecture | validated | 1861 | 9 / 9 | 2026-04-28 | All 10 gates pass |
| 5 | Configuration and Environments | validated | 2030 | 10 / 10 | 2026-04-28 | One soft warning ("simply") accepted |
| 6 | Dependency Injection | validated | 1943 | 10 / 10 | 2026-04-28 | All 10 gates pass |

**Part III totals:** 6 chapters, 11,772 words of prose, 12 HTML slide decks. Cross-Part anchor check: 0 unresolved.

---

## Part IV — Data Access

**Course tag:** BCD
**Companion exercise:** `content/exercises/5-cloud-databases/`, `content/exercises/10-webapp-development/3-data-layer/`
**Studieguide week:** Course week 6 (v.10)
**Glossary file:** `docs/coursebook-mining/part-4-glossary.md`
**Run completed:** 2026-04-28

| # | Title | Status | Words | Slides EN/SWE | Last gate | Notes |
|---|-------|--------|-------|---------------|-----------|-------|
| 1 | Relational vs NoSQL Data Models | validated | 1883 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 2 | ORM and the Repository Pattern | validated | 2215 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 3 | Connections, Pooling, and Transactions | validated | 2602 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 4 | Object Storage and File Uploads | validated | 2444 | 10 / 10 | 2026-04-28 | All 10 gates pass |

**Part IV totals:** 4 chapters, 9,144 words of prose, 8 HTML slide decks. All four chapters pass voice-check clean.

---

## Part V — Identity & Security

**Course tag:** BCD + ACD
**Companion exercises:** `content/exercises/10-webapp-development/4-authentication-authorization/`, `content/exercises/10-webapp-development/5-identity-and-user-stores/`, `content/exercises/4-services-and-apis/1-rest-api-and-dtos/`
**Studieguide weeks:** BCD week 8 (v.12) and ACD week 3 (v.17)
**Glossary file:** `docs/coursebook-mining/part-5-glossary.md`
**Run completed:** 2026-04-28

| # | Title | Status | Words | Slides EN/SWE | Last gate | Notes |
|---|-------|--------|-------|---------------|-----------|-------|
| 1 | Authentication vs Authorization | validated | 2109 | 9 / 9 | 2026-04-28 | All 10 gates pass |
| 2 | Cookie-Based Authentication and Sessions | validated | 2272 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 3 | ASP.NET Core Identity | validated | 1989 | 11 / 11 | 2026-04-28 | All 10 gates pass |
| 4 | Roles, Claims, and Policies | validated | 1671 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 5 | Bearer Tokens and JWT | validated | 2072 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 6 | API Keys and Machine-to-Machine | validated | 2141 | 11 / 11 | 2026-04-28 | All 10 gates pass |
| 7 | OAuth 2.0 and OpenID Connect | validated | 2527 | 9 / 9 | 2026-04-28 | All 10 gates pass |
| 8 | Secret Management | validated | 2254 | 10 / 10 | 2026-04-28 | All 10 gates pass |

**Part V totals:** 8 chapters, 17,035 words of prose, 16 HTML slide decks. All chapters pass voice-check clean.

---

## Part VI — Services and APIs

**Course tag:** ACD
**Companion exercise:** `content/exercises/4-services-and-apis/`
**Studieguide week:** ACD week 6 (v.20)
**Glossary file:** `docs/coursebook-mining/part-6-glossary.md`
**Run completed:** 2026-04-28

| # | Title | Status | Words | Slides EN/SWE | Last gate | Notes |
|---|-------|--------|-------|---------------|-----------|-------|
| 1 | REST Principles | validated | 2096 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 2 | Resource Modeling and URIs | validated | 2156 | 11 / 11 | 2026-04-28 | All 10 gates pass |
| 3 | DTOs vs Entities | validated | 1810 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 4 | Status Codes, Versioning, and Error Responses | validated | 2022 | 11 / 11 | 2026-04-28 | All 10 gates pass |
| 5 | OpenAPI and Swagger | validated | 2077 | 11 / 11 | 2026-04-28 | All 10 gates pass |
| 6 | Pagination, Idempotency, and Rate Limiting | validated | 2132 | 10 / 10 | 2026-04-28 | All 10 gates pass |

**Part VI totals:** 6 chapters, 12,293 words of prose, 12 HTML slide decks. All chapters pass voice-check clean.

---

## Part VII — Containers

**Course tag:** ACD
**Companion exercise:** `content/exercises/20-docker/`
**Studieguide week:** ACD week 2 (v.16)
**Glossary file:** `docs/coursebook-mining/part-7-glossary.md`
**Run completed:** 2026-04-28

| # | Title | Status | Words | Slides EN/SWE | Last gate | Notes |
|---|-------|--------|-------|---------------|-----------|-------|
| 1 | Containers vs Virtual Machines | validated | 1948 | 9 / 9 | 2026-04-28 | All 10 gates pass |
| 2 | Images and Layers | validated | 1980 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 3 | Dockerfiles and Multi-Stage Builds | validated | 1908 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 4 | Multi-Platform Builds | validated | 1652 | 11 / 11 | 2026-04-28 | All 10 gates pass |
| 5 | Docker Compose | validated | 2065 | 12 / 12 | 2026-04-28 | All 10 gates pass |
| 6 | Container Registries | validated | 2109 | 10 / 10 | 2026-04-28 | All 10 gates pass |

**Part VII totals:** 6 chapters, 11,662 words of prose, 12 HTML slide decks. All chapters pass voice-check clean.

---

## Part VIII — DevOps and Delivery

**Course tag:** ACD
**Companion exercise:** `content/exercises/3-deployment/9-cicd-to-container-apps/`
**Studieguide week:** ACD week 4 (v.18)
**Glossary file:** `docs/coursebook-mining/part-8-glossary.md`
**Run completed:** 2026-04-28

| # | Title | Status | Words | Slides EN/SWE | Last gate | Notes |
|---|-------|--------|-------|---------------|-----------|-------|
| 1 | The DevOps Philosophy | validated | 2317 | 9 / 9 | 2026-04-28 | All 10 gates pass |
| 2 | Continuous Integration vs Continuous Deployment | validated | 2365 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 3 | Pipelines as Code | validated | 2123 | 11 / 11 | 2026-04-28 | All 10 gates pass |
| 4 | Build, Test, and Smoke Gates | validated | 2226 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 5 | Deployment Strategies | validated | 2182 | 10 / 10 | 2026-04-28 | All 10 gates pass |
| 6 | Pipeline Secrets and OIDC Federation | validated | 2170 | 9 / 9 | 2026-04-28 | All 10 gates pass |
| 7 | Azure Container Apps as a Deployment Target | validated | 2258 | 10 / 10 | 2026-04-28 | All 10 gates pass |

**Part VIII totals:** 7 chapters, 15,641 words of prose, 14 HTML slide decks. All chapters pass voice-check clean.

---

## Part IX — Operations & Observability

**Course tag:** ACD
**Companion exercise:** `content/exercises/3-deployment/10-logging-and-monitoring/`
**Studieguide week:** ACD week 5 (v.19), BCD week 9 (v.13)
**Glossary file:** `docs/coursebook-mining/part-9-glossary.md`

| # | Title | Status | Words | Slides EN/SWE | Reviewer | Last gate | Notes |
|---|-------|--------|-------|---------------|----------|-----------|-------|
| _(populated during Part IX run)_ | | | | | | | |

---

## Part X — Collaboration & Process

**Course tag:** ACD
**Companion exercise:** `content/exercises/15-code-collaboration/`
**Studieguide week:** ACD week 1 (v.15)
**Glossary file:** `docs/coursebook-mining/part-10-glossary.md`

| # | Title | Status | Words | Slides EN/SWE | Reviewer | Last gate | Notes |
|---|-------|--------|-------|---------------|----------|-----------|-------|
| _(populated during Part X run)_ | | | | | | | |

---

## Validated chapter detail

> Detailed gate-pass records per validated chapter. The orchestrator appends one entry here per chapter when it passes B7. Format defined in `.claude/skills/develop-theory-chapter/PHASES.md` under "Tracker update".

_(empty until Workstream C produces validated chapters)_

---

## Blocked chapters

> Chapters that failed at least one gate and need manual triage. The orchestrator surfaces these at end of each Part-run.

_(empty)_
