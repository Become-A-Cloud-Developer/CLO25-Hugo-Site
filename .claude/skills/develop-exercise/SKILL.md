---
name: develop-exercise
description: Drive an end-to-end exercise-chapter development workflow for the CLO25 Hugo course site. Use when the user wants to build a new chapter (typically 2–4 related exercises) and have it validated end-to-end against real cloud resources. Conversationally aligns on chapter scope, then executes parallel authoring, cross-review, live provisioning, automated validation, and dual reports. Delegates markdown authoring to the create-exercise skill.
allowed-tools: Read, Write, Edit, Glob, Grep, Bash, Agent, AskUserQuestion, EnterPlanMode, ExitPlanMode, TaskCreate, TaskUpdate, TaskList
metadata:
  version: "1.0.0"
  last_updated: "2026-04-28"
---

# Develop Exercise Skill

Orchestrates the full lifecycle of a new chapter of exercises in this Hugo course site: conversational alignment with the user, parallel authoring of the markdown, cross-review for narrative continuity, live execution against real cloud and external services, automated validation of the deployed artifact, and dual reports (chat summary + durable validation record in the reference project). Markdown authoring rules are not duplicated here — Phase 1 sub-agents read the existing `create-exercise` skill's `TEMPLATE.md`, `GUIDE.md`, and `EXAMPLE.md` directly.

## When to use

- The user is starting a new **chapter** (typically 2–4 related exercises) for an upcoming course week.
- The chapter has a deployable artifact you want to validate end-to-end (a web app, API, CLI tool) — *or* the chapter is docs-only and Phases 3–4 will be skipped cleanly.
- For a single isolated exercise, prefer `create-exercise` directly.

## How this skill operates

You run in **two internal phases**.

- **Phase A — Alignment** (this turn or several turns): converse with the user, capture the chapter scope, ask only the discrete questions still ambiguous, then enter plan mode and present a concrete chapter plan via `ExitPlanMode`.
- **Phase B — Execute** (only after the plan is approved): follow `PHASES.md` as a runbook. Use `TaskCreate` to track each deliverable. Confirm before commit/push.

## Phase A — Alignment

### Step A1: read the user's freeform description

The user has typed `/develop-exercise` and likely included a description. Read what they wrote. Identify which of these are **specified** vs. **ambiguous**:

| Decision | Default if unspecified |
|----------|-----------------------|
| Chapter title and where it belongs in `content/exercises/` | Ask |
| Course week (study guide reference) | Ask if not derivable from current date + memory |
| Number of exercises | Ask |
| Technology stack (.NET / Node / Python / etc.) | Often clear from context |
| Deployment target (Azure / AWS / local Docker / none) | Ask |
| External accounts needed (Docker Hub / npm / GitHub repo) | Ask if deployment target is non-none |
| Validation method (Playwright / curl smoke / CLI assertion / none) | Inferable from deployment target |
| Reference project (extend existing / create new sibling / none) | Ask |
| Cleanup scope at end of final exercise (resource group / tenant identities / none) | Default: include in final exercise if any cloud was provisioned |

### Step A2: ask only what's still ambiguous

Use `AskUserQuestion`. **Maximum 4 question rounds** (one round can include up to 4 questions). After that, the plan becomes the alignment vehicle — the user can reject the plan and request more discussion.

Question packs that work well:

- **Scope + count**: "How many exercises in this chapter?" + "Where does this chapter live in `content/exercises/`?"
- **Deployment**: "Which cloud (or none)?" + "What external accounts are needed?"
- **Reference project**: "New sibling reference project, extend an existing one, or none?"
- **Validation**: "How do we automatically verify the deployed artifact works?"

Use prose conversation (no `AskUserQuestion`) when the topic is open-ended like "what should this chapter actually teach?" — that needs back-and-forth, not a multiple-choice button.

### Step A3: honour `"go"` and `"just go"`

If the user types `"go"`, `"just go"`, `"proceed"`, or similar, **stop asking questions immediately** and enter plan mode with reasonable assumptions. Note the assumptions in the plan file so the user can override them at the plan-approval gate.

### Step A4: enter plan mode and write the chapter plan

Call `EnterPlanMode`. Write the chapter plan to the plan file the system points you at. The plan must include:

- **Context**: course week, why this chapter, what it teaches.
- **Outcome**: list of concrete deliverables (exercise files, `_index.md`, reference project, live deployment URL, validation report).
- **Critical files**: paths of new and modified files in the Hugo site and the reference project.
- **Naming and resource conventions**: project name, GitHub repo, cloud resource names, image names, identity names.
- **Phased execution overview**: what each of Phases 1–5 will do for *this specific chapter*.
- **Risks and open considerations**: permissions, name collisions, time budget.
- **Verification checklist**: build cleanly, files present, deployment reachable, screenshots captured.

End by calling `ExitPlanMode`. The user approves (or rejects and pushes back).

## Phase B — Execute

Once the plan is approved:

1. **Read `PHASES.md`** for the detailed runbook of Phases 1–5.
2. **Use `TaskCreate`** to populate one task per file/deliverable + per phase gate. Mark `in_progress`/`completed` as you go.
3. **Run Phase 1 in parallel**, **Phase 2 review as a single agent**, **Phases 3–5 sequentially** as the leader (subagents are too isolated for stateful cloud operations).
4. **Skip Phases 3 and 4 cleanly** if the chapter has no deployment target.
5. **Always ask before commit and before push** — per project `CLAUDE.md`.

## Companion files

| File | Purpose |
|------|---------|
| `PHASES.md` | The five-phase runbook with concrete agent prompts and gate criteria |
| `REFERENCE-PROJECT.md` | Lightweight reference-project pattern (README, CLAUDE.md, validation report templates) |
| `REVIEW-CHECKLIST.md` | Cross-review focus areas for Phase 2 |

## Operating rules

- Treat the chapter as **incomplete until Phase 4 validation passes**. A chapter without an executed live run is a draft.
- **Document every deviation** between the published exercise text and what live execution actually required, in the reference project's `docs/EXERCISE-VALIDATION-REPORT.md`. Then fold the fix back into the exercise text.
- **Never write secrets to disk**. Pipe Docker Hub PATs and Azure credential JSON via stdin to `gh secret set --body -` or `gh secret set < file && rm file`.
- **Redact secrets in the chat report** at the end. Show shape (`dckr_pat_…`) not value.

## Changelog

- **v1.0** (2026-04-28) — Initial release. Captures the five-phase pattern (parallel authoring → cross-review → live execution → validation → dual reports) developed during the CI/CD chapter build. Companion files: `PHASES.md`, `REFERENCE-PROJECT.md`, `REVIEW-CHECKLIST.md`. Delegates markdown authoring to the existing `create-exercise` skill rather than duplicating it.
