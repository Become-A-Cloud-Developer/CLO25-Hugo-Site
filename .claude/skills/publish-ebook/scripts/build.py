#!/usr/bin/env python3
"""
publish-ebook · build orchestrator

Reads books.yaml at the project root, selects the requested book(s),
runs the preprocessor over each book's source tree, and produces an
EPUB and a PDF using Pandoc + WeasyPrint.

Usage
─────
    python3 scripts/build.py <book-id>      build a single book
    python3 scripts/build.py all            build every book in books.yaml
    python3 scripts/build.py --list         list known book ids and exit

The script is normally invoked via the publish-ebook skill or a thin
shell wrapper; running it directly works too.
"""

from __future__ import annotations

import argparse
import datetime
import html
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

import yaml

# ──────────────────────────────────────────────────────────────────────
# Paths
# ──────────────────────────────────────────────────────────────────────

SCRIPT_DIR = Path(__file__).resolve().parent
SKILL_DIR  = SCRIPT_DIR.parent
ASSETS     = SKILL_DIR / "assets"
PRINT_CSS  = ASSETS / "print.css"
EPUB_CSS   = ASSETS / "epub.css"
TEMPLATE   = ASSETS / "pandoc-html.template"

# Project root is the first ancestor that contains books.yaml.
def _find_project_root(start: Path) -> Path:
    for parent in [start, *start.parents]:
        if (parent / "books.yaml").is_file():
            return parent
    raise SystemExit("books.yaml not found in any ancestor of " + str(start))

PROJECT_ROOT = _find_project_root(SKILL_DIR)
BOOKS_YAML   = PROJECT_ROOT / "books.yaml"

# ──────────────────────────────────────────────────────────────────────
# Pandoc invocation
# ──────────────────────────────────────────────────────────────────────

PANDOC_FROM = (
    "markdown"
    "+fenced_divs"
    "+fenced_code_attributes"
    "+pipe_tables"
    "+yaml_metadata_block"
    "+footnotes"
    "+inline_notes"
    "+smart"
    "+raw_html"
    "+implicit_figures"
    "+table_captions"
    "+bracketed_spans"
)


def _ensure_tools() -> None:
    missing = [t for t in ("pandoc", "weasyprint") if shutil.which(t) is None]
    if missing:
        raise SystemExit(
            "missing required tools on PATH: " + ", ".join(missing)
            + "\n  install with:  brew install pandoc  &&  pipx install weasyprint"
        )


def _write_metadata_yaml(book: dict, target: Path, meta: dict) -> None:
    """Write the per-book Pandoc metadata file."""
    md = {
        "title":       book["title"],
        "subtitle":    book.get("subtitle", ""),
        "author":      book.get("author", ""),
        "language":    book.get("language", "en"),
        "rights":      book.get("rights", ""),
        "identifier": {
            "scheme": "uuid",
            "text":   f"publish-ebook-{book['id']}",
        },
        "description":   book.get("subtitle", ""),
        "toc":           True,
        "toc-depth":     2,
        "build-version": meta["build_version"],
        "build-date":    meta["build_date"],
    }
    target.write_text(yaml.safe_dump(md, sort_keys=False, allow_unicode=True))


QUIET = False


def _run(cmd: list[str], **kw) -> None:
    if not QUIET:
        print("  $ " + " ".join(str(c) for c in cmd))
    subprocess.run(cmd, check=True, **kw)


# ──────────────────────────────────────────────────────────────────────
# Cover image
# ──────────────────────────────────────────────────────────────────────

PALETTE_HEX = {"blue": "#2a6f8f", "red": "#a23a4f"}


def _split_title(title: str) -> tuple[str, str]:
    """Break a long title into two visually balanced lines for the cover."""
    if len(title) <= 22:
        return title, ""
    words = title.split()
    midpoint = len(title) // 2
    line1 = ""
    for w in words:
        if len(line1) + len(w) + 1 <= midpoint:
            line1 = (line1 + " " + w).strip()
        else:
            break
    if not line1:
        line1, line2 = title, ""
    else:
        line2 = title[len(line1):].strip()
    return line1, line2


def make_cover_svg(book: dict, build_version: str, tmp: Path) -> Path:
    template = (ASSETS / "cover.svg.template").read_text()
    line1, line2 = _split_title(book["title"])
    accent = PALETTE_HEX.get(book.get("palette", "blue"), "#2a6f8f")
    svg = (
        template
        .replace("{{TITLE_LINE_1}}", html.escape(line1))
        .replace("{{TITLE_LINE_2}}", html.escape(line2))
        .replace("{{SUBTITLE}}", html.escape(book.get("subtitle", "")))
        .replace("{{AUTHOR}}", html.escape(book.get("author", "")))
        .replace("{{BUILD_VERSION}}", html.escape(build_version))
        .replace("{{ACCENT}}", accent)
    )
    svg_path = tmp / "cover.svg"
    svg_path.write_text(svg, encoding="utf-8")
    return svg_path


# ──────────────────────────────────────────────────────────────────────
# Build version
# ──────────────────────────────────────────────────────────────────────

def build_meta() -> dict:
    try:
        sha = subprocess.check_output(
            ["git", "rev-parse", "--short", "HEAD"],
            cwd=PROJECT_ROOT,
            stderr=subprocess.DEVNULL,
        ).decode().strip()
    except Exception:
        sha = "uncommitted"
    today = datetime.date.today()
    return {
        "build_version": f"v{today:%Y.%m.%d}-{sha}",
        "build_date":    today.isoformat(),
    }


# ──────────────────────────────────────────────────────────────────────
# Build
# ──────────────────────────────────────────────────────────────────────

def build_book(book: dict, *, force: bool = False, strict: bool = False,
               check_only: bool = False) -> int:
    """Build one book.

    Returns an exit code:
      0  success (or up-to-date cache hit, or check-only with no warnings)
      1  built / preflighted with warnings under --strict
    """
    book_id = book["id"]
    src = (PROJECT_ROOT / book["source"]).resolve()
    out = (PROJECT_ROOT / book["output"]).resolve()
    out.mkdir(parents=True, exist_ok=True)

    meta = build_meta()

    if not src.is_dir():
        raise SystemExit(f"[{book_id}] source directory does not exist: {src}")

    print(f"\n=== Building '{book_id}' ===")
    if not QUIET:
        print(f"  source: {src}")
        print(f"  output: {out}")
        print(f"  palette: {book.get('palette', 'blue')}")
    print(f"  build:   {meta['build_version']}")

    # Cache check — skip the whole build if nothing changed.
    sys.path.insert(0, str(SCRIPT_DIR))
    from cache import compute_build_hash, cached, write_cache  # noqa: E402

    build_hash = compute_build_hash(book, PROJECT_ROOT, SCRIPT_DIR, ASSETS)
    if not check_only and not force and cached(out, build_hash):
        print(f"  ✓ up to date ({build_hash[:12]})")
        return 0

    # 1. Preprocess source tree → single concatenated markdown.
    from preprocessor import preprocess  # noqa: E402
    md_text, report = preprocess(
        src,
        skip_preprocess=bool(book.get("skip-preprocess", False)),
        relref_mode=book.get("relref-mode", "drop-link"),
        book=book,
        project_root=PROJECT_ROOT,
    )

    if check_only:
        report_path = out / f"{book_id}-build-report.txt"
        report_path.write_text(report.render(), encoding="utf-8")
        print(report.render())
        warnings = bool(report.unknown_shortcodes or report.missing_images
                        or any("parse error" in n.lower() for n in report.notes))
        return 1 if warnings else 0

    with tempfile.TemporaryDirectory(prefix=f"publish-ebook-{book_id}-") as tmp_s:
        tmp = Path(tmp_s)
        md_path  = tmp / "book.md"
        meta_yml = tmp / "metadata.yaml"

        md_path.write_text(md_text, encoding="utf-8")
        _write_metadata_yaml(book, meta_yml, meta)

        # Cover SVG (used for both PDF first page and EPUB cover image).
        cover_svg = make_cover_svg(book, meta["build_version"], tmp)

        # PDF cover include — a stand-alone HTML fragment that drops the
        # cover SVG on its own page before the rest of the body.
        cover_include = tmp / "cover-include.html"
        cover_include.write_text(
            f'<div class="cover-page">'
            f'  <img src="{cover_svg}" alt="cover"'
            f'       style="width:100%;height:auto;display:block;"/>'
            f'</div>\n',
            encoding="utf-8",
        )

        html_path = tmp / "book.html"
        epub_path = out / f"{book_id}.epub"
        pdf_path  = out / f"{book_id}.pdf"

        # 2. Markdown → HTML (intermediate for PDF).
        print("  → HTML (intermediate)")
        _run([
            "pandoc",
            "--from",          PANDOC_FROM,
            "--to",            "html5",
            "--standalone",
            "--template",      str(TEMPLATE),
            "--embed-resources",
            "--toc", "--toc-depth=2",
            "--metadata-file", str(meta_yml),
            "--css",           str(PRINT_CSS),
            "--syntax-highlighting=tango",
            "--include-before-body", str(cover_include),
            "--output",        str(html_path),
            str(md_path),
        ])

        # 3. HTML → PDF via WeasyPrint.
        print("  → PDF (WeasyPrint)")
        _run(["weasyprint", str(html_path), str(pdf_path)])

        # 4. Markdown → EPUB (separate Pandoc run).
        print("  → EPUB")
        _run([
            "pandoc",
            "--from",          PANDOC_FROM,
            "--to",            "epub3",
            "--toc", "--toc-depth=2",
            "--metadata-file", str(meta_yml),
            "--css",           str(EPUB_CSS),
            "--syntax-highlighting=tango",
            "--epub-cover-image", str(cover_svg),
            "--output",        str(epub_path),
            str(md_path),
        ])

    # 5. Write build report alongside the artefacts.
    report_path = out / f"{book_id}-build-report.txt"
    report_path.write_text(report.render(), encoding="utf-8")
    if not QUIET:
        print(report.render())

    write_cache(out, build_hash)

    print(f"  ✓ {epub_path.relative_to(PROJECT_ROOT)}")
    print(f"  ✓ {pdf_path.relative_to(PROJECT_ROOT)}")
    print(f"  ✓ {report_path.relative_to(PROJECT_ROOT)}")

    if strict and (report.unknown_shortcodes or report.missing_images):
        print(f"  ✗ strict mode: book has warnings — exit 1")
        return 1
    return 0


# ──────────────────────────────────────────────────────────────────────
# Entry
# ──────────────────────────────────────────────────────────────────────

_REQUIRED_BOOK_KEYS = {"id", "title", "author", "source", "output", "palette"}
_ALLOWED_PALETTES = {"blue", "red"}


def validate_books(books: list[dict]) -> list[str]:
    """Return a list of human-readable schema errors, empty if config is valid."""
    errors: list[str] = []
    seen_ids: set[str] = set()
    for i, b in enumerate(books):
        prefix = f"books[{i}] ({b.get('id', '?')})"
        missing = _REQUIRED_BOOK_KEYS - b.keys()
        if missing:
            errors.append(f"{prefix}: missing keys: {sorted(missing)}")
        palette = b.get("palette")
        if palette not in _ALLOWED_PALETTES:
            errors.append(
                f"{prefix}: palette must be one of {sorted(_ALLOWED_PALETTES)}, "
                f"got {palette!r}"
            )
        bid = b.get("id")
        if bid in seen_ids:
            errors.append(f"{prefix}: duplicate id {bid!r}")
        if bid is not None:
            seen_ids.add(bid)
    return errors


def _load_books() -> list[dict]:
    with BOOKS_YAML.open() as f:
        cfg = yaml.safe_load(f) or {}
    books = cfg.get("books", [])
    if not books:
        raise SystemExit(f"no books defined in {BOOKS_YAML}")
    errors = validate_books(books)
    if errors:
        sys.stderr.write(f"books.yaml schema errors in {BOOKS_YAML}:\n")
        for e in errors:
            sys.stderr.write(f"  - {e}\n")
        raise SystemExit(2)
    return books


def main() -> None:
    p = argparse.ArgumentParser(description="publish-ebook build orchestrator")
    p.add_argument("target", nargs="?",
                   help="book id, or 'all', or omit to use --list")
    p.add_argument("--list", action="store_true",
                   help="list available book ids from books.yaml")
    p.add_argument("--check", action="store_true",
                   help="run preflight (validate + preprocess) and exit "
                        "without writing PDF/EPUB; exits 1 on warnings")
    p.add_argument("--strict", action="store_true",
                   help="exit 1 if the build report contains unknown "
                        "shortcodes or missing images")
    p.add_argument("--quiet", action="store_true",
                   help="suppress per-command echo and report dump; "
                        "keep summary lines")
    p.add_argument("--force", action="store_true",
                   help="ignore cached build hash and rebuild")
    args = p.parse_args()

    global QUIET
    QUIET = args.quiet

    books = _load_books()
    by_id = {b["id"]: b for b in books}

    if args.list or (not args.target and not args.check):
        print(f"books defined in {BOOKS_YAML.relative_to(PROJECT_ROOT)}:")
        for b in books:
            print(f"  · {b['id']:<16} {b['title']}")
        return

    if not args.target:
        raise SystemExit("--check requires a target book id (or 'all')")

    if not args.check:
        _ensure_tools()

    targets = books if args.target == "all" else [by_id.get(args.target)]
    if targets[0] is None:
        raise SystemExit(
            f"unknown book id: {args.target}\n"
            f"  known ids: {', '.join(by_id)}"
        )

    rc = 0
    for b in targets:
        rc = max(rc, build_book(
            b,
            force=args.force,
            strict=args.strict,
            check_only=args.check,
        ))
    if rc != 0:
        sys.exit(rc)


if __name__ == "__main__":
    main()
