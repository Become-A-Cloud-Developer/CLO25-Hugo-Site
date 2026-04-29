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

