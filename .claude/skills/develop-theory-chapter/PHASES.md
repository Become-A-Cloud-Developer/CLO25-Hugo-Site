# Execution Phases

Detailed runbook for Phase B of the `develop-theory-chapter` skill. Read this in full before starting Phase B0.

The orchestrator runs eight sequential sub-phases. Per-Part wall-clock target: ~2 hours, mostly unattended after B0.

| Phase | Owner | Parallel? | Output |
|-------|-------|-----------|--------|
| B0 Mining | Leader | No | Mining notes |
| B1 Glossary | Leader | No | Part glossary |
| B2 Authoring | Workers | Yes | Triplet markdown per chapter |
| B3 Slide render | Leader | No | Rendered HTML decks |
| B4 Review | One review agent | No | Punch list per chapter |
| B5 Fix-up | Leader | No | Edits applied |
| B6 Voice rewrite | One voice agent | No | Voice-aligned prose |
| B7 Validation | Leader | No | All gates green; tracker updated |

## Phase B0 — Mining

**Goal:** ground the Part in the existing exercise(s), the studieguide reflection questions, and the existing theory style.

**Mechanics:** leader-driven, no subagents.

1. **Read the studieguide week(s)** the Part supports. Both `docs/course-plan/studieguide-clo25-grundlaggande-molnapplikationer.md` and `docs/course-plan/studieguide-clo25-molnapplikationer-fordjupning.md`. Extract the reflection questions for the relevant week(s) — these become natural section anchors per chapter.
2. **Read the companion exercise chapter `_index.md` and every sub-exercise file**. List the concrete code patterns, CLI commands, file names, and library APIs the exercise uses. These become the chapter's worked-example material.
3. **Read 2–3 sibling pages from Parts I or II** for tonal calibration. Use `content/course-book/2-infrastructure/network/2-ip-addresses-and-cidr-ranges/ip-addresses-and-cidr-ranges.md` as the gold-standard tone reference.
4. **Write working notes** to `docs/coursebook-mining/<part>-mining-notes.md`. Format:

   ```markdown
   # Part <N> — <Title> — Mining Notes

   ## Studieguide alignment
   - Course week: <week N (v.<X>)>
   - Reflection questions:
     - <Q1>
     - <Q2>

   ## Companion exercise(s)
   - Path: content/exercises/<...>
   - Concepts the exercise teaches: <list>
   - Code patterns mentioned: <list>
   - File names referenced: <list>

   ## Per-chapter brief
   ### Chapter 1 — <Working Title>
   - Owns terms: <list>
   - Borrows terms: <list>
   - Reflection questions to answer: <list from studieguide>
   - Worked example to mine from exercise: <one or two specific code/CLI patterns>
   - Slide-pair: yes/no
   - Course tag: BCD / ACD / both
   - Cross-link target: <exercise path>
   ### Chapter 2 — <Working Title>
   ... (one section per chapter)
   ```

**Gate:** mining notes file exists and contains a per-chapter brief for every chapter. Every chapter brief names at least one cross-link target under `/exercises/`.

## Phase B1 — Part glossary

**Goal:** prevent terminology drift across parallel workers by deciding upfront which chapter "owns" each term.

**Mechanics:** leader-driven. Read `GLOSSARY-PROTOCOL.md` before starting.

1. **Enumerate terms** by scanning the mining notes' "Owns terms" and "Borrows terms" lists, plus a grep for bolded terms in the companion exercise files (`grep -hE '\*\*[A-Z][^*]+\*\*' <exercise files>`).
2. **Decide owner per term**. A term is owned by the first chapter where it appears in the reading order. Borrowed terms are linked, not redefined.
3. **Write canonical definitions** — 1 to 2 sentences each, in the third-person voice mandated by `student-technical-writer`.
4. **Write to `docs/coursebook-mining/<part>-glossary.md`**. Format:

   ```markdown
   # Part <N> — Glossary

   ## Terms owned by this Part

   ### <Term>
   - **Owner chapter**: <slug>
   - **Canonical definition**: <1–2 sentences>
   - **Used by chapters**: <list>

   ## Terms borrowed from earlier Parts

   ### <Term>
   - **Defined in**: <Part / chapter slug>
   - **Reference link**: <relative URL to the defining chapter>
   ```

**Gate:** glossary file exists. Every term in the mining notes has either an owner chapter (if owned) or a reference link (if borrowed). At least 90% of bolded terms in the companion exercise files appear in the glossary (sanity check; not strict).

## Phase B2 — Parallel chapter authoring

**Goal:** produce one chapter triplet per chapter — `<slug>.md` (textbook prose), `<slug>-slides.md` (English slide markdown), `<slug>-slides-swe.md` (Swedish slide markdown) — in parallel.

**Mechanics:** dispatch one Agent per chapter in **a single message** containing multiple `Agent` tool calls. Workers never read each other's output.

**Standard worker agent prompt** (one per Agent call):

> You are authoring chapter `<slug>` of Part <N> — `<Part Title>` of the CLO25 Course Book at `/Users/lasse/Developer/CLO_Development/CLO25-Hugo-Site`.
>
> Read these files in order before writing:
> 1. `.claude/skills/student-technical-writer/SKILL.md` — the voice rules you MUST follow
> 2. `.claude/skills/develop-theory-chapter/CHAPTER-TEMPLATE.md` — the structural template
> 3. `.claude/skills/develop-theory-chapter/SLIDES-TEMPLATE.md` — the slide markdown template
> 4. `.claude/skills/revealjs-skill/SKILL.md` — the slide HTML conventions (you do NOT render the HTML; that happens in B3, but your slide markdown must match the structure the renderer expects)
> 5. `docs/coursebook-mining/<part>-mining-notes.md` — your chapter-specific brief is in the per-chapter section
> 6. `docs/coursebook-mining/<part>-glossary.md` — the terminology contract
> 7. `content/course-book/2-infrastructure/network/2-ip-addresses-and-cidr-ranges/ip-addresses-and-cidr-ranges.md` — the gold-standard tonal reference; emulate its rhythm
> 8. `<companion exercise paths from your brief>` — for worked-example material
>
> Glossary contract (load-bearing):
> - For every term in the glossary's "Terms owned by this Part" section that lists YOUR chapter as owner: introduce it in your chapter using the canonical definition string verbatim (you may reword by no more than 5 words). Bold the term on first use.
> - For every term in the glossary that is owned by a DIFFERENT chapter: do NOT redefine it. Link to its owning chapter using the relative URL pattern `[term](/course-book/<part>/<section>/<owner-slug>/)`.
> - For every term in the "Terms borrowed from earlier Parts" section: do NOT redefine. Link to the existing path.
>
> Output three files at exactly these paths:
> 1. `content/course-book/<part-N>-<part-slug>/<section>/<chapter-N>-<slug>/<slug>.md` — the textbook prose, 1500–3500 words, following CHAPTER-TEMPLATE.md's structure
> 2. `content/course-book/<part-N>-<part-slug>/<section>/<chapter-N>-<slug>/<slug>-slides.md` — English slide markdown, following SLIDES-TEMPLATE.md
> 3. `content/course-book/<part-N>-<part-slug>/<section>/<chapter-N>-<slug>/<slug>-slides-swe.md` — Swedish translation of the slide markdown, same structure
>
> Hard rules:
> - Frontmatter on `<slug>.md` MUST include `title`, `program = "CLO"`, `cohort = "25"`, `courses = [...]` (per your brief), `weight`, `draft = false`. Add `aliases = [...]` only if your brief specifies migration aliases.
> - The first two lines of body content MUST be the EN and SWE presentation links: `[Watch the presentation](/presentations/course-book/<part-N>-<part-slug>/<section>/<slug>.html)` followed by a blank line and `[Se presentationen på svenska](/presentations/course-book/<part-N>-<part-slug>/<section>/<slug>-swe.html)`.
> - The chapter MUST contain at least one link to its companion exercise under `/exercises/...`.
> - The chapter MUST end with a `## Summary` section recapping the chapter's load-bearing claims in 3–6 sentences.
> - You MUST follow `student-technical-writer/SKILL.md` voice rules. No first-person plural. No rhetorical questions. No analogies to non-technical domains. No temporal filler ("modern", "today's").
> - DO NOT edit any file outside your three target paths.
> - DO NOT spawn sub-agents.
>
> Verify before returning: all three files exist and the prose chapter is 1500–3500 words.

**Gate criteria for B2 completion:**

- All three files per chapter exist (verify with `Glob`).
- Each `.md` prose file is 1500–3500 words (verify with `wc -w`).
- Mark each B2 task `completed` as the corresponding agent returns.

## Phase B3 — Slide rendering

**Goal:** render every chapter's `*-slides.md` and `*-slides-swe.md` into standalone HTML decks under `static/presentations/course-book/<part-N>-<part-slug>/<section>/<slug>.html` and `<slug>-swe.html`.

**Mechanics:** leader-driven. Use the `revealjs-skill` for the HTML structure. Each rendered deck must use the Swedish tech CSS at `static/presentations/swedish-tech-slides.css` via a relative path that resolves correctly from the deck's location.

For each chapter:

1. Read the chapter's `<slug>-slides.md` and `<slug>-slides-swe.md`.
2. Map each `## Heading` and following bullets to the appropriate `revealjs-skill` slide type:
   - First slide → Hero slide using the chapter title
   - Each `## Heading` block of bullets → Bullet slide
   - Final slide → Closing slide ("Frågor?") for the SWE deck; ("Questions?") for the EN deck
3. Render to `static/presentations/course-book/<part-N>-<part-slug>/<section>/<slug>.html`. The relative path to `swedish-tech-slides.css` will be `../../../../swedish-tech-slides.css` (4 levels up: section → part → course-book → presentations root).
4. Repeat for the SWE variant.

**Gate criteria for B3 completion:**

- Both HTML files exist for every chapter at the expected path (verify with `Glob`).
- Each HTML file references `swedish-tech-slides.css` via a relative path that resolves to the actual file.
- Each HTML file's section count matches the slide source's `## Heading` count ±1.

## Phase B4 — Cross-review

**Goal:** catch glossary violations, voice slips, and structural errors before they multiply across the Part.

**Mechanics:** dispatch a **single** Agent (not multiple — the whole point is one mind reading all chapters in sequence). Pass it `REVIEW-CHECKLIST.md` as the focus list, plus the glossary file.

**Agent prompt template:**

> You are reviewing every chapter of Part <N> of the CLO25 Course Book.
>
> Read these files in order:
> 1. `.claude/skills/develop-theory-chapter/REVIEW-CHECKLIST.md` — the focus list
> 2. `.claude/skills/student-technical-writer/SKILL.md` — the voice rules
> 3. `docs/coursebook-mining/<part>-glossary.md` — the terminology contract
> 4. Each chapter triplet (`<slug>.md`, `<slug>-slides.md`, `<slug>-slides-swe.md`) under `content/course-book/<part>/<section>/`
>
> Return: Critical issues (must fix), Significant issues (should fix), Minor issues (nice to fix), Cross-cutting issues, Strengths to preserve. For each issue give file, approximate line, the offending text, and a one-sentence suggested fix. Cap at ~1000 words.
>
> Do NOT fix anything. Do NOT spawn sub-agents.

**Gate criteria for B4 completion:**

- Punch list returned for every chapter.
- Cross-cutting issues section is present (may be empty).

## Phase B5 — Fix-up

**Goal:** apply the review findings.

**Mechanics:** leader-driven. Apply Critical and Significant findings via `Edit`. Defer Minor findings to a single end-of-Part nit pass (or skip if low-value).

After fixes:

- Re-run `hugo --gc --minify` to confirm the build still passes.
- Update the tracker entry per chapter to reflect the review-pass count.

**Gate criteria for B5 completion:**

- All Critical and Significant findings resolved.
- Hugo builds clean.

## Phase B6 — Voice rewrite

**Goal:** unify paragraph rhythm, sentence length, and tone across the Part.

**Mechanics:** dispatch a **single** Agent that reads all chapter prose files together and emits prose-only rewrites.

**Agent prompt template:**

> You are the voice-consistency editor for Part <N> of the CLO25 Course Book. Your job is to make six chapters read as if one author wrote them, not six.
>
> Read these files in order:
> 1. `.claude/skills/student-technical-writer/SKILL.md` — the voice rules
> 2. `content/course-book/2-infrastructure/network/2-ip-addresses-and-cidr-ranges/ip-addresses-and-cidr-ranges.md` — the gold-standard rhythm
> 3. Every `<slug>.md` prose file under `content/course-book/<part>/<section>/` (do NOT read `*-slides.md` or `*-slides-swe.md` — leave those untouched)
>
> Edit only the prose. Do NOT edit:
> - Frontmatter (anything between the `+++` delimiters)
> - Code blocks (anything between fenced backticks)
> - Tables
> - The first two presentation-link lines after frontmatter
> - The summary section's load-bearing claims
>
> What to edit:
> - Paragraph rhythm: a chapter should not have a run of all-3-sentence paragraphs followed by all-7-sentence paragraphs. Smooth.
> - Voice slips that survived B5: residual first-person plural, residual rhetorical questions, lingering temporal filler.
> - Section openings: no two adjacent sections should open with the same construction (e.g. both starting with "The ...").
>
> Use `Edit` with concrete `old_string` / `new_string` pairs. Do NOT spawn sub-agents.

**Gate criteria for B6 completion:**

- Voice rewrite agent returned without error.
- `hugo --gc --minify` still builds clean.

## Phase B7 — Validation

**Goal:** verify every gate for every chapter, then run the cross-Part anchor check.

**Mechanics:** leader-driven. Run validation in two passes — per-chapter and per-Part.

### Per-chapter (run for every chapter in the Part)

> ℹ Migrated chapters in Parts I and II are exempt from gates 9 (glossary) and 10 (cross-link) because those rules were introduced after they were written. New chapters in Parts III–X must pass all 10 gates.

| # | Gate | Command / check |
|---|------|-----------------|
| 1 | Hugo build | `hugo --gc --minify` exits 0 |
| 2 | Markdown lint | `lint-md` skill on the chapter triplet |
| 3 | Internal link check | every `[text](/path/...)` in `<slug>.md` resolves to an existing Hugo page; every `{{< ref >}}` shortcode resolves at build |
| 4 | Word count | `wc -w` on `<slug>.md` body (excluding frontmatter): warn <1400 or >3600, hard fail <1200 or >4000 |
| 5 | Slide pair present | both `*-slides.md` and `*-slides-swe.md` exist; both rendered HTML files exist |
| 6 | Slide section parity | `grep -c '<section' <slug>.html` and `<slug>-swe.html` differ by at most 1 |
| 7 | Frontmatter complete | `title`, `program`, `cohort`, `courses`, `weight`, `draft = false` all present |
| 8 | Voice patterns | `tools/voice-check.sh <slug>.md` exits 0 |
| 9 | Glossary compliance | for each term where this chapter is owner, the canonical definition string appears within edit distance ≤5 words |
| 10 | Cross-link to exercise | `<slug>.md` contains at least one `/exercises/...` link |

A chapter is **done** only when all 10 gates pass with no Critical review-checklist findings outstanding.

### Per-Part (run once after all chapters pass per-chapter gates)

- `tools/anchor-check.sh` — walks the entire `content/course-book/` tree, extracts every `{{< ref >}}` target and every `#anchor` fragment in any chapter, verifies each target heading still exists. Exits 0 on success.
- `check-links` skill (existing project tool) on the local Hugo build — catches broken external links.
- Final `hugo --gc --minify` clean build.

### Tracker update

For every chapter, append or update its entry in `docs/coursebook-progress.md`:

```markdown
### <Chapter number>. <Title>
- Status: validated
- Files: content/course-book/<part>/<section>/<slug>/<slug>.md (+slides, +slides-swe)
- Last writer: agent-<id>
- Gates:
  - hugo-build: pass <YYYY-MM-DD HH:MM>
  - lint-md: pass <YYYY-MM-DD HH:MM>
  - link-check: pass <YYYY-MM-DD HH:MM>
  - word-count: <N> (pass)
  - slide-pair: pass
  - slide-parity: pass (en=<N>, swe=<N>)
  - frontmatter: pass
  - voice: pass
  - glossary: pass
  - cross-link: pass (links to <exercise path>)
- Review: <C> Critical, <S> Significant, <M> Minor (deferred)
```

If any gate fails: do NOT mark the chapter `validated`. Mark the chapter `blocked` and surface to the user at the end of the Part-run.

**Gate criteria for B7 completion:**

- Tracker shows `validated` status for every chapter in the Part.
- `tools/anchor-check.sh` exits 0.
- Final `hugo --gc --minify` exits 0.
- `check-links` reports no broken internal links.

## After B7

- **Ask the user before committing** if auto-commit was not approved at the start of the Part-run.
- **Ask the user before pushing**, even if auto-commit was approved.
- **Send a chat report** with: Part name, chapter count, total word count, total slide count, link to the tracker, list of any deferred Minor findings, list of any blocked chapters.
- **Mark the Part-run task `completed`** in the TaskList.
