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

---

## 2026-04-29 03:50 — PR 2 (Tier 2 textbook completeness)

Status: completed

- Cover SVG template at `assets/cover.svg.template` with placeholders
  for title, subtitle, author, palette accent and build version.
- `make_cover_svg()` in build.py renders the template per book.
- WeasyPrint 68 dropped `--format png`, so the plan's PNG conversion
  is skipped. EPUB takes the SVG directly via `--epub-cover-image`,
  PDF embeds it as the first page via `--include-before-body`.
- New `.cover-page` CSS rule + `@page :first { margin: 0 }` so the
  cover fills the page without margins or running heads.
- `build_meta()` produces a `vYYYY.MM.DD-<sha>` build version that is
  baked into the cover SVG and exposed in Pandoc metadata.
- `preprocess()` now emits the source root `_index.md` body (with H1
  stripped and shortcodes resolved) as a `.preface` fenced div in
  course-book; exercise-book's root index is shortcode-only so no
  preface is emitted (correct).
- 5 new tests (38 total).

Decisions made during run:

- Cover image format: SVG only, no PNG raster step. Removes a
  potential blocker (no working PNG output in WeasyPrint 68) and
  keeps the cover cleanly vector-rendered in PDF and EPUB readers.

Build results:

- course-book.pdf: 692 pages, 2.1 MB (+4: cover + preface)
- exercise-book.pdf: 902 pages, 3.2 MB (+4: cover, no preface)
- sample-ebook.pdf: 24 pages, 113 KB (+2: cover; passthrough mode
  skips preface logic).

Test results: 38 tests, 38 passing.

---

## 2026-04-29 04:05 — PR 3 (Tier 3 developer experience)

Status: completed

- New `scripts/cache.py` with content-hash incremental build (book
  config + scripts + assets + source `*.md` files).
- `build.py` gained four flags: `--check`, `--strict`, `--quiet`, `--force`.
  Cache hit short-circuits the whole build with one summary line.
- `--highlight-style=tango` → `--syntax-highlighting=tango` everywhere
  (Pandoc deprecation gone).
- `bin/publish-ebook` shell wrapper at the repo root, PATH-friendly.
- `static/books/*/.build-hash` added to `.gitignore`.
- 5 new cache tests (43 total).
- SKILL.md updated with the new flag table and file inventory.

Decisions made during run:

- The cache file lives inside the book's output dir
  (`<output>/.build-hash`) rather than a separate state dir; gitignored.

Verification of plan success criteria:

- `bin/publish-ebook --list` → ok.
- `bin/publish-ebook course-book` then again → second run completes in
  ~0.1s with `✓ up to date` (32s → 0.1s).
- `bin/publish-ebook course-book --force` → re-runs the full pipeline.
- `bin/publish-ebook exercise-book --strict --force` → exits 1
  (because of the 2 missing images).
- `bin/publish-ebook --check sample-ebook` → exits 0 without rewriting
  artefacts; `--check exercise-book` → exits 1.
- `--quiet` collapses the per-pandoc-command echo into bare
  `→ HTML`/`→ PDF`/`→ EPUB` markers.
- No more "Deprecated: --highlight-style" warning from Pandoc.

Build results:

- All three books rebuilt cleanly under `--force --quiet`.
- course-book.pdf 692p, exercise-book.pdf 902p, sample-ebook.pdf 24p
  (no change vs PR 2 — DX-only PR).

Test results: 43 tests, 43 passing.

---

## 2026-04-29 04:20 — PR 4 (Page-bottom footnotes via Lua filter)

Status: completed

- New `assets/footnotes-inline.lua` rewrites every `Note` element into
  an inline `<span class="footnote">` at the call site. Uses
  `pandoc.write(... 'html')` to preserve inline formatting (em, strong,
  code, links) instead of the lossy `stringify`.
- Wired into the **PDF** Pandoc invocation only via `--lua-filter`;
  the EPUB invocation keeps default endnote rendering (conventional
  for reflowable readers).
- `print.css` updated: removed the `section.footnotes` endnote block,
  added `span.footnote { float: footnote }` plus `::footnote-call`,
  `::footnote-marker` and `@page { @footnote { … } }` rules.

Build results:

- sample-ebook.pdf: 24 → 23 pages — the trailing "NOTES" section is
  gone; footnotes now appear at the bottom of the page where the
  reference is. Spot-checked at the page where footnotes were
  expected and the float worked under WeasyPrint 68 (no
  `BlockReplacedBox` assertion).
- course-book.pdf, exercise-book.pdf: unchanged (no footnotes in
  current corpus, so the filter is a no-op).

Test results: 43 tests, 43 passing.

Decisions made during run:

- Did not need the marginalia escape-hatch: WeasyPrint 68 happily
  renders `float: footnote` next to the existing column-bound
  marginalia float used in the sample. Marginalia stay column-bound
  for now; PR 5 may push them out into the page margin.

---

## 2026-04-29 04:25 — PR 5 (Outer-margin marginalia)

Status: completed

- `.marginnote` switched from `float: right` to
  `position: absolute; right: -1.7in` riding on a relatively-positioned
  paragraph parent.
- Verso (left) pages mirror with `left: -1.7in` plus a flipped border
  so the note sits in the outer margin on every spread.

Build results:

- All 3 books still build cleanly under `--force`.
- No `BlockReplacedBox` crash from WeasyPrint 68 with the new
  position-absolute rule sharing pages with `float: footnote`.
- sample-ebook.pdf: 23 pages (unchanged); marginalia escape the
  text column visually but page flow is unchanged.

Test results: 43 tests, 43 passing.

