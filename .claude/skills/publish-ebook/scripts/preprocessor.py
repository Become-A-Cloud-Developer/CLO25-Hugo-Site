"""
publish-ebook · preprocessor (v2)

Walks a Hugo content tree and produces a single concatenated markdown
document fit for Pandoc, with Hugo front matter stripped, shortcodes
expanded or dropped, and chapter / part scaffolding emitted as fenced
divs that match the publish-ebook CSS.

Hierarchy
─────────
- parts: true   →  source/<part>/<chapter>/<section>.md
- parts: false  →  source/<chapter>/<section>.md

Source-file roles
─────────────────
- _index.md            metadata + (sometimes) intro body for a Part or Chapter
- *-slides.md          presentation files; skipped via type="slide"/hidden=true
- *.md (other)         section content; concatenated under the chapter

Filtering
─────────
A file is skipped if its front matter sets any of:
- draft = true
- hidden = true
- type = "slide"
"""

from __future__ import annotations

import re
import tomllib
from dataclasses import dataclass, field
from pathlib import Path

import yaml


# ──────────────────────────────────────────────────────────────────────
# Front matter
# ──────────────────────────────────────────────────────────────────────

_FM_TOML = re.compile(r"\A\+\+\+\s*\n(.*?)\n\+\+\+\s*\n?", re.DOTALL)
_FM_YAML = re.compile(r"\A---\s*\n(.*?)\n---\s*\n?", re.DOTALL)


@dataclass
class FrontMatter:
    title: str
    weight: int = 0
    draft: bool = False
    hidden: bool = False
    type: str = "page"
    raw: dict = field(default_factory=dict)

    @property
    def skip(self) -> bool:
        return self.draft or self.hidden or self.type == "slide"


def _title_from_path(path: Path) -> str:
    name = path.stem.lstrip("0123456789-")
    return name.replace("-", " ").replace("_", " ").title()


def parse_frontmatter(text: str, path: Path) -> tuple[FrontMatter, str]:
    """Return (FrontMatter, body) — supports TOML and YAML delimiters."""
    if text.startswith("+++"):
        m = _FM_TOML.match(text)
        if not m:
            raise ValueError(f"{path}: malformed TOML front matter")
        try:
            fm = tomllib.loads(m.group(1))
        except tomllib.TOMLDecodeError as e:
            raise ValueError(f"{path}: TOML parse error: {e}")
        body = text[m.end():]
    elif text.startswith("---"):
        m = _FM_YAML.match(text)
        if not m:
            raise ValueError(f"{path}: malformed YAML front matter")
        fm = yaml.safe_load(m.group(1)) or {}
        body = text[m.end():]
    else:
        fm = {}
        body = text

    return FrontMatter(
        title=fm.get("title", _title_from_path(path)),
        weight=int(fm.get("weight", 0)),
        draft=bool(fm.get("draft", False)),
        hidden=bool(fm.get("hidden", False)),
        type=fm.get("type", "page"),
        raw=fm,
    ), body


def read_index(dir_path: Path) -> tuple[FrontMatter, str]:
    """Read `_index.md` for a directory; return (front-matter, body)."""
    idx = dir_path / "_index.md"
    if not idx.exists():
        return FrontMatter(title=_title_from_path(dir_path)), ""
    return parse_frontmatter(idx.read_text(encoding="utf-8"), idx)


# ──────────────────────────────────────────────────────────────────────
# Shortcodes
# ──────────────────────────────────────────────────────────────────────

# Pandoc-style relref *inside* a markdown link: [text]({{< relref "x.md" >}})
_RELREF_LINK_RE = re.compile(
    r"""\[(?P<text>[^\]]+)\]\(\s*\{\{<\s*relref\s+"(?P<target>[^"]+)"\s*>\}\}\s*\)"""
)

# Any other Hugo shortcode {{< name args >}} or {{< name args /}}
_SHORTCODE_RE = re.compile(r"\{\{<\s*(?P<name>[a-zA-Z][\w-]*)[^>}]*?/?\s*>\}\}")

# Markdown image: ![alt](url) — captures both Hugo-absolute paths and
# external/relative URLs so we can inspect each one and decide.
_IMAGE_RE = re.compile(
    r"!\[(?P<alt>[^\]]*)\]\((?P<url>[^)\s]+)\)"
)


def rewrite_image_paths(body: str, project_root: Path,
                        report: "BuildReport") -> str:
    """
    Hugo treats markdown URLs that begin with `/` as rooted at the
    site's `static/` directory. Rewrite each such image URL to the
    on-disk absolute path so Pandoc's --embed-resources can inline it.

    If a referenced image does not exist on disk, replace the image
    syntax with an italic placeholder so the build doesn't fail, and
    record the miss in the build report.
    """
    static_dir = project_root / "static"

    def _replace(m: re.Match) -> str:
        alt = m.group("alt")
        url = m.group("url")
        # External URLs already have a scheme — leave alone.
        if "://" in url:
            return m.group(0)
        # Hugo-absolute → on-disk under static/
        if url.startswith("/"):
            on_disk = (static_dir / url.lstrip("/")).resolve()
            if on_disk.exists():
                return f"![{alt}]({on_disk})"
            report.missing_images.append(url)
            label = alt or url
            return f"*[image: {label} — not found]*"
        # Relative path — leave as-is; Pandoc will resolve relative to cwd
        return m.group(0)

    return _IMAGE_RE.sub(_replace, body)


def handle_shortcodes(body: str, *, relref_mode: str, report: "BuildReport",
                      path: Path) -> str:
    """
    Resolve the two shortcodes used in this codebase:
      {{< children ... />}}    drop entirely (it is nav, not content)
      {{< relref "x.md" >}}    when wrapping a markdown link, handle by mode:
                                 drop-link  → keep text only
                                 footnote   → text + numbered footnote
                                 keep       → leave the URL in place

    Any other shortcode encountered is dropped and noted in the build
    report so the next iteration can decide what to do with it.
    """
    # 1. Markdown links wrapped in relref → handle per relref-mode.
    def _relref_link(m: re.Match) -> str:
        text, target = m.group("text"), m.group("target")
        report.relref_handled += 1
        if relref_mode == "drop-link":
            return text
        if relref_mode == "keep":
            return f"[{text}]({target})"
        if relref_mode == "footnote":
            return f"{text}[^{_anchor(target)}]"
        raise ValueError(f"unknown relref-mode: {relref_mode}")

    body = _RELREF_LINK_RE.sub(_relref_link, body)

    # 2. Remaining bare shortcodes.
    def _shortcode(m: re.Match) -> str:
        name = m.group("name")
        if name == "children":
            return ""
        if name == "relref":
            # Bare relref outside a link — drop, but warn.
            report.notes.append(f"{path}: bare {{{{< relref >}}}} dropped")
            return ""
        report.unknown_shortcodes.add(f"{name} (in {path.name})")
        return ""

    return _SHORTCODE_RE.sub(_shortcode, body)


def _anchor(s: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", s.lower()).strip("-")


# ──────────────────────────────────────────────────────────────────────
# Blockquote → callout conversion
# ──────────────────────────────────────────────────────────────────────

# Patterns are matched against the first non-blank line of a blockquote
# block (after stripping the leading `> `). Each entry is:
#   (regex, callout-class, leading-icon-chars-to-strip)
# The icon (ℹ / ⚠ / ✓) is removed before re-emission so CSS coloured
# bars render cleanly without doubled-up Unicode glyphs.
# Patterns are matched against the first non-blank line of a blockquote
# block (after stripping the leading `> `). Each entry is:
#   (regex, callout-class, leading-icon-chars-to-strip)
# `\*{0,2}` keeps the bold markers optional so prose written either as
# `**Concept Deep Dive**` or `Concept Deep Dive:` matches.
_CALLOUT_PATTERNS = [
    (re.compile(r"^\s*[ℹi]\s*\*{0,2}\s*(?:Concept|Note)\b", re.I),
        "callout-concept", "ℹi"),
    (re.compile(r"^\s*\*{0,2}\s*Concept\b", re.I),
        "callout-concept", ""),
    (re.compile(r"^\s*\*{0,2}\s*Key takeaway[:\.\*]", re.I),
        "callout-tip", ""),
    (re.compile(r"^\s*[⚠️]+\s*\*{0,2}\s*(?:Warning|Common Mistakes|Gotcha|Caveat)\b", re.I),
        "callout-warning", "⚠️"),
    (re.compile(r"^\s*\*{0,2}\s*(?:Warning|Common Mistakes|Gotcha|Caveat)\b", re.I),
        "callout-warning", ""),
    (re.compile(r"^\s*✓\s*\*{0,2}\s*(?:Quick check|Verify|Checkpoint|Tip)\b", re.I),
        "callout-tip", "✓"),
    (re.compile(r"^\s*✦?\s*\*{0,2}\s*Tip\b", re.I),
        "callout-tip", ""),
    (re.compile(r"^\s*\*{0,2}\s*(?:What you[’']?ll learn|Learning objectives|Objectives)\b", re.I),
        "exercise-overview", ""),
    (re.compile(r"^\s*\*{0,2}\s*(?:Before starting|Prerequisites|Requirements)\b", re.I),
        "callout-prereq", ""),
    (re.compile(r"^\s*📝?\s*\*{0,2}\s*Note\b", re.I),
        "callout-note", ""),
]


def convert_blockquote_callouts(body: str) -> str:
    """Walk markdown line by line; group consecutive `> ` lines into
    blockquote blocks. If the block's first non-empty content line
    matches a callout pattern, rewrite the block as a fenced div
    (Pandoc syntax) so it lands on the same CSS path as native callouts.
    Lines inside fenced code (``` / ~~~) are left alone."""
    lines = body.splitlines(keepends=True)
    out: list[str] = []
    i = 0
    in_fence = False
    while i < len(lines):
        stripped = lines[i].lstrip()
        if stripped.startswith("```") or stripped.startswith("~~~"):
            in_fence = not in_fence
            out.append(lines[i])
            i += 1
            continue
        if in_fence or not lines[i].lstrip().startswith(">"):
            out.append(lines[i])
            i += 1
            continue

        # Collect a contiguous blockquote block (allow blank lines
        # between quoted lines as long as the next non-blank line is
        # also a quote line).
        block: list[str] = []
        j = i
        while j < len(lines):
            ln = lines[j]
            if ln.lstrip().startswith(">"):
                block.append(ln)
                j += 1
                continue
            if block and ln.strip() == "":
                # Lookahead: is the next non-blank line a quote line?
                k = j + 1
                while k < len(lines) and lines[k].strip() == "":
                    k += 1
                if k < len(lines) and lines[k].lstrip().startswith(">"):
                    block.append(ln)
                    j += 1
                    continue
            break
        # Strip trailing blanks from the captured block.
        while block and block[-1].strip() == "":
            block.pop()

        # Strip the leading `>` (and one optional space) from each line
        # to recover the inner content.
        inner_lines = []
        for ln in block:
            stripped_ln = ln.lstrip()
            if stripped_ln.startswith(">"):
                rest = stripped_ln[1:]
                if rest.startswith(" "):
                    rest = rest[1:]
                inner_lines.append(rest)
            else:
                inner_lines.append(ln)
        first = next((s.rstrip("\n") for s in inner_lines if s.strip()), "")
        match = next(
            ((pat, k, strip) for pat, k, strip in _CALLOUT_PATTERNS
             if pat.match(first)),
            None,
        )
        if match:
            _, klass, strip_chars = match
            # Drop a single leading icon glyph from the first content
            # line (e.g. ℹ, ⚠, ✓) so rendered output is not double-iconed.
            if strip_chars:
                for idx, ln in enumerate(inner_lines):
                    if ln.strip():
                        new = re.sub(
                            rf"^(\s*)[{strip_chars}]\s*",
                            r"\1",
                            ln,
                            count=1,
                        )
                        inner_lines[idx] = new
                        break
            out.append(f"::: {{.{klass}}}\n")
            out.extend(inner_lines)
            if not (inner_lines and inner_lines[-1].endswith("\n")):
                out.append("\n")
            out.append(":::\n\n")
        else:
            out.extend(block)
            out.append("\n")
        i = j
    return "".join(out)


# ──────────────────────────────────────────────────────────────────────
# Chapter-title normalisation
# ──────────────────────────────────────────────────────────────────────

_NUMERIC_PREFIX = re.compile(r"^\d+\.\s+(?=[A-Za-z])")


def normalize_chapter_title(raw: str) -> str:
    """Strip a leading '1. ' / '2. ' from a chapter title — the book
    auto-numbers chapters, so the manual prefix is redundant in print.
    Falls back to the original string if the result would be too short
    to be a useful title."""
    candidate = _NUMERIC_PREFIX.sub("", raw.strip(), count=1)
    return candidate if len(candidate) >= 4 else raw.strip()


# ──────────────────────────────────────────────────────────────────────
# Single-section detection
# ──────────────────────────────────────────────────────────────────────

_SHORTCODE_ONLY = re.compile(r"\{\{<.*?>\}\}", re.DOTALL)


def is_chapter_index_effectively_empty(idx_body: str) -> bool:
    """A chapter `_index.md` body that is just shortcodes plus an
    optional title heading should be treated as empty for layout
    purposes: the chapter's real content lives in the section file."""
    s = _SHORTCODE_ONLY.sub("", idx_body)
    s = re.sub(r"^\s*#.*$", "", s, flags=re.MULTILINE)
    return len(s.strip()) < 80


# ──────────────────────────────────────────────────────────────────────
# Heading manipulation
# ──────────────────────────────────────────────────────────────────────

# Match ATX headings (start of line, 1–6 #s, then space) but skip lines
# inside fenced code blocks. We do that by tracking ``` state.

def shift_headings(body: str, by: int) -> str:
    """Demote ATX headings outside fenced code blocks by `by` levels."""
    lines = body.splitlines(keepends=True)
    in_fence = False
    out = []
    for line in lines:
        stripped = line.lstrip()
        if stripped.startswith("```") or stripped.startswith("~~~"):
            in_fence = not in_fence
            out.append(line)
            continue
        if in_fence:
            out.append(line)
            continue
        m = re.match(r"^(#{1,6}) ", line)
        if m:
            new_level = min(len(m.group(1)) + by, 6)
            line = "#" * new_level + line[m.end(1):]
        out.append(line)
    return "".join(out)


def strip_leading_h1_matching_title(body: str, title: str) -> str:
    """
    Hugo Part `_index.md` files often repeat the title as an H1 in the
    body. If we are about to wrap that body with our own title heading,
    strip the leading `# {title}` so it doesn't appear twice.
    """
    pattern = rf"^\s*#\s+{re.escape(title)}\s*\n+"
    return re.sub(pattern, "", body, count=1)


# ──────────────────────────────────────────────────────────────────────
# Roman numerals & Part-title parsing
# ──────────────────────────────────────────────────────────────────────

_ROMAN = [(1000,"M"),(900,"CM"),(500,"D"),(400,"CD"),
          (100,"C"),(90,"XC"),(50,"L"),(40,"XL"),
          (10,"X"),(9,"IX"),(5,"V"),(4,"IV"),(1,"I")]

def to_roman(n: int) -> str:
    parts: list[str] = []
    for v, s in _ROMAN:
        while n >= v:
            parts.append(s)
            n -= v
    return "".join(parts) or "I"


_PART_TITLE_RE = re.compile(
    r"^Part\s+([IVXLCDM]+)\s*[—–-]\s*(.+)$", re.IGNORECASE
)

def parse_part_title(raw: str, fallback_idx: int) -> tuple[str, str]:
    """
    'Part I — Cloud Foundations' → ('I', 'Cloud Foundations').
    Anything that doesn't match falls back to (roman(idx), raw).
    """
    m = _PART_TITLE_RE.match(raw.strip())
    if m:
        return m.group(1).upper(), m.group(2).strip()
    return to_roman(fallback_idx), raw.strip()


# ──────────────────────────────────────────────────────────────────────
# Build report
# ──────────────────────────────────────────────────────────────────────

@dataclass
class BuildReport:
    parts: int = 0
    chapters: int = 0
    sections: int = 0
    drafts_skipped: list[str] = field(default_factory=list)
    slides_skipped: int = 0
    relref_handled: int = 0
    unknown_shortcodes: set = field(default_factory=set)
    missing_images: list[str] = field(default_factory=list)
    notes: list[str] = field(default_factory=list)

    def render(self) -> str:
        lines = ["", "Build report", "─" * 40,
                 f"  Parts        : {self.parts}",
                 f"  Chapters     : {self.chapters}",
                 f"  Sections     : {self.sections}",
                 f"  Slides hidden: {self.slides_skipped}",
                 f"  Relref links : {self.relref_handled}"]
        if self.drafts_skipped:
            lines.append(f"  Drafts skipped: {len(self.drafts_skipped)}")
            for p in self.drafts_skipped:
                lines.append(f"    · {p}")
        if self.unknown_shortcodes:
            lines.append("  ⚠ Unknown shortcodes (dropped):")
            for s in sorted(self.unknown_shortcodes):
                lines.append(f"    · {s}")
        if self.missing_images:
            lines.append(f"  ⚠ Missing images: {len(self.missing_images)}")
            for url in sorted(set(self.missing_images)):
                lines.append(f"    · {url}")
        if self.notes:
            lines.append("  Notes:")
            for n in self.notes:
                lines.append(f"    · {n}")
        return "\n".join(lines) + "\n"


# ──────────────────────────────────────────────────────────────────────
# Listing helpers
# ──────────────────────────────────────────────────────────────────────

def list_subdirs_by_weight(parent: Path) -> list[tuple[FrontMatter, Path]]:
    out = []
    for d in parent.iterdir():
        if not d.is_dir():
            continue
        fm, _ = read_index(d)
        out.append((fm, d))
    return sorted(out, key=lambda x: (x[0].weight, x[1].name))


def list_section_files(chapter_dir: Path,
                       report: BuildReport) -> list[tuple[FrontMatter, Path, str]]:
    """Return (front-matter, path, body) for non-skipped section files,
    sorted by weight."""
    out = []
    for f in sorted(chapter_dir.glob("*.md")):
        if f.name == "_index.md":
            continue
        try:
            fm, body = parse_frontmatter(f.read_text(encoding="utf-8"), f)
        except ValueError as e:
            report.notes.append(str(e))
            continue
        if fm.draft:
            report.drafts_skipped.append(str(f))
            continue
        if fm.hidden or fm.type == "slide":
            report.slides_skipped += 1
            continue
        out.append((fm, f, body))
    return sorted(out, key=lambda x: (x[0].weight, x[1].name))


# ──────────────────────────────────────────────────────────────────────
# Renderers
# ──────────────────────────────────────────────────────────────────────

def render_part_divider(*, part_roman: str, part_topic: str,
                        part_body: str) -> str:
    body = part_body.strip()
    body_block = f"\n\n::: {{.part-body}}\n{body}\n:::\n" if body else ""
    return (
        "::: {.part-divider}\n"
        f"::: {{.part-eyebrow}}\nPart {part_roman}\n:::\n\n"
        f"# {part_topic} {{.part-title .unnumbered .unlisted}}"
        f"{body_block}\n"
        ":::\n\n"
    )


def render_chapter_opener(*, book: dict, part_roman: str | None,
                          part_topic: str | None,
                          chapter_num: int, chapter_title: str) -> str:
    is_exercise = book.get("palette") == "red"
    eyebrow_prefix = book.get("eyebrow-prefix", "Part")
    chapter_prefix = book.get("chapter-prefix", "Chapter")

    if part_roman is not None:
        eyebrow = f"{eyebrow_prefix} {part_roman} · {part_topic}"
    else:
        eyebrow = f"{eyebrow_prefix} {chapter_num}"

    eyebrow_classes = "chapter-eyebrow"
    title_classes = "chapter-title"
    if is_exercise:
        eyebrow_classes += " exercise-eyebrow"
        title_classes += " exercise-title"

    chap_id = f"ch-{_anchor(chapter_title)}"

    return (
        f"::: {{.{eyebrow_classes.replace(' ', ' .')}}}\n"
        f"{eyebrow}\n"
        ":::\n\n"
        "::: {.chapter-number}\n"
        f"{chapter_prefix} {chapter_num}\n"
        ":::\n\n"
        f"# {chapter_title} {{#{chap_id} .{title_classes.replace(' ', ' .')}}}\n\n"
    )


def render_section(section_fm: FrontMatter, body: str) -> str:
    body = shift_headings(body, by=1).strip()
    sec_id = f"sec-{_anchor(section_fm.title)}"
    return f"## {section_fm.title} {{#{sec_id}}}\n\n{body}\n\n"


# ──────────────────────────────────────────────────────────────────────
# Main entry
# ──────────────────────────────────────────────────────────────────────

def _passthrough(source: Path) -> str:
    files = sorted(p for p in source.rglob("*.md") if p.name != "_index.md")
    if not files:
        raise RuntimeError(f"no markdown files found under {source}")
    chunks = [f.read_text(encoding="utf-8") for f in files]
    return "\n\n".join(chunks) + "\n"


def preprocess(source: Path, *, skip_preprocess: bool, relref_mode: str,
               book: dict | None = None,
               project_root: Path | None = None) -> tuple[str, BuildReport]:
    """
    Walk `source` and return (concatenated_markdown, report).

    If `skip_preprocess` is set, the function bypasses Hugo handling
    entirely and returns a simple lexical concatenation. Used for
    hand-authored sources like the design-sample volume.
    """
    if skip_preprocess:
        return _passthrough(source), BuildReport()

    if book is None:
        raise ValueError("book config required when skip_preprocess is False")

    report = BuildReport()
    parts_mode = bool(book.get("parts", False))
    chapter_num = 0
    out: list[str] = []

    def _handle_chapter(chapter_dir: Path, *,
                        part_roman: str | None, part_topic: str | None) -> None:
        nonlocal chapter_num
        chapter_fm, _ = read_index(chapter_dir)
        if chapter_fm.draft or chapter_fm.hidden:
            report.drafts_skipped.append(str(chapter_dir))
            return

        sections = list_section_files(chapter_dir, report)
        if not sections:
            report.notes.append(f"empty chapter: {chapter_dir}")
            return

        has_index = (chapter_dir / "_index.md").exists()
        _, idx_body = read_index(chapter_dir) if has_index else (None, "")
        single_section = (
            len(sections) == 1
            and (not has_index or is_chapter_index_effectively_empty(idx_body))
        )

        chapter_num += 1
        report.chapters += 1

        if single_section:
            # Chapter directory exists only to group a single content file
            # with its slide siblings. Use the section's title as the
            # chapter title and emit its body directly — its H2s become
            # the chapter's top-level subsections.
            sole_fm, sole_path, sole_body = sections[0]
            out.append(render_chapter_opener(
                book=book,
                part_roman=part_roman,
                part_topic=part_topic,
                chapter_num=chapter_num,
                chapter_title=normalize_chapter_title(sole_fm.title),
            ))
            body = convert_blockquote_callouts(sole_body)
            body = handle_shortcodes(
                body, relref_mode=relref_mode, report=report,
                path=sole_path,
            )
            if project_root is not None:
                body = rewrite_image_paths(body, project_root, report)
            out.append(body.strip() + "\n\n")
            report.sections += 1
            return

        # Multi-section chapter. Title comes from _index.md (or, as a
        # fallback, the directory name). Each section gets its own H2.
        out.append(render_chapter_opener(
            book=book,
            part_roman=part_roman,
            part_topic=part_topic,
            chapter_num=chapter_num,
            chapter_title=normalize_chapter_title(chapter_fm.title),
        ))
        for fm, path, body in sections:
            body = convert_blockquote_callouts(body)
            body = handle_shortcodes(
                body, relref_mode=relref_mode, report=report, path=path,
            )
            if project_root is not None:
                body = rewrite_image_paths(body, project_root, report)
            out.append(render_section(fm, body))
            report.sections += 1

    if parts_mode:
        for part_idx, (part_fm, part_dir) in enumerate(
                list_subdirs_by_weight(source), start=1):
            if part_fm.draft or part_fm.hidden:
                report.drafts_skipped.append(str(part_dir))
                continue
            report.parts += 1

            part_roman, part_topic = parse_part_title(part_fm.title, part_idx)
            _, part_body = read_index(part_dir)
            part_body = strip_leading_h1_matching_title(part_body, part_fm.title)
            part_body = handle_shortcodes(
                part_body, relref_mode=relref_mode, report=report,
                path=part_dir / "_index.md",
            )
            out.append(render_part_divider(
                part_roman=part_roman,
                part_topic=part_topic,
                part_body=part_body,
            ))

            for _, chapter_dir in list_subdirs_by_weight(part_dir):
                _handle_chapter(
                    chapter_dir,
                    part_roman=part_roman,
                    part_topic=part_topic,
                )
    else:
        for _, chapter_dir in list_subdirs_by_weight(source):
            _handle_chapter(chapter_dir, part_roman=None, part_topic=None)

    return "\n".join(out) + "\n", report
