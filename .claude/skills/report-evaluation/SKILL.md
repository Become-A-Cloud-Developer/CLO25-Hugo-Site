---
name: report-evaluation
version: 1.2.0
description: Evaluate student assignment reports using three independent reviewers for consensus grading. Each reviewer reads the PDF and context files independently, then provides section assessments in Swedish. Results compiled with majority voting into GRADING-RESULTS.md. Use when grading prepared student submissions.
allowed-tools: Read, Write, Edit, Glob, Task, AskUserQuestion, WebFetch
triggers:
  - evaluate reports
  - grade assignments
  - student grading
  - assessment
---

# Report Evaluation Skill

Evaluate student reports using three independent reviewers for reliable consensus grading.

## Critical: Before Starting

**MUST READ from skill folder:**

1. Read `FEEDBACK-EXAMPLES.md` - Swedish feedback tone and style
2. Read `OUTPUT-FORMAT.md` - Expected output structure

## Session Recovery with EVALUATION-STATUS.json

**Critical: This skill uses EVALUATION-STATUS.json to track progress and survive compacting events.**

The status file enables:
- **Resumption** - Pick up exactly where you left off after compacting
- **Progress visibility** - Clear tracking of what's done and what remains
- **Incremental writes** - Results saved after each batch (not lost on compaction)
- **Batch management** - Pre-planned batches of 3 students each

### Status File Structure

```json
{
  "evaluation_session": {
    "assignment": "Assignment Name",
    "reports_folder": "/full/path/to/student-reports/assignment-N",
    "assignments_folder": "/full/path/to/assignments/assignment-N",
    "started": "YYYY-MM-DD",
    "last_updated": "YYYY-MM-DD",
    "status": "in_progress|completed",
    "batch_size": 3
  },
  "files": {
    "grading_results": "[reports_folder]/GRADING-RESULTS.md",
    "student_list": "[reports_folder]/STUDENT-LIST.md",
    "assignment_instructions": "[assignments_folder]/assignment-N.md",
    "grading_criteria": "[assignments_folder]/assignment-N-grading-criteria.md",
    "course_description": "[found_path]/COURSE-DESCRIPTION.md",
    "background": "[assignments_folder]/BACKGROUND.md (if exists)",
    "special_considerations": "[assignments_folder]/SPECIAL-CONSIDERATIONS.md (if exists)",
    "student_reports_folder": "[reports_folder]/"
  },
  "progress": {
    "total_students": 0,
    "submitted_reports": 0,
    "missing_reports": 0,
    "evaluated": 0,
    "remaining": 0
  },
  "batches": {
    "batch_1": {
      "status": "completed|in_progress|pending",
      "students": [
        {"name": "Student Name", "file": "lastname_firstname_rapport.pdf"}
      ],
      "results": {"VG": 0, "G": 0}
    }
  },
  "completed_evaluations": [
    {"name": "Student Name", "grade": "G", "consensus": "3/3"}
  ],
  "missing_submissions": ["Student Name"],
  "next_batch": "batch_N"
}
```

### Recovery Behavior

**On skill invocation:**
1. Check for existing `EVALUATION-STATUS.json` in reports folder
2. If found: Read status and continue from `next_batch`
3. If not found: Initialize new session (Step 1)

## Split Directory Layout

This skill uses a **split directory layout** that separates public assignment instructions from private student data:

```
docs/
├── COURSE-DESCRIPTION.md              # Shared course description (parent search)
├── assignments/
│   └── assignment-N/
│       ├── assignment-N.md            # Assignment instructions
│       ├── assignment-N-grading-criteria.md  # Grading rubric
│       ├── BACKGROUND.md             # Optional: project context
│       └── SPECIAL-CONSIDERATIONS.md  # Optional: exceptions
└── student-reports/                   # GITIGNORED — private student data
    ├── CLASS-LIST.md                  # Master class roster
    └── assignment-N/
        ├── STUDENT-LIST.md            # Who to evaluate
        ├── *.pdf                      # Student report PDFs
        ├── *_source-code.zip          # Student source code (optional)
        ├── GRADING-RESULTS.md         # OUTPUT (created by skill)
        └── EVALUATION-STATUS.json     # Progress tracking (created by skill)
```

### Input: Reports Folder

The skill takes the **reports folder** as input (e.g., `docs/student-reports/assignment-1/`).

### Auto-Discovery Logic

From the reports folder, the skill automatically derives all other paths:

```
Input:     docs/student-reports/assignment-1/
Derives:   docs/assignments/assignment-1/      (replace student-reports → assignments)
Searches:  docs/COURSE-DESCRIPTION.md           (parent search from reports folder)
```

### File Locations

| File | Location | Required? |
|------|----------|-----------|
| `STUDENT-LIST.md` | Reports folder (input) | Yes |
| `*.pdf` | Reports folder (input) | Yes |
| `assignment-*.md` | Assignments folder (derived) | Yes |
| `*grading-criteria*.md` | Assignments folder (derived) | Yes |
| `COURSE-DESCRIPTION.md` | Parent search from reports folder | Yes |
| `BACKGROUND.md` | Assignments folder (derived) | No — optional context |
| `SPECIAL-CONSIDERATIONS.md` | Assignments folder (derived) | No — optional exceptions |
| `GRADING-RESULTS.md` | Reports folder (output) | Created by skill |
| `EVALUATION-STATUS.json` | Reports folder (output) | Created by skill |

## Core Tone Principles

**All feedback must be framed positively.** Never use negative critique.

| Principle | Description |
|-----------|-------------|
| **Dry tone** | Minimal adjectives, no exclamation marks, no superlatives, no filler phrases |
| **Positive framing** | Frame everything positively — never mention what's missing or lacking |
| **Vague on specifics** | Do not reference specific filenames, code, section names, or error messages from the report — risk of inaccuracy |
| **Grade signal** | First sentence subtly signals the grade level (VG = "förståelse/resonemang/röd tråd", G = "fungerande/alla delmoment/på plats") |
| **No name** | Do not address the student by first name in the feedback |
| **Natural Swedish** | Use conversational language, but keep English tech terms that are used as-is in Swedish (deploy, CI/CD, reverse proxy, IaC, etc.) |

## Three-Reviewer Method

Each student report is evaluated by **three independent subagents in parallel**:

| Benefit | Explanation |
|---------|-------------|
| **Reliability** | Multiple perspectives reduce bias |
| **Consensus validation** | Unanimous vs split decisions visible |
| **Better feedback** | Select best feedback from variety |

### Consensus Rules

| Voting Pattern | Final Grade |
|----------------|-------------|
| 3/3 unanimous | Reviewer grade |
| 2/3 majority | Majority grade |
| 1/1/1 split | Flag for instructor review |

## Evaluation Workflow

### Step 0: Check for Existing Session

**Before anything else, check if a session already exists.**

1. Look for `EVALUATION-STATUS.json` in the reports folder
2. **If found with status "in_progress":**
   - Display: `Resuming evaluation session from [last_updated]`
   - Display current progress from status file
   - Skip to Step 3 and continue from `next_batch`
3. **If found with status "completed":**
   - Display: `Previous session completed. Starting fresh evaluation.`
   - Proceed to Step 0.5 (validate inputs)
4. **If not found:**
   - Display: `No existing session. Starting new evaluation.`
   - Proceed to Step 0.5 (validate inputs)

**Resume display format:**
```
## Resuming Evaluation Session

Assignment: [assignment name]
Last updated: [date]
Progress: [evaluated]/[total] students evaluated

Completed batches: [N]
Next batch: [batch_N] with [3] students

Continuing evaluation...
```

### Step 0.5: Validate Input Files

**Before starting evaluation, derive paths and check that all required input files exist.**

**Path derivation:**
1. **Reports folder** = input path (e.g., `docs/student-reports/assignment-1/`)
2. **Assignments folder** = replace `student-reports` with `assignments` in path (e.g., `docs/assignments/assignment-1/`)
3. **COURSE-DESCRIPTION.md** = parent search upward from reports folder

**Required files — check in assignments folder (derived):**
1. `assignment-*.md` (assignment instructions — matches pattern like `assignment-1.md`)
2. `*grading-criteria*.md` (grading rubric — matches pattern like `assignment-1-grading-criteria.md`)

**Required files — check in reports folder (input):**
1. `STUDENT-LIST.md`
2. `*.pdf` (at least one student report PDF)

**Parent-searchable files** — check reports folder first, then parent folders up to project root:
1. `COURSE-DESCRIPTION.md` - Search upward until found

**Optional files — check in assignments folder (derived):**
1. `BACKGROUND.md` — context about parallel group work, project scenario
2. `SPECIAL-CONSIDERATIONS.md` — exceptions, adjustments

**If any required files are missing**, display this table to the terminal:

```
## Missing Input Files

The following files are required for evaluation:

| File | Status | Search Location | Purpose |
|------|--------|-----------------|---------|
| STUDENT-LIST.md | [Found/MISSING] | Reports folder | Roster with student names, submission status, and grade column |
| assignment-*.md | [Found: filename / MISSING] | Assignments folder | Assignment instructions |
| *grading-criteria*.md | [Found: filename / MISSING] | Assignments folder | Grading rubric with criteria and weights |
| COURSE-DESCRIPTION.md | [Found at: path / MISSING] | Parent folders → root | Formal course learning objectives and G/VG criteria |
| *.pdf | [N found / MISSING] | Reports folder | Student report PDFs to evaluate |

Optional files (not blocking):

| File | Status | Location |
|------|--------|----------|
| BACKGROUND.md | [Found/Not found] | Assignments folder |
| SPECIAL-CONSIDERATIONS.md | [Found/Not found] | Assignments folder |
```

Then use **AskUserQuestion** to ask:

> "Some required input files are missing. Do you want to continue anyway?"
> - Options: "Yes, continue with available files" / "No, stop and fix missing files"

**If all required files are present**, display a brief confirmation:

```
## Input Validation Passed

Reports folder: [reports_folder]
Assignments folder: [assignments_folder]

Required files found:
- STUDENT-LIST.md
- [assignment-file.md]
- [grading-criteria-file.md]
- COURSE-DESCRIPTION.md (at [path])
- [N] PDFs in reports folder

Optional files:
- BACKGROUND.md: [Found/Not found]
- SPECIAL-CONSIDERATIONS.md: [Found/Not found]

Proceeding with evaluation...
```

### Step 1: Load Context Files

Read all context files, using the paths determined in Step 0.5:

```
[assignments_folder]/assignment-*.md           # Assignment instructions
[assignments_folder]/*grading-criteria*.md     # Grading rubric
[found_path]/COURSE-DESCRIPTION.md             # Formal G/VG criteria (may be in parent folder)
[assignments_folder]/BACKGROUND.md             # Optional: project context
[assignments_folder]/SPECIAL-CONSIDERATIONS.md # Optional: exceptions
```

**Note:** COURSE-DESCRIPTION.md path comes from the parent folder search in Step 0.5. Pass this resolved path to reviewer subagents.

From these files, extract:
- **Criteria to evaluate** (from grading criteria file)
- **Criteria weights** (from grading criteria file)
- **Pass (G) criteria** (from COURSE-DESCRIPTION.md)
- **Distinction (VG) criteria** (from COURSE-DESCRIPTION.md)
- **Which criteria can earn VG** (from grading criteria file or COURSE-DESCRIPTION.md)

### Step 2: Build Student List

Read `[reports_folder]/STUDENT-LIST.md` and identify:
- Students with "Report Submitted: Yes"
- Students without a grade in "Betyg" column

### Step 2.5: Initialize or Update EVALUATION-STATUS.json

**For new sessions only** (skip if resuming from existing session):

Create `EVALUATION-STATUS.json` in the **reports folder** with:
- Session metadata (assignment name, reports folder, assignments folder, date, batch_size: 3)
- File paths for all context files
- Progress counters (all starting at 0)
- Pre-planned batches (groups of 3 students each)
- Empty completed_evaluations array
- Missing submissions list
- next_batch set to "batch_1"

```json
{
  "evaluation_session": {
    "assignment": "[from assignment instructions title]",
    "reports_folder": "[full path]",
    "assignments_folder": "[full path]",
    "started": "[today's date]",
    "last_updated": "[today's date]",
    "status": "in_progress",
    "batch_size": 3
  },
  ...
}
```

**Write the initial status file before starting any evaluations.**

### Step 3: Parallel Batch Evaluation

**Batch size:** Maximum 3 students per batch. This smaller batch size enables:
- Faster recovery after compacting events
- More frequent progress saves
- Lower risk of losing work

#### 3a. Create Batches

```
students_to_evaluate = [students with submitted reports but no grade]
batches = split into groups of max 3 students
```

**Example for 28 students:**
- batch_1: students 1-3
- batch_2: students 4-6
- ...
- batch_10: students 28 (1 student in final batch)

#### 3b. For Each Batch: Spawn All Reviewers in Parallel

For a batch of N students (max 3), spawn **N × 3 = up to 9 subagents in parallel**.

Each subagent receives the same prompt (see `REVIEWER-PROMPT.md`) instructing them to:
1. Read all context files from the assignments folder
2. Read and evaluate the student's PDF from the reports folder
3. **Follow any code repository links** (GitHub, GitLab, etc.) found in the report
4. Assess each criterion defined in the grading criteria file
5. Determine overall grade based on COURSE-DESCRIPTION.md criteria
6. Write feedback in Swedish

**Spawning pattern for batch:**
```
# Single message with all Task calls for the batch (max 9 parallel tasks):
Task: Student 1 - Reviewer 1
Task: Student 1 - Reviewer 2
Task: Student 1 - Reviewer 3
Task: Student 2 - Reviewer 1
Task: Student 2 - Reviewer 2
Task: Student 2 - Reviewer 3
Task: Student 3 - Reviewer 1
Task: Student 3 - Reviewer 2
Task: Student 3 - Reviewer 3
```

#### 3c. Collect All Results

Wait for **all subagents in the batch** to complete before proceeding. Each returns:
- Student name (to match results)
- Grade (G or VG)
- Criterion assessments (term + comment for each criterion)
- Feedback (3 sentences in Swedish)
- Reasoning (brief justification)

#### 3d. Process Results for Each Student

For each student in the batch, apply consensus:

**Majority voting:**
```
If all 3 agree: Final grade = reviewer grade (unanimous)
If 2/3 agree: Final grade = majority grade (majority)
If all different: Flag for manual review (split)
```

**Select best feedback** from the 3 options:
1. Driest tone (fewest adjectives, no exclamation marks)
2. Most vague on specifics (no risky claims about specific files/code/sections)
3. Clearest grade signal in first sentence (VG = understanding words, G = functional words)
4. No student first name, no filler phrases like "Bra jobbat!"

**Compile instructor summary** from all 3 reviewers:
- Unlike student feedback (select best), instructor summaries are **synthesized** — combine the most useful observations from all 3 reviewers into one 3-5 sentence paragraph
- Cover: strengths, weaknesses, code quality (if reviewed), what to watch for
- Written in Swedish, candid and direct — this is never shown to students
- Check each reviewer's "Sources Reviewed" table to verify code was actually reviewed

**Verify code review** from Sources Reviewed tables:
- Check that at least 2 of 3 reviewers fetched the GitHub repo
- If no reviewer checked the code, note this in the terminal output
- The instructor summary should reflect whether the code was actually reviewed

#### 3e. Batch Write Results

After processing all students in the batch:

1. **Append all evaluations** to `GRADING-RESULTS.md` in the reports folder in one write operation
2. **Update all grades** in `STUDENT-LIST.md` in the reports folder in one edit operation
3. **Update EVALUATION-STATUS.json** in the reports folder with batch completion

**Status file updates after each batch:**
```json
{
  "evaluation_session": {
    "last_updated": "[current date]"
  },
  "progress": {
    "evaluated": [previous + batch count],
    "remaining": [previous - batch count]
  },
  "batches": {
    "[current_batch]": {
      "status": "completed",
      "results": {"VG": N, "G": M}
    }
  },
  "completed_evaluations": [
    // Append new evaluations
    {"name": "...", "grade": "...", "consensus": "..."}
  ],
  "next_batch": "[next_batch_key or null if done]"
}
```

This batch write approach:
- **Saves progress immediately** - Results survive compacting events
- Reduces file I/O operations
- Prevents partial state if interrupted
- Enables exact resumption from status file

#### 3f. Display Batch Summary

After each batch completes, display summary for all students in the batch:

```
## Batch 1/10 Complete (3 students)

| Student | Grade | Consensus |
|---------|-------|-----------|
| Andersson, Anna | VG | 3/3 |
| Eriksson, Erik | G | 2/3 |
| Johansson, Johan | VG | 3/3 |

✓ 3 evaluations saved to GRADING-RESULTS.md
✓ STUDENT-LIST.md updated with grades
✓ EVALUATION-STATUS.json updated (next: batch_2)

Progress: 3/28 students evaluated (10.7%)
```

#### 3g. Continue with Next Batch

If more batches remain, repeat steps 3b-3f for the next batch.

**Important:** Each batch is self-contained. If a compacting event occurs:
1. The skill will restart from Step 0
2. Status file will show which batch to resume
3. Only the current incomplete batch needs re-evaluation
4. All previously completed batches are preserved

### Step 4: Mark Session Complete and Display Final Summary

After all batches complete:

1. **Update EVALUATION-STATUS.json** to mark session completed:
```json
{
  "evaluation_session": {
    "status": "completed",
    "last_updated": "[current date]"
  },
  "next_batch": null
}
```

2. **Display overall progress:**

```
## Evaluation Complete

**Total students evaluated:** [N]
**Batches processed:** [M] (batch size: 3)

| Grade | Count |
|-------|-------|
| VG | [X] |
| G | [Y] |

**Consensus quality:**
- Unanimous (3/3): [N]
- Majority (2/3): [N]
- Split (flagged): [N]

✓ EVALUATION-STATUS.json marked as completed

Proceeding to generate summary tables...
```

### Step 5: Generate Summary Tables

After all students are evaluated, add **two summary sections** to GRADING-RESULTS.md:

#### 5a. Compact Assessment Overview Table

Create a table showing all evaluated students with their grades and criterion assessments at a glance. This table should:

1. **Use abbreviated criterion names** derived from the grading criteria file
2. **Include all students** sorted alphabetically by last name
3. **Show the consensus vote count** (e.g., "3/3", "2/3", or "Override" for instructor adjustments)
4. **Display criterion assessments** using the grading scale terms (Okej/Bra/Mycket bra/Utmärkt)

**Template format:**

```markdown
## Sammanfattning

| Student | Betyg | Röster | [Criterion 1] | [Criterion 2] | [Criterion 3] | ... |
|---------|-------|--------|----------------|----------------|----------------|-----|
| Lastname, Firstname | VG | 3/3 | Bra | Mycket bra | Bra | ... |
| ... | ... | ... | ... | ... | ... | ... |
```

**Note:** Column names come from the assignment's grading criteria file. Each assignment may have different criteria names and counts.

**Column guidelines:**
- **Student**: "Lastname, Firstname" format for easy alphabetical sorting
- **Betyg**: Final grade (G or VG)
- **Röster**: Vote count (3/3, 2/3) or "Override" if instructor adjusted
- **Criterion columns**: Use the most representative assessment from the three reviewers (majority or most common)

#### 5b. Statistics Summary

After the compact table, add statistical summaries:

```markdown
# Summary Statistics

## Grade Distribution

| Grade | Count | Percentage |
|-------|-------|------------|
| **VG (Väl godkänt)** | [N] | [X]% |
| **G (Godkänt)** | [N] | [X]% |
| **Total Evaluated** | [N] | 100% |

### VG Recipients ([N] students)

| Student | Consensus | Notable Strength |
|---------|-----------|------------------|
| [Name] | 3/3 | [Key observation from evaluation] |
| ... | ... | ... |

### Consensus Breakdown

| Voting Pattern | Count |
|----------------|-------|
| Unanimous (3/3) | [N] |
| Majority (2/3) | [N] |
| Split (1/1/1) | [N] |

### Missing Submissions ([N] students)

- [Name]
- ...

---

*Evaluation completed: [DATE]*
*Method: Three-reviewer consensus grading*
*All feedback written in Swedish using du/din form*
```

#### 5c. Table Placement

The summaries should be placed at the **end** of GRADING-RESULTS.md, after all individual student evaluations. Structure:

```
# Grading Results - [Assignment Name]
[Individual student evaluations...]
---
## Sammanfattning
[Compact assessment overview table]
---
# Summary Statistics
[Statistics tables]
```

## Grading Scale Reference

| Swedish Term | English | Grade Level |
|--------------|---------|-------------|
| Okej | Okay | Pass minimum |
| Bra | Good | Solid pass |
| Mycket bra | Very Good | Distinction level |
| Utmärkt | Excellent | Beyond requirements |

| Grade | Swedish | Criteria |
|-------|---------|----------|
| G | Godkänt | All criteria meet minimum |
| VG | Väl godkänt | G criteria + VG-eligible criteria show deeper understanding |

## Single Student Evaluation

To evaluate just one student:

```
Evaluate the report for [Student Name] in [reports-folder-path]
Use the three-reviewer method from report-evaluation skill.
```

## Batch Evaluation

To evaluate all remaining students:

```
Evaluate all ungraded students in [reports-folder-path]
Use parallel batch mode (3 students per batch, 3 reviewers each).
Write results to GRADING-RESULTS.md, STUDENT-LIST.md, and EVALUATION-STATUS.json after each batch.
```

**Performance characteristics:**
- Up to 9 parallel subagents per batch (3 students × 3 reviewers)
- Results saved after each batch (survives compacting events)
- Automatic resumption from EVALUATION-STATUS.json
- Progress visible at each batch completion

**Resuming after compaction:**
```
Resume evaluation in [reports-folder-path]
The EVALUATION-STATUS.json will be read automatically.
```

## Handling Split Decisions

If reviewers split 1/1/1 (e.g., G, VG, G with different reasoning):

1. Display all three assessments in terminal
2. Note the split in GRADING-RESULTS.md
3. Use majority grade but flag: `G (split - instructor review)`
4. Include all three feedback options for instructor to choose

## Quality Controls

Before completing:

- [ ] All students in STUDENT-LIST.md have grades
- [ ] GRADING-RESULTS.md has entry for each evaluated student
- [ ] EVALUATION-STATUS.json shows status: "completed"
- [ ] EVALUATION-STATUS.json progress matches actual evaluated count
- [ ] Summary table reflects all evaluations
- [ ] Split decisions flagged for review
- [ ] Feedback is in Swedish and uses "du/din"

## Privacy Note

All files in the reports folder (`docs/student-reports/`) are gitignored and contain student names and grades. They must be:

1. Never committed to public repositories
2. Shared only with authorized instructors
3. Deleted after course completion

---

## Changelog

### 1.2.0 — Feedback tone and instructor summaries

- Dry feedback tone: no exclamation marks, no superlatives, no filler phrases, no student names
- Grade-signal vocabulary in first sentence (VG = "förståelse/resonemang/röd tråd", G = "fungerande/alla delmoment/på plats")
- Vague on specifics: no filenames, code snippets, or section titles in student feedback
- Tech terminology guide: keep English terms used in Swedish as-is (deploy, CI/CD, IaC, etc.)
- Added Sources Reviewed table: reviewers must report which URLs they fetched
- Code review verification: at least 2/3 reviewers must fetch the GitHub repo
- Added Instructor Summary section: candid per-student assessment in Swedish (strengths, weaknesses, code quality, watch for)
- Instructor summaries synthesized from all 3 reviewers (not just best pick)

### 1.1.0 — Split directory layout

- Split layout: reports folder (input) + assignments folder (auto-derived)
- Auto-discovery: derives assignments folder from reports folder path
- Flexible file matching: `assignment-*.md` and `*grading-criteria*.md` patterns
- BACKGROUND.md and SPECIAL-CONSIDERATIONS.md now optional
- Removed hardcoded EVALUATION-CRITERIA.md (use assignment-specific grading criteria)
- Outputs (GRADING-RESULTS.md, EVALUATION-STATUS.json) go to reports folder (gitignored)
- Moved CLASS-LIST.md to `docs/student-reports/CLASS-LIST.md`

### 1.0.0 — Initial release

- Three independent reviewer consensus grading
- Section-by-section assessment in Swedish
- Majority voting compilation into GRADING-RESULTS.md
- Privacy protection for grading output files
