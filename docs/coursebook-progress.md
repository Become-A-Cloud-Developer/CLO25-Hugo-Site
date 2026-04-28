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
**Glossary file:** `docs/coursebook-mining/part-3-glossary.md` (created in B1)

| # | Title | Status | Words | Slides EN/SWE | Reviewer | Last gate | Notes |
|---|-------|--------|-------|---------------|----------|-----------|-------|
| _(populated during Part III run)_ | | | | | | | |

---

## Part IV — Data Access

**Course tag:** BCD
**Companion exercise:** `content/exercises/5-cloud-databases/`, `content/exercises/10-webapp-development/3-data-layer/`
**Studieguide week:** Course week 6 (v.10)
**Glossary file:** `docs/coursebook-mining/part-4-glossary.md`

| # | Title | Status | Words | Slides EN/SWE | Reviewer | Last gate | Notes |
|---|-------|--------|-------|---------------|----------|-----------|-------|
| _(populated during Part IV run)_ | | | | | | | |

---

## Part V — Identity & Security

**Course tag:** BCD + ACD
**Companion exercise:** `content/exercises/10-webapp-development/4-authentication-authorization/`, `content/exercises/10-webapp-development/5-identity-and-user-stores/`, `content/exercises/4-services-and-apis/1-rest-api-and-dtos/2-api-key-middleware.md`, plus the future JWT chapter
**Studieguide week:** ACD week 3 (v.17)
**Glossary file:** `docs/coursebook-mining/part-5-glossary.md`

| # | Title | Status | Words | Slides EN/SWE | Reviewer | Last gate | Notes |
|---|-------|--------|-------|---------------|----------|-----------|-------|
| _(populated during Part V run)_ | | | | | | | |

---

## Part VI — Services and APIs

**Course tag:** ACD
**Companion exercise:** `content/exercises/4-services-and-apis/`
**Studieguide week:** ACD week 6 (v.20)
**Glossary file:** `docs/coursebook-mining/part-6-glossary.md`

| # | Title | Status | Words | Slides EN/SWE | Reviewer | Last gate | Notes |
|---|-------|--------|-------|---------------|----------|-----------|-------|
| _(populated during Part VI run)_ | | | | | | | |

---

## Part VII — Containers

**Course tag:** ACD
**Companion exercise:** `content/exercises/20-docker/`
**Studieguide week:** ACD week 2 (v.16)
**Glossary file:** `docs/coursebook-mining/part-7-glossary.md`

| # | Title | Status | Words | Slides EN/SWE | Reviewer | Last gate | Notes |
|---|-------|--------|-------|---------------|----------|-----------|-------|
| _(populated during Part VII run)_ | | | | | | | |

---

## Part VIII — DevOps and Delivery

**Course tag:** ACD
**Companion exercise:** `content/exercises/3-deployment/9-cicd-to-container-apps/`
**Studieguide week:** ACD week 4 (v.18)
**Glossary file:** `docs/coursebook-mining/part-8-glossary.md`

| # | Title | Status | Words | Slides EN/SWE | Reviewer | Last gate | Notes |
|---|-------|--------|-------|---------------|----------|-----------|-------|
| _(populated during Part VIII run)_ | | | | | | | |

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
