# Reviewer Prompt Template

This is the exact prompt template to use when spawning reviewer subagents. Each student report is evaluated by 3 independent subagents using this same prompt.

## Template

Replace placeholders with actual values:
- `[REPORTS_FOLDER]` - Full path to reports folder (where PDFs and student data are)
- `[ASSIGNMENTS_FOLDER]` - Full path to assignments folder (where instructions and criteria are)
- `[COURSE_DESC_PATH]` - Full resolved path to COURSE-DESCRIPTION.md
- `[FULL_NAME]` - Student's full name
- `[FILENAME]` - PDF filename

```
Evaluate this student report.

**Step 1: Read context files**

Read these files to understand what to evaluate and the grading criteria:

1. [ASSIGNMENTS_FOLDER]/assignment-*.md
   - The assignment instructions
   - Describes what each section/criterion should contain

2. [ASSIGNMENTS_FOLDER]/*grading-criteria*.md
   - Grading rubric with criteria, weights, and G/VG levels
   - Defines the sections/criteria to assess

3. [COURSE_DESC_PATH]
   - Formal course learning objectives
   - Pass (G) and Distinction (VG) criteria
   - Which objectives this assignment examines
   - Note: This file may be in a parent folder shared across assignments

4. [ASSIGNMENTS_FOLDER]/BACKGROUND.md (if exists — optional)
   - Project scenario and context
   - Learning context (group work, weekly demos, etc.)
   - How this assignment connects to course objectives

5. [ASSIGNMENTS_FOLDER]/SPECIAL-CONSIDERATIONS.md (if exists — optional)
   - Exceptions and adjustments for this assignment
   - What was NOT required due to constraints
   - How to handle missing components

**Step 2: Read and evaluate the student report**

- Student: [FULL_NAME]
- File: [REPORTS_FOLDER]/[FILENAME]

Read the entire PDF and assess each criterion defined in the grading criteria file.

**Step 2b: Follow code repository links and source code**

If a source-code ZIP exists at [REPORTS_FOLDER]/[PREFIX]_source-code.zip, the student's code has been downloaded. Use the GitHub URL from STUDENT-LIST.md to review the repository via WebFetch for supplementary context.

If the report contains links to GitHub, GitLab, or other code repositories/snippets:

1. **Follow the link** using WebFetch to investigate the repository
2. **Review relevant code** that relates to the report content
3. **Use the code as context** for your assessment - does the code support what the report claims?
4. **Note code quality** if it strengthens or clarifies the student's work

This provides a more complete picture of the student's actual implementation, not just their description of it.

**Examples of links to follow:**
- GitHub repository links (github.com/...)
- GitLab repository links (gitlab.com/...)
- GitHub Gist links (gist.github.com/...)
- Azure DevOps repository links (dev.azure.com/...)
- Code snippet links (pastebin, codepen, etc.)

**Do NOT penalize** students who don't include code links - this is supplementary context, not a requirement.

**Step 3: Provide criterion-by-criterion evaluation**

For each criterion in the grading criteria file, provide:
1. Swedish assessment term: Okej / Bra / Mycket bra / Utmärkt
2. One sentence explanation in Swedish

Assessment scale:
- Okej = Meets minimum, just passes
- Bra = Clearly meets requirements, solid work
- Mycket bra = Exceeds requirements, deeper understanding
- Utmärkt = Exceptional, goes beyond requirements

**Step 4: Determine overall grade**

Based on COURSE-DESCRIPTION.md criteria:
- Godkänt (G): All criteria meet minimum requirements
- Väl godkänt (VG): G criteria met AND VG-eligible criteria show deeper understanding

Check the grading criteria file for which criteria can earn VG.

**Step 5: Write feedback**

Write 3 sentences in Swedish, speaking directly to the student using "du/din".

Tone guidelines:
- Be warm, encouraging, and genuine
- Speak like a teacher talking face-to-face
- Use natural conversational Swedish
- Avoid bureaucratic or stiff language
- It's okay to use exclamation marks
- Be specific about what stood out

Avoid (in ALL parts - criterion comments, Återkoppling, AND Motivering):
- Superlatives like "imponerande" (impressive) - too much praise
- Unnecessary capital letters (only for abbreviations like NSG, SSH)
- Any mention of VG or other grades ("För VG...", "VG kräver...", "VG-nivå", "VG-kriterierna")
- Words like "saknar", "saknas", "men saknar" (missing/lacks) - don't describe what's NOT there
- "men" statements ("men beskrivningen...", "men endast", "men verifieringen") - no "but" clauses
- Words like "dock", "emellertid", "å andra sidan" - no contrast/limitation phrases
- Suggesting "next steps" or improvements
- Describing what the student DIDN'T do - focus ONLY on what they DID

Structure:
1. Strength: What the student did well (be specific and enthusiastic)
2. Achievement: Highlight something that stood out
3. Recognition: Warm, encouraging closing

**Critical tone test:** Before finalizing feedback, ask yourself:
1. Would I actually say this to the student face-to-face?
2. Does it sound like a real person, not a template?
3. Is there genuine warmth in the words?

If any answer is "no", rewrite until all answers are "yes".

**Return your evaluation in this format:**

**IMPORTANT:** Always include the student's full name exactly as provided - this is required for matching results in parallel batch processing.

---

## [Student Name]

### Bedömning per avsnitt

| Avsnitt | Bedömning | Kommentar |
|---------|-----------|-----------|
| [Criterion 1 from grading criteria] | [term] | [comment in Swedish] |
| [Criterion 2 from grading criteria] | [term] | [comment in Swedish] |
| [Continue for all criteria...] | | |

### Betyg: **[Godkänt/Väl godkänt]**

### Återkoppling

[3-sentence feedback in Swedish]

### Motivering

[1-2 sentences explaining why the student earned THIS grade. ONLY describe what the student DID - never mention VG, never compare to other grades, never say what is "missing" or "lacking".]

---
```

## Example Completed Prompt

```
Evaluate this student report.

**Step 1: Read context files**

Read these files to understand what to evaluate and the grading criteria:

1. /path/to/docs/assignments/assignment-1/assignment-1.md
2. /path/to/docs/assignments/assignment-1/assignment-1-grading-criteria.md
3. /path/to/docs/COURSE-DESCRIPTION.md  # Found in parent folder
4. /path/to/docs/assignments/assignment-1/BACKGROUND.md  # (if exists)
5. /path/to/docs/assignments/assignment-1/SPECIAL-CONSIDERATIONS.md  # (if exists)

**Step 2: Read and evaluate the student report**

- Student: Anna Andersson
- File: /path/to/docs/student-reports/assignment-1/andersson_anna_rapport.pdf

[... rest of template ...]
```

## Notes for Main Agent

### Single Student Mode

When spawning 3 reviewer subagents for one student:

1. **Use identical prompts** - All 3 reviewers get the same prompt
2. **Spawn in parallel** - Use Task tool 3 times in same message
3. **Wait for all 3** - Don't proceed until all return
4. **Handle failures** - If one fails, note which and continue with 2/3

```
# Spawn 3 parallel tasks for single student:
Task 1: [Full prompt with folder paths, student name, and filename]
Task 2: [Identical prompt]
Task 3: [Identical prompt]
```

### Parallel Batch Mode (Recommended for Multiple Students)

When evaluating multiple students, spawn all reviewers for a batch in one message:

1. **Batch size** - Maximum 3 students per batch (9 subagents)
2. **Single spawn message** - All Task calls in one message for true parallelism
3. **Wait for entire batch** - Collect all results before processing
4. **Match by student name** - Group results by student name for consensus

```
# Spawn all reviewers for batch of N students (max 3):
Task: Student 1, Reviewer 1 [prompt with student 1 details]
Task: Student 1, Reviewer 2 [prompt with student 1 details]
Task: Student 1, Reviewer 3 [prompt with student 1 details]
Task: Student 2, Reviewer 1 [prompt with student 2 details]
Task: Student 2, Reviewer 2 [prompt with student 2 details]
Task: Student 2, Reviewer 3 [prompt with student 2 details]
Task: Student 3, Reviewer 1 [prompt with student 3 details]
Task: Student 3, Reviewer 2 [prompt with student 3 details]
Task: Student 3, Reviewer 3 [prompt with student 3 details]
```

### Collecting Results

Each subagent returns:
- **Student name** (for matching in parallel batch processing)
- Criterion assessments (term + comment for each criterion in grading criteria file)
- Overall grade (G or VG)
- Feedback (3 sentences in Swedish)
- Reasoning (brief justification)

**Batch processing note:** When spawning multiple students in parallel (up to 3 students × 3 reviewers = 9 subagents), use the student name in each result to group the 3 reviews for consensus voting.

### Determining Consensus

```
grades = [reviewer1.grade, reviewer2.grade, reviewer3.grade]

if grades.count(grades[0]) == 3:
    final_grade = grades[0]  # Unanimous
    vote_display = "3/3"
elif grades.count("VG") >= 2:
    final_grade = "VG"  # Majority VG
    vote_display = "2/3"
else:
    final_grade = "G"  # Majority G or split
    vote_display = "2/3" if grades.count("G") >= 2 else "split"
```

### Selecting Best Feedback

From 3 feedback options, select based on:

1. **Warmth** - Does it sound genuinely encouraging?
2. **Specificity** - Does it mention specific things from the report?
3. **Natural Swedish** - Does it avoid bureaucratic phrases?

Avoid selecting feedback that:
- Uses formal phrases like "visar på mogen förståelse"
- Sounds like a template
- Is too brief or generic
- Uses superlatives like "imponerande" (impressive)
- Uses unnecessary capital letters
- Tells student what to do for a different grade
- Suggests improvements or "next steps"
