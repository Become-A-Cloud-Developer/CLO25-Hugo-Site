---
name: markdown-to-pdf
description: Render a single markdown file to a polished PDF using a CLO25 visual design (A4, IBM Plex typography, accent rule, tinted callouts, page numbers). Trigger whenever the user asks to "make a PDF", "PDF this file", "convert markdown to PDF", "export this to PDF", or any request to produce a PDF from a single .md source — including assignments, reports, notes, handouts, or one-off documents. Also trigger when the user references a markdown file and says the output is "ugly" or "looks bad" and wants it styled. Skip when the user wants a multi-chapter book with TOC and chapters (use publish-ebook instead) or slides (use revealjs-skill).
---

# markdown-to-pdf

Render a single markdown file to a clean, modern PDF. Pipeline is Pandoc + WeasyPrint with a bundled stylesheet that gives the document a textbook-style look without setup.

## When to use

- Single-document PDFs: assignments, handouts, reports, briefs, technical notes, meeting notes, one-off articles.
- A previous PDF of the same content was generated with default Pandoc styling and the user wants it to look better.

## When *not* to use

- A multi-chapter book with a TOC, drop caps, chapter rotation — use the project's `publish-ebook` skill.
- Slides — use `revealjs-skill`.
- Plain-text or LaTeX-only requests where styling is not the point.

## How it works

The skill bundles one CSS file (`assets/style.css`) that defines the look. The build is a single Pandoc invocation that points WeasyPrint at this stylesheet.

### Required tools

- `pandoc` (≥ 3.0) on PATH
- `weasyprint` on PATH

Both are already installed in the CLO25 environment. If a future system is missing them, install via Homebrew (`brew install pandoc`) and pipx (`pipx install weasyprint`).

### Default conventions

- **Output path:** `<input>.pdf` next to the source, unless the user specifies otherwise.
- **Language:** detect from content. If the source contains common Swedish words (`och`, `att`, `är`, `på`, `inte`, `som`) or has a Swedish title, pass `--metadata lang=sv`. Otherwise pass `lang=en`. Hyphenation rules differ — getting this right matters.
- **First H1 becomes the running header** (via CSS `string-set: doctitle content()`). The first page suppresses the header so the title block sits clean at the top.

### The build command

Run from anywhere; both paths can be relative or absolute. Resolve `<skill_root>` to the absolute path of this skill's directory (the directory containing this `SKILL.md`).

```bash
pandoc <input.md> \
  -o <output.pdf> \
  --pdf-engine=weasyprint \
  --css=<skill_root>/assets/style.css \
  --metadata lang=<sv|en> \
  --standalone
```

WeasyPrint will print a few CSS warnings to stderr (about responsive media queries and vendor properties in pandoc's auto-injected HTML). They're harmless — the output renders correctly. Don't surface them to the user unless the build actually fails.

### After the build

Report the output path and file size to the user. On macOS, offer to open it with `open <output.pdf>` if that fits the conversation.

## Customizing the look

The bundled stylesheet is intentionally a single self-contained file so it's easy to fork. If a user wants a tweak (different accent colour, A5 instead of A4, no justification, etc.), edit `assets/style.css` directly. Common knobs:

- **Page size and margins** — `@page { size: ...; margin: ...; }`
- **Accent colour** — `--accent` and `--accent-deep` in `:root`
- **Body font size** — `html, body { font-size: ...; }`
- **Justification on/off** — `text-align: justify` on `body`

If the user wants project-specific variants (e.g., "make a 'memo' style"), keep the variants as additional CSS files in `assets/` and let the user pick by name.

## Known limitations

- WeasyPrint does not render Mermaid or other JS-based diagrams. Pre-render those to SVG/PNG before the build.
- `@import` of Google Fonts requires network access during the build. If running offline, swap the `@import` line for locally bundled fonts and `@font-face` declarations.
- The first H1 in the document drives the running header. If the document has no H1, the header simply stays empty — that's fine.
