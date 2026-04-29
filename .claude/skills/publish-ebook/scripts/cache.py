"""
publish-ebook · build cache

A skinny content hash over a book's source tree plus the skill's own
scripts and assets. If the hash matches the value stored in
`<output>/.build-hash`, the next `build` invocation can short-circuit
and skip running pandoc/weasyprint.

What goes into the hash
───────────────────────
- The book's entry from books.yaml (title, source, palette, etc.).
- All `*.py` files under the skill's `scripts/` dir.
- Every regular file under the skill's `assets/` dir.
- All `*.md` files under the book's source tree, including their
  on-disk relative paths so a rename invalidates the hash.

What is excluded
────────────────
- `.pyc` and `__pycache__/` — those derive from the .py files.
- The output dir itself.
"""

from __future__ import annotations

import hashlib
import json
from pathlib import Path


def compute_build_hash(
    book: dict,
    project_root: Path,
    script_dir: Path,
    assets_dir: Path,
) -> str:
    h = hashlib.sha256()

    # 1. Book config — the YAML entry exactly as parsed.
    h.update(json.dumps(book, sort_keys=True, default=str).encode())

    # 2. Skill scripts (.py only).
    for f in sorted(script_dir.glob("*.py")):
        h.update(b"|script|")
        h.update(f.name.encode())
        h.update(f.read_bytes())

    # 3. Assets — every regular file (CSS, templates, fonts, etc.).
    for f in sorted(assets_dir.iterdir()):
        if f.is_file():
            h.update(b"|asset|")
            h.update(f.name.encode())
            h.update(f.read_bytes())

    # 4. Source tree.
    src = project_root / book["source"]
    if src.is_dir():
        for f in sorted(src.rglob("*.md")):
            h.update(b"|src|")
            h.update(str(f.relative_to(src)).encode())
            h.update(f.read_bytes())

    return h.hexdigest()


def cache_path(out_dir: Path) -> Path:
    return out_dir / ".build-hash"


def cached(out_dir: Path, build_hash: str) -> bool:
    p = cache_path(out_dir)
    if not p.exists():
        return False
    return p.read_text().strip() == build_hash


def write_cache(out_dir: Path, build_hash: str) -> None:
    cache_path(out_dir).write_text(build_hash + "\n")
