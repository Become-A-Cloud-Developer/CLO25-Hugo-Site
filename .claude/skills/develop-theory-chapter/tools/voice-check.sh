#!/usr/bin/env bash
# voice-check.sh — enforce the style and frontmatter rules in STYLE-CHECKLIST.md
# against a single chapter `<slug>.md`. Exits 0 on full pass, 1 on hard fail,
# 2 on soft warnings only (no hard fails).
#
# Usage:
#   voice-check.sh <path-to-slug.md>
#
# This script reads STYLE-CHECKLIST.md as the spec; if you change rules, update both.

set -uo pipefail

if [[ $# -ne 1 ]]; then
    echo "usage: $0 <path-to-chapter.md>" >&2
    exit 64
fi

FILE="$1"

if [[ ! -f "$FILE" ]]; then
    echo "voice-check: file not found: $FILE" >&2
    exit 64
fi

HARD_FAILS=0
SOFT_WARNS=0
REPORT=""

note_hard() {
    HARD_FAILS=$((HARD_FAILS + 1))
    REPORT+="  HARD: $1"$'\n'
}

note_soft() {
    SOFT_WARNS=$((SOFT_WARNS + 1))
    REPORT+="  WARN: $1"$'\n'
}

# --- 1. Frontmatter check -----------------------------------------------------

# Frontmatter must be the first thing in the file, delimited by `+++`.
if ! head -n 1 "$FILE" | grep -qE '^\+\+\+'; then
    note_hard "frontmatter delimiter '+++' missing on first line"
fi

# Extract frontmatter block (between first two `+++` lines).
FRONTMATTER=$(awk '/^\+\+\+/{c++; next} c==1' "$FILE")

if [[ -z "$FRONTMATTER" ]]; then
    note_hard "frontmatter block is empty"
fi

for KEY in title program cohort courses weight draft; do
    if ! echo "$FRONTMATTER" | grep -qE "^${KEY}\s*=" ; then
        note_hard "frontmatter missing required key: $KEY"
    fi
done

if echo "$FRONTMATTER" | grep -qE '^draft\s*=\s*true'; then
    note_hard "frontmatter has draft = true (must be false for validated chapters)"
fi

# Confirm program = "CLO" and cohort = "25"
if ! echo "$FRONTMATTER" | grep -qE '^program\s*=\s*"CLO"'; then
    note_hard "frontmatter program must be \"CLO\""
fi
if ! echo "$FRONTMATTER" | grep -qE '^cohort\s*=\s*"25"'; then
    note_hard "frontmatter cohort must be \"25\""
fi

# --- 2. Strip frontmatter, code blocks, and blockquotes for prose checks -----

# awk script:
#   - skip frontmatter (between first two `+++`)
#   - skip code fences (between matching ``` ... ```)
#   - skip blockquote lines (start with `>` followed by space)
#   - skip table lines (contain `|` and look like a table row)
PROSE=$(awk '
    BEGIN { in_fm=0; fm_seen=0; in_code=0 }
    /^\+\+\+/ {
        if (fm_seen == 0) { in_fm=1; fm_seen=1; next }
        else if (in_fm == 1) { in_fm=0; next }
    }
    in_fm == 1 { next }
    /^```/ { in_code = !in_code; next }
    in_code == 1 { next }
    /^>\s/ { next }
    /^\s*\|/ { next }
    { print }
' "$FILE")

# --- 3. Word count ------------------------------------------------------------

WORD_COUNT=$(echo "$PROSE" | wc -w | tr -d ' ')

if [[ $WORD_COUNT -lt 1200 ]]; then
    note_hard "word count $WORD_COUNT < 1200 (hard floor)"
elif [[ $WORD_COUNT -gt 4000 ]]; then
    note_hard "word count $WORD_COUNT > 4000 (hard ceiling)"
elif [[ $WORD_COUNT -lt 1400 ]] || [[ $WORD_COUNT -gt 3600 ]]; then
    note_soft "word count $WORD_COUNT outside soft target 1400–3600"
fi

# --- 4. Forbidden voice patterns (hard fail) ---------------------------------

declare -a HARD_PATTERNS=(
    '\bwe will\b'
    "\\bwe'll\\b"
    '\bwe can\b'
    '\bour application\b'
    "\\blet's\\b"
)

for PAT in "${HARD_PATTERNS[@]}"; do
    HITS=$(echo "$PROSE" | grep -inE "$PAT" || true)
    if [[ -n "$HITS" ]]; then
        FIRST_HIT=$(echo "$HITS" | head -n 1)
        note_hard "forbidden first-person plural pattern '$PAT' at: $FIRST_HIT"
    fi
done

# --- 5. Forbidden voice patterns (soft warning) ------------------------------

declare -a SOFT_PATTERNS=(
    '^\s*Modern\b'
    "\\btoday's\\b"
    '\bin the digital age\b'
    '\bin the current landscape\b'
    '\bcontemporary\b'
    '\bsimply\b'
    '\bpowerful\b'
    '\brobust\b'
    '\bseamless\b'
)

for PAT in "${SOFT_PATTERNS[@]}"; do
    HITS=$(echo "$PROSE" | grep -inE "$PAT" || true)
    if [[ -n "$HITS" ]]; then
        FIRST_HIT=$(echo "$HITS" | head -n 1)
        note_soft "soft-warn pattern '$PAT' at: $FIRST_HIT"
    fi
done

# --- 6. Rhetorical question detection (soft warning) ------------------------

# A line that starts with a capital letter and ends in `?` (and is not in
# strip blocks). Quote markers excluded.
RHETORICAL=$(echo "$PROSE" | grep -nE '^[A-Z][^"`]*\?\s*$' || true)
if [[ -n "$RHETORICAL" ]]; then
    COUNT=$(echo "$RHETORICAL" | wc -l | tr -d ' ')
    FIRST=$(echo "$RHETORICAL" | head -n 1)
    note_soft "$COUNT possible rhetorical question(s); first at: $FIRST"
fi

# --- 7. H1 in body (hard fail — H1 must come from frontmatter title) --------

H1_HITS=$(echo "$PROSE" | grep -nE '^# [^#]' || true)
if [[ -n "$H1_HITS" ]]; then
    FIRST_HIT=$(echo "$H1_HITS" | head -n 1)
    note_hard "H1 found in body (must come from frontmatter title) at: $FIRST_HIT"
fi

# --- 8. Code fences without language tags (hard fail) -----------------------

# Unlanguaged fences: line is exactly ``` (opening fence with no language).
# The closing fence is also exactly ```, so we count opening fences only by
# parity. We re-walk the original file to detect.
UNLANG=$(awk '
    /^```$/ {
        in_code = !in_code
        if (in_code == 1) { print NR ": " $0 }
    }
    /^```[a-zA-Z]/ {
        in_code = !in_code
    }
' "$FILE")

if [[ -n "$UNLANG" ]]; then
    note_hard "code fence without language tag at line(s): $(echo "$UNLANG" | head -n 1)"
fi

# --- 9. Cross-link to a companion exercise (hard fail) ----------------------

if ! grep -qE '\(/exercises/[^)]+\)' "$FILE"; then
    note_hard "no cross-link to /exercises/... (chapter must reference its companion exercise)"
fi

# --- 10. Presentation links present (hard fail) -----------------------------

if ! grep -qE '\(/presentations/[^)]+\.html\)' "$FILE"; then
    note_hard "no presentation link to /presentations/...html"
fi
if ! grep -qE '\(/presentations/[^)]+-swe\.html\)' "$FILE"; then
    note_hard "no Swedish presentation link to /presentations/...-swe.html"
fi

# --- Final verdict -----------------------------------------------------------

echo "voice-check: $FILE"
echo "  Word count: $WORD_COUNT"
if [[ -n "$REPORT" ]]; then
    printf "%s" "$REPORT"
fi

if [[ $HARD_FAILS -gt 0 ]]; then
    echo "  RESULT: HARD FAIL ($HARD_FAILS hard, $SOFT_WARNS soft)"
    exit 1
elif [[ $SOFT_WARNS -gt 0 ]]; then
    echo "  RESULT: PASS WITH WARNINGS ($SOFT_WARNS soft)"
    exit 2
else
    echo "  RESULT: PASS"
    exit 0
fi
