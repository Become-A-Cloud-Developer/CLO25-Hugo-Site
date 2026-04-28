#!/usr/bin/env bash
# anchor-check.sh — verify every internal anchor link in the Course Book still
# resolves to an existing heading. Run at end of every Part-run.
#
# Walks `content/course-book/` and extracts:
#   - Markdown links of the form `(/path...#anchor)`
#   - Hugo ref shortcodes of the form `{{< ref "path#anchor" >}}`
#
# For each anchor, resolves the target file and verifies the slugified heading
# exists. Exits 0 on success, 1 on any unresolved anchor.
#
# Portable to bash 3.x (macOS default). Uses temp files instead of associative
# arrays.
#
# Usage:
#   anchor-check.sh                      # walks content/course-book
#   anchor-check.sh <root-dir>           # walks the supplied directory

set -uo pipefail

ROOT="${1:-content/course-book}"

if [[ ! -d "$ROOT" ]]; then
    echo "anchor-check: directory not found: $ROOT" >&2
    exit 64
fi

# Slugify a heading text to match Hugo's default slug.
slugify() {
    echo "$1" \
        | tr '[:upper:]' '[:lower:]' \
        | sed -E 's/[^a-z0-9 -]+//g' \
        | sed -E 's/[[:space:]]+/-/g' \
        | sed -E 's/-+/-/g' \
        | sed -E 's/^-//; s/-$//'
}

HEADINGS_FILE=$(mktemp)
ANCHORS_FILE=$(mktemp)
trap 'rm -f "$HEADINGS_FILE" "$ANCHORS_FILE"' EXIT

# Build a global index of `path#slug` for every heading in every .md file under
# content/ — anchors may target any chapter, not just course-book/.
build_heading_index() {
    local md_file rel_path slug header_text
    while IFS= read -r -d '' md_file; do
        rel_path="${md_file#content/}"
        rel_path="${rel_path%/_index.md}"
        rel_path="${rel_path%.md}"
        # Hugo URL convention: directory pages get trailing slash; file pages also.
        rel_path="/${rel_path}/"
        rel_path=$(echo "$rel_path" | sed 's|//*|/|g')

        # H2 and H3 only — H1 in body is forbidden.
        while IFS= read -r header_text; do
            slug=$(slugify "$header_text")
            [[ -n "$slug" ]] && echo "${rel_path}#${slug}" >> "$HEADINGS_FILE"
        done < <(grep -E '^#{2,3} ' "$md_file" 2>/dev/null | sed -E 's/^#{2,3} +//' || true)
    done < <(find content -type f -name '*.md' -print0 2>/dev/null)
    sort -u "$HEADINGS_FILE" -o "$HEADINGS_FILE"
}

build_heading_index

# Extract anchor references from every .md file under $ROOT.
extract_anchors() {
    local md_file
    while IFS= read -r -d '' md_file; do
        # Markdown anchor links: (/path...#anchor)
        grep -oE '\(/[A-Za-z0-9._/-]+#[A-Za-z0-9._-]+\)' "$md_file" 2>/dev/null \
            | while IFS= read -r m; do
                m="${m#(}"
                m="${m%)}"
                local link_path="${m%#*}"
                local anchor="${m##*#}"
                [[ "${link_path: -1}" != "/" ]] && link_path="${link_path}/"
                echo "${link_path}#${anchor}|${md_file}" >> "$ANCHORS_FILE"
            done

        # Hugo ref shortcodes: {{< ref "path#anchor" >}}
        grep -oE '\{\{<\s*ref\s+"[^"]+#[^"]+"\s*>\}\}' "$md_file" 2>/dev/null \
            | while IFS= read -r m; do
                local inner
                inner=$(echo "$m" | sed -E 's/.*ref[[:space:]]+"([^"]+)".*/\1/')
                local link_path="${inner%#*}"
                local anchor="${inner##*#}"
                [[ "${link_path:0:1}" != "/" ]] && link_path="/${link_path}"
                [[ "${link_path: -1}" != "/" ]] && link_path="${link_path}/"
                echo "${link_path}#${anchor}|${md_file}" >> "$ANCHORS_FILE"
            done
    done < <(find "$ROOT" -type f -name '*.md' -print0 2>/dev/null)
}

extract_anchors

UNRESOLVED=0
TOTAL=0

if [[ -s "$ANCHORS_FILE" ]]; then
    sort -u "$ANCHORS_FILE" -o "$ANCHORS_FILE"
    while IFS='|' read -r key src_file; do
        [[ -z "$key" ]] && continue
        TOTAL=$((TOTAL + 1))
        if ! grep -Fxq "$key" "$HEADINGS_FILE"; then
            UNRESOLVED=$((UNRESOLVED + 1))
            echo "UNRESOLVED: $key (referenced from $src_file)"
        fi
    done < "$ANCHORS_FILE"
fi

echo
echo "anchor-check: scanned $TOTAL anchor references under $ROOT"
echo "anchor-check: $UNRESOLVED unresolved"

if [[ $UNRESOLVED -gt 0 ]]; then
    exit 1
fi
exit 0
