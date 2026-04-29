# publish-ebook — Run log

Records each PR of the unattended v3+ run. One section per PR.

---

## 2026-04-29 03:19 — Run started

Plan: `IMPLEMENTATION-PLAN.md` (PRs 1–6 plus pre-flight 2a/2b).
Mode: unattended, no `git push`. Commit after each PR.

Environment:

- pandoc 3.8.3
- WeasyPrint 68.0
- Python 3.14.3
- PyYAML 6.0.3

Existing artefacts (baseline):

- `static/books/course-book/` 2.6 MB
- `static/books/exercise-book/` 4.5 MB

---

## 2026-04-29 03:22 — Pre-flight 2a (test infrastructure)

Status: completed

- Added `tests/` with 6 fixture trees (2-level, 3-level, single-section,
  slide-siblings, shortcodes) and `preprocessor_test.py`.
- `tests/run.sh` runs `python3 -m unittest discover -s tests -p '*_test.py' -v`.
- 18 tests passing on first green run.

Decisions made during run:

- Followed plan as written; one minor adjustment: shortcode-test input
  uses `{{< … >}}` form (not `/}}`) to match the live regex.
- Pattern flag `-p '*_test.py'` added because unittest discovery defaults
  to `test*.py`, and our file is named `preprocessor_test.py` per spec.

Test results: 18 tests, 18 passing.
Build results: not run this step.

---

## 2026-04-29 03:25 — Pre-flight 2b (books.yaml schema)

Status: completed

- `validate_books()` added to `scripts/build.py`; called in `_load_books()`.
- Errors written to stderr; exits 2 on any schema problem.
- 4 schema tests added (valid, missing keys, bad palette, duplicate id).
- Existing `books.yaml` still passes (`build.py --list` clean).

Test results: 22 tests, 22 passing.

---

## 2026-04-29 03:35 — PR 1 (Tier 1 content fidelity)

Status: completed

- `convert_blockquote_callouts()` now rewrites `> Concept Deep Dive`,
  `> Common Mistakes`, `> Quick check`, `> Before starting`, etc. into
  fenced divs with the existing `.callout-*` / `.exercise-overview` /
  `.callout-prereq` classes. Patterns are tolerant of optional bold
  markers and leading icon glyphs (ℹ ⚠ ✓), and the icon is stripped
  before re-emission so CSS doesn't double-paint.
- `normalize_chapter_title()` strips a leading "1. " from the chapter
  title — the layout already auto-numbers chapters.
- `is_chapter_index_effectively_empty()` lets a chapter with one
  section file and a near-empty `_index.md` (just `{{< children />}}`)
  collapse into a single-section chapter that uses the section's title.
- 11 new tests covering the above (33 total).

Decisions made during run:

- The plan's literal patterns had to be widened: actual corpus uses
  `**Concept Deep Dive**` with leading `ℹ`, not bare `Concept`.
- 3-level fixture got a second section so it still exercises the
  multi-section path after the new single-section detection.

Build results:

- course-book.pdf: 688 pages, 2.1 MB (no change — course content
  has no labelled callouts yet, no numeric prefixes).
- exercise-book.pdf: 898 pages, 3.2 MB (was 940 — 42-page saving
  from callout boxes replacing endnote-style quote blocks plus
  prefix stripping).
- sample-ebook.pdf: 22 pages, 111 KB (no change — passthrough mode).

Test results: 33 tests, 33 passing.

