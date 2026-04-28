---
name: develop-theory-chapter
description: Drive an end-to-end theory-chapter development workflow for the CLO25 Hugo course site. Use when the user wants to author a Part of the unified Course Book (typically 4–8 related theory chapters under content/course-book/) with EN/SWE slide pairs and validate it against the existing exercise chapters. Conversationally aligns on Part scope, then executes Part-level glossary, parallel chapter authoring, slide rendering, cross-review, voice-rewrite, and full validation. Delegates prose to the student-technical-writer skill and slide rendering to the revealjs-skill.
allowed-tools: Read, Write, Edit, Glob, Grep, Bash, Agent, AskUserQuestion, EnterPlanMode, ExitPlanMode, TaskCreate, TaskUpdate, TaskList
metadata:
  version: "1.0.0"
  last_updated: "2026-04-28"
---

# Develop Theory Chapter Skill

Orchestrates the full lifecycle of a Part of the Course Book — typically 4 to 8 textbook-style chapters — with the same shape as the existing `develop-exercise` skill. Phase A aligns on the Part's scope through conversation. Phase B runs an eight-step pipeline: mining → glossary → parallel authoring → slide rendering → cross-review → fix-up → voice rewrite → validation. Voice and structure rules are not duplicated here — Phase B2 workers read the existing `student-technical-writer` skill for prose voice and the `revealjs-skill` for slide rendering.

## When to use

- The user is starting a new **Part** of the Course Book under `content/course-book/<part>/` (typically 4–8 related theory chapters).
- The Part has companion exercise chapter(s) under `content/exercises/` that the theory will reference and align with.
- For a single isolated theory chapter, prefer invoking the worker prompt manually with `student-technical-writer`. This skill exists for whole Parts.

## How this skill operates

You run in **two internal phases**.

- **Phase A — Alignment** (this turn or several turns): converse with the user, capture Part scope, ask only the discrete questions still ambiguous, then enter plan mode and present a concrete Part plan via `ExitPlanMode`.
- **Phase B — Execute** (only after the plan is approved): follow `PHASES.md` as a runbook. Use `TaskCreate` to track each chapter and each gate. Confirm before commit/push.

Honor pre-aligned input prompts at `docs/develop-theory-chapter-prompts/part-N-<slug>.md`. If one exists and the user references it, skip Phase A entirely and jump to Phase B.

## Phase A — Alignment

### Step A1: read the user's freeform description

The user has typed `/develop-theory-chapter` and likely included a description. Identify which of these are **specified** vs. **ambiguous**:

| Decision | Default if unspecified |
|----------|-----------------------|
| Part number and title (e.g. "Part III — Application Development") | Ask |
| Chapter list (slugs + working titles) | Ask |
| Companion exercise chapter(s) the Part should align with | Ask if not derivable from the studieguide |
| Course tag per chapter (BCD, ACD, or both) | Default: derive from the companion exercise chapter's `courses` frontmatter |
| Whether every chapter ships with EN/SWE slide pair | Default: yes |
| Whether to commit per chapter as gates pass | Ask once at the start of Phase B |
| Whether to push at end of Part-run | Default: ask before pushing |

### Step A2: ask only what's still ambiguous

Use `AskUserQuestion`. **Maximum 4 question rounds** (one round can include up to 4 questions). After that, the plan becomes the alignment vehicle.

Question packs that work well:

- **Scope**: "Which Part are we authoring?" + "How many chapters?" + "Working titles?"
- **Alignment**: "Which exercise chapter(s) does this Part support?" + "Which course (BCD/ACD/both)?"
- **Commit cadence**: "Auto-commit per chapter as gates pass during this Part-run?"

Use prose conversation (no `AskUserQuestion`) when the topic is open-ended like "what should chapter 3 actually teach?" — that needs back-and-forth, not multiple choice.

### Step A3: honor `"go"` and `"just go"`

If the user types `"go"`, `"just go"`, `"proceed"`, or similar, **stop asking questions immediately** and enter plan mode with reasonable assumptions. Note the assumptions in the plan file so the user can override them at the plan-approval gate.

### Step A4: enter plan mode and write the Part plan

Call `EnterPlanMode`. Write the Part plan to the plan file the system points you at. The plan must include:

- **Context**: which Part, why this Part, which course week and exercise it supports.
- **Outcome**: list of chapter slugs + working titles + rough word-count target. Whether each chapter ships a slide pair.
- **Critical files**: paths under `content/course-book/<part>/<section>/<slug>/` for each chapter triplet, and `static/presentations/course-book/<part>/<section>/<slug>.html` and `-swe.html`.
- **Glossary candidates**: terms this Part introduces and terms this Part borrows from earlier Parts.
- **Phased execution overview**: B0–B7 for this specific Part.
- **Open considerations**: course-tag boundaries, cross-Part dependencies, any chapters that may need to be split or merged after B0 mining.
- **Verification checklist**: build cleanly, all 10 gates per chapter, anchor check passes Part-wide.

End by calling `ExitPlanMode`. The user approves (or rejects and pushes back).

## Phase B — Execute

Once the plan is approved:

1. **Read `PHASES.md`** for the detailed runbook of Phases B0–B7.
2. **Read `GLOSSARY-PROTOCOL.md`** before starting B1.
3. **Use `TaskCreate`** to populate one task per chapter + per phase gate. Mark `in_progress`/`completed` as you go.
4. **Run B2 in parallel** (one Agent per chapter), **B4 review and B6 voice rewrite as single agents**, **B0/B1/B3/B5/B7 sequentially as the leader**.
5. **Update `docs/coursebook-progress.md`** continuously — the tracker is the source of truth across runs.
6. **Always ask before commit and before push** — per project `CLAUDE.md`.

## Companion files

| File | Purpose |
|------|---------|
| `PHASES.md` | The eight-phase runbook with concrete agent prompts and gate criteria |
| `GLOSSARY-PROTOCOL.md` | How the Part glossary is produced and enforced |
| `REVIEW-CHECKLIST.md` | Cross-review focus areas for B4 |
| `STYLE-CHECKLIST.md` | Concrete grep patterns and word-count thresholds for `voice-check.sh` |
| `CHAPTER-TEMPLATE.md` | Frontmatter + skeleton for `<slug>.md` |
| `SLIDES-TEMPLATE.md` | Frontmatter + skeleton for `<slug>-slides.md` and `<slug>-slides-swe.md` |
| `tools/voice-check.sh` | Bash script enforcing forbidden voice patterns and word-count thresholds |
| `tools/anchor-check.sh` | Bash script verifying every internal heading anchor used across the Course Book still resolves |

## Operating rules

- A chapter is **incomplete until all 10 gates pass**. Never mark a chapter "validated" without a green gate row in the tracker.
- **Document every drift** between sibling chapters (terminology, voice rhythm) in the B6 voice rewrite, not as a manual fix scattered across chapters.
- **Never lose work on resume**. Read `docs/coursebook-progress.md` first; reconcile against the filesystem; only then start new work.
- **Glossary is leader-owned**. Workers receive the glossary verbatim; they never decide which term they "own."
- **Voice rewrite is prose-only**. The B6 agent does not edit slides, frontmatter, or code blocks.

## Tracker file

`docs/coursebook-progress.md` is the single source of truth across runs. The orchestrator reads it at the start of every Part-run and updates it after every gate. See `PHASES.md` for the format.

## Changelog

- **v1.0** (2026-04-28) — Initial release. Eight-phase pattern (mining → glossary → parallel authoring → slide rendering → cross-review → fix-up → voice rewrite → validation) for Course Book Part development. Companion files: `PHASES.md`, `GLOSSARY-PROTOCOL.md`, `REVIEW-CHECKLIST.md`, `STYLE-CHECKLIST.md`, `CHAPTER-TEMPLATE.md`, `SLIDES-TEMPLATE.md`, `tools/voice-check.sh`, `tools/anchor-check.sh`. Delegates prose to `student-technical-writer` and slide rendering to `revealjs-skill`.
