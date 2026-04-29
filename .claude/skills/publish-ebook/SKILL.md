---
name: publish-ebook
description: Build textbook-quality EPUB and PDF e-books from the CLO25 Hugo content trees (course-book and exercises). Use whenever the user wants to publish, regenerate, or preview a downloadable book — phrases like "publish the course book", "rebuild the e-book", "make a PDF of the exercises", "ebook", "EPUB". Drives off `books.yaml` at the project root; one entry per book. Pipeline is Pandoc + WeasyPrint with a custom CSS Paged Media stylesheet (IBM Plex typography, drop caps, callouts, marginalia, running heads, ToC).
---

# publish-ebook

Builds the Course Book and Exercise Book as **EPUB + PDF** from the
markdown under `content/course-book/` and `content/exercises/`. The
visual design is fixed (a textbook aesthetic with IBM Plex
typography, two accent palettes, and a callout system). The skill
exists so a single command rebuilds any volume in one step, and so
the design lives in one place rather than being copied per book.

## When to use this skill

- "Publish the Course Book" / "rebuild the e-book" / "make a PDF of
  the exercises".
- Any request mentioning EPUB, e-book, or downloadable PDF for the
  course content.
- After a content commit that changes anything under
  `content/course-book/` or `content/exercises/` and the user wants
  the artefact refreshed.

Do **not** invoke this skill for slide presentations (use
`revealjs-skill`) or for the Hugo site itself.

## How invocation maps to action

There is a thin wrapper at `bin/publish-ebook` (PATH-friendly). All
invocations below also work as `python3 .claude/skills/publish-ebook/scripts/build.py …`.

| User asks                                       | Run                              |
|-------------------------------------------------|----------------------------------|
| "publish the course book"                       | `bin/publish-ebook course-book`   |
| "rebuild the exercise book"                     | `bin/publish-ebook exercise-book` |
| "build all e-books"                             | `bin/publish-ebook all`           |
| "list books"  /  "what e-books are configured?" | `bin/publish-ebook --list`        |
| "rebuild the sample"                            | `bin/publish-ebook sample-ebook`  |
| "force a fresh build"                           | `bin/publish-ebook <id> --force`  |
| "preflight the exercise book"                   | `bin/publish-ebook --check exercise-book` |
| "fail on warnings"                              | `bin/publish-ebook <id> --strict` |
| "quiet output"                                  | `bin/publish-ebook <id> --quiet`  |

Flags:

- `--force` — ignore the `<output>/.build-hash` cache and rebuild.
- `--check` — preflight (validate + preprocess) without writing PDF/EPUB.
  Exits 1 if the build report contains warnings, 2 if `books.yaml`
  is invalid.
- `--strict` — exit 1 after a successful build if the report contains
  unknown shortcodes or missing images. Empty chapters and drafts
  do not trigger strict.
- `--quiet` — suppress per-command echo and the report dump; keep
  the summary lines.

After a successful build, surface the output paths so the user can
open the files. Apple Books opens `.epub`; Preview opens `.pdf`.

## Configuration: `books.yaml`

The build is driven by `books.yaml` at the project root. Each entry
declares a book. Adding a new volume means adding an entry — not
writing new code.

Field reference is in `books.yaml` itself; the important keys are:

- `id`            — short identifier; the argument to this skill
- `source`        — directory under the project root to walk for `.md` files
- `output`        — where the EPUB and PDF land
- `palette`       — `blue` (Course Book) or `red` (Exercise Book)
- `parts`         — `true` if the source uses 3-level Hugo hierarchy
- `relref-mode`   — how to handle `{{< relref >}}` links (`drop-link`,
                    `footnote`, or `keep`)
- `skip-preprocess` — `true` for hand-authored sources without Hugo
                      front matter (e.g. the `sample-ebook` entry)

## Toolchain

The skill assumes these are on `PATH`:

| Tool         | Used for                              |
|--------------|---------------------------------------|
| `pandoc`     | markdown → HTML and markdown → EPUB   |
| `weasyprint` | HTML + CSS Paged Media → PDF          |
| `python3`    | the orchestrator and preprocessor     |

Install once on macOS:

```sh
brew install pandoc
pipx install weasyprint        # or: brew install weasyprint
```

WeasyPrint fetches IBM Plex from Google Fonts at build time, so
internet is required during a build. To self-contain, embed the
font files in `assets/fonts/` and switch the `@import` in
`assets/print.css` to local `@font-face` rules.

## What the build does

1. Read `books.yaml` and resolve the requested book.
2. Call `scripts/preprocessor.py` to walk the source tree and
   produce one big concatenated markdown document (see *Limitations*
   below — v1 is a near-passthrough).
3. Generate book metadata YAML (title, author, identifier).
4. Run Pandoc → standalone HTML using
   `assets/pandoc-html.template` + `assets/print.css`, then hand
   the HTML to WeasyPrint to produce the PDF.
5. Run Pandoc → EPUB3 with `assets/epub.css`.
6. Write `<book-id>.epub` and `<book-id>.pdf` into the configured
   `output` directory.

## Files in this skill

```
.claude/skills/publish-ebook/
├── SKILL.md                       this file
├── scripts/
│   ├── build.py                   orchestrator (entry point)
│   ├── cache.py                   incremental-build hash
│   └── preprocessor.py            hugo-tree → flat markdown
├── assets/
│   ├── cover.svg.template         type-driven cover, palette-coloured
│   ├── print.css                  PDF stylesheet (CSS Paged Media)
│   ├── epub.css                   EPUB stylesheet (reflowable)
│   └── pandoc-html.template       suppresses Pandoc's auto title-block
└── tests/
    ├── run.sh                     stdlib unittest runner
    ├── preprocessor_test.py       unit + smoke tests
    └── fixtures/                  small Hugo trees for tests
```

## Design decisions worth knowing

- **Two-CSS, one-source.** EPUB and PDF share a design language but
  have different stylesheets because EPUB readers ignore most
  `@page` rules and PDF needs CSS Paged Media. The shared design
  tokens (IBM Plex, accent palette, callout colours) live in both
  files; keep them in sync when changing palette.
- **Drop caps via explicit `<span class="dropcap">`,** not
  `::first-letter { float: left }` — the latter crashes WeasyPrint
  when combined with `target-counter()` in the ToC.
- **Footnotes render as endnotes at the end of the book.** True
  page-bottom footnotes via `float: footnote` need Pandoc to emit
  inline notes rather than a `<section class="footnotes">` block;
  not done in v1.
- **Marginalia float on the right edge of the text column,** not in
  the outer page margin. Pure-outer-margin floats clash with
  WeasyPrint's footnote machinery.
- **Bold "Label" prefixes** inside callouts get small-caps treatment
  via `> p:first-child > strong:first-child` — only the first
  paragraph's leading bold is treated as a label.

## What the preprocessor does

For each book whose `skip-preprocess` is not set, the preprocessor:

1. **Walks the source tree by Hugo `weight`** — directory weight
   from each `_index.md`, file weight from per-file front matter.
   Lexical numeric prefixes (`1-foo`) are a hint; weight is
   authoritative.
2. **Parses TOML front matter** (`+++ ... +++`) on every file.
   Extracts `title`, `weight`, `draft`, `hidden`, `type`.
3. **Filters out non-content files** — anything with `draft = true`,
   `hidden = true`, or `type = "slide"` is excluded. This is what
   keeps the slide siblings out of the book.
4. **Maps the Hugo hierarchy to book structure**:
    - `parts: true`  →  Part / Chapter / Section
    - `parts: false` →  Chapter / Section
   Part `_index.md` titles like *"Part I — Cloud Foundations"* are
   parsed; the prefix becomes the eyebrow, the suffix becomes the
   Part title.
5. **Recognises single-section chapters** — a chapter directory
   with no `_index.md` and exactly one content file (a slide-pair
   sibling pattern) uses the section's title as the chapter title
   and inlines its body. Avoids ToC duplication.
6. **Resolves Hugo shortcodes**:
    - `{{< children >}}` — dropped (it is nav, not content).
    - `{{< relref "x.md" >}}` — handled per book's `relref-mode`.
    - Any other shortcode is dropped and reported as unknown.
7. **Rewrites Hugo-absolute image URLs** (`/images/foo.png`) to
   absolute on-disk paths under `static/`. Missing images are
   replaced with an italic placeholder and listed in the report.
8. **Emits a build report** alongside the artefacts:
    - parts / chapters / sections counts
    - slide files filtered out
    - drafts skipped (with paths)
    - relref links handled
    - missing images
    - unknown shortcodes (with file references)

## Testing the skill

```sh
# Hand-authored design sample (skip-preprocess: true):
python3 .claude/skills/publish-ebook/scripts/build.py sample-ebook

# Real Hugo trees:
python3 .claude/skills/publish-ebook/scripts/build.py course-book
python3 .claude/skills/publish-ebook/scripts/build.py exercise-book

# Build everything in books.yaml at once:
python3 .claude/skills/publish-ebook/scripts/build.py all
```

After a build, inspect the report — anything in the *missing
images* or *unknown shortcodes* sections is a content issue worth
fixing in the source markdown rather than in the skill.

## Known limitations

- **Footnotes render as endnotes** at the end of the book. True
  page-bottom footnotes require Pandoc to emit inline notes via
  `float: footnote`; not enabled.
- **Marginalia** float on the right edge of the text column, not
  in the outer page margin. WeasyPrint's outer-margin floats clash
  with the footnote float machinery.
- **Cross-references between books** (e.g. exercise → course book)
  are not rewritten; they will appear as plain text or broken
  links. Within-book section anchors do work.
- **Hugo blockquote-style callouts** (`> i Concept Deep Dive`) are
  now rewritten into Pandoc fenced divs and pick up the structured
  Note / Tip / Warning / Concept callout boxes automatically. Authors
  can still write fenced divs directly when they want explicit control.
