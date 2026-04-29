# publish-ebook — Implementation Plan (v3+)

This is a self-contained execution plan for the next session. It is
written to be runnable **unattended**: every decision point has been
pre-resolved, every PR has explicit success criteria, and every
blocker has a documented fall-back.

---

## 0. Execution rules (read this first)

You are executing this plan **autonomously**. Follow these rules:

1. **Do not ask the user questions.** Every decision in this document
   is final. If a new decision arises that this document does not
   cover, log it under *Decisions made during run* in
   `RUN-LOG.md` (see §11) with your chosen default and continue.
2. **Commit after each PR.** Use the commit message template in §10.
   Never push (`git push` is forbidden — see global memory).
3. **Run the full test suite before each commit.** A red test or
   broken build aborts the PR; fix forward in the same PR or skip
   the PR with a note in `RUN-LOG.md` and proceed to the next.
4. **Don't touch student data.** Anything under `docs/student-reports/`
   is off-limits. Anything matching that path causes a hard stop.
5. **Run order is the order in §3.** Do not reorder. Each PR depends
   on the prior ones landing. Skipping is allowed only if a PR
   blocks (with rationale in `RUN-LOG.md`).
6. **No new external dependencies** beyond what is already on the
   user's machine: `pandoc`, `weasyprint`, `python3` (with stdlib
   plus PyYAML which is already installed). If a PR seems to need
   one, redesign it to use what's there or skip the PR.
7. **Stop and write a final report only at the end.** No interactive
   pauses, no "should I continue?" — push through to the end and
   report back what you accomplished and what got skipped.
8. **Time budget**: aim to finish PRs 1–4 within a single session.
   PRs 5 and 6 are stretches; do them only if PRs 1–4 land cleanly
   with time remaining.

A blocker is one of:
- A test you cannot make pass after two attempts.
- A toolchain failure not caused by your own code.
- A merge conflict (shouldn't happen — you are alone on this branch).

For every blocker: log it in `RUN-LOG.md`, skip the PR, continue.

---

## 1. Pre-resolved decisions

These were debated in the prior session. Treat as final.

| Question | Decision |
|----------|----------|
| Test framework | Python stdlib `unittest`. No pytest. |
| Font hosting | Self-host IBM Plex woff2 in `assets/fonts/`. License: OFL-1.1 (commercial use OK, attribution in book metadata). |
| Cover style | Type-driven SVG, palette-coloured. No illustrated covers. |
| Marginalia approach (PR 5) | `position: absolute` workaround inside `position: relative` parent. Do NOT pull in Paged.js. |
| Cross-book refs (PR 6) | **Intra-book link rewriting only.** True cross-book resolution (Course Book → Exercise Book) is descoped — too speculative for unattended. |
| Pandoc deprecation | Replace `--highlight-style` with `--syntax-highlighting` everywhere. |
| Empty chapters | Keep as warnings, do NOT fail `--strict`. They're placeholders the user knows about. |
| Build hash inputs | source tree contents + book config + scripts/*.py + assets/*. Excludes `.pyc` and tests. |
| Top-level wrapper | `bin/publish-ebook` shell script in repo root. |
| `--quiet` mode default | Off. Print one summary line at the end, suppress per-command echo. |
| Cover for PDF | Same SVG as EPUB cover, embedded as the first HTML page (with `@page :first` style). |
| Books.yaml schema | Hand-rolled validator (~30 lines), no `jsonschema` dep. |
| Build version format | `vYYYY.MM.DD-<git-short-sha>`, derived in build.py. |

---

## 2. Pre-flight (before PR 1)

These two items create the foundation; they are not full PRs but
must land before the numbered PRs to keep them small and safe.

### 2a. Test infrastructure

Create:
```
.claude/skills/publish-ebook/tests/
├── __init__.py
├── fixtures/
│   ├── 2-level/                  # mimics content/exercises layout
│   │   ├── _index.md
│   │   ├── 1-foo/
│   │   │   ├── _index.md
│   │   │   └── 1-section.md
│   │   └── 2-bar/
│   │       ├── _index.md
│   │       └── 1-section.md
│   ├── 3-level/                  # mimics content/course-book layout
│   │   └── 1-part/
│   │       ├── _index.md         # title="Part I — Foo"
│   │       └── 1-chapter/
│   │           ├── _index.md
│   │           └── 1-sec.md
│   ├── single-section/           # chapter dir with no _index.md
│   │   └── 1-foo/
│   │       └── foo.md
│   ├── slide-siblings/           # chapter dir with section + slide files
│   │   └── 1-foo/
│   │       ├── foo.md
│   │       ├── foo-slides.md     # type="slide" hidden=true
│   │       └── foo-slides-swe.md
│   └── shortcodes/
│       └── 1-section.md          # contains {{< children >}} and relref
├── preprocessor_test.py
└── run.sh                        # `python3 -m unittest discover -v`
```

Tests to write in `preprocessor_test.py` (one method per concern):
- `test_parse_toml_frontmatter`
- `test_parse_yaml_frontmatter`
- `test_no_frontmatter_falls_back_to_filename`
- `test_shift_headings_skips_fenced_code`
- `test_strip_leading_h1_matching_title`
- `test_parse_part_title_with_em_dash`
- `test_parse_part_title_with_hyphen`
- `test_parse_part_title_no_match`
- `test_to_roman`
- `test_handle_shortcodes_drops_children`
- `test_handle_shortcodes_relref_drop_link_mode`
- `test_handle_shortcodes_unknown_logged_in_report`
- `test_rewrite_image_paths_existing`
- `test_rewrite_image_paths_missing`
- `test_full_passthrough_on_skip_preprocess`
- One end-to-end test: `preprocess(fixtures/3-level, ...)` returns
  expected chapter count + at least one chapter title in output.

**Success criteria**: `bash .claude/skills/publish-ebook/tests/run.sh`
exits 0 with all tests passing.

**Commit message**:
> Add test infrastructure for publish-ebook preprocessor

### 2b. books.yaml schema validation

Add to `scripts/build.py` (or a new `scripts/schema.py`):

```python
REQUIRED = {"id", "title", "author", "source", "output", "palette"}
ALLOWED_PALETTES = {"blue", "red"}

def validate_books(books: list[dict]) -> list[str]:
    errors = []
    seen = set()
    for i, b in enumerate(books):
        prefix = f"books[{i}] ({b.get('id', '?')})"
        missing = REQUIRED - b.keys()
        if missing:
            errors.append(f"{prefix}: missing keys: {missing}")
        if b.get("palette") not in ALLOWED_PALETTES:
            errors.append(f"{prefix}: palette must be one of "
                         f"{ALLOWED_PALETTES}, got {b.get('palette')!r}")
        if b.get("id") in seen:
            errors.append(f"{prefix}: duplicate id {b['id']!r}")
        seen.add(b.get("id"))
    return errors
```

Call before any build. Print errors and exit 2 if any. No new tests
required (the smoke test is: existing `books.yaml` still passes).

**Commit message**:
> Validate books.yaml schema before building

---

## 3. PR sequence (follow this order)

| # | PR | Status when complete |
|--:|----|----------------------|
| 0a | Test infrastructure | green tests, no behaviour change |
| 0b | books.yaml schema validation | unchanged builds, clear errors on bad config |
| 1 | Tier 1 content fidelity | rebuilt course-book + exercise-book look better |
| 2 | Tier 2 polish (cover, preface, version) | books look finished |
| 3 | Tier 3 DX (preflight, incremental, strict, wrapper) | builds are fast and CI-ready |
| 4 | Page-bottom footnotes | typographic polish |
| 5 | Outer-margin marginalia (stretch) | sample-ebook marginalia in margin |
| 6 | Intra-book link rewriting (stretch, descoped from cross-book) | internal links work |

After each PR, run the full test suite and `bin/publish-ebook all`.
Both must pass before committing.

---

## 4. PR 1 — Tier 1 content fidelity

### Scope
Three preprocessor enhancements that visibly improve the rendered books.

### Files
- `.claude/skills/publish-ebook/scripts/preprocessor.py`
- `.claude/skills/publish-ebook/tests/preprocessor_test.py` (extend)

### Changes

**1.1 Blockquote → callout conversion**

Add to `preprocessor.py`:

```python
_CALLOUT_PATTERNS = [
    (re.compile(r"^\s*[ℹ️i]\s+(?:Concept|Note)\b", re.I),     "callout-concept"),
    (re.compile(r"^\s*Key takeaway[:\.]", re.I),                "callout-tip"),
    (re.compile(r"^\s*⚠️?\s*(?:Warning|Gotcha|Caveat)\b", re.I), "callout-warning"),
    (re.compile(r"^\s*✦?\s*Tip\b", re.I),                       "callout-tip"),
    (re.compile(r"^\s*📝?\s*Note\b", re.I),                     "callout-note"),
]


def convert_blockquote_callouts(body: str) -> str:
    """Walk markdown line by line, group consecutive `> ` lines into
    blockquote blocks. If the block's first non-empty content line
    matches a callout pattern, rewrite the block as a fenced div."""
    lines = body.splitlines(keepends=True)
    out, i = [], 0
    while i < len(lines):
        if lines[i].lstrip().startswith(">"):
            # Collect the block
            block, j = [], i
            while j < len(lines) and (
                lines[j].lstrip().startswith(">") or
                (block and lines[j].strip() == "")
            ):
                block.append(lines[j]); j += 1
            # Strip trailing blank lines from the block
            while block and block[-1].strip() == "":
                block.pop()
            # Detect callout
            inner = [ln.lstrip()[1:].lstrip() for ln in block
                     if ln.lstrip().startswith(">")]
            first = next((ln.strip() for ln in inner if ln.strip()), "")
            klass = next(
                (k for pat, k in _CALLOUT_PATTERNS if pat.match(first)),
                None
            )
            if klass:
                out.append(f"::: {{.{klass}}}\n")
                out.append("\n".join(inner) + "\n")
                out.append(":::\n\n")
            else:
                out.extend(block)
                out.append("\n")
            i = j
        else:
            out.append(lines[i]); i += 1
    return "".join(out)
```

Call **before** `handle_shortcodes` in the section-handling loop.

**1.2 Strip numeric prefix from chapter titles**

```python
_NUMERIC_PREFIX = re.compile(r"^\d+\.\s+(?=[A-Za-z])")

def normalize_chapter_title(raw: str) -> str:
    """Strip a leading '1. ' / '2. ' from a chapter title — the book
    auto-numbers chapters, so the manual prefix becomes redundant."""
    candidate = _NUMERIC_PREFIX.sub("", raw.strip(), count=1)
    return candidate if len(candidate) >= 4 else raw.strip()
```

Apply at every site that uses a chapter title:
- `chapter_fm.title` in `_handle_chapter` (multi-section path)
- `sole_fm.title` in `_handle_chapter` (single-section path)

**1.3 Smarter single-section detection**

```python
_SHORTCODE_ONLY = re.compile(r"\{\{<.*?>\}\}", re.DOTALL)

def is_chapter_index_effectively_empty(idx_body: str) -> bool:
    """A chapter `_index.md` body that is just shortcodes plus an
    optional title heading should be treated as empty for layout
    purposes: the chapter's real content lives in the section file."""
    s = _SHORTCODE_ONLY.sub("", idx_body)
    s = re.sub(r"^\s*#.*$", "", s, flags=re.MULTILINE)
    return len(s.strip()) < 80
```

In `_handle_chapter`:
```python
has_index = (chapter_dir / "_index.md").exists()
_, idx_body = read_index(chapter_dir) if has_index else (None, "")
single_section = (
    len(sections) == 1 and (
        not has_index
        or is_chapter_index_effectively_empty(idx_body)
    )
)
```

### Tests to add
- `test_blockquote_callout_concept`
- `test_blockquote_callout_warning`
- `test_blockquote_no_match_left_alone`
- `test_blockquote_inside_code_block_left_alone`
- `test_normalize_strips_numeric_prefix`
- `test_normalize_keeps_short_titles`
- `test_normalize_keeps_non_numeric`
- `test_index_effectively_empty_just_children_shortcode`
- `test_index_with_real_intro_paragraph_not_empty`

### Success criteria
- All tests pass.
- `bin/publish-ebook course-book` and `bin/publish-ebook exercise-book`
  succeed.
- Spot-check: open the new `exercise-book.pdf` and verify "Concept
  Deep Dive" / "Key takeaway" callouts now appear as boxes.
- Spot-check: chapter titles like "Chapter 5 — Hello World" instead
  of "Chapter 5 — 1. Hello World".

### Commit
> PR 1: Tier 1 content fidelity (callouts, titles, single-section)

---

## 5. PR 2 — Tier 2 textbook completeness

### Scope
Cover image (PDF + EPUB), Preface from book root `_index.md`, build
version on title page.

### Files
- `.claude/skills/publish-ebook/assets/cover.svg.template` *(new)*
- `.claude/skills/publish-ebook/scripts/build.py`
- `.claude/skills/publish-ebook/scripts/preprocessor.py`
- `.claude/skills/publish-ebook/assets/print.css`
- `.claude/skills/publish-ebook/assets/pandoc-html.template`

### 5.1 Cover SVG template

`assets/cover.svg.template` (use placeholders, not Jinja — we string-replace):

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 600 900"
     width="600" height="900">
  <rect x="0" y="0" width="600" height="900" fill="#ffffff"/>
  <rect x="0" y="0" width="600" height="120" fill="{{ACCENT}}"/>
  <text x="60" y="80" font-family="IBM Plex Sans, Helvetica, sans-serif"
        font-size="22" font-weight="500" letter-spacing="6" fill="#ffffff">
    CLOUD DEVELOPERS
  </text>
  <text x="60" y="380" font-family="IBM Plex Sans, Helvetica, sans-serif"
        font-size="44" font-weight="600" fill="#1f2933">
    {{TITLE_LINE_1}}
  </text>
  <text x="60" y="430" font-family="IBM Plex Sans, Helvetica, sans-serif"
        font-size="44" font-weight="600" fill="#1f2933">
    {{TITLE_LINE_2}}
  </text>
  <text x="60" y="490" font-family="IBM Plex Serif, Georgia, serif"
        font-size="20" font-style="italic" fill="#52606d">
    {{SUBTITLE}}
  </text>
  <line x1="60" y1="780" x2="540" y2="780" stroke="{{ACCENT}}" stroke-width="2"/>
  <text x="60" y="820" font-family="IBM Plex Sans, Helvetica, sans-serif"
        font-size="14" letter-spacing="2" fill="#52606d">
    {{AUTHOR}}
  </text>
  <text x="60" y="850" font-family="IBM Plex Sans, Helvetica, sans-serif"
        font-size="11" letter-spacing="3" fill="#52606d">
    {{BUILD_VERSION}}
  </text>
</svg>
```

Title is split into two lines for visual balance: split at the
midpoint word boundary if title is longer than 22 chars; otherwise
all on line 1.

### 5.2 Cover generation in `build.py`

```python
import textwrap, html, datetime, subprocess

PALETTE_HEX = {"blue": "#2a6f8f", "red": "#a23a4f"}

def _split_title(title: str) -> tuple[str, str]:
    if len(title) <= 22:
        return title, ""
    words, midpoint, line1 = title.split(), len(title) // 2, ""
    for w in words:
        if len(line1) + len(w) + 1 <= midpoint:
            line1 = (line1 + " " + w).strip()
        else:
            break
    line2 = title[len(line1):].strip()
    return line1, line2


def make_cover_png(book: dict, build_version: str, tmp: Path) -> Path:
    template = (ASSETS / "cover.svg.template").read_text()
    line1, line2 = _split_title(book["title"])
    accent = PALETTE_HEX.get(book.get("palette", "blue"), "#2a6f8f")
    svg = (template
           .replace("{{TITLE_LINE_1}}", html.escape(line1))
           .replace("{{TITLE_LINE_2}}", html.escape(line2))
           .replace("{{SUBTITLE}}", html.escape(book.get("subtitle", "")))
           .replace("{{AUTHOR}}", html.escape(book.get("author", "")))
           .replace("{{BUILD_VERSION}}", html.escape(build_version))
           .replace("{{ACCENT}}", accent))
    svg_path = tmp / "cover.svg"
    png_path = tmp / "cover.png"
    svg_path.write_text(svg)
    # WeasyPrint will rasterize SVG when given as input HTML
    html_wrapper = f"""<!DOCTYPE html><html><body>
        <img src="{svg_path}" style="width:600px;height:900px"/>
        </body></html>"""
    wrapper_path = tmp / "cover-wrapper.html"
    wrapper_path.write_text(html_wrapper)
    _run(["weasyprint", str(wrapper_path), str(png_path),
          "--resolution", "150"])
    return png_path
```

Pass to Pandoc EPUB call: `--epub-cover-image=<png_path>`.

For PDF: emit a `<div class="cover-page"><img src="..."></div>`
fragment as the first thing in the body via a Pandoc
`--include-before` file.

### 5.3 Preface from source root `_index.md`

In `preprocessor.preprocess()`, after parts_mode/non-parts branch
opens, before the parts/chapters loop:

```python
root_idx = source / "_index.md"
if root_idx.exists():
    fm, body = parse_frontmatter(root_idx.read_text(encoding="utf-8"), root_idx)
    body = strip_leading_h1_matching_title(body, fm.title)
    body = handle_shortcodes(body, relref_mode=relref_mode,
                             report=report, path=root_idx)
    if project_root is not None:
        body = rewrite_image_paths(body, project_root, report)
    if body.strip():
        out.append(
            "::: {.preface}\n"
            f"# Preface {{.unnumbered #preface}}\n\n"
            f"{body.strip()}\n"
            ":::\n\n"
        )
```

### 5.4 Build version

In `build.py`:

```python
def _build_meta() -> dict:
    try:
        sha = subprocess.check_output(
            ["git", "rev-parse", "--short", "HEAD"],
            cwd=PROJECT_ROOT, stderr=subprocess.DEVNULL,
        ).decode().strip()
    except Exception:
        sha = "uncommitted"
    today = datetime.date.today()
    return {
        "build_version": f"v{today:%Y.%m.%d}-{sha}",
        "build_date":    str(today),
    }
```

Pass into the metadata YAML written for Pandoc:
```python
md["build-version"] = build_meta["build_version"]
md["build-date"]    = build_meta["build_date"]
```

In `pandoc-html.template`, add inside the body, before `$body$`:
```html
$if(build-version)$
<div class="build-stamp">$build-version$</div>
$endif$
```

In `print.css`, style:
```css
.build-stamp {
  display: none;   /* visible only on title page via @page :nth(2) */
}
```

(Skip the per-page positioning for now — too fiddly. The cover SVG
already carries `{{BUILD_VERSION}}` in the bottom-left; that's
enough for v3.)

### Tests to add
- `test_split_title_short_returns_one_line`
- `test_split_title_long_splits_at_word_boundary`
- `test_cover_svg_substitutes_all_placeholders`
- `test_preprocess_emits_preface_when_root_index_has_body`
- `test_preprocess_skips_preface_when_root_index_empty`

### Success criteria
- All books rebuild.
- EPUBs show the cover thumbnail in Apple Books / Calibre.
- PDF page 1 is the cover (no "Cover" header — just the SVG-based image).
- Course-book has a Preface chapter visible in the ToC, with text
  derived from `content/course-book/_index.md`.
- The cover shows a build version like `v2026.04.29-79c33b5`.

### Commit
> PR 2: Tier 2 polish (cover image, preface, build version)

---

## 6. PR 3 — Tier 3 developer experience

### Scope
`--check` (preflight), `--strict`, `--quiet`, `--force`, incremental
build via cache, top-level wrapper script, fix Pandoc deprecation.

### Files
- `.claude/skills/publish-ebook/scripts/build.py`
- `.claude/skills/publish-ebook/scripts/cache.py` *(new)*
- `bin/publish-ebook` *(new)*
- `.claude/skills/publish-ebook/SKILL.md` (document new flags)

### 6.1 Pandoc deprecation fix
Replace `--highlight-style=tango` with `--syntax-highlighting=tango`
in both Pandoc invocations. Test that current output is identical.

### 6.2 Caching

`scripts/cache.py`:

```python
import hashlib, json
from pathlib import Path

def compute_build_hash(book: dict, project_root: Path,
                       script_dir: Path, assets_dir: Path) -> str:
    h = hashlib.sha256()
    # Book entry
    h.update(json.dumps(book, sort_keys=True).encode())
    # Skill version: hash all .py and asset files
    for f in sorted(script_dir.glob("*.py")):
        h.update(f.read_bytes())
    for f in sorted(assets_dir.iterdir()):
        if f.is_file():
            h.update(f.read_bytes())
    # Source tree
    src = project_root / book["source"]
    if src.is_dir():
        for f in sorted(src.rglob("*.md")):
            h.update(str(f.relative_to(src)).encode())
            h.update(f.read_bytes())
    return h.hexdigest()


def cached(out_dir: Path, build_hash: str) -> bool:
    cache_file = out_dir / ".build-hash"
    if cache_file.exists() and cache_file.read_text().strip() == build_hash:
        return True
    return False


def write_cache(out_dir: Path, build_hash: str) -> None:
    (out_dir / ".build-hash").write_text(build_hash + "\n")
```

In `build.py`:
- Compute hash before building.
- If cached AND not `--force`, print "✓ up to date" and return.
- After successful build, write cache.

### 6.3 `--check` mode

New flag in argparse. When set, run the validation and preprocess
phases only; print the report; exit 0 if clean, 1 if any
warnings (unknown shortcodes / missing images / parse errors), 2 if
schema invalid.

### 6.4 `--strict` mode
After build, check report. Exit 1 if any of:
- `report.unknown_shortcodes`
- `report.missing_images`

`drafts_skipped` and `notes` (empty chapters) do NOT trigger strict.

### 6.5 `--quiet` mode

When set, suppress the per-command echo (`$ pandoc …`) lines. Keep
the `=== Building ===` line and the build report. Replace per-step
echoes with simple `→ HTML`, `→ PDF`, `→ EPUB` markers.

### 6.6 Top-level wrapper

`bin/publish-ebook`:
```bash
#!/usr/bin/env bash
set -euo pipefail
ROOT="$(git rev-parse --show-toplevel 2>/dev/null || echo "$PWD")"
exec python3 "$ROOT/.claude/skills/publish-ebook/scripts/build.py" "$@"
```

`chmod +x bin/publish-ebook`. Also add `bin/` to PATH-friendly
location, OR document the absolute invocation in SKILL.md.

### Tests to add
- `test_cache_unchanged_returns_true`
- `test_cache_changed_source_invalidates`
- `test_cache_changed_book_config_invalidates`
- `test_cache_changed_assets_invalidates`

### Success criteria
- `bin/publish-ebook --list` works.
- `bin/publish-ebook course-book` builds; running it again says
  "✓ up to date" in <1s.
- `bin/publish-ebook course-book --force` rebuilds.
- `bin/publish-ebook exercise-book --strict` exits 1 (because of
  the 2 missing images).
- `bin/publish-ebook --check sample-ebook` exits 0 without writing
  any artefacts.
- No more "Deprecated: --highlight-style" warnings.

### Commit
> PR 3: Tier 3 DX (--check, --strict, incremental cache, wrapper)

---

## 7. PR 4 — Page-bottom footnotes

### Scope
Pandoc Lua filter that inlines footnotes for the PDF path.

### Files
- `.claude/skills/publish-ebook/assets/footnotes-inline.lua` *(new)*
- `.claude/skills/publish-ebook/assets/print.css`
- `.claude/skills/publish-ebook/scripts/build.py`

### 7.1 Lua filter

`assets/footnotes-inline.lua`:

```lua
-- Inline footnotes for WeasyPrint's float: footnote support.
-- Replaces Pandoc's default endnote-section emission with inline
-- <span class="footnote">…</span> at each ref site.

function Note(elem)
  -- Wrap the footnote contents in a span so CSS `float: footnote`
  -- can place them at the bottom of the page.
  local content = pandoc.utils.stringify(elem.content)
  return pandoc.RawInline('html',
    '<span class="footnote">' .. content .. '</span>')
end
```

(If the simple `stringify` loses formatting, switch to walking the
content blocks and serialising via Pandoc's HTML writer; see Pandoc
Lua filter docs.)

### 7.2 CSS

In `print.css`, ADD:

```css
span.footnote {
  float: footnote;
  font-family: 'IBM Plex Serif', serif;
  font-size: 8.5pt;
  line-height: 1.4;
  color: var(--ink-soft);
}

::footnote-call {
  font-size: 0.7em;
  vertical-align: super;
  line-height: 0;
  color: var(--accent);
}

::footnote-marker {
  font-family: 'IBM Plex Sans', sans-serif;
  font-size: 8pt;
  font-weight: 600;
  color: var(--accent);
}

@page {
  @footnote {
    border-top: 0.4pt solid var(--rule);
    padding-top: 4pt;
    margin-top: 8pt;
  }
}
```

REMOVE the existing `section.footnotes` block (the endnote section
shouldn't exist anymore for the PDF path).

### 7.3 Wire in build.py

For the PDF path only, add:
```python
"--lua-filter", str(ASSETS / "footnotes-inline.lua"),
```

EPUB keeps default footnote rendering (endnotes are conventional
for EPUB readers).

### Success criteria
- `sample-ebook.pdf` page 12 (or wherever footnotes were) now shows
  the footnote at the bottom of the page where the ref appears.
- The trailing "NOTES" section is gone from the PDF.
- EPUB unchanged: footnotes still listed at end.

### If WeasyPrint floats clash with marginalia

If the build crashes with the same `BlockReplacedBox` assertion as
in v1, **temporarily disable the marginalia float** by replacing
`float: right` with `display: none` for `.marginnote` in print.css.
Note in `RUN-LOG.md`: "PR 4 disabled marginalia rendering — PR 5
restores via position-absolute approach." Continue.

### Commit
> PR 4: Page-bottom footnotes via Pandoc Lua filter

---

## 8. PR 5 (stretch) — Outer-margin marginalia

### Scope
Marginalia floats out into the outer page margin via
position-absolute, avoiding the WeasyPrint float-footnote clash.

### Files
- `.claude/skills/publish-ebook/assets/print.css`

### Changes

Replace the `.marginnote` block:

```css
/* Wrapping paragraph relativeises so absolute marginnotes work */
p { position: relative; }

.marginnote {
  position: absolute;
  width: 1.5in;
  right: -1.7in;        /* push out into outer margin on recto */
  top: 0;
  margin: 0;
  font-family: 'IBM Plex Sans', sans-serif;
  font-size: 8.2pt;
  line-height: 1.4;
  color: var(--ink-soft);
  border-left: 0.6pt solid var(--accent);
  padding-left: 8pt;
}

/* Mirror to the other side on verso pages */
@page :left {
  .marginnote {
    right: auto;
    left: -1.7in;
    border-left: none;
    border-right: 0.6pt solid var(--accent);
    padding-left: 0;
    padding-right: 8pt;
  }
}
```

### Success criteria
- Sample-ebook chapter 1 marginalia appear in the right margin on
  recto pages, not inside the text column.
- No build crashes.

### If it crashes
Revert to the column-bound float, log the WeasyPrint version, and
note in `RUN-LOG.md` that PR 5 is blocked on upstream.

### Commit
> PR 5: Outer-margin marginalia via position-absolute

---

## 9. PR 6 (stretch, descoped) — Intra-book link rewriting

### Scope
Markdown links to other sections of the same book resolve to in-book
anchors. Cross-book links are NOT in scope (descoped).

### Files
- `.claude/skills/publish-ebook/scripts/preprocessor.py`

### Changes

Two-pass within the same book:

**Pass 1**: walk the source tree, build an index:
```python
{
  "1-cloud-foundations/1-understanding-cloud-computing/1-what-is-cloud-computing.md":
    "sec-what-is-cloud-computing",
  ...
}
```

**Pass 2**: when a markdown link uses a relative `.md` path or a
relref to a same-book file, rewrite the link to `#<anchor>`. Links
out of the book are left as-is.

A regex to match:
```python
_INTRABOOK_LINK = re.compile(
    r"\[([^\]]+)\]\((?P<path>(?!https?://)[^)]+\.md)\)"
)
```

For each match, resolve `path` against the section's source dir,
look up in the index, and rewrite to `#anchor`. If not found in the
index, leave the link alone and log to `report.notes`.

### Tests to add
- `test_link_to_sibling_md_rewrites_to_anchor`
- `test_link_to_external_url_left_alone`
- `test_link_to_unknown_md_logged_in_report`

### Success criteria
- Build still succeeds.
- A test fixture with a known intra-book link produces a working
  ToC anchor in the output.
- No existing builds regress.

### Commit
> PR 6: Intra-book link rewriting

---

## 10. Commit message template

For each PR, use:

```
PR <N>: <one-line summary>

<2–3 lines of why / what changed at the level of an architect>

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

---

## 11. RUN-LOG.md

Create `.claude/skills/publish-ebook/RUN-LOG.md` at the start of
the run. Append one entry per PR:

```markdown
## YYYY-MM-DD HH:MM — PR <N>

Status: completed | skipped | partial

Decisions made during run (if any):
- ...

Issues / surprises:
- ...

Test results: <N> tests, <M> passing
Build results: course-book <X> pages, exercise-book <Y> pages, sample-ebook 22 pages
```

If a PR is skipped, explain why and what would unblock it.

This file is the single source of truth for the user when they
return after the run. Make it readable.

---

## 12. Final report

When done (or out of time), append to RUN-LOG.md:

```markdown
## Final summary

PRs completed: <list>
PRs skipped: <list with reasons>
Total commits: <N>
Total time: <approximate>
Branch state: ahead of origin/main by <N> commits, NOT pushed

Books rebuilt:
- course-book.pdf: <NEW pages> (was 688)
- exercise-book.pdf: <NEW pages> (was 940)
- sample-ebook.pdf: <NEW pages> (was 22)

Next steps for the user:
- ...
```

---

## 13. Hard stops

Abort the entire run (don't continue to subsequent PRs) if:

- The test infrastructure (§2a) cannot be made to run at all.
- `bin/publish-ebook --list` ever returns a non-zero exit during
  routine operation (means books.yaml is broken).
- More than 3 consecutive PRs are skipped — the plan has structural
  issues that need human review.
- `git status` shows any modification to `docs/student-reports/`.

In any of these cases, write the final report and stop.
