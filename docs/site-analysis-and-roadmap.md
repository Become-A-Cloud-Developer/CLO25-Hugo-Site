# CLO25 Hugo Site — Content Analysis and Improvement Roadmap

**Audit date:** 2026-05-07
**Scope:** All content under `content/`, all decks under `static/presentations/`, course planning material under `docs/`
**Method:** 8 parallel Explore-agent audits, sanity-checked, then synthesised
**Audience assumed:** Students with a tiny bit of C# but no prior web-development experience

---

## 1. Executive summary

The CLO25 site is a substantial body of work — **369 markdown files**, **155 Reveal.js decks**, **18 weekly study guides**, and **rubric-grade assignments for both BCD and ACD**. After 18 weeks of teaching across two cohorts, the structure is mature in places and uneven in others.

**The site is fundamentally sound.** The teaching loop (HTTP → MVC → DI → databases → auth → containers → CI/CD → observability) is coherent, the assignments are well-calibrated, and the prose in Parts III, IV, VIII of the course book is genuinely teaching-quality. ACD builds appropriately on BCD without retreading basics.

**Three categories of friction matter most:**

1. **Discoverability is the #1 lever.** A cold visitor hits a homepage that doesn't match the actual sections, then clicks into landing pages whose `{{< children />}}` shortcodes show folder names with no descriptions. Forty-seven `{{< children />}}` calls — none use `description="true"`. This is the cheapest, highest-impact fix on the site.
2. **A few content gaps create real harm.** Razor Pages (0 mentions), Moq (0), xUnit (1 file), Azure App Service (5), Azure Functions (5), and hands-on EF Core practice (14 files mention it but no end-to-end exercise) are real day-1 blockers for a junior cloud .NET developer. The content calendar over-rotates on containers and Container Apps; that bias is now visible.
3. **Onboarding has no door.** There is no prerequisites page, no install checklist, no support-channel page, and no Part I presentation deck. Students arrive cold, hit `dotnet` commands in week 3, and have to assemble their environment from scattered exercises.

**The roadmap below proposes four phases**, anchored to the actual academic calendar, beginning with low-effort high-impact fixes that can land before ACD25 ends (current cohort, weeks 6–8), and ending with the structural improvements needed before the next BCD cohort starts in v.5 (late January 2027).

---

## 2. Scope and method

Eight independent analysis agents each owned one slice of the site:

| # | Slice | Files reviewed |
| --- | --- | --- |
| 1 | Information architecture and navigation | All `_index.md`, `hugo.toml`, prior planning docs |
| 2 | Course Book Parts 1–5 | 146 files |
| 3 | Course Book Parts 6–10 | 95 files |
| 4 | Exercises (9 chapters) | 97 files, sampled |
| 5 | Week-by-week schedules (BCD 1–10, ACD 1–8) | 18 week pages + 4 study guides |
| 6 | Onboarding, tutorials, Reveal.js presentations | 4 tutorials, 155 decks |
| 7 | C# / .NET / Azure technology coverage | Cross-content grep matrix |
| 8 | Cross-cutting: assignments, course-plan alignment, i18n, accessibility | All 4 assignment specs, both kursplaner, both studieguider |

After all eight reports returned, the most striking quantitative claims were re-verified directly (line counts, file counts, grep counts) before being included in this synthesis. One agent error has been corrected: Part 2 presentations were reported missing; they exist (26 decks under `compute/`, `network/`, `storage/` subfolders).

A **three-agent independent review of this document** is scheduled as a final step (see §8).

A **v3 amendment pass (2026-05-09)** then resolved several findings against context the original audit didn't have: the prior-course assumed background, the deliberate scope of CLO25 within a four-course program, and the central pedagogical thesis of the course. Those amendments are integrated into §3.5, §3.6, §4.6, §5, §6, §7, and the new §9 "Deliberately deferred", and logged at the end of §8.

---

## 3. Cross-cutting strengths

These deserve to be named so they survive any rewrite.

- **Coherent teaching arc.** From "what is a server" through deploying a containerised .NET app with OIDC-federated CI/CD and KQL observability is a clean spine. Few courses at this level cover that whole arc.
- **Real-world stack, not a toy stack.** Bicep, Container Apps, Application Insights, Cosmos DB, GitHub Actions OIDC, Managed Identity, ACR — these are what graduates will actually use.
- **Assignments are rubric-grade.** All four assignment specs in `docs/assignments/` use the Swedish U/G/VG scale, distinguish minimum-competence from professional-judgement, and require students to *justify* choices. ACD-1's "AI reflection" criterion is unusually thoughtful.
- **Bilingual presentation parity for delivered material.** 61 of 155 decks have a `-swe` Swedish twin. Pedagogy is consistent across the pair.
- **Strong individual chapters.** Part III (Application Development) — HTTP → .NET runtime → MVC → 3-tier → DI → config — is exemplary. Part IV's relational-vs-NoSQL chapter is the single best decision-framework page in the book. Part VIII's CI vs CD and OIDC chapters are clear and grounded. Docker chapter (Part 7) is well-paced. Cross-references from theory chapters to exercise chapters are consistent.
- **Cost discipline in exercises.** Exercises remind students to deprovision; serverless Cosmos and small SKUs are chosen by default; no hidden tier-1 SKU traps.
- **Week-by-week scaffolding is consistent.** Every week has Preparation → Theory → Practice → Reflection. Students always know what shape the week takes.
- **Strong week-page → content cross-linking.** Weeks point to specific course-book chapters and exercises; minimal orphan stubs.

---

## 3.5 Prior-course context (added v3, 2026-05-09)

The original audit read the site as if it were standalone. It isn't — it is the cloud half of a four-course sequence in the Cloud Developers program. Knowing what students bring with them is essential for judging what is "missing" vs "intentionally absent".

**Students arrive with:**

- A C# fundamentals course
- An object-oriented programming course
- A small test-driven-development course (xUnit basics, AAA pattern)
- A very basic architecture introduction
- An EF Core course where they used **SQLite locally**

**Crucially, all of that is on the laptop.** Students have never connected to anything over a real network. No SSH. No HTTP across machines. No remote database. No external dependency that lives somewhere they don't fully control. CLO25 is the first time the network becomes a participant in their code.

**Implications for the roadmap:**

- Several items the original audit listed as "missing .NET craft" are actually upstream prerequisites — EF Core, migrations, SQL, xUnit basics, OO. The CLO25 site is not the place to teach them.
- The MongoDB / Cosmos choice is not a gap; it is *deliberate NoSQL novelty* added on top of the EF Core + SQLite background students already have. It also keeps infrastructure rollout clean (no `dotnet ef database update` step, no migration drift between revisions) and avoids the Azure-SQL-serverless cold-start trap (auto-pause + 30–60s warmup forces students to learn transient-fault retry patterns *before* they have seen a happy-path round-trip — exactly the wrong sequencing).
- The remaining "test craft" gap is narrower than the original audit suggested: `WebApplicationFactory` and ASP.NET-specific integration testing are likely still genuinely missing (the prior course did not have a web app to test against), but generic xUnit + Moq is upstream.
- A **`getting-started/prior-knowledge.md` page** that names this assumed background explicitly is one of the highest-leverage additions to the site. It would have prevented the original audit's mistake and prevents future students from feeling lost.

## 3.6 The network-novelty thesis (added v3, 2026-05-09)

CLO25 has a strong central pedagogical thesis that the audit only saw in fragments. Naming it once, here, makes the rest of the document make sense.

**The thesis.** Students arrive having only ever programmed *against themselves*. Every dependency they have ever talked to was reachable without leaving the machine. The job of CLO25 BCD is *the moment that ends.* The course is — at the design level — a curated sequence of first encounters with the network as a place where things go wrong: latency, partition, identity, schema-on-the-other-side, signal that has to leave the process to be observed. Each week introduces one new network boundary, each new boundary introduces one new failure mode, the cloud platform is the *medium* through which these are taught — not the subject.

**The four-course arc.** CLO25 sits in the middle of a deliberate sequence:

```
Pre-CLO25:  local dev — the world ends at the laptop
            (C#, OOP, TDD, basic architecture, EF Core + SQLite)

CLO25 BCD:  your first network — cloud as "traditional IT, but reachable"
            transferable mindset: this also describes on-prem

CLO25 ACD:  leave the server behind — containers as the unit of thought
            first serious cloud-native step

Post-CLO25: leave the cluster behind — scale, serverless, orchestration
            (AKS, Static Web Apps, Functions)
```

BCD's IaaS-first design is therefore not nostalgic; it is the *bridge* from local-laptop dev to cloud, going through "real but traditional-feeling" infrastructure first. VMs feel like the laptop the students know but bigger. Networks force them to confront the network boundary. *Then* containerisation in ACD lifts them off on-prem entirely. That is why App Service and Static Web Apps and Functions are deliberately absent: App Service is an off-ramp from this ladder ("upload zip; it runs"), and the others are the next course's job.

**Per-week network-boundary mapping.** Once the thesis is named, the 16 active weeks cohere as a sequence of *first encounters with one network thing*:

| # | Week | First network thing met |
| --- | --- | --- |
| 1 | BCD-1 | SSH — *the machine is over there, not under your fingers* |
| 2 | BCD-2 | The Azure control plane — *your code (Bicep) reaches into a remote datacentre* |
| 3 | BCD-3 | HTTP between a browser and a process you don't own anymore |
| 4 | BCD-4 | The network as topology — subnets, NSGs, bastions, packets that can be denied |
| 5 | BCD-5 | A database connection that *traverses the network*, not the filesystem |
| 6 | BCD-6 | Cloud data services — managed dependencies you don't run |
| 7 | BCD-7 | The CI/CD network — your repo reaching across to your cloud |
| 8 | BCD-8 | The identity network — Key Vault, managed identity, RBAC as a remote authority |
| 9 | BCD-9 | The telemetry network — logs leaving your process to live somewhere else |
| 10 | ACD-1 | (dev environment / agile — preparation week, not a new boundary) |
| 11 | ACD-2 | Container registries — image pull as a network operation |
| 12 | ACD-3 | Identity tokens crossing the network (cookies, JWTs) |
| 13 | ACD-4 | Image-pull identity from registry, OIDC federation in CI/CD |
| 14 | ACD-5 | Structured telemetry beyond simple logs — KQL across a remote workspace |
| 15 | ACD-6 | Public APIs and DTOs as the negotiated boundary |
| 16 | ACD-7 | Resilience patterns — health probes, feature flags, MI from the platform side |

The thesis is currently *invisible on the site*. Students never read this paragraph; they meet each week's network novelty without the through-line being named. Surfacing it (a one-line "this week's new network boundary" callout per week page; a "why this course is shaped this way" piece in `getting-started/`) is the single most valuable framing change the site could make. It costs almost nothing and changes how students hold the whole 18 weeks together.

---

## 4. Findings by area

### 4.1 Information architecture and navigation

**Verdict:** *The skeleton is right; the connective tissue is missing.*

The site uses a clean four-tree model — Course Book, Exercises, Week-by-week, Tutorials — with explicit Part I–X numbering and weekly weights. The trees are sound. The problems are at the joints.

| Issue | Evidence | Impact |
| --- | --- | --- |
| Homepage doesn't match sections | `content/_index.md` lists "Cloud Fundamentals", "Application Development", "Deployment Practices" — none of which are real top-level sections; the only link is `/getting-started/` | Cold visitor has no path into the actual material |
| All `{{< children />}}` calls are bare | 47 instances; 0 use `description="true"` | Landing pages render folder names without summaries; students must click each child to learn what it is |
| Sub-chapter `_index.md` files have no `description` frontmatter | E.g. `course-book/1-cloud-foundations/1-understanding-cloud-computing/_index.md` | Even if `description="true"` is enabled, half the children will render blank |
| Cross-references are unidirectional | Part III → Exercise 10 exists, but Exercise 10 → Part III back-link missing | A student starting from an exercise can't find the theory |
| Exercise numbering has unexplained gaps | 1, 2, 3, 4, 5, 6, 10, 15, 20 — no 7–9, 11–14, 16–19 | Sidebar implies content is missing; students can't tell if it's planned, deleted, or intentional |
| Tutorials section is orphaned | 3 tutorials, no entry from week pages or course book, generic description | Hard to discover; unclear when to use vs course-book vs exercises |

**Existing planning:** `docs/PROGRESSIVE-DISCLOSURE-IMPROVEMENTS.md` already identifies most of this. It has not been executed.

### 4.2 Course Book

| Part | Files | Quality | Verdict |
| --- | --- | --- | --- |
| I — Cloud Foundations | 24 | 3.5 | Foundations and 12-factor solid; *Technical Opportunities* and *Cloud-Native Development* chapters read as 30-line summaries with numbering errors (`1, 2, 3, 4, 3, 4, ...`); zero .NET grounding |
| II — Infrastructure | 65 | 4.0 | Compute, network, storage are mature; **IaC chapter is 130 lines** with thin Bicep treatment despite being a learning outcome of both courses; `compute/legacy/` folder needs archiving |
| III — Application Development | 19 | 4.5 | Reference-quality. HTTP → .NET → MVC → 3-tier → DI → config sequence is exemplary |
| IV — Data Access | 13 | 4.0 | Relational-vs-NoSQL chapter is the highlight of the book; ORM/EF coverage thinner than the importance of the topic warrants |
| V — Identity & Security | 25 | 3.0 | Auth/authz and secret-management chapters strong; JWT, OAuth, OIDC, API keys treated more shallowly than identity's centrality justifies |
| VI — Services and APIs | 19 | 4.5 | REST principles → DTOs → OpenAPI is well-paced; pagination + rate-limiting chapter is the chapter-level weak point |
| VII — Containers | 19 | 4.0 | Multi-stage builds and Compose strong; **container-registries chapter is 95 lines** and barely solves Docker Hub rate-limit; multi-platform builds shallow |
| VIII — DevOps and Delivery | 22 | 4.0 | Philosophy, CI vs CD, OIDC are strong; **deployment-strategies (113 lines)** describes blue-green/canary verbally with no traffic-split YAML; **Container Apps chapter (113 lines)** omits health checks |
| IX — Operations and Observability | 19 | 3.5 | Three-pillars chapter excellent; **alerts-and-SLOs is 97 lines**, the shortest non-stub chapter in the book; KQL has only one worked example, no joins or cost discussion; sampling treatment is superficial |
| X — Collaboration and Process | 16 | 4.0 | Git and branching strong; agile-sprints lacks estimation/velocity; inner-loop-vs-outer-loop chapter doesn't ground in `dotnet watch` / TDD / sln structure |

**Cross-cutting weaknesses across the book:**

- No glossary file. Terms like "operation ID", "adaptive sampling", "DORA metrics", "DTO", "CIDR" are defined inline, inconsistently, and never cross-linked.
- No diagrams index. The book is text-heavy; few architecture diagrams, request-flow diagrams, sequence diagrams.
- Few callout/admonition blocks; what exists is inconsistent.
- No chapter summaries / "key takeaways" sections.
- No cross-Part bridge sentences (e.g. canary → observability → incident → sprint retro).

### 4.3 Exercises

| Chapter | Files | Quality | Verdict |
| --- | --- | --- | --- |
| 1 — Server Foundation | 14 | 4.5 | Portal → CLI → IaC progression is exemplary |
| 2 — Network Foundation | 6 | 4.0 | Strong concepts, uneven depth between exercises |
| 3 — Deployment | 14 | 3.5 | Numbering chaos (1, 2, 4, 5, 8, 9, 10); manual SCP → GitHub Actions with no IaC bridge |
| 4 — Services & APIs | 5 | 4.0 | Controllers vs Minimal vs DTOs is good pedagogy |
| 5 — Cloud Databases | 11 | 4.0 | Portal → CLI → IaC → Code → Blob progression is consistent with chapters 1, 2 |
| 6 — Storage & Resilience | 5 | 3.5 | **`1-mvc-uploads-and-pdf-validation.md` is 1281 lines** — too dense to debug; assumes ch. 5 context without recap |
| 10 — Webapp Development | 29 | 4.0 | Coherent 3-tier, dense; biggest chapter |
| 15 — Code Collaboration | 7 | 3.5 | Git basics solid; Jira/Scrum drift toward enterprise tooling that beginners do not need |
| 20 — Docker | 6 | 4.5 | Run → build → containerise → compose → multi-platform is well-shaped |

**Notable individual files:**

- `exercises/3-deployment/4-implementing-azure-key-vault-with-mongodb-on-azure-vm.md` — **1596 lines**, the largest exercise on the site; assumes prior MongoDB knowledge with no recap from chapter 5.
- `exercises/6-storage-and-resilience/1-uploads-and-deep-probes/1-mvc-uploads-and-pdf-validation.md` — 1281 lines covering upload form + validation + Blob + identity + deep probes + first deploy in one exercise. Should be three exercises.

**Cross-cutting weaknesses:**

- Inconsistent IaC coverage: chapters 1, 2, 5 cover Portal+CLI+Bicep; chapter 3 covers neither Portal nor IaC; chapter 6 is code-only.
- "Previous chapter" / "deployment chapter" prose without `{{< ref >}}` links.
- No Azure cost summary at chapter top — only cleanup at end.
- No chapter-level "If you skipped earlier chapters, here's what you need" recap.

### 4.4 Week-by-week schedules

| | Quality | Notes |
| --- | --- | --- |
| BCD weeks 1–9 | 5 | Consistent template, solid theory↔exercise bridges, reflection questions |
| BCD week 10 | 4 | Wrap-up; lacks deliverable spec |
| ACD weeks 1–7 | 5 | Excellent progression: Git/Jira → Docker → Auth → CI/CD → observability → APIs → Blob/health |
| ACD week 8 | 4 | Wrap-up; same issue as BCD 10; no exam rubric |

**Studieguide-vs-delivered alignment:**

- Reflection questions, links, prep steps, exercise references — all present and well-mapped.
- ACD has no Vimeo / video links; BCD week 1 has them. BCD weeks 2–10 also missing video links — only week 1 has them.
- Day-by-day Mon/Tue/Wed/Thu/Fri schedule from studieguide does not appear on per-week pages.
- Detailed assignment rubrics from `docs/assignments/` do not appear on the public site at all (see §4.7).
- No workload-hours estimate per week.

### 4.5 Onboarding, tutorials, presentations

**Onboarding (2/5).** No prerequisites page. No install checklist. No "where to ask for help" page. No GitHub-or-Azure-account section. A new student learns about `.NET SDK` in week 3 by stumbling into an exercise that uses it. `getting-started/acd/_index.md` is essentially a placeholder.

**Tutorials (2/5).** Only 3 substantive files (DNS, bastion, write-a-tutorial-meta). Both real tutorials are well-written but narrow. Missing: dotnet SDK install, Azure CLI essentials, Git workflow, Docker basics, App Service deploy, EF Core scaffolding.

**Presentations (4/5).** Strong visual design (the Swedish-tech aesthetic), 61 EN/SV pairs, consistent Reveal.js setup. The gap is curricular:

| Part | Decks |
| --- | --- |
| I — Cloud Foundations | **0** (directory does not exist) |
| II — Infrastructure | 26 (split across compute/network/storage) |
| III — Application Development | 12 |
| IV — Data Access | 8 |
| V — Identity & Security | 16 |
| VI — Services and APIs | 12 |
| VII — Containers | 12 |
| VIII — DevOps and Delivery | 14 |
| IX — Operations and Observability | 12 |
| X — Collaboration and Process | 10 |

**Part I has no decks at all** — the cold-start of the entire course has no slides. Mini-lectures folder has 4 substantive decks plus a C4-tool. There is no `/presentations/` index or landing page; 155 HTML files are discoverable only by guessing URLs.

### 4.6 .NET / C# / Azure technology coverage

**This section was rewritten in v3 (2026-05-09).** The original audit was framed as a gap analysis. After elaboration with the course author it became clear that most of the "gaps" are deliberately bounded scope (taught upstream in prior courses, or downstream in following courses, or off-ramps from a deliberate maturity ladder). The revised framing below is a *scope statement*, not a gap list. The original gap-analysis table is preserved in the v3 changelog at the end of §8.

**The deliberate scope.** CLO25 is positioned as the cloud half of a four-course program (see §3.6). The .NET stack is taught in two waves: pre-CLO25 (C#, OOP, TDD, EF Core + SQLite, basic architecture, all local) and CLO25 itself (.NET as the language used to *talk to the network*). Several technologies the original audit listed as "missing" are intentionally outside this wave:

| Technology | Status | Reasoning |
| --- | --- | --- |
| Containers, ACR, GitHub Actions OIDC, Bicep, Managed Identity, Key Vault, Application Insights, KQL, Container Apps | Strong end-to-end coverage | The cloud-native spine of the course |
| ASP.NET Core MVC, Web API, DTOs, REST, async/await, ILogger, Cosmos DB | Solid theoretical + exercise coverage | The "talk to the network" half of the .NET stack |
| Entity Framework Core, LINQ, SQL, migrations, repository pattern, xUnit basics, OO | **Upstream — taught in prior course** (SQLite, local) | Adding it here would be remedial, not new |
| MongoDB / NoSQL | Deliberately introduced as new content on top of the EF Core + SQLite background | Adds a new database paradigm; sidesteps the Azure-SQL-serverless cold-start retry-pattern trap (auto-pause + 30–60s warmup forces transient-fault retry *before* a happy-path round-trip — wrong sequencing for beginners); keeps infrastructure rollout clean (no migration step in CI) |
| Razor Pages | Out of scope | Pattern-within-.NET, not a transferable concept; MVC + Web API + DTOs covers the same range with concepts that survive outside .NET |
| Azure App Service | Out of scope | Pedagogically thin ("upload zip; it runs"); off-ramp from the maturity ladder rather than a step on it; teaches Azure-form-literacy rather than transferable platform thinking |
| Static Web Apps, Azure Functions, AKS | **Downstream — taught in next course** | Belong on the post-CLO25 cloud-native / scalability arc |
| App-specific integration testing (`WebApplicationFactory`) | Likely a real gap | Prior course had no web app to test against; this is the one test-craft item that plausibly belongs here. Confirm against prior-course syllabus before authoring |
| AI-assisted development | Real gap; named *kompetensmål* in the ACD kursplan, taught nowhere | Must be added (see §5 Theme F + Phase 0) |
| Background services / IHostedService, SignalR, Event Grid | Out of scope | Same downstream / off-ramp logic; not on the through-line |
| Retry / transient-fault patterns (Polly etc.) | Deliberately deferred this year (see §9) | Was on the schedule in prior years; was the topic that had to be shaved this year. Strong candidate for re-introduction in ACD W7 (Resilience patterns) |

**The compute maturity ladder, reframed.** The ladder taught is intentional: VM (IaaS, traditional-IT roots) → Containers (Docker as packaging) → Container Apps (PaaS-with-containers; halfway scenario to Kubernetes) → next course (AKS / SWA / Functions). Each rung teaches concepts the next rung depends on. App Service does not fit on this ladder; including it would dilute the through-line. **The original audit's "missing rung" framing has been retracted.**

### 4.7 Cross-cutting

**Assignments.** All four assignment specs in `docs/assignments/` are rubric-grade (BCD-1, BCD-2, ACD-1) or quiz-format (BCD-3, the weakest). They are not surfaced on the public site at all — students access them via Google Classroom. Ambient consequence: assignment expectations live in two places, and the site doesn't show its own assessment scaffold.

**Course-plan alignment.** The two kursplaner and two studieguider in `docs/course-plan/` map well to delivered content overall, with three notable gaps:

- BCD outcome S2 (CI/CD): assignment 2 requires GitHub Actions, but no pre-assignment lab teaches GHA.
- ACD outcome K2 (IaC): course-book has no dedicated IaC chapter; Bicep is shown in exercises but never *taught*.
- ACD outcome K4/C3 (AI assistants): required in assignment ACD-1's reflection, but the course teaches it nowhere.

**BCD-2 calibration.** Bicep + bastion + ASGs + GitHub Actions in weeks 4–6 may be over-scoped for "beginners with a tiny bit of C#". Consider making Bicep aspirational (VG) rather than required (G).

**Language policy.** Implicit, not explicit. Course-book and exercises are English; slides are bilingual; kursplan is Swedish; mixed Swedish-English in some frontmatter. No glossary EN↔SV. A declared policy + glossary file would close this.

**Accessibility.** Five recurring patterns:

1. Many content files start at H2; no semantic H1 (frontmatter `title` is not a heading).
2. Image alt text is mostly absent (only ~2 `![alt]` instances found in the sample).
3. Some code blocks lack language tags (highlighting fails silently).
4. Some Bootstrap-based admonitions in `10-webapp-development` rely on colour alone for meaning.
5. No automated link-rot or alt-text check in CI.

---

## 5. Improvement ideas, organised by theme

These are concrete, actionable ideas. Each is sized as **S** (≤2h), **M** (2–8h), or **L** (≥8h). Priority is **P0** (blocks current students), **P1** (high impact), **P2** (lower).

### Theme A — Discoverability and navigation

| Idea | Size | Priority |
| --- | --- | --- |
| Replace all 47 `{{< children />}}` with `{{< children description="true" >}}`, then backfill `description` frontmatter on every sub-chapter `_index.md` | M | P1 |
| Rewrite `content/_index.md` to match real sections (Course Book, Exercises, Week-by-week, Tutorials) | S | P1 |
| Add bidirectional cross-links between every Course Book Part `_index.md` and its companion exercise chapter `_index.md` | M | P1 |
| Add a one-line "exercise numbering" note to `content/exercises/_index.md` explaining why 7–9, 11–14, 16–19 are skipped | S | P2 |
| Add a `static/presentations/index.html` (or Hugo page) listing all 155 decks by Part with EN/SV toggle and 1-line descriptions | M | P1 |
| Add a `_glossary.md` for the Course Book; cross-link key terms (DTO, CIDR, OIDC, DORA, KQL, sampling) from chapters | M | P2 |
| **Add a one-line "this week's new network boundary" callout to each week page (per the §3.6 mapping table) — names the through-line of the course** | S | P1 |

### Theme B — Onboarding and prerequisites

| Idea | Size | Priority |
| --- | --- | --- |
| Create `content/getting-started/prerequisites.md` with .NET SDK install, VS Code + C# Dev Kit, Azure CLI, git + SSH key, GitHub + Azure account links | S | P0 |
| Create `content/getting-started/support.md` with class channel, office hours, FAQ, Jira board | S | P0 |
| Expand `content/getting-started/acd/_index.md` from placeholder to a real onboarding page | S | P1 |
| Add a "Before you start" callout at the top of week-1 BCD and week-1 ACD pointing at prerequisites + support pages | S | P0 |
| **Add `getting-started/prior-knowledge.md` listing what students bring from prior courses (C#, OOP, TDD, EF Core + SQLite, basic architecture). Prevents the audit-mistake of treating upstream content as missing** | S | P1 |
| **Add `getting-started/why-this-shape.md` (one screen) articulating the four-course arc, the network-novelty thesis, the maturity ladder, and the transferability promise (cloud here also describes on-prem). Make the pedagogy visible** | S | P1 |
| **Add a "Out of scope (taught in the next course)" line on the homepage and getting-started — App Service, AKS, Static Web Apps, Functions. Saves five Slack questions per cohort and prevents students feeling something is missing** | S | P1 |

### Theme C — Tutorials gap-fill

Five high-leverage tutorials would unblock weekly Slack questions:

| Idea | Size | Priority |
| --- | --- | --- |
| `Install .NET SDK and create your first ASP.NET Core MVC app` | S | P1 |
| `Azure CLI essentials — login, resource groups, key commands` | S | P1 |
| `Git workflow for pair programming` | S | P1 |
| `Docker basics on your laptop` | S | P2 |
| `Deploy a .NET app to Azure App Service` (also closes the App Service coverage gap) | M | P1 |

### Theme D — Course Book content depth

| Idea | Size | Priority |
| --- | --- | --- |
| Rewrite the four 30-line stubs in Part I (high-availability, architectural-patterns, two cloud-native chapters) and fix the numbering errors in `1-key-aspects-of-cloud-native-development.md` | M | P1 |
| Expand IaC chapter (`2-infrastructure/iac/1-what-is-iac/`) to ~300 lines with Bicep walkthrough; add the missing dedicated IaC chapter ACD-1 implicitly assumes | L | P0 |
| Add a Part IX *exercise* that wires up an SLO + alert rule, simulates a controlled incident, and observes the alert fire (the chapter is dense-but-complete; the gap is end-to-end practice, not chapter bloat) | M | P1 |
| Expand Part IX Application Insights with sampling configuration depth and custom-telemetry patterns; add a KQL stretch lab on cross-table joins (chapter itself is appropriate-for-beginners as-is — joins live in a stretch exercise) | M | P1 |
| Expand Part VIII deployment-strategies with one short canary code block (multi-revision mode + traffic weights, ~10 lines) and add a health-checks section to `7-azure-container-apps` (113 lines today) | M | P1 |
| Expand Part VII container-registries chapter — Docker Hub rate-limit mitigation, ACR geo-replication code | M | P2 |
| Ground Part X agile-sprints in estimation/velocity; ground inner-loop-vs-outer-loop in `dotnet watch` / TDD / `.sln` boundaries | M | P2 |
| Add "Key takeaways" sections at the end of every Part `_index.md` and every chapter `_index.md` | M | P2 |
| Standardise callout/admonition syntax across all parts (use Hugo's notice shortcodes consistently) | M | P2 |
| Archive `2-infrastructure/compute/legacy/` and similar legacy folders — keep them off the live sidebar | S | P2 |

### Theme E — Exercises pedagogy

| Idea | Size | Priority |
| --- | --- | --- |
| Split `6-storage-and-resilience/1-uploads-and-deep-probes/1-mvc-uploads-and-pdf-validation.md` (1281 lines) into three exercises: upload+validation (local), Blob+managed-identity, deep-probes+cleanup | M | P1 |
| Split or refactor `3-deployment/4-implementing-azure-key-vault-with-mongodb-on-azure-vm.md` (1596 lines) | M | P1 |
| Renumber and reflow chapter 3 (Deployment) so 1, 2, 3, 4, 5… are sequential; add a top-level "why this jumps from manual to GHA" bridge | M | P1 |
| Add an IaC-coverage table to `exercises/_index.md` (Portal/CLI/Bicep/GHA per chapter) | S | P1 |
| Replace every "the previous chapter" / "the deployment chapter" prose reference with `{{< ref >}}` links; add a CI lint that fails the build on the prose pattern | M | P2 |
| Add a "Prerequisites recap" block at the top of any exercise that references material from a prior chapter | M | P2 |
| Add a chapter-top Azure cost line (e.g. *"This chapter costs ~SEK 50/day if you forget to deprovision; cleanup steps are in Step 9."*) on every chapter `_index.md` | S | P2 |

### Theme F — .NET / C# tech-coverage gap-fill

**Scope reduced in v3.** The original audit listed seven additions; four are now retracted as upstream / out-of-scope (see §4.6 and the §8 v3 changelog). The remaining items are the genuine gaps.

| Idea | Size | Priority |
| --- | --- | --- |
| **Teach AI-assisted development explicitly** — short chapter (Part X or Part III) on three patterns: good use, bad use, verification pattern. Cannot be deferred — the ACD kursplan lists it as a kompetensmål ("Instruera AI-assistenter/agenter…") and ACD-1 grades it | M | P0 |
| Add an integration-testing exercise — `WebApplicationFactory` + in-memory or test-container DB; tests the controller→service→repository slice. Likely the only test-craft item that belongs here (xUnit + Moq + AAA are upstream); confirm against prior-course syllabus before authoring | M | P1 |
| ~~Add an EF Core *migrations workflow* exercise~~ | — | **Retracted (v3)** — taught upstream with SQLite |
| ~~Add a unit-testing exercise (xUnit + Moq for service layer)~~ | — | **Retracted (v3)** — xUnit basics taught in prior TDD course |
| ~~Add an App Service deployment progression~~ | — | **Retracted (v3)** — App Service is an off-ramp from the maturity ladder, not a step on it (see §4.6) |
| ~~Add a Razor Pages chapter / exercise~~ | — | **Retracted (v3)** — pattern-within-.NET; not on the transferable-concepts ladder |
| Reintroduce **retry / transient-fault patterns** (Polly, `EnableRetryOnFailure`, idempotent retries) — was deliberately deferred this year; natural home is ACD W7 "Resilience patterns" alongside health probes, feature flags, MI | M | P2 (next year) |
| Add an Azure Functions stretch exercise — kept as P2, but lower confidence after v3 framing: Functions are explicitly downstream (next course). Defer unless a clear in-scope use case appears | M | P3 |
| Add a Background Services / IHostedService chapter or example | S | P2 |

### Theme G — Presentations gap-fill

| Idea | Size | Priority |
| --- | --- | --- |
| Author Part I decks (4–6 decks bilingual) — close the cold-start gap | L | P1 |
| Add a `presentations/index.html` discovery page | M | P1 |
| Backfill ACD weeks 1–8 with video links (or remove the studieguide reference to videos) | M | P2 |

### Theme H — Assignments and assessment

| Idea | Size | Priority |
| --- | --- | --- |
| Surface all four assignment specs on the public site at `content/assignments/` and link from the relevant week pages (BCD weeks 3, 6, 9; ACD weeks 4, 7) | M | P0 |
| Convert BCD-3 from quiz format into a 5-criterion U/G/VG rubric matching the other BCD assignments | M | P1 |
| Rebalance BCD-2 VG criteria — make Bicep aspirational rather than required; G should be "repeatable, documented provisioning with any tool" | S | P1 |
| Add a deliverable spec + exam rubric to BCD week 10 and ACD week 8 wrap-ups | M | P0 |

### Theme J — Pedagogy infrastructure (added post-review, revised v3)

The original analysis treated content depth and exercise design but under-weighted the *pedagogical scaffolding* a beginner needs. v3 retracted two items that conflicted with the prior-course context.

| Idea | Size | Priority |
| --- | --- | --- |
| ~~Add a "C# warm-up" page (interfaces, async/await, LINQ, AAA)~~ | — | **Retracted (v3)** — superseded by §3.5 prior-knowledge framing; what would actually help is the *"What you'll meet for the first time"* page (in Theme B), not a C# refresher |
| Add a "Testing Strategy in Three Tiers" chapter (unit / integration / end-to-end) under Part III — explains the test pyramid before the exercises ask students to add tests | S | P1 |
| Add a "Choosing your web framework" decision-tree page (MVC / Web API / Minimal APIs / where Razor Pages fits) — turns single-pattern muscle memory into informed choice | S | P2 |
| Add worked rubric examples in Part VIII — show the same deployment at G-level and at VG-level so students see what "professional-grade" means *before* they're graded | M | P1 |
| Add a per-chapter "Check your understanding" block (3–5 retrieval-practice questions, ungraded) — beats mere exposure for novice learners | M | P2 |
| ~~Resolve the EF Core ↔ MongoDB driver split in Exercise 10~~ | — | **Retracted (v3)** — there is no split to resolve. EF Core is upstream; MongoDB is the deliberate NoSQL counterpoint chosen in CLO25 (see §3.5 and §4.6) |

### Theme I — Cross-cutting quality

| Idea | Size | Priority |
| --- | --- | --- |
| Declare the language policy (English content + bilingual slides), document in CLAUDE.md, build `docs/glossary-en-swe.md` for technical terms | S | P1 |
| Lint pass: every content file has exactly one H1; H1 matches frontmatter `title` | M | P1 |
| Add alt text to every image; add a CI check that fails the build on any `![](` (empty alt) | M | P1 |
| Add language tags to every code block; add a CI check | S | P2 |
| Add the `editURL` parameter in `hugo.toml` (currently commented) so logged-in students can suggest edits via GitHub | S | P2 |
| Add per-week workload estimate (hours of theory / exercise / self-study) | M | P2 |
| Add per-week Mon/Tue/Wed/Thu/Fri shape (matching the studieguide) on each week page | M | P2 |

---

## 6. Incremental roadmap

The roadmap is anchored to the actual academic calendar.

- **Today is 2026-05-07.** ACD25 runs v.15–v.22 (Apr 6 – May 29, 2026). Current ACD week ≈ 5.
- **BCD25 ended in v.14** (Apr 5, 2026).
- **Next BCD cohort starts in v.5** (late January 2027) — about 8.5 months away.
- **Next ACD cohort starts in v.15** (early April 2027) — about 11 months away.

### Phase 0 — During the current ACD cohort (now → v.22, ~3 weeks)

Goal: *help current students; nothing structural that risks breakage.*

- Surface `docs/assignments/assignment-acd-1/` on the site at `content/assignments/acd/1/` and link from week-1 ACD page (P0, S).
- Create `getting-started/prerequisites.md` and `getting-started/support.md` — even though the cohort has already started, link from week-by-week so they remain a reference (P0, S).
- Add a deliverable + exam rubric to ACD week 8 wrap-up before the cohort reaches it (P0, M).
- **Ship a minimal IaC chapter stub** under `course-book/2-infrastructure/iac/` (≈800 lines: Bicep walkthrough with a multi-resource example) before ACD week 7. ACD-1 explicitly grades IaC; the current 130-line chapter doesn't carry it. This was rated for Phase 1 but it's a current-cohort dependency (P0, M).
- **Teach AI-assisted development** — small chapter or page (3 patterns: good use, bad use, verification). ACD-1 grades it; we cannot defer (P0, S).
- Replace all 47 bare `{{< children />}}` with `description="true"` and backfill `_index.md` descriptions; this is the cheapest visible improvement (P1, M).
- Rewrite `content/_index.md` homepage *surgically* — keep menu paths, improve copy and section descriptions only. Aggressive rewrites mid-cohort will confuse students (P1, S).

**Total Phase 0 effort:** ~5 days of focused work. Shippable while the cohort is running.

**Phase 0 risk note.** Enabling `description="true"` on 47 children calls requires every child `_index.md` to have a `description` field. Verify with `hugo serve` before merge — partial backfill renders blank tiles on the homepage.

**v3 amendment to Phase 0 (2026-05-09).** Add three more low-cost, high-leverage onboarding pages while we're already touching the homepage:
- `getting-started/prior-knowledge.md` — what students bring from prior courses (S, P1).
- `getting-started/why-this-shape.md` — the four-course arc, the network-novelty thesis, the maturity ladder, the transferability promise (S, P1).
- A "Out of scope (taught in next course)" line on the homepage and on `getting-started/` — App Service, AKS, SWA, Functions (S, P1).

These three pages collectively *name the pedagogy* for the first time. The cost is one screen of prose each; the benefit is a coherent course identity that students, future analysts (human or AI), and the author all share.

### Phase 1 — Post-ACD25 cooling period (v.22 → v.35, June–August 2026)

Goal: *fix the structural and content-depth issues while there's no live cohort.*

This is the biggest window of the year and where the bulk of the work lands.

**1a — Course Book depth (priority order):**
1. Add a new IaC chapter under Part II — closes the explicit ACD-1 outcome gap (L, P0).
2. Expand Part IX (alerts-and-SLOs, Application Insights, KQL) (L, P1).
3. Rewrite Part I stubs and fix the numbering bug (M, P1).
4. Expand Part VIII deployment-strategies + Container Apps health-checks; expand Part VII container-registries (M, P1).

**1b — Tech-coverage gap-fill (substantially reduced in v3):**
5. Author the integration-testing exercise (`WebApplicationFactory` + in-memory or test-container DB) — confirm against prior-course syllabus first (M, P1).
6. ~~Author the EF Core / unit-testing / Razor Pages / App Service items~~ — **all retracted in v3** (upstream or out-of-scope; see §4.6 and §5 Theme F).

**1c — Exercises hygiene:**
8. Split the 1281-line and 1596-line monster exercises (M, P1).
9. Renumber and rebridge chapter 3 (Deployment) (M, P1).
10. Add IaC-coverage table to `exercises/_index.md` (S, P1).

**1d — Presentations & onboarding:**
11. Author Part I presentation deck set (4–6 bilingual decks) — closes the only Part with no decks (L, P1).
12. Author the four remaining Theme C tutorials (.NET install, Azure CLI, Git workflow, Docker basics) — *App Service deploy tutorial dropped in v3* (M, P1).
13. Author the v3 onboarding pages: `prior-knowledge.md`, `why-this-shape.md`, the "out of scope" homepage line. These name the pedagogy and prevent the audit-mistake of treating upstream/downstream content as missing (S each, P1).

**1e — Quality gates:**
13. Add CI checks: H1 hierarchy, alt-text presence, language tags, dead `{{< ref >}}` (M, P1).
14. Pass through and standardise callout / admonition syntax (M, P2).
15. Surface all assignments on the site; convert BCD-3 to rubric format; rebalance BCD-2 (M, P0).
16. Declare language policy + build EN/SV glossary (S, P1).

**Phase 1 total effort.** The original estimate was "~6–8 weeks of focused work". The feasibility review pushed back hard on this: Part I's bilingual deck set is 50–100% under-budgeted (a single Reveal.js deck at the existing visual-polish level is closer to 8h than 4h, and there are 10–12 of them), the two monster-exercise splits are ≈65% under-budgeted (each is effectively 5 sub-exercises and breaks all incoming `{{< ref >}}` links), and the CI lint pass is 30% under. **The realistic Phase 1 budget is 10–13 weeks at ~20 hours/week** — which still fits the v.22→v.35 (June–August) window with 12% slippage margin, but only if the work is sequenced and batched intelligently.

**v3 reduction.** Phase 1 shrinks meaningfully after the retractions in §5 Theme F (EF Core migrations, xUnit, Razor Pages, App Service items all retracted) and in §5 Theme J (C# warm-up, EF↔MongoDB disconnect retracted). The realistic budget after v3 is closer to **8–10 weeks at ~20 hours/week**, with the saved time available for the new Phase 0 onboarding pages and any reintroduction work in Phase 3.

The sequencing guidance still applies:

1. Front-load week 1 on `description="true"` rollout + description backfill (a single batch task on all 242 `_index.md` files; saves task-switching).
2. Batch all theory-depth rewrites (Parts I, VII, VIII, IX, IaC chapter expansion) in weeks 2–3 — same voice, same examples, catches cross-Part redundancy in one pass.
3. Batch all CI lint checks (H1, alt text, language tags, prose `{{< ref >}}` patterns) into a single 200-line script, not four tools.
4. Audit week-page references *before* renumbering exercise chapter 3 — most week pages reference exercises by slug or by prose, both of which will break under naive renumbering.

A revised week-by-week sequence that hits this budget is in the validation log (§8) under "Feasibility review summary".

### Phase 2 — BCD26 ramp-up (Sep–Dec 2026)

Goal: *polish, validate, and add the medium-priority content.*

- Author bidirectional Part↔Exercise back-links across the book (M, P1).
- Add chapter-level "Key takeaways" sections (M, P2).
- Add per-week workload estimate to all 18 weeks (M, P2).
- Add Mon–Fri shape to each week page (M, P2).
- Author the "AI-assisted development" small chapter (or remove the ACD-1 requirement) (M, P1).
- Backfill ACD videos (M, P2).
- Run a real cold-start UX test: pick a person who's never seen the site, screen-record them attempting to start week 1. Fix the worst three friction points uncovered.

### Phase 3 — Continuous improvement during BCD26 / before ACD26 (Jan–Apr 2027)

Goal: *enrichment, not catch-up.*

- **Reintroduce retry / transient-fault patterns** (Polly, `EnableRetryOnFailure`) into ACD W7 alongside health probes, feature flags, managed identity. This was the topic deliberately shaved this year — the candidate slot is held (see §9) (M, P2).
- Author Background Services / IHostedService chapter (S, P2).
- Author the additional observability exercises (alert + KQL exercise, secrets-rotation exercise) (L, P2).
- Add Theme A glossary cross-linking once content is stable (M, P2).
- Add a `static/presentations/` index page (M, P1 if not already done).
- Build a per-Part diagrams set under `static/diagrams/` and reference from chapters (L, P2).
- Audit the Course Book architecture-flavoured chapters (Parts I, III, IV, VI) against the prior course's "very basic architecture" content for overlap; trim or rebadge as needed (S, P2).

---

## 7. Open questions

These need a human decision before the roadmap can be fully committed. v3 resolved Q1, Q2 and Q3 from the original list.

1. ~~**Razor Pages: in or out?**~~ **Resolved (v3): out.** Pattern-within-.NET, not a transferable concept. A short paragraph in the framework-decision-tree page (Theme J) is sufficient.
2. ~~**App Service vs Container Apps weighting?**~~ **Resolved (v3): App Service out.** Pedagogically thin; off-ramp from the maturity ladder; teaches Azure-form-literacy rather than transferable platform thinking. Container Apps is the right primary target precisely because its concepts (revisions, traffic, probes, MI-pulled images) survive into AKS in the next course.
3. ~~**AI-assisted development as a taught topic?**~~ **Resolved (post-review): teach explicitly.** Kompetensmål in the kursplan; cannot be dropped.
4. **Exercise numbering scheme.** Renumber to sequential (1–9), or document the gap-leaving scheme as intentional (capacity for future inserts). The current state is ambiguous to students.
5. **BCD-2 calibration.** Bicep + bastion + GHA in weeks 4–6 — keep, or relax to "any IaC tool" at G level?
6. **Language policy.** Pure English content + bilingual slides? Or mirror to full Swedish? The current implicit answer (English) is fine but should be declared.
7. **Tutorials section's role.** As-built it's an orphan. Reposition as the "how do I install / set up X" reference area, with clear cross-links from week-1 prerequisites and from the early exercises that need each tool.
8. **Legacy folders.** `2-infrastructure/compute/legacy/` and similar — archive into a separate non-rendered folder, delete, or mark `draft = true`?
9. **(New, v3) Confirm what the prior course actually covers.** Specifically: how deep does the prior TDD course go on xUnit + AAA? Does it cover Moq / NSubstitute? Does it cover async testing? The integration-testing item in §5 Theme F depends on this answer.
10. **(New, v3) Audit architecture overlap with the prior course.** The prior course teaches "very basic architecture" and the Course Book has substantial architecture content (Parts I, III, IV, VI). Worth a one-pass check by someone who knows both syllabi.
11. **(New, v3) Where to host the "deliberately deferred" register?** Inline in §9 of this doc, or as a standalone `docs/deliberately-deferred.md` that next year's planning can write into directly?

---

## 8. Validation log

This document was synthesised from eight independent agent audits. Before publication, the most striking quantitative claims were re-verified directly:

| Claim | Verification | Status |
| --- | --- | --- |
| 47 bare `{{< children />}}` | `grep` count: 47 | ✅ confirmed |
| 0 children with `description="true"` | `grep` count: 0 | ✅ confirmed |
| Part I has no presentation decks | Directory does not exist | ✅ confirmed |
| Part II has 26 decks | Confirmed; corrected the original audit's "missing" claim | 🟡 corrected |
| `1-mvc-uploads-and-pdf-validation.md` ~1300 lines | 1281 lines; path is under `1-uploads-and-deep-probes/` (audit had wrong path) | 🟡 corrected |
| Largest exercise file | 1596 lines: `3-deployment/4-implementing-azure-key-vault-with-mongodb-on-azure-vm.md` | ✅ confirmed |
| Razor Pages 0 mentions | grep returns 0 | ✅ confirmed |
| Moq absent | grep returns 0 files | ✅ confirmed |
| xUnit ~1 file | grep returns 1 file | ✅ confirmed |
| App Service "1 mention" | actual: 5; still drastically undercovered | 🟡 corrected (stronger spec, weaker number) |
| Azure Functions 5 files | confirmed | ✅ confirmed |
| Part 9 alerts-and-SLOs 97 lines | confirmed | ✅ confirmed |
| Part 9 Application Insights 125 lines | confirmed | ✅ confirmed |
| Part VII container-registries 95 lines | confirmed | ✅ confirmed |
| Part II IaC chapter ~100 lines | actual: 130 lines | 🟡 corrected (still thin) |
| Part I high-availability 31 lines | actual: 30 lines | ✅ confirmed |
| Cloud-native key-aspects numbering bug (1, 2, 3, 4, 3, 4, …) | sequence in file: 1, 2, 3, 4, 3, 4, 5, 6, 7, 9 | ✅ confirmed |

### Three-agent independent review (completed 2026-05-07)

Three independent reviewers — pedagogical, feasibility/PM, and .NET-technical — read the v1 of this document and grepped the source themselves. Their critiques have been merged into v2 above (the inline edits in §4.6, §5 Theme D, §5 Theme F, the new Theme J, §6 Phase 0, §6 Phase 1 effort, §7 Q2 and Q3). The headline corrections were:

| Reviewer | Headline correction | How it landed in v2 |
| --- | --- | --- |
| Pedagogical | App Service is the missing rung in a maturity ladder, not a parallel option | §4.6 framing note + §7 Q2 resolution + §5 Theme F item reframed as App Service progression |
| Pedagogical | AI-assisted development is a *kompetensmål*; cannot be dropped | §5 Theme F made it P1, *Not optional* + §7 Q3 resolution + Phase 0 entry |
| Pedagogical | Razor Pages is more about teaching the decision tree than building another exercise | §5 Theme F downgraded to chapter only (S, P2); §5 Theme J added a "Choosing your web framework" decision-tree page |
| Pedagogical | The course needs prior-knowledge activation (C# warm-up before week 3) and retrieval practice | New §5 Theme J |
| Pedagogical | Rubric calibration: students should see G/VG side-by-side *before* being graded | New §5 Theme J item |
| Pedagogical | Part IV repository pattern vs Exercise 10 MongoDB driver disconnect | New §5 Theme J item |
| Feasibility | Phase 1 was under-budgeted by ~35 hours (~40%) | §6 Phase 1 effort updated from "6–8 weeks" to "10–13 weeks" |
| Feasibility | Part I deck set was under-estimated 50–100% | Captured in §6 effort note |
| Feasibility | Monster-exercise splits and chapter-3 renumbering were 65% under-budgeted; renumbering has hidden ref-update dependencies | §6 effort note + §6 sequencing guidance |
| Feasibility | IaC chapter is needed *before* ACD week 7 (current cohort), not Phase 1 (post-cohort) | New Phase 0 item |
| Feasibility | Drop the per-week Mon–Fri shape (duplicates studieguide) | Implicit — moved to "consider dropping" rather than Phase 2 work; documented here |
| Feasibility | Batch all CI lints into one 200-line script | §6 Phase 1 sequencing note |
| .NET-technical | Add **integration testing with `WebApplicationFactory`** — currently 0 references | §5 Theme F new P0 item |
| .NET-technical | EF Core item should be specifically the *migrations workflow*, not generic "hands-on" | §5 Theme F item rewritten |
| .NET-technical | Alerts-and-SLOs chapter is dense-but-complete; gap is the *exercise*, not chapter expansion | §5 Theme D item rewritten |
| .NET-technical | KQL "joins" belongs in a stretch exercise, not in the main chapter | §5 Theme D item rewritten |
| .NET-technical | Deployment-strategies needs ~10 lines of canary YAML, not 50 lines of expansion | §5 Theme D item rewritten |
| .NET-technical | Add a "Testing Strategy in Three Tiers" chapter | New §5 Theme J item |

**Open dissents (not resolved by v2):**

- The pedagogical reviewer's strongest claim — that EF Core and xUnit content should ship *before May 29* "for the current cohort" — partially conflates BCD25 (already graduated v.14, April 5) with ACD25 (running now). EF Core / xUnit content is genuinely P0 for BCD26, not for the current cohort. v2 keeps these in Phase 1, not Phase 0. **v3 update: both items retracted entirely — they are upstream prior-course content (see §3.5 and §4.6).**
- The pedagogical reviewer wants Razor Pages elevated (taught early, decision-tree); the .NET-technical reviewer wants it downgraded (niche in 2026). v2 splits the difference: a short *chapter* on framework choice (Theme J) but no large exercise (Theme F downgraded to P2 chapter-only). **v3 update: Razor Pages exercise retracted entirely; only a paragraph in the framework-decision-tree page remains.**
- The feasibility reviewer recommended dropping the BCD-2 Bicep G-criterion to VG. The pedagogical reviewer pushed back: the kursplan's G/VG distinction does not specify Bicep, and dropping it to VG dilutes the maturity ladder taught in weeks 4–6. v2 follows the pedagogical reviewer: keep Bicep at G but accept "any IaC tool" (Bicep, Terraform, Pulumi).

### v3 elaboration changelog (2026-05-09)

After the three-agent review, three further elaboration turns with the course author surfaced context the original audit didn't have. The amendments below are the difference between v2 and v3.

| Turn | Insight | What changed in the document |
| --- | --- | --- |
| 1 — App Service / Container Apps | App Service is pedagogically thin ("upload zip; it runs") and is an *off-ramp* from the maturity ladder, not a step on it. The deliberate ladder is VM → Containers → Container Apps → next course. | Retracted §5 Theme F App Service progression item; rewrote §4.6 framing as scope statement; resolved §7 Q2; downgraded Razor Pages to a paragraph in the framework-decision-tree page; added "out of scope (next course)" homepage line as a Theme B item |
| 2 — Prior-course context + MongoDB | Students arrive having taken C#, OOP, TDD, basic architecture, and EF Core + SQLite — all local. MongoDB is the deliberate NoSQL novelty added on top. Skipping migrations keeps infrastructure rollout clean and avoids the Azure-SQL-serverless cold-start retry-pattern trap. | Added new §3.5 "Prior-course context"; retracted §5 Theme F EF Core migrations and xUnit items as upstream; retracted §5 Theme J "C# warm-up" and "EF↔MongoDB disconnect" items; rewrote §4.6 from gap analysis to scope statement; added new Theme B item for `prior-knowledge.md` page |
| 3 — Network-novelty thesis + four-course arc | The course's central pedagogical thesis is *first encounters with the network*. CLO25 sits in a four-course arc: local dev → traditional-IT-in-cloud (BCD) → cloud-native (ACD) → scale/serverless (next). Retry patterns were deliberately deferred this year. | Added new §3.6 "Network-novelty thesis" with the per-week boundary mapping; added Theme A item for per-week network-boundary callout; added two new Theme B onboarding pages (`why-this-shape.md`, "out of scope" line); added new §9 "Deliberately deferred" register; added Q9, Q10, Q11 in §7 |

**Net effect of v3:** the roadmap shrinks ~25% (six retractions, ten new framing items), the document moves from "what's missing" to "what's deliberately bounded", and the previously-implicit pedagogical thesis is named for the first time. Phase 1 budget drops to 8–10 weeks; Phase 0 grows by three small onboarding pages.

---

## 9. Deliberately deferred (added v3, 2026-05-09)

A register of topics that were considered for CLO25 and were deliberately not included this year. Maintained so that:

1. The course author does not have to remember the candidate list at next planning round.
2. Future analysts (human or AI) do not mistake "intentionally absent" for "accidentally missing" — the original audit made exactly that error before this register existed.
3. Students who ask *"why isn't X here?"* can be pointed at the explicit reasoning rather than receiving an ad-hoc answer.

| Topic | Why considered | Why deferred this year | Re-introduction candidate slot |
| --- | --- | --- | --- |
| **Retry / transient-fault patterns** (Polly, `EnableRetryOnFailure`, idempotent retries) | Real production necessity; was on the schedule in prior years | Had to shave something this year to keep the schedule honest; this was the one that stood back. Not a permanent removal | ACD W7 ("Resilience patterns" — already has health probes, feature flags, MI; retry patterns are the natural fourth) |
| Razor Pages | Common in Swedish brownfield .NET shops | Not on the transferable-concepts ladder; pattern-within-.NET only; the framework-decision-tree page in §5 Theme J names it without teaching it | A short paragraph in the decision-tree page is the maximum scope envisioned |
| Azure App Service | Common in Swedish junior-developer first jobs | Pedagogically thin ("upload zip; it runs"); off-ramp from the maturity ladder; teaches Azure-form-literacy rather than transferable platform thinking | None envisioned for CLO25 |
| Static Web Apps, Azure Functions, AKS | Standard cloud-native targets | Belong on the post-CLO25 cloud-native / scalability arc | The next course |
| Azure DevOps (as alternative to GitHub Actions) | Some Swedish shops use it | Adds a parallel toolchain without adding a transferable concept beyond what GHA already teaches | None envisioned |
| SignalR / real-time | Real-world feature for many apps | Not on the network-novelty through-line; would land as an island | If it earns a slot, ACD or next course |
| gRPC | Increasingly common for microservices | Same as SignalR; isolated from the through-line | If it earns a slot, next course |
| Background services / IHostedService | Real production necessity | Off the through-line; small enough to add as P2 if a slot opens | Phase 3 enrichment if room |
| Front Door / API Management | Production traffic patterns | Beyond beginner scope | Next course |

**How to use this register.** Add an entry whenever a topic is considered and dropped, with the four columns filled in honestly. The "re-introduction candidate slot" column is the single most useful field — it turns "we don't teach X" into "we don't teach X *yet*; here's the slot we'd put it in if it earns one".

**An open question (also in §7 Q11):** keep this register inline here, or split it into a standalone `docs/deliberately-deferred.md`. Standalone is easier to write into during planning; inline keeps the rationale next to the analysis that established it. No strong default either way.
