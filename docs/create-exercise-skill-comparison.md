# `create-exercise` skill vs. CLO25 chapter practice

A comparison of the `.claude/skills/create-exercise/` skill (its `SKILL.md`, `GUIDE.md`, `TEMPLATE.md`, and `EXAMPLE.md`) against the four exercises currently in `content/exercises/4-services-and-apis/1-rest-api-and-dtos/`. The goal is to identify whether the skill should be updated and, if so, where.

The four exercises sampled:

- `1-rest-controllers-and-dtos.md` (~900 lines, 12 steps)
- `2-consuming-the-api-and-cors.md` (~515 lines, 8 steps) — written without invoking the skill
- `3-api-key-middleware.md` (~600 lines, 11 steps)
- `4-jwt-bearer-and-cleanup.md` (~840 lines, 13 steps)

## TL;DR

The skill captures a useful **information architecture** (bold actions + iconed blockquotes for depth) and a useful **section structure** (Goal / Prerequisites / Steps / Common Issues / Summary / Going Deeper / Done). Both should stay.

The skill's **specific rules** are wrong for this codebase in three load-bearing ways:

1. It has no Hugo TOML frontmatter, so a file written by following it literally won't render.
2. It forbids cross-references, but the chapters here are deliberate sequences whose forward/back pointers carry pedagogical weight.
3. It targets "4–5 main steps + testing", but cloud exercises with provisioning, deployment, and verification routinely need 8–13.

The skill **omits** patterns that the four sampled exercises use heavily (forward-pointer paragraph in the closing section, named Concept Deep Dives, full-file code listings, teardown steps, cost-awareness, recap-style steps for repeated patterns, links to upstream docs in angle brackets).

There are also a handful of small **bugs in the skill files themselves** worth fixing in the same pass.

## What the skill gets right

These are conventions our exercises follow because they are genuinely useful:

| Skill rule | Used in practice? | Notes |
|---|---|---|
| Three-layer hierarchy: bold actions, blockquoted depth, optional sections | ✓ | The single most important convention. Lets a skimmer execute and a deep reader understand. |
| Icon system: ℹ Concept Deep Dive / ⚠ Common Mistakes / ✓ Quick check / ✓ Success indicators / ☐ Final checklist | ✓ | All four exercises use the full set. |
| Section order: Goal → Prerequisites → Exercise Steps → Common Issues → Summary → Going Deeper → Done | ✓ | Universal across the chapter. |
| File-path callouts as `> path/to/file` blockquote above the code block | ✓ | Universal. |
| Step intro paragraph **before** the numbered actions | ✓ | Universal. The intro paragraph is where the *why* lives. |
| Bold action verbs in steps (`**Create**`, `**Add**`, `**Open**`, `**Edit**`) | ✓ | Universal. |
| `## Exercise Steps` `### Overview` numbered list of step titles | ✓ | All four exercises start with this — it functions as a chapter map. |
| Final verification checklist with `☐` | ✓ | All four. |
| Continuous blockquotes (MD028 — no blank lines between blockquote sections) | ✓ | Universal. |
| Language identifier on every code block (MD040) | ✓ | Universal. |
| URLs wrapped in angle brackets — `<https://...>` | ✓ | Used for upstream docs (MDN, Microsoft Learn). |

## What the skill gets wrong

These are rules in the skill that the chapter actively violates because following them would degrade the work.

### 1. No Hugo TOML frontmatter

**Skill states:** `TEMPLATE.md` and `EXAMPLE.md` start with `# Title`, no frontmatter. `GUIDE.md` shows the same.

**Practice:** Every exercise in the chapter starts with:

```toml
+++
title = "..."
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "..."
weight = N
draft = false
+++
```

**Why this matters:** Hugo treats files without a `title` and `weight` as drafts at best, broken at worst. A file written by following the skill template literally wouldn't show up in `{{< children />}}` in the chapter index. The taxonomy fields (`program`, `cohort`, `courses`) are also used by site filters and the homepage. **This is the highest-impact omission.**

### 2. "No cross-references" is wrong for sequenced chapters

**Skill states (`GUIDE.md`):** "No cross-references — Each exercise stands alone." Repeated in `SKILL.md` Quality Checklist: "No references to other exercises".

**Practice:** Every one of the four exercises references at least one of:

- "the previous exercise" / "the deployment chapter" / "the logging-and-monitoring chapter"
- "the next exercise closes that gap by..." (forward pointer in Done!)
- "Same pattern as the logging and monitoring chapter — only the names change. If any sub-step is fuzzy, revisit that chapter for the full reasoning." (recap-style step)

**Why this matters:** The chapters here are not a *library* of standalone artefacts; they are a *course*, taught in order, where each exercise builds on infrastructure or concepts from the last. Ripping out cross-references would force every exercise to re-teach OIDC federation, the Container Apps pattern, etc., which would balloon length and obscure what's actually new.

The skill's "stands alone" rule probably came from a context where exercises were random-access reference material. For sequential teaching, the rule is actively harmful.

### 3. Step count target is too low

**Skill states (`SKILL.md` Step 2):** "4–5 main steps + testing step". `TEMPLATE.md` shows exactly five steps.

**Practice:**

| Exercise | Steps |
|---|---|
| REST Controllers and DTOs | 12 |
| Consuming the API and CORS | 8 |
| API Key Middleware | 11 |
| JWT Bearer and Cleanup | 13 |

**Why this matters:** Cloud-development exercises include provisioning waits, OIDC federation setup, image push + revision rollout, smoke tests, and (often) teardown. Compressing those into 4–5 steps either elides them or buries multiple tasks under one step number. The 8–13 range matches how the work actually decomposes.

### 4. The `Done!` emoji

**Skill states:** `TEMPLATE.md` and `EXAMPLE.md` both end with `## Done! 🎉`.

**Practice:** Every exercise uses plain `## Done!`. The project convention (and a stored memory) is no decorative emoji in content files.

### 5. Templated Goal-section language

**Skill states:** `## Goal\n\nBuild [specific feature] to enable [business capability] in your application.`

**Practice:** Every Goal section opens with a *paragraph of prose* that names the artefact, the trade-off being taught, and the connection back to the previous exercise. Example from `2-consuming-the-api-and-cors.md`:

> The previous exercise left you with a deployed `CloudCiApi` that responds to `curl` with valid JSON — and that is where most introductory API exercises stop. Real APIs get *consumed*, and the moment you point a browser-based client at one, a new layer of HTTP semantics shows up that `curl` never makes you think about ...

**Why this matters:** "Business capability" is the wrong frame for a teaching exercise. The Goal section's job is to set up *why this exercise exists in the course*, which is a pedagogical question, not a product-management one.

### 6. Default base directory

**Skill states (`SKILL.md` Output Contract):** `Base dir (default: ./docs/new-exercises)`.

**Practice:** Exercises live in `content/exercises/<chapter>/<sub-chapter>/`. The skill's default would put files in the wrong tree.

### 7. The "Operating Rules" mandate `TodoWrite` for every file

**Skill states (`SKILL.md` Output Contract):** `for each file: 1. Write 2. Glob to confirm 3. Read first ~200 chars 4. Append a checklist entry via TodoWrite`.

**Practice:** A new exercise is one file. Spinning up TaskCreate machinery for a single coherent write is overhead that adds no value. Existing exercises were written without TaskCreate-per-file.

## What's missing from the skill

These are patterns the four exercises use that the skill doesn't mention.

### A. Forward-pointer paragraph in `Done!`

Every exercise except the last in a chapter ends `Done!` with a paragraph naming the next exercise and the gap it closes:

> The next exercise closes that gap by gating every request behind a shared API key, the simplest possible authentication scheme that's still better than nothing.

This is what gives a chapter narrative coherence. The skill's `## Done!` template has only encouragement ("Great job! ...").

### B. Named Concept Deep Dives

Skill template uses unnamed `> ℹ **Concept Deep Dive**`. Practice uses **named** ones with the topic in the heading:

```markdown
> ℹ **Concept Deep Dive: 401 vs 403**
> ℹ **Concept Deep Dive: same-origin policy and CORS in one breath**
> ℹ **Concept Deep Dive: middleware order is part of the contract**
```

When an exercise has 5+ deep dives, named topics make them navigable.

### C. Full-file code listings, not snippets

Skill template shows snippets ("// Code here"). Practice includes complete, copy-pasteable file contents — the entire `Program.cs`, the entire controller, the entire `appsettings.json`. The student is meant to be able to type or paste the block and have it compile.

### D. Teardown / cleanup steps

The JWT exercise ends with **Step 13: Tear down the cloud resources** — `az group delete`, `az ad app delete`, verification with `az group exists`. Cloud exercises that provision real resources need this; the skill says nothing about cleanup.

### E. Cost-awareness mentions

Several exercises note "your subscription's billing is no longer accruing for any of the Week 6 resources" or similar. Cloud teaching has a money dimension the skill is silent on.

### F. Recap-style steps with explicit pointers back

When a pattern is reused (e.g., the OIDC federation setup in the REST controllers exercise), the step recaps the pattern in compressed form and points back to the full explanation in the deployment chapter. The skill, with its "no cross-references" rule, has no concept of this.

### G. Specific verifiable Quick checks

Skill examples: "*File created at correct location*" / "*Application starts without dependency injection errors*".

Practice examples: ``echo "$API_KEY" | wc -c`` *prints `65` (64 base64 chars + newline)* / `az containerapp secret list` *shows `api-key` and `appinsights-connstr`* / *The JSON output shows `secretRef: "jwt-signing-key"` for `Jwt__SigningKey`, with no `value` field*.

The practice form gives the student a *command* to run and an *exact output* to look for. It's a specification, not a vibe.

### H. Going Deeper as a meaningful exit ramp

Skill examples: "Try adding [enhancement A]" / "Research how [concept] works under the hood".

Practice examples (one bullet from the CORS exercise):

> **Move the policy to the gateway.** In production setups, CORS often lives on Azure API Management, Front Door, or an ingress controller, not in the application. The trade-off is *configuration vs. code*: gateway-side policies change without redeploy, but the application loses the ability to vary CORS by route.

Each Going Deeper bullet names a real next move *with its trade-off*. This is what makes the section useful instead of decorative.

### I. Common Issues with specific error messages → specific causes

Skill examples: "*Build errors:* Ensure all namespaces match" / "*Service not found error:* Check service registration".

Practice examples: "*Every request returns 401 even with a valid token:* Either `app.UseAuthentication()` is missing from the pipeline, or it comes after `app.UseAuthorization()`. The order is `UseAuthentication` first, `UseAuthorization` second, then the endpoint mapping."

The practice form is debuggable. A student who hits the symptom can match it to the cause without further searching.

### J. Upstream documentation links

Practice exercises end Common Issues with links to the canonical reference (`<https://developer.mozilla.org/.../CORS>`, `<https://learn.microsoft.com/aspnet/core/security/cors>`). The skill template doesn't mention this pattern.

## Bugs and stale entries in the skill files

Independent of the divergence question, the skill has a few small bugs that should be fixed regardless.

### `SKILL.md`

- **`version: 1.0.0` in frontmatter is invalid.** Skill metadata uses `name`, `description`, `allowed-tools`, `triggers` — there is no top-level `version` key in the spec. The user has explicit feedback memory about this (`feedback_skill_frontmatter.md`).
- **`allowed-tools` includes `TodoWrite` and `Glob`/`Grep`** but the skill text mandates `TodoWrite` per-file. If we revise the skill not to require TodoWrite, drop it from allowed-tools too.
- **"Don't write outside the repo"** is a sensible rail but the default base dir (`./docs/new-exercises`) doesn't match this repo's actual layout (`content/exercises/...`).

### `GUIDE.md`

- **Malformed code-fence nesting** in section 6 ("Test Step Structure") and section 7 ("Remaining Sections"). The outer ``` ` ``` ` ` ` ` ``` ` `markdown` fences are not matched cleanly with the inner ones — visible if you preview the file. The escaped numbering trick (`1\.`) is documented, but the actual nested examples don't work as displayed in some renderers.
- **MD028 example** under "Important Linting Rule" shows the right pattern but elsewhere the file blank-line rules are inconsistent.

### `TEMPLATE.md`

- Ends with `## Done! 🎉` (emoji — see divergence #4).
- The full template runs ~275 lines, but it's all generic placeholders. A working *example* (which `EXAMPLE.md` is supposed to be) demonstrates the patterns better than a placeholder.

### `EXAMPLE.md`

- Also ends with `## Done! 🎉`.
- The example exercise (Repository Pattern) is well-formed but small. It's a useful skeleton but doesn't exercise any of the patterns that show up in cloud-development exercises (provisioning steps, secret-store patterns, deployment verification, teardown).

## Recommendations

In rough priority order:

### Must fix

1. **Add the Hugo TOML frontmatter to `TEMPLATE.md` and `EXAMPLE.md`.** Without this, the skill produces files that don't render. This is the single most impactful change.
2. **Drop the "no cross-references" rule from `SKILL.md` and `GUIDE.md`.** Replace with: "If your exercise is part of a sequence, end with a one-paragraph forward pointer in `Done!` naming the next exercise and the gap it closes."
3. **Fix `version: 1.0.0` in `SKILL.md` frontmatter.** Remove it (it's not a valid metadata key).
4. **Fix the default base dir in `SKILL.md` Output Contract.** Either drop the default entirely or make it project-aware.

### Should fix

5. **Raise the step-count target.** Either widen the range to "4–13 steps depending on scope" or drop the explicit number altogether — the right number is whatever decomposes the work cleanly.
6. **Drop the `Done! 🎉` emoji** from `TEMPLATE.md` and `EXAMPLE.md`.
7. **Replace the templated Goal phrasing** ("Build [feature] to enable [business capability]") with guidance to write a paragraph of prose naming the artefact and the trade-off.
8. **Loosen the per-file `TodoWrite` mandate.** A single-file exercise doesn't need a task list. Mention TaskCreate as a tool for multi-step authoring sessions, not a per-file requirement.

### Nice to have

9. **Document the named-Concept-Deep-Dive pattern** as a recommendation when an exercise has many deep dives.
10. **Document the full-file-listings convention.** Snippets are fine for tiny insertions; for new files, paste the whole thing.
11. **Add cleanup/teardown step guidance** for cloud exercises that provision real resources, with a note on cost-awareness.
12. **Add a recap-style step pattern** for re-applying a pattern from a previous exercise — compressed body, explicit "if any sub-step is fuzzy, revisit chapter X for the full reasoning."
13. **Tighten the Quick-check examples** in `TEMPLATE.md` toward verifiable commands and exact expected outputs, not abstract "no errors" wording.
14. **Tighten the Common-Issues examples** toward specific error messages → specific causes (the JWT exercise has good models to crib from).
15. **Add a Going-Deeper guideline:** each bullet should name a real next move *with its trade-off*, not a vague suggestion.
16. **Replace `EXAMPLE.md`** with one of the existing chapter exercises (or a trimmed version), so the example demonstrates real patterns: cloud provisioning, secret stores, deployment, teardown.

### Out of scope but worth noting

- The skill currently optimises for *generic .NET MVC + EF + Razor view* exercises. The CLO25 chapters are *cloud-developer* exercises (Container Apps, OIDC federation, App Insights, secret stores). If the skill is supposed to serve both, the differences should be called out as alternative tracks; if it's supposed to serve cloud teaching specifically, several of the recommendations above are non-negotiable.
- A separate `develop-exercise` skill exists in this repo — that one orchestrates the multi-exercise authoring/validation workflow. Worth checking whether its expectations conflict with this skill's per-file rules.

## Session context (the work this report closes out)

The work that prompted this comparison, captured here so the report stands as a single closing record. All code changes are in git; this section is the index.

### Commit `2408d31` — REST controllers exercise: add local-dev user-secrets step

**File:** `content/exercises/4-services-and-apis/1-rest-api-and-dtos/1-rest-controllers-and-dtos.md`

Step 11 (Application Insights) gained a new sub-step that configures the connection string for local development. The pattern mirrors the logging-and-monitoring chapter: an intentionally invalid placeholder in `appsettings.json` (committed; documents the binding key) and the real value in user-secrets via `dotnet user-secrets init` + `dotnet user-secrets set`. Sub-steps were renumbered (Container Apps secret 3→4, commit/push 4→5, traffic generation 5→6); the `git add` in the commit/push sub-step was updated to include `appsettings.json`; the closing Quick check was extended to verify the user-secrets entry.

### Commit `7c26bbe` — Add CORS exercise to REST API chapter, renumber follow-ups

Five files changed, 522 insertions:

- **New:** `2-consuming-the-api-and-cors.md` (weight 2, 8 steps, ~515 lines). Drives the deployed Quotes API from a `.http` file and a vanilla-JS browser page; provokes the CORS error and resolves it with a named policy in `Program.cs`. Steps cover `.http` files as in-editor request collections, building a single-file HTML+JS client, hitting the same-origin block, adding `AddCors`/`UseCors` with correct middleware order, deploying through the existing pipeline, observing the `OPTIONS` preflight on `POST` in DevTools, confirming the policy is restrictive, and a final test step.
- **Renamed:** `2-api-key-middleware.md` → `3-api-key-middleware.md`, weight `2`→`3` (via `git mv`, 99% similarity).
- **Renamed:** `3-jwt-bearer-and-cleanup.md` → `4-jwt-bearer-and-cleanup.md`, weight `3`→`4` (via `git mv`, 99% similarity).
- **Modified:** `_index.md` — chapter description and arc paragraph rewritten for the four-exercise sequence (anonymous-and-curl-only → anonymous-with-CORS → keyed → authenticated).
- **Modified:** `1-rest-controllers-and-dtos.md` — closing forward-pointer paragraph updated to point at the new CORS exercise instead of the API-key exercise.

### Decisions made during the session

- **CORS exercise placement:** inserted between REST controllers and API key. Putting it before authentication keeps the debugging surface small — once API keys are in play, a failed browser call could be CORS, missing header, or wrong key, and the lesson gets muddied.
- **No Postman or Bruno** in the exercise. A `.http` file is enough; Postman's account-and-cloud model adds friction without teaching value at this stage.
- **Target the deployed FQDN, not localhost,** from the JS client. This forces the CORS fix through the OIDC pipeline, which is the cloud-developer muscle the course is training.
- **No Static Web Apps hosting in this exercise.** That belongs in a future chapter; covering it here would dilute the CORS focus.
- **File renames via `git mv`** (rather than weight-only changes) so filenames match weights. URL slugs for the API-key and JWT exercises change; if any external links reference the old slugs, Hugo `aliases = [...]` can be added per file.

### State at session close

- **Branch:** `main`, two commits ahead of `origin/main`. Not pushed (per project convention to ask first).
- **Untracked:** `docs/site-analysis-and-roadmap.md` was already untracked at session start and is unrelated to this work.
- **Not yet acted on:** the recommendations in this report itself. The skill files in `.claude/skills/create-exercise/` were not modified.

## Appendix: file inventory of the comparison

Sampled chapter directory:

```text
content/exercises/4-services-and-apis/1-rest-api-and-dtos/
├── _index.md
├── 1-rest-controllers-and-dtos.md
├── 2-consuming-the-api-and-cors.md
├── 3-api-key-middleware.md
└── 4-jwt-bearer-and-cleanup.md
```

Skill files reviewed:

```text
.claude/skills/create-exercise/
├── SKILL.md
├── GUIDE.md
├── TEMPLATE.md
└── EXAMPLE.md
```

Frontmatter inspected via `grep -nE '^### |^## |^> [ℹ⚠✓]|^> \*\*'` for structural skeletons; full reads for `1-rest-controllers-and-dtos.md` and the four skill files.
