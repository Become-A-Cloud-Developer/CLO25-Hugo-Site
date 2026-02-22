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

Write exactly 3 sentences in Swedish, addressing the student with "du/din". Keep the tone dry — minimal adjectives, no exclamation marks, no superlatives, no filler phrases like "Bra jobbat!".

**Do NOT address the student by first name** in the feedback.

**Stay vague on specifics.** Do not reference specific filenames, code snippets, section names, error messages, or tools from the report. If you cannot verify a specific claim with 100% certainty, keep it general. It is always better to be vague than to be wrong.

**Tech terminology:** Keep established English tech terms in English (deploy, commit, CI/CD, pipeline, reverse proxy, Infrastructure as Code, SSH, NSG). Do not invent Swedish translations for words that Swedish developers use in English.

**Sentence structure:**

1. **Grade signal** — A vague general statement that subtly reflects the grade level:
   - For **VG**: Use words like "förståelse", "resonemang", "sammanhängande", "genomtänkt", "röd tråd", "djup"
   - For **G**: Use words like "fungerande", "alla delmoment", "på plats", "fått ihop", "täcker", "redovisade"
2. **Observation** — One safe, general observation about the work
3. **Closing** — A remark that rounds out the picture (not a filler phrase)

The grade signal must be subtle enough that a student would not notice the pattern, but clear enough that the instructor can identify the grade from the first sentence alone.

Avoid (in criterion comments AND Återkoppling — does NOT apply to the Instructor Summary which should be candid):
- Superlatives like "imponerande" (impressive)
- Enthusiastic phrases ("Riktigt snyggt!", "Kul att se...", "Vilken genomarbetad...")
- Unnecessary capital letters (only for abbreviations like NSG, SSH)
- Any mention of VG or other grades
- Words like "saknar", "saknas" — don't describe what's missing
- "men" statements — no "but" clauses
- Contrast words like "dock", "emellertid"
- Suggesting improvements or "next steps"
- Describing what the student DIDN'T do
- Specific filenames, code, section titles, or error messages from the report
- Student's first name

**Tone test:** Before finalizing, verify:
1. Can the instructor tell the grade from sentence 1 alone?
2. Would a student notice the G/VG pattern? (they should not)
3. Are there any specific claims that could be wrong? (there should not be)
4. Are there exclamation marks or filler phrases? (there should not be)

**Step 6: Write instructor summary**

Write a candid summary for the instructor (not shown to the student). This is the opposite of the dry student feedback — here you should be specific, detailed, and honest about both strengths and weaknesses. The instructor uses this to understand the student going forward in the course.

Write in Swedish. Be direct. You CAN mention weaknesses, gaps, and things the student struggled with.

Cover:
- **Strengths**: What the student genuinely does well. Be specific — reference actual things from the report and code.
- **Weaknesses**: Where the student falls short. What's shallow, missing depth, or poorly explained.
- **Code quality**: If you reviewed the repo, what did the code look like? Well-structured? Messy? Copy-pasted from tutorials?
- **Watch for**: What the instructor should look for in this student's future assignments. Are they on a good trajectory? Do they need support in a specific area?

Keep it to 3-5 sentences total. Be useful, not exhaustive.

**Return your evaluation in this format:**

**IMPORTANT:** Always include the student's full name exactly as provided — this is required for matching results in parallel batch processing.

---

## [Student Name]

### Sources Reviewed

| Source | URL / Path | Status |
|--------|------------|--------|
| Report PDF | [REPORTS_FOLDER]/[FILENAME] | Read in full |
| GitHub repo | [URL or "No URL found"] | [What was reviewed, e.g., "Reviewed README, Program.cs, deployment scripts" / "Repo not accessible" / "No URL in report"] |
| Other | [Any additional sources] | [Status] |

### Bedömning per avsnitt

| Avsnitt | Bedömning | Kommentar |
|---------|-----------|-----------|
| [Criterion 1 from grading criteria] | [term] | [comment in Swedish] |
| [Criterion 2 from grading criteria] | [term] | [comment in Swedish] |
| [Continue for all criteria...] | | |

### Betyg: **[Godkänt/Väl godkänt]**

### Återkoppling

[3-sentence feedback in Swedish — dry tone, vague on specifics, grade signal in sentence 1]

### Instructor Summary

[3-5 sentences in Swedish. Candid assessment of strengths, weaknesses, code quality, and what to watch for. Be specific — this is not shown to the student.]

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
- **Sources reviewed** (table of URLs fetched and what was found — verify code was actually checked)
- Criterion assessments (term + comment for each criterion in grading criteria file)
- Overall grade (G or VG)
- Feedback (3 sentences in Swedish — dry tone)
- Instructor summary (3-5 sentences in English — candid strengths/weaknesses)

**Batch processing note:** When spawning multiple students in parallel (up to 3 students × 3 reviewers = 9 subagents), use the student name in each result to group the 3 reviews for consensus voting.

**Code review verification:** Check each reviewer's Sources Reviewed table. If a reviewer did not fetch the GitHub repo, note this when reporting results. At least 2 of 3 reviewers should have reviewed the code for the assessment to be considered code-verified.

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

1. **Driest tone** - Fewest adjectives, no exclamation marks, no filler
2. **Vagueness on specifics** - No risky claims about specific files, code, sections, or error messages
3. **Grade signal** - First sentence clearly (but subtly) signals VG or G through word choice
4. **Natural Swedish** - No invented tech translations, no bureaucratic phrases

Avoid selecting feedback that:
- Uses enthusiastic phrases ("Riktigt snyggt!", "Kul att se...", "Bra jobbat!")
- References specific filenames, code snippets, or section titles from the report
- Addresses the student by first name
- Uses superlatives like "imponerande"
- Sounds like a template
- Tells student what to do for a different grade
- Suggests improvements or "next steps"
