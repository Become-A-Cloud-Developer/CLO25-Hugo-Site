# Style Checklist

Concrete grep patterns and word-count thresholds enforced by `tools/voice-check.sh`. This file is the executable counterpart to the prose-style guidance in `student-technical-writer/SKILL.md` — the writer skill describes the spirit, this file describes the letter.

## Word count thresholds

| Range | Outcome |
|-------|---------|
| 1500 – 3500 words | Pass |
| 1400 – 1499 or 3501 – 3600 words | Soft warning |
| 1200 – 1399 or 3601 – 4000 words | Soft warning (escalating) |
| < 1200 or > 4000 words | Hard fail |

Word count is measured on the body of `<slug>.md` excluding frontmatter, fenced code blocks, and tables. The script in `tools/voice-check.sh` strips these before counting.

## Forbidden voice patterns (hard fail)

These cause `voice-check.sh` to exit non-zero. They appear case-insensitively as whole-word matches in body prose only (not inside code blocks, blockquotes, or quoted user input).

| Pattern | Reason |
|---------|--------|
| `\bwe will\b` | First-person plural |
| `\bwe'll\b` | First-person plural |
| `\bwe can\b` | First-person plural |
| `\bwe'll see\b` | First-person plural |
| `\blet's\b` | First-person plural |
| `\bour application\b` | First-person plural (unless in a quoted code comment) |

## Forbidden voice patterns (soft warning)

These produce a warning but do not block. Manual review during B5 fix-up.

| Pattern | Reason |
|---------|--------|
| `\bmodern\b` (sentence-initial only: `^\s*Modern\b`) | Temporal filler |
| `\btoday's\b` | Temporal filler |
| `\bin the digital age\b` | Temporal filler |
| `\bin the current landscape\b` | Temporal filler |
| `\bcontemporary\b` | Temporal filler |
| `\bsimply\b` | Marketing fluff (per student-technical-writer) |
| `\bjust\b` (when preceded by "you", e.g. "you just need") | Marketing fluff |
| `\beasy\b` (in claim form: "is easy", "easy to") | Marketing fluff |
| `\bpowerful\b` | Marketing fluff |
| `\brobust\b` | Marketing fluff |
| `\bseamless\b` | Marketing fluff |

## Rhetorical question detection

Flag prose lines (excluding code, blockquotes, table cells) that end in `?` and contain none of `(", ', ‘, ’, “, ”)` quote markers. The heuristic regex:

```
^[A-Z][^"'`]*\?\s*$
```

False-positive rate is moderate — the B4 reviewer can mark legitimate cases (e.g. quoted student question) as accepted in the punch list. Reported as soft warning.

## Heading and structure checks

| Check | How |
|-------|-----|
| H1 not in body | `grep -n '^# ' <slug>.md` returns nothing (H1 comes from frontmatter title) |
| Code blocks have language tag | `grep -E '^```$' <slug>.md` returns nothing — every fence has a language |
| Frontmatter present | First line is `+++`; `+++` appears at least twice |
| Required frontmatter keys | `title`, `program`, `cohort`, `courses`, `weight`, `draft` all present |

## Cross-link to exercise

Every `<slug>.md` must contain at least one link matching `/exercises/[^)]+`. If absent, hard fail.

## Frontmatter validation

Required keys, all present:

```toml
title = "..."
program = "CLO"
cohort = "25"
courses = [...]   # non-empty list
weight = N        # integer
draft = false
```

Optional keys allowed:

```toml
date = YYYY-MM-DD
description = "..."
aliases = [...]   # only for migrated pages
```

Any required key missing or `draft = true`: hard fail.

## Slide-pair checks (run as part of B7 gate 5/6)

| Check | How |
|-------|-----|
| Both slide source files exist | `<slug>-slides.md` and `<slug>-slides-swe.md` present in same directory as `<slug>.md` |
| Both rendered HTML files exist | `<slug>.html` and `<slug>-swe.html` present in `static/presentations/course-book/<part>/<section>/` |
| Section parity | `grep -c '<section' <html>` for EN and SWE differ by ≤1 |
| Slide source `## Heading` count | EN and SWE differ by ≤1 |

## Internal link resolution

For each `[text](/path/...)` in `<slug>.md`:

1. If the path starts with `/exercises/`, `/course-book/`, `/getting-started/`, `/tutorials/`, or `/week-by-week/`: verify the corresponding directory or file exists under `content/`. The path's terminal element may be:
   - A directory containing `_index.md`
   - A `.md` file (path strips trailing `/`)
   - A page with frontmatter `aliases = [...]` matching this path
2. If the path starts with `/presentations/`: verify the corresponding `.html` file exists under `static/presentations/`.
3. If the path is external (`https?://`): skipped here; handled by `check-links` skill at end of Part-run.

Any unresolved internal link is a hard fail.

## Anchor link resolution (cross-Part check, run by `anchor-check.sh`)

For each `[text](/path/...#anchor)` and `{{< ref "path#anchor" >}}` in any chapter under `content/course-book/`:

1. Resolve the path to its target `.md` file.
2. Slug-ify each `## Heading` and `### Heading` in the target file (lowercase, replace whitespace with `-`, strip non-`[a-z0-9-]`).
3. Verify the anchor matches at least one slugified heading.

Any unmatched anchor is a hard fail. The script runs at end of every Part-run because anchor breakage from later edits is non-local.
