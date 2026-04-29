"""
Unit tests for publish-ebook/scripts/preprocessor.py.

Designed to run with the Python stdlib unittest runner — no pytest.
Run via:  bash tests/run.sh
"""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
SKILL_DIR = HERE.parent
SCRIPTS = SKILL_DIR / "scripts"
FIXTURES = HERE / "fixtures"

sys.path.insert(0, str(SCRIPTS))

import preprocessor  # noqa: E402
from preprocessor import (  # noqa: E402
    BuildReport,
    build_anchor_index,
    convert_blockquote_callouts,
    handle_shortcodes,
    is_chapter_index_effectively_empty,
    normalize_chapter_title,
    parse_frontmatter,
    parse_part_title,
    preprocess,
    rewrite_image_paths,
    rewrite_intrabook_links,
    shift_headings,
    strip_leading_h1_matching_title,
    to_roman,
)
from build import (  # noqa: E402
    _split_title,
    make_cover_svg,
    validate_books,
)
from cache import compute_build_hash  # noqa: E402


# ──────────────────────────────────────────────────────────────────────
# Front matter parsing
# ──────────────────────────────────────────────────────────────────────

class FrontMatterTests(unittest.TestCase):
    def test_parse_toml_frontmatter(self):
        text = '+++\ntitle = "Hello"\nweight = 3\n+++\nbody\n'
        fm, body = parse_frontmatter(text, Path("x.md"))
        self.assertEqual(fm.title, "Hello")
        self.assertEqual(fm.weight, 3)
        self.assertEqual(body, "body\n")

    def test_parse_yaml_frontmatter(self):
        text = '---\ntitle: Hello\nweight: 2\n---\nbody\n'
        fm, body = parse_frontmatter(text, Path("x.md"))
        self.assertEqual(fm.title, "Hello")
        self.assertEqual(fm.weight, 2)
        self.assertEqual(body, "body\n")

    def test_no_frontmatter_falls_back_to_filename(self):
        text = "just some body"
        fm, body = parse_frontmatter(text, Path("01-hello-world.md"))
        self.assertEqual(fm.title, "Hello World")
        self.assertEqual(body, text)


# ──────────────────────────────────────────────────────────────────────
# Heading manipulation
# ──────────────────────────────────────────────────────────────────────

class HeadingTests(unittest.TestCase):
    def test_shift_headings_skips_fenced_code(self):
        body = "# Top\n```\n# not a heading\n```\n## Real\n"
        out = shift_headings(body, by=1)
        self.assertIn("## Top", out)
        self.assertIn("### Real", out)
        # Untouched inside fence
        self.assertIn("# not a heading", out)

    def test_strip_leading_h1_matching_title(self):
        body = "# Hello\n\nbody\n"
        out = strip_leading_h1_matching_title(body, "Hello")
        self.assertEqual(out, "body\n")

    def test_strip_leading_h1_keeps_unrelated(self):
        body = "# Other\n\nbody\n"
        out = strip_leading_h1_matching_title(body, "Hello")
        self.assertEqual(out, body)


# ──────────────────────────────────────────────────────────────────────
# Part title parsing
# ──────────────────────────────────────────────────────────────────────

class PartTitleTests(unittest.TestCase):
    def test_em_dash(self):
        roman, topic = parse_part_title("Part I — Cloud Foundations", 1)
        self.assertEqual(roman, "I")
        self.assertEqual(topic, "Cloud Foundations")

    def test_en_dash(self):
        roman, topic = parse_part_title("Part II – Networking", 2)
        self.assertEqual(roman, "II")
        self.assertEqual(topic, "Networking")

    def test_hyphen(self):
        roman, topic = parse_part_title("Part III - Storage", 3)
        self.assertEqual(roman, "III")
        self.assertEqual(topic, "Storage")

    def test_no_match_falls_back(self):
        roman, topic = parse_part_title("Just a title", 4)
        self.assertEqual(roman, "IV")
        self.assertEqual(topic, "Just a title")


class RomanTests(unittest.TestCase):
    def test_to_roman_basic(self):
        self.assertEqual(to_roman(1), "I")
        self.assertEqual(to_roman(4), "IV")
        self.assertEqual(to_roman(9), "IX")
        self.assertEqual(to_roman(14), "XIV")
        self.assertEqual(to_roman(40), "XL")


# ──────────────────────────────────────────────────────────────────────
# Shortcodes
# ──────────────────────────────────────────────────────────────────────

class ShortcodeTests(unittest.TestCase):
    def setUp(self):
        self.report = BuildReport()
        self.path = Path("fixture.md")

    def test_drops_children(self):
        body = "Before\n{{< children sort=\"weight\" />}}\nAfter"
        out = handle_shortcodes(body, relref_mode="drop-link",
                                report=self.report, path=self.path)
        self.assertNotIn("children", out)
        self.assertIn("Before", out)
        self.assertIn("After", out)

    def test_relref_drop_link_mode(self):
        body = 'See [the spec]({{< relref "spec.md" >}}) for details.'
        out = handle_shortcodes(body, relref_mode="drop-link",
                                report=self.report, path=self.path)
        self.assertEqual(out, "See the spec for details.")
        self.assertEqual(self.report.relref_handled, 1)

    def test_unknown_shortcode_logged_in_report(self):
        body = '{{< youtube id="abc" >}}'
        out = handle_shortcodes(body, relref_mode="drop-link",
                                report=self.report, path=self.path)
        self.assertEqual(out.strip(), "")
        # Set entries are formatted as "name (in path.name)".
        self.assertTrue(any("youtube" in s for s in self.report.unknown_shortcodes))


# ──────────────────────────────────────────────────────────────────────
# Image-path rewriting
# ──────────────────────────────────────────────────────────────────────

class ImagePathTests(unittest.TestCase):
    def test_rewrite_existing_image(self):
        # Use the project static dir — the run is inside the real repo, so
        # we rely on a known existing file to assert rewriting works.
        report = BuildReport()
        # Find an existing static image somewhere — fall back to creating a
        # dummy under a temp dir if none exists.
        project_root = SKILL_DIR.parent.parent.parent  # repo root
        static = project_root / "static"
        # Pick any png/svg under static; if none, skip the existence path.
        candidates = list(static.rglob("*.png")) + list(static.rglob("*.svg"))
        if not candidates:
            self.skipTest("no static image to use as fixture")
        target = candidates[0]
        rel = "/" + str(target.relative_to(static))
        body = f"![alt]({rel})"
        out = rewrite_image_paths(body, project_root, report)
        self.assertIn(str(target), out)
        self.assertEqual(report.missing_images, [])

    def test_rewrite_missing_image_replaced_with_placeholder(self):
        report = BuildReport()
        project_root = SKILL_DIR.parent.parent.parent
        body = "![diagram](/no-such-image.png)"
        out = rewrite_image_paths(body, project_root, report)
        self.assertIn("not found", out)
        self.assertEqual(report.missing_images, ["/no-such-image.png"])


# ──────────────────────────────────────────────────────────────────────
# Skip-preprocess passthrough
# ──────────────────────────────────────────────────────────────────────

class PassthroughTests(unittest.TestCase):
    def test_full_passthrough_on_skip_preprocess(self):
        # Use single-section fixture — passthrough simply concatenates.
        md, report = preprocess(
            FIXTURES / "single-section",
            skip_preprocess=True,
            relref_mode="drop-link",
        )
        self.assertIn("Single Foo", md)
        self.assertEqual(report.parts, 0)
        self.assertEqual(report.chapters, 0)


# ──────────────────────────────────────────────────────────────────────
# End-to-end preprocess on 3-level fixture
# ──────────────────────────────────────────────────────────────────────

class EndToEndTests(unittest.TestCase):
    def test_3_level_book_emits_part_and_chapter(self):
        book = {
            "id": "fixture",
            "title": "Fixture",
            "author": "Test",
            "source": "tests/fixtures/3-level",
            "output": "/tmp/fixture-out",
            "palette": "blue",
            "parts": True,
            "eyebrow-prefix": "Part",
            "chapter-prefix": "Chapter",
        }
        md, report = preprocess(
            FIXTURES / "3-level",
            skip_preprocess=False,
            relref_mode="drop-link",
            book=book,
            project_root=SKILL_DIR.parent.parent.parent,
        )
        self.assertEqual(report.parts, 1)
        self.assertEqual(report.chapters, 1)
        self.assertGreaterEqual(report.sections, 1)
        # The Part divider should appear with the Roman numeral.
        self.assertIn("Part I", md)
        # The chapter title should appear.
        self.assertIn("Hello World", md)


# ──────────────────────────────────────────────────────────────────────
# books.yaml schema
# ──────────────────────────────────────────────────────────────────────

class SchemaTests(unittest.TestCase):
    def _ok_book(self, **over):
        b = {"id": "x", "title": "t", "author": "a",
             "source": "s", "output": "o", "palette": "blue"}
        b.update(over)
        return b

    def test_valid_books_no_errors(self):
        self.assertEqual(validate_books([self._ok_book()]), [])

    def test_missing_required_keys(self):
        errs = validate_books([{"id": "x"}])
        self.assertTrue(any("missing keys" in e for e in errs))

    def test_invalid_palette(self):
        errs = validate_books([self._ok_book(palette="green")])
        self.assertTrue(any("palette" in e for e in errs))

    def test_duplicate_id(self):
        errs = validate_books([self._ok_book(id="dup"),
                                self._ok_book(id="dup")])
        self.assertTrue(any("duplicate id" in e for e in errs))


# ──────────────────────────────────────────────────────────────────────
# PR 1 — Tier 1 content fidelity
# ──────────────────────────────────────────────────────────────────────

class CalloutTests(unittest.TestCase):
    def test_concept_blockquote_becomes_concept_callout(self):
        body = "> Concept: Cloud computing is a deployment model.\n"
        out = convert_blockquote_callouts(body)
        self.assertIn("::: {.callout-concept}", out)
        self.assertIn("Cloud computing", out)

    def test_warning_blockquote_becomes_warning_callout(self):
        body = "> ⚠️ Warning: this destroys data\n"
        out = convert_blockquote_callouts(body)
        self.assertIn("::: {.callout-warning}", out)

    def test_key_takeaway_becomes_tip_callout(self):
        body = "> Key takeaway: prefer reliability over novelty\n"
        out = convert_blockquote_callouts(body)
        self.assertIn("::: {.callout-tip}", out)

    def test_unmatched_blockquote_left_alone(self):
        body = "> Just some quoted prose\n> with no marker\n"
        out = convert_blockquote_callouts(body)
        self.assertNotIn(":::", out)
        # original `>` lines should still be present
        self.assertIn("> Just", out)

    def test_blockquote_inside_fence_left_alone(self):
        body = "```\n> Concept: this is code, not a callout\n```\n"
        out = convert_blockquote_callouts(body)
        self.assertNotIn(":::", out)
        self.assertIn("> Concept", out)


class NormalizeTitleTests(unittest.TestCase):
    def test_strips_numeric_prefix(self):
        self.assertEqual(normalize_chapter_title("1. Hello World"), "Hello World")
        self.assertEqual(normalize_chapter_title("12. Networking Deep Dive"),
                         "Networking Deep Dive")

    def test_keeps_short_result_intact(self):
        self.assertEqual(normalize_chapter_title("1. AB"), "1. AB")

    def test_keeps_non_numeric(self):
        self.assertEqual(normalize_chapter_title("Hello World"), "Hello World")


class IndexEmptyTests(unittest.TestCase):
    def test_empty_when_only_children_shortcode(self):
        body = "{{< children sort=\"weight\" />}}"
        self.assertTrue(is_chapter_index_effectively_empty(body))

    def test_empty_when_heading_plus_shortcode(self):
        body = "# Chapter\n\n{{< children />}}\n"
        self.assertTrue(is_chapter_index_effectively_empty(body))

    def test_not_empty_when_real_intro_paragraph(self):
        body = (
            "Cloud platforms have changed how applications are deployed and "
            "operated. This chapter walks through the foundational concepts.\n"
        )
        self.assertFalse(is_chapter_index_effectively_empty(body))


# ──────────────────────────────────────────────────────────────────────
# PR 2 — Cover + Preface
# ──────────────────────────────────────────────────────────────────────

class CoverTests(unittest.TestCase):
    def test_split_title_short_returns_one_line(self):
        line1, line2 = _split_title("Short Title")
        self.assertEqual(line1, "Short Title")
        self.assertEqual(line2, "")

    def test_split_title_long_splits_at_word_boundary(self):
        line1, line2 = _split_title(
            "Cloud Developers — Course Book Foundations"
        )
        self.assertGreater(len(line1), 0)
        self.assertGreater(len(line2), 0)
        # Each side has at least one word, no half-broken word.
        self.assertNotIn("—", line2[:1])

    def test_make_cover_svg_substitutes_placeholders(self):
        import tempfile
        with tempfile.TemporaryDirectory() as tmp_s:
            tmp = Path(tmp_s)
            book = {
                "title": "A Long Book Title For Testing",
                "subtitle": "A Subtitle",
                "author": "An Author",
                "palette": "blue",
            }
            svg = make_cover_svg(book, "v2026.04.29-abc1234", tmp).read_text()
        # The title is split across two SVG lines for visual balance,
        # so each half should appear separately.
        self.assertIn("A Long Book", svg)
        self.assertIn("Title For Testing", svg)
        self.assertIn("A Subtitle", svg)
        self.assertIn("An Author", svg)
        self.assertIn("v2026.04.29-abc1234", svg)
        self.assertIn("#2a6f8f", svg)  # blue palette
        # Placeholders replaced.
        self.assertNotIn("{{", svg)


class PrefaceTests(unittest.TestCase):
    def test_emits_preface_when_root_index_has_body(self):
        import tempfile
        # Build a fixture on the fly: 2-level book with a non-empty
        # root _index.md.
        with tempfile.TemporaryDirectory() as tmp_s:
            root = Path(tmp_s)
            (root / "_index.md").write_text(
                "+++\ntitle = \"Book\"\n+++\n\n"
                "# Book\n\n"
                "An introductory paragraph for the preface that has "
                "more than the empty-detection threshold of words to "
                "make sure it survives the empty-check.\n"
            )
            chap = root / "1-foo"
            chap.mkdir()
            (chap / "_index.md").write_text(
                "+++\ntitle = \"Foo\"\n+++\n\n{{< children />}}\n"
            )
            (chap / "1-section.md").write_text(
                "+++\ntitle = \"Sec\"\n+++\n\nbody\n"
            )
            book = {"id": "x", "title": "x", "author": "a", "source": ".",
                    "output": ".", "palette": "blue", "parts": False,
                    "eyebrow-prefix": "Part", "chapter-prefix": "Chapter"}
            md, _ = preprocess(
                root, skip_preprocess=False, relref_mode="drop-link",
                book=book, project_root=root,
            )
        self.assertIn("::: {.preface}", md)
        self.assertIn("# Preface", md)
        self.assertIn("introductory paragraph", md)

    def test_skips_preface_when_root_index_only_shortcode(self):
        import tempfile
        with tempfile.TemporaryDirectory() as tmp_s:
            root = Path(tmp_s)
            (root / "_index.md").write_text(
                "+++\ntitle = \"Exercises\"\n+++\n\n"
                "{{< children />}}\n"
            )
            chap = root / "1-foo"
            chap.mkdir()
            (chap / "_index.md").write_text(
                "+++\ntitle = \"Foo\"\n+++\n"
            )
            (chap / "1-section.md").write_text(
                "+++\ntitle = \"Sec\"\n+++\n\nbody\n"
            )
            book = {"id": "x", "title": "x", "author": "a", "source": ".",
                    "output": ".", "palette": "blue", "parts": False,
                    "eyebrow-prefix": "Part", "chapter-prefix": "Chapter"}
            md, _ = preprocess(
                root, skip_preprocess=False, relref_mode="drop-link",
                book=book, project_root=root,
            )
        self.assertNotIn("::: {.preface}", md)


# ──────────────────────────────────────────────────────────────────────
# PR 3 — Build cache
# ──────────────────────────────────────────────────────────────────────

class CacheTests(unittest.TestCase):
    """Verify the cache hash is sensitive to all the inputs we care about."""

    def setUp(self):
        import tempfile
        self.tmp = Path(tempfile.mkdtemp(prefix="cache-test-"))
        self.scripts = self.tmp / "scripts"
        self.assets = self.tmp / "assets"
        self.scripts.mkdir()
        self.assets.mkdir()
        (self.scripts / "build.py").write_text("# build script v1\n")
        (self.scripts / "preprocessor.py").write_text("# preprocessor v1\n")
        (self.assets / "print.css").write_text("/* css v1 */\n")
        # Source tree.
        src = self.tmp / "src"
        src.mkdir()
        (src / "_index.md").write_text("# Hello\n")
        self.book = {
            "id": "x", "title": "x", "author": "a",
            "source": "src", "output": "out", "palette": "blue",
        }

    def tearDown(self):
        import shutil
        shutil.rmtree(self.tmp, ignore_errors=True)

    def _hash(self):
        return compute_build_hash(self.book, self.tmp, self.scripts, self.assets)

    def test_unchanged_returns_same_hash(self):
        self.assertEqual(self._hash(), self._hash())

    def test_changed_source_invalidates(self):
        h1 = self._hash()
        (self.tmp / "src" / "_index.md").write_text("# Hello edited\n")
        self.assertNotEqual(h1, self._hash())

    def test_changed_book_config_invalidates(self):
        h1 = self._hash()
        self.book["palette"] = "red"
        self.assertNotEqual(h1, self._hash())

    def test_changed_assets_invalidates(self):
        h1 = self._hash()
        (self.assets / "print.css").write_text("/* css v2 */\n")
        self.assertNotEqual(h1, self._hash())

    def test_changed_script_invalidates(self):
        h1 = self._hash()
        (self.scripts / "build.py").write_text("# build script v2\n")
        self.assertNotEqual(h1, self._hash())


# ──────────────────────────────────────────────────────────────────────
# PR 6 — Intra-book link rewriting
# ──────────────────────────────────────────────────────────────────────

class IntraBookLinkTests(unittest.TestCase):
    def setUp(self):
        import tempfile
        self.root = Path(tempfile.mkdtemp(prefix="intra-"))
        # Two chapters, each with one section.
        for chap, sec, title in [
            ("1-foo", "1-foo-section.md", "Foo Section"),
            ("2-bar", "1-bar-section.md", "Bar Section"),
        ]:
            d = self.root / chap
            d.mkdir()
            (d / sec).write_text(
                f"+++\ntitle = \"{title}\"\n+++\n\nbody\n"
            )

    def tearDown(self):
        import shutil
        shutil.rmtree(self.root, ignore_errors=True)

    def test_anchor_index_collects_section_files(self):
        idx = build_anchor_index(self.root)
        self.assertEqual(
            sorted(idx.keys()),
            ["1-foo/1-foo-section.md", "2-bar/1-bar-section.md"],
        )
        self.assertEqual(idx["1-foo/1-foo-section.md"], "sec-foo-section")

    def test_link_to_sibling_md_rewrites_to_anchor(self):
        idx = build_anchor_index(self.root)
        report = BuildReport()
        body = "See [the bar]( ../2-bar/1-bar-section.md) for context."
        out = rewrite_intrabook_links(
            body, source_dir=self.root / "1-foo",
            book_root=self.root, index=idx, report=report,
        )
        self.assertIn("[the bar]( ../2-bar/1-bar-section.md)", out) \
            if False else None
        # The path with a leading space won't match — make a clean test:
        body = "See [the bar](../2-bar/1-bar-section.md) for context."
        out = rewrite_intrabook_links(
            body, source_dir=self.root / "1-foo",
            book_root=self.root, index=idx, report=report,
        )
        self.assertIn("[the bar](#sec-bar-section)", out)

    def test_link_to_external_url_left_alone(self):
        idx = build_anchor_index(self.root)
        report = BuildReport()
        body = "Spec at [the docs](https://example.com/foo.md)."
        out = rewrite_intrabook_links(
            body, source_dir=self.root / "1-foo",
            book_root=self.root, index=idx, report=report,
        )
        self.assertEqual(out, body)

    def test_link_to_unknown_md_logged(self):
        idx = build_anchor_index(self.root)
        report = BuildReport()
        body = "Broken: [missing](../3-missing/file.md)."
        out = rewrite_intrabook_links(
            body, source_dir=self.root / "1-foo",
            book_root=self.root, index=idx, report=report,
        )
        # Original link preserved.
        self.assertIn("../3-missing/file.md", out)
        self.assertTrue(any("intra-book" in n for n in report.notes))


if __name__ == "__main__":
    unittest.main()
