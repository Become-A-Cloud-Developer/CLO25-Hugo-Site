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
    handle_shortcodes,
    parse_frontmatter,
    parse_part_title,
    preprocess,
    rewrite_image_paths,
    shift_headings,
    strip_leading_h1_matching_title,
    to_roman,
)
from build import validate_books  # noqa: E402


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


if __name__ == "__main__":
    unittest.main()
