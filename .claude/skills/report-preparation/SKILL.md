---
name: report-preparation
version: 1.1.0
description: Process student report submissions from Google Classroom. Renames files with lastname_firstname prefix, converts DOCX to PDF, cross-references against class roster, removes duplicates, merges multi-file submissions, and generates submission tracking. Use when processing downloaded student assignment submissions.
allowed-tools: Read, Write, Edit, Glob, Grep, Bash, Task
triggers:
  - student reports
  - process submissions
  - rename PDFs
  - assignment submissions
---

# Report Preparation Skill

Process a folder of downloaded student reports into a standardized format for grading.

## Critical: Before Starting

**MUST READ FIRST:**

1. Read `GUIDE.md` - Detailed processing rules and edge cases
2. Read `CLASS-LIST-TEMPLATE.md` - Required format for class roster

## Required Inputs

When invoking this skill, provide:

1. **Reports folder path** - Location of downloaded student reports
   - Example: `docs/assignments/assignment-1/student-reports/`

2. **Class roster** (auto-detected) - The skill will search for CLASS-LIST.md automatically
   - See "Class List Discovery" below

## Class List Discovery

The skill automatically searches for a class list by traversing upward from the reports folder:

```
Starting from: docs/assignments/assignment-1/student-reports/
Search order:
  1. docs/assignments/assignment-1/student-reports/CLASS-LIST.md
  2. docs/assignments/assignment-1/CLASS-LIST.md
  3. docs/assignments/CLASS-LIST.md
  4. docs/CLASS-LIST.md
  5. CLASS-LIST.md (project root)
```

**When found:**
```
✓ Found class roster: docs/assignments/CLASS-LIST.md
  Contains 31 students
```

**When not found:**
```
⚠ No CLASS-LIST.md found in directory hierarchy.

Without a class roster:
- Cannot cross-reference student names
- Cannot detect missing submissions
- Must rely on PDF content for name extraction

Do you want to continue without a class list?
```

If user chooses to continue without class list:
- Skip Step 4 (cross-reference)
- Skip "Missing submissions" in STUDENT-LIST.md
- Report only successfully renamed files

### Supported Class List Filenames

The search looks for these filenames (case-insensitive):
- `CLASS-LIST.md`
- `CLASSLIST.md`
- `class-list.md`
- `students.md`
- `roster.md`

## Processing Workflow

### Step 0: Privacy Protection (MANDATORY FIRST)

**Before ANY processing, verify gitignore protection:**

```bash
# Check if folder is in .gitignore
grep -r "student-reports" .gitignore

# If not present, add it immediately:
echo "assignment-N/student-reports/" >> docs/assignments/.gitignore
```

**STOP and warn user if:**
- Folder not in .gitignore
- Any student files already tracked by git

### Step 1: List All Files

```bash
# List all files in the reports folder
ls -la [reports_folder]/
```

Document:
- Total file count
- File types present (PDF, DOCX, DOC)
- Any unexpected file types

### Step 2: Convert Non-PDF Files

For each DOCX/DOC file, use AppleScript to convert:

```bash
osascript scripts/convert-docx.applescript "[input.docx]" "[output.pdf]"
```

After successful conversion:
- Verify PDF was created
- Delete original DOCX file

### Step 3: Extract Names and Rename (Parallel Subagents)

**Spawn one subagent per PDF** to avoid context overflow:

For each PDF file, create a Task with prompt:

```
Read the PDF at [path] and extract the student name.
Look for the name in:
1. Title page or cover
2. Document headers
3. Author signature at the end

Return ONLY:
- First name
- Last name
- Confidence: high/medium/low

If name cannot be determined, return "UNIDENTIFIED" with reason.
```

After subagent returns:
- Normalize Swedish characters: ö→o, ä→a, å→a, é→e
- Create prefix: `lastname_firstname_` (lowercase)
- Rename file: `lastname_firstname_[original_name].pdf`

### Step 4: Cross-Reference Unidentified Files

**Requires:** Class list (skip if not available)

For files marked UNIDENTIFIED:

1. Read CLASS-LIST.md for all student names
2. Check if filename contains partial match
3. Check if any student missing from identified files

Match strategies:
- Partial first name in filename → look up full name in roster
- File contains abbreviation (e.g., "J. Andersson") → match to roster
- Only first name visible → find matching entry

Flag remaining unidentified files for manual review.

**If no class list available:**
- Skip this step
- All UNIDENTIFIED files flagged for manual review
- Cannot determine missing submissions

### Step 5: Detect and Remove Duplicates

```bash
# Find students with multiple submissions
ls *.pdf | sed 's/_.*//' | sort | uniq -c | sort -rn | grep -v "^ *1 "
```

For each student with duplicates, spawn subagent to:
1. Read all versions from the same student
2. Compare content (page count, sections present, completeness)
3. **Prefer the latest version** (usually what the student intends to submit)
4. **Verify the latest is complete** - if latest appears incomplete or corrupted, choose the most complete version instead
5. Delete inferior versions
6. Report which files were removed and why

**Selection priority (in order):**

| Priority | Check | Action |
|----------|-------|--------|
| 1 | Is latest version complete? | If yes, keep latest |
| 2 | Latest incomplete/corrupted? | Keep most complete version |
| 3 | Content identical? | Keep file with cleaner name |
| 4 | Still unclear? | Keep latest, flag for review |

**Verification for "latest" selection:**
- Check that the latest has all expected sections
- Verify it's not a draft or partial submission
- Look for signs of completeness (conclusions, appendices, proper formatting)
- If latest looks like an accident (e.g., blank pages, wrong file), choose the complete one

### Step 6: Merge Multi-file Submissions

Some students submit multiple files (e.g., report + screenshots, or separate PDFs per sub-task). After renaming, detect students with multiple PDFs and merge them into a single file.

```bash
# Find students with multiple PDFs
ls *.pdf | sed 's/_.*//' | sort | uniq -c | sort -rn | grep -v "^ *1 "
```

For each student with multiple PDFs:

1. **Determine logical order** — spawn a subagent to read the files and determine the correct reading order (e.g., development → provisioning → deployment, or report → screenshots)
2. **Concatenate** using `pdfunite`:

```bash
pdfunite file1.pdf file2.pdf [file3.pdf ...] lastname_firstname_report.pdf
```

3. **Verify** the merged PDF was created successfully
4. **Remove** the individual PDFs
5. **Record** which files were merged (for STUDENT-LIST.md)

**Non-PDF attachments** (ZIP files, source code archives) are kept as-is alongside the merged PDF. Only PDFs are concatenated.

### Step 7: Generate STUDENT-LIST.md

**Important:** The STUDENT-LIST.md must reflect the final state after merging. Include a **Notes** section documenting multi-file submissions and merges, and a **Files Removed** section listing all deleted files with reasons.

Create tracking file at `assignment-N/STUDENT-LIST.md`:

**With class list:**
```markdown
# Student Submission List - Assignment N

| Full Name | File Prefix | Report Submitted | Betyg |
|-----------|-------------|------------------|-------|
| Firstname Lastname | `lastname_firstname` | Yes | |
| Another Student | `student_another` | No | |

## Summary

- **Total students:** X (from class list)
- **Reports submitted:** Y
- **Missing reports:** Z

### Missing Submissions

- Student Name 1
- Student Name 2

## Notes

- **Student A:** Also submitted source code ZIP (`prefix_source-code.zip`)
- **Student B:** Report + screenshots merged into single PDF
- **Student C:** 3 PDFs (Part 1, Part 2, Part 3) merged into single PDF

## Files Removed

- `original-filename.pdf` — reason (e.g., blank page, failed conversion)
- `part1.pdf`, `part2.pdf` — merged into `prefix_report.pdf`
```

**Without class list:**
```markdown
# Student Submission List - Assignment N

| Full Name | File Prefix | Report Submitted | Betyg |
|-----------|-------------|------------------|-------|
| Firstname Lastname | `lastname_firstname` | Yes | |
| Another Student | `student_another` | Yes | |

## Summary

- **Reports processed:** X
- **Successfully renamed:** Y
- **Unidentified (manual review):** Z

⚠ No class list available - cannot determine missing submissions.

## Notes

(same format as above)

## Files Removed

(same format as above)
```

### Step 8: Report Statistics

Display final summary:

```
## Processing Complete

✓ Files processed: X
✓ Successfully renamed: Y
✓ Duplicates removed: Z
✓ Multi-file submissions merged: N
⚠ Unidentified (manual review needed): N

Final state: 1 PDF per student (+ any non-PDF attachments)
Missing submissions: [list names]
```

## Edge Cases

### Swedish Character Normalization

| Original | Normalized |
|----------|-----------|
| ö | o |
| ä | a |
| å | a |
| é | e |
| ü | u |

### Compound Surnames

Combine without spaces:
- "Martinez Löfgren" → `martinezlofgren`
- "von Essen" → `vonessen`
- "De La Cruz" → `delacruz`

### Name Variations

- "Anna-Isabel" → `annaisabel` (remove hyphen in prefix)
- "Johan (Johnny)" → `johan` (ignore nickname)

### Unidentifiable Files

When subagent cannot extract name:
1. Log the file and reason
2. Cross-reference with missing students in roster
3. If only one student missing and one file unidentified → assume match
4. Otherwise flag for manual review

## AppleScript Usage (macOS Only)

This skill requires Microsoft Word installed for DOCX conversion.

```bash
# Verify Word is available
osascript -e 'tell application "System Events" to get name of processes' | grep -i word
```

If Word not available, report error and list unconverted files.

## Privacy Checklist

Before completing, verify:

- [ ] All student-reports folders in .gitignore
- [ ] No student files staged in git
- [ ] CLASS-LIST.md also protected (contains student names)
- [ ] STUDENT-LIST.md also protected
- [ ] No cloud uploads occurred during processing

```bash
# Final verification
git status --ignored | grep student-reports
```

## Output

After successful processing:
1. **One PDF per student** — renamed with `lastname_firstname_` prefix, multi-file submissions merged
2. Non-PDF attachments (ZIP, source code) kept alongside with same prefix
3. STUDENT-LIST.md created with submission tracking, notes on merges, and list of removed files
4. Statistics displayed in terminal
5. Unidentified files flagged for manual review

---

## Changelog

### 1.1.0 — 2026-02-22

- Added Step 6: Merge multi-file submissions into single PDF per student using `pdfunite`
- Updated STUDENT-LIST.md template with Notes and Files Removed sections
- Updated statistics output to include merge count and final state summary
- Updated description to mention multi-file merge capability

### 1.0.0 — Initial release

- DOCX to PDF conversion via AppleScript
- Parallel name extraction from PDFs using subagents
- Cross-referencing against CLASS-LIST.md
- Duplicate detection and removal
- STUDENT-LIST.md generation
- Privacy protection checks
