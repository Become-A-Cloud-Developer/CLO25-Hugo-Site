# Output Format

This document defines the exact structure for grading results output.

## GRADING-RESULTS.md Structure

### File Header

```markdown
# Grading Results - [Assignment Name]

**Course:** [Course Name]
**Evaluated by:** Claude Code (3-reviewer consensus method)
**Date:** YYYY-MM-DD

---
```

### Per-Student Entry

Each evaluated student gets this exact structure. The criterion names (Avsnitt column) come from the assignment's grading criteria file — they vary per assignment.

```markdown
## [Student Full Name]

**Fil:** `lastname_firstname_originalname.pdf`

### Bedömning per avsnitt

| Avsnitt | Bedömning | Kommentar |
|---------|-----------|-----------|
| [Criterion 1] | [Okej/Bra/Mycket bra/Utmärkt] | [Swedish comment] |
| [Criterion 2] | [Okej/Bra/Mycket bra/Utmärkt] | [Swedish comment] |
| [Criterion N] | [Okej/Bra/Mycket bra/Utmärkt] | [Swedish comment] |

### Betyg: **[Godkänt/Väl godkänt]** ([3/3] or [2/3])

### Återkoppling

[3-sentence feedback in Swedish, addressing student with "du/din"]

---
```

### Example Complete Entry

```markdown
## Anna Andersson

**Fil:** `andersson_anna_rapport.pdf`

### Bedömning per avsnitt

| Avsnitt | Bedömning | Kommentar |
|---------|-----------|-----------|
| [Criterion 1] | Bra | Tydlig översikt av projektets syfte och komponenter |
| [Criterion 2] | Mycket bra | Detaljerad förklaring med bra motiveringar |
| [Criterion 3] | Bra | Fungerande lösning demonstrerad med tydliga screenshots |
| [Criterion 4] | Mycket bra | Genomtänkt approach med verifiering |
| [Criterion 5] | Bra | Relevanta punkter med praktiska åtgärder |
| [Criterion 6] | Bra | Ärlig reflektion om process och lärdomar |

### Betyg: **Väl godkänt (VG)** (3/3)

### Återkoppling

Riktigt snyggt jobbat med din lösning - du visar tydligt att du förstår hur alla delar hänger ihop! Din dokumentation är genomarbetad och lätt att följa. Roligt att läsa en rapport med personlig touch som ändå håller tekniskt.

---
```

### Summary Table (End of File)

After all students, add summary table. Column names come from the grading criteria file — use abbreviated forms for space:

```markdown
# Sammanfattning

| Student | Betyg | [Crit 1] | [Crit 2] | [Crit 3] | [Crit 4] | [Crit 5] | [Crit 6] |
|---------|-------|----------|----------|----------|----------|----------|----------|
| Andersson, Anna | VG (3/3) | Bra | Mycket bra | Bra | Mycket bra | Bra | Bra |
| Eriksson, Erik | G (2/3) | Okej | Bra | Bra | Bra | Okej | Bra |
| Johansson, Johan | VG (3/3) | Bra | Bra | Mycket bra | Utmärkt | Bra | Mycket bra |

## Statistik

- **Totalt utvärderade:** X
- **Väl godkänt (VG):** Y
- **Godkänt (G):** Z
- **Enhälliga beslut (3/3):** N
- **Majoritetsbeslut (2/3):** M
```

**Note:** The number and names of criterion columns depend on the assignment's grading criteria file. Each assignment may have different criteria.

## STUDENT-LIST.md Updates

After evaluation, update the Betyg column:

### Before Evaluation

```markdown
| Full Name | File Prefix | Report Submitted | Betyg |
|-----------|-------------|------------------|-------|
| Anna Andersson | `andersson_anna` | Yes | |
| Erik Eriksson | `eriksson_erik` | Yes | |
| Johan Johansson | `johansson_johan` | No | |
```

### After Evaluation

```markdown
| Full Name | File Prefix | Report Submitted | Betyg |
|-----------|-------------|------------------|-------|
| Anna Andersson | `andersson_anna` | Yes | VG (3/3) |
| Erik Eriksson | `eriksson_erik` | Yes | G (2/3) |
| Johan Johansson | `johansson_johan` | No | - |
```

## Vote Count Format

| Pattern | Meaning | Display |
|---------|---------|---------|
| 3 agree | Unanimous | `(3/3)` |
| 2 agree VG | Majority VG | `(2/3)` |
| 2 agree G | Majority G | `(2/3)` |
| Split | No majority | `(split)` |

## Terminal Output Format

After each student evaluation, display:

```
## Anna Andersson - Evaluation Complete

| Reviewer | Grade | Key Observation |
|----------|-------|-----------------|
| 1 | VG | Strong documentation with verification |
| 2 | VG | Thorough approach throughout |
| 3 | VG | Clear understanding demonstrated |

**Final Grade: VG (3/3 unanimous)**

✓ Saved to GRADING-RESULTS.md
✓ Updated STUDENT-LIST.md
```

For split decisions:

```
## Erik Eriksson - Evaluation Complete

| Reviewer | Grade | Key Observation |
|----------|-------|-----------------|
| 1 | VG | Good depth in key areas |
| 2 | G | Meets requirements solidly |
| 3 | G | Basic verification shown |

**Final Grade: G (2/3 majority)**

⚠ Split decision: 1 reviewer gave VG

✓ Saved to GRADING-RESULTS.md
✓ Updated STUDENT-LIST.md
```

## Section Comment Guidelines

Keep comments concise (under 10 words) and in Swedish:

| Context | Good Example | Too Long |
|---------|--------------|----------|
| Strong work | "Tydlig och genomarbetad" | "Studenten ger en tydlig och genomarbetad beskrivning av..." |
| Solid pass | "Fungerande lösning med bra screenshots" | "Lösningen fungerar och studenten visar detta med screenshots..." |
| Good depth | "Välmotiverad approach" | "Visar en välmotiverad approach med tydliga förklaringar..." |

## Privacy Note

GRADING-RESULTS.md contains:
- Student names
- Grades
- Individual feedback

Must be protected:
- Add to `.gitignore`
- Never commit to public repositories
- Share only with authorized instructors
- Delete after course completion
