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


def _write_metadata_yaml(book: dict, target: Path) -> None:
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
        "description": book.get("subtitle", ""),
        "toc":         True,
        "toc-depth":   2,
    }
    target.write_text(yaml.safe_dump(md, sort_keys=False, allow_unicode=True))


def _run(cmd: list[str], **kw) -> None:
    print("  $ " + " ".join(str(c) for c in cmd))
    subprocess.run(cmd, check=True, **kw)


# ──────────────────────────────────────────────────────────────────────
# Build
# ──────────────────────────────────────────────────────────────────────

def build_book(book: dict) -> None:
    book_id = book["id"]
    src = (PROJECT_ROOT / book["source"]).resolve()
    out = (PROJECT_ROOT / book["output"]).resolve()
    out.mkdir(parents=True, exist_ok=True)

    if not src.is_dir():
        raise SystemExit(f"[{book_id}] source directory does not exist: {src}")

    print(f"\n=== Building '{book_id}' ===")
    print(f"  source: {src}")
    print(f"  output: {out}")
    print(f"  palette: {book.get('palette', 'blue')}")

    # 1. Preprocess source tree → single concatenated markdown.
    sys.path.insert(0, str(SCRIPT_DIR))
    from preprocessor import preprocess  # noqa: E402
    md_text, report = preprocess(
        src,
        skip_preprocess=bool(book.get("skip-preprocess", False)),
        relref_mode=book.get("relref-mode", "drop-link"),
        book=book,
        project_root=PROJECT_ROOT,
    )

    with tempfile.TemporaryDirectory(prefix=f"publish-ebook-{book_id}-") as tmp_s:
        tmp = Path(tmp_s)
        md_path  = tmp / "book.md"
        meta_yml = tmp / "metadata.yaml"

        md_path.write_text(md_text, encoding="utf-8")
        _write_metadata_yaml(book, meta_yml)

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
            "--highlight-style=tango",
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
            "--highlight-style=tango",
            "--output",        str(epub_path),
            str(md_path),
        ])

    # 5. Write build report alongside the artefacts.
    report_path = out / f"{book_id}-build-report.txt"
    report_path.write_text(report.render(), encoding="utf-8")
    print(report.render())

    print(f"  ✓ {epub_path.relative_to(PROJECT_ROOT)}")
    print(f"  ✓ {pdf_path.relative_to(PROJECT_ROOT)}")
    print(f"  ✓ {report_path.relative_to(PROJECT_ROOT)}")


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
    args = p.parse_args()

    books = _load_books()
    by_id = {b["id"]: b for b in books}

    if args.list or not args.target:
        print(f"books defined in {BOOKS_YAML.relative_to(PROJECT_ROOT)}:")
        for b in books:
            print(f"  · {b['id']:<16} {b['title']}")
        return

    _ensure_tools()

    if args.target == "all":
        for b in books:
            build_book(b)
        return

    if args.target not in by_id:
        raise SystemExit(
            f"unknown book id: {args.target}\n"
            f"  known ids: {', '.join(by_id)}"
        )
    build_book(by_id[args.target])


if __name__ == "__main__":
    main()
