# Verify Exercise Command

Test exercises end-to-end by following them step-by-step like a student would, then generate test reports.

## Usage

```
/verify-exercise <exercise-path> [exercise-numbers...]
```

**Examples:**
```bash
# Test all exercises in directory
/verify-exercise reference/news-flask/docs/exercises

# Test specific exercises (dependencies auto-included)
/verify-exercise reference/news-flask/docs/exercises 1 2 3

# Test just exercise 3 (runs 1, 2 first as dependencies)
/verify-exercise reference/news-flask/docs/exercises 3
```

## Arguments

- `$ARGUMENTS` contains all arguments passed to the command
- First argument: Path to exercise directory (required)
- Remaining arguments: Exercise numbers to test (optional, space-separated)

## Execution Instructions

### Step 1: Parse Arguments

Parse `$ARGUMENTS` to extract:
1. **Exercise path**: First argument (required)
2. **Exercise numbers**: Remaining arguments (optional integers)

**Validation:**
- If no arguments provided, show error: `Error: Exercise path required. Usage: /verify-exercise <path> [numbers...]`
- Verify the exercise path exists

### Step 2: Discover Exercises

Find all files matching pattern `N-*.md` in the exercise path where N is a number.

**Discovery process:**
1. Use Glob to find `*.md` files in the exercise path
2. Filter to files starting with a digit followed by `-`
3. Sort numerically by the leading number
4. Extract title from first `# ` heading in each file

**Example discovery:**
```
Found exercises:
  1: 1-from-script.md - "From Script to Web Application"
  2: 2-project-structure.md - "Project Structure and Application Factory"
  3: 3-template-inheritance.md - "Template Inheritance"
```

**Error handling:**
- If path doesn't exist: `Error: Exercise path '<path>' not found.`
- If no numbered exercises found: `Error: No numbered exercise files found in '<path>'.`
- If requested number exceeds available: `Error: Exercise N requested but only M exercises found.`

### Step 3: Determine Test Plan

**If no exercise numbers provided:** Test ALL discovered exercises in order.

**If exercise numbers provided:**
- Include all dependencies (earlier exercises)
- Mark which are dependencies vs requested

**Dependency rule:** Exercises are sequential. If user requests exercise 3, automatically include 1 and 2 as dependencies.

**Display test plan:**
```
Test Plan:
  - Exercise 1: From Script to Web Application (dependency)
  - Exercise 2: Project Structure (dependency)
  - Exercise 3: Template Inheritance (requested)
```

### Step 4: Setup Test Directory

1. Extract directory name from path: `reference/news-flask/docs/exercises` → `exercises`
2. Set test root: `test-exercises/<directory-name>/`
3. If test directory exists, delete it completely
4. Create fresh test directory

**Example:**
```bash
rm -rf test-exercises/exercises/
mkdir -p test-exercises/exercises/
cd test-exercises/exercises/
```

### Step 5: Execute Each Exercise

For each exercise in the test plan (in numerical order):

#### 5.1 Read and Parse Exercise

1. Read the exercise markdown file completely
2. Identify:
   - Prerequisites section
   - Step-by-step instructions
   - Code blocks with file contents
   - Commands to run
   - Expected outcomes/success indicators
   - Verification checklist

#### 5.2 Execute Steps

Follow the exercise exactly as a student would:

1. **Create files** when exercise shows code blocks with filenames
2. **Run commands** as specified (pip install, flask run, etc.)
3. **For Flask applications:**
   - Start on port 5001 (avoids macOS AirPlay conflict on 5000)
   - Run in background
   - Wait 3 seconds for startup
   - Test with curl
   - Capture output
   - Stop the server
4. **Verify outcomes** against what the exercise says should happen

#### 5.3 Flask Server Testing Pattern

When an exercise requires testing a Flask application:

```bash
# Activate virtual environment if it exists
source .venv/bin/activate 2>/dev/null || true

# Start Flask in background on port 5001
flask run --port 5001 &
FLASK_PID=$!
sleep 3

# Test endpoints as specified in exercise
curl -s http://localhost:5001/
# ... additional tests from exercise ...

# Cleanup
kill $FLASK_PID 2>/dev/null
```

#### 5.4 Virtual Environment Handling

- Create virtual environment when exercise instructs
- Activate before running pip or flask commands
- State persists between exercises (like a real student working through them)

#### 5.5 Record Results

For each step, record:
- What was attempted
- What happened (output, errors)
- Whether it matched expectations
- PASS or FAIL status

### Step 6: Generate Per-Exercise Report

After completing (or failing) each exercise, create `TEST-REPORT-N.md`:

```markdown
# Test Report: Exercise N - [Title]

**Date:** [ISO timestamp]
**Exercise File:** [filename]
**Status:** PASS / FAIL

## Summary

[One-line summary of outcome]

## Steps Executed

| Step | Description | Status | Notes |
|------|-------------|--------|-------|
| 1 | [description] | PASS | - |
| 2 | [description] | FAIL | [error details] |

## Flask Application Test

**Command:** `flask run --port 5001`
**Expected Output:** [from exercise]
**Actual Output:** [captured]
**Match:** YES / NO

## Files Created

```
[tree output of files created during this exercise]
```

## Issues Encountered

[List any issues, or "None"]

## Verification Checklist

[Copy checklist from exercise with actual results]
- [x] Item that passed
- [ ] Item that failed - [reason]
```

### Step 7: Handle Failures

**On exercise failure:**

1. Write the TEST-REPORT-N.md with FAIL status and details
2. Display clear error message:
   ```
   EXERCISE N FAILED: [Title]

   [Brief error description]

   Stopping test run. Exercises [list] were not tested (depend on Exercise N).
   See: test-exercises/<dir>/TEST-REPORT-N.md
   ```
3. Skip to generating final report
4. Do NOT attempt later exercises

### Step 8: Generate Final Report

After all exercises complete (or on failure), create `FINAL-REPORT.md`:

```markdown
# [Exercise Set Name] - Final Test Report

**Generated:** [ISO timestamp]
**Exercise Path:** [original path]
**Test Directory:** [test directory path]

## Overall Result: PASS / FAIL

## Summary Table

| # | Exercise | Status | Duration | Issues |
|---|----------|--------|----------|--------|
| 1 | [title] | PASS | - | None |
| 2 | [title] | PASS | - | None |
| 3 | [title] | FAIL | - | [brief] |
| 4 | [title] | SKIPPED | - | Dependency failed |

## Detailed Results

### Exercise 1: [Title]
**Status:** PASS
**Steps:** 5/5 passed
[Key observations]

### Exercise 2: [Title]
...

## Test Environment

- **Working Directory:** [path]
- **Python Version:** [version]
- **Flask Version:** [version if installed]
- **Port Used:** 5001

## Final Directory Structure

```
[tree output of test-exercises/<dir>/]
```

## Conclusion

[Summary of results, any patterns in failures, recommendations]
```

### Step 9: Report to User

Display final summary:

```
=== EXERCISE VERIFICATION COMPLETE ===

Result: PASS / FAIL
Tested: N exercises
Passed: M
Failed: F
Skipped: S

Reports:
  - test-exercises/<dir>/TEST-REPORT-1.md
  - test-exercises/<dir>/TEST-REPORT-2.md
  - test-exercises/<dir>/FINAL-REPORT.md
```

## Error Messages Reference

| Situation | Message |
|-----------|---------|
| No arguments | `Error: Exercise path required. Usage: /verify-exercise <path> [numbers...]` |
| Path not found | `Error: Exercise path '<path>' not found.` |
| No exercises | `Error: No numbered exercise files found in '<path>'.` |
| Invalid number | `Error: Exercise N requested but only M exercises found.` |
| Exercise fails | `EXERCISE N FAILED: [Title]\n\n[Details]\n\nStopping test run...` |

## Output Structure

```
test-exercises/
└── <exercise-dir-name>/
    ├── [exercise-created-dirs]/
    │   └── [exercise-created-files]
    ├── TEST-REPORT-1.md
    ├── TEST-REPORT-2.md
    ├── TEST-REPORT-N.md
    └── FINAL-REPORT.md
```

## Important Notes

1. **Port 5001**: Always use port 5001 for Flask to avoid macOS AirPlay conflict on port 5000
2. **Fresh start**: Test directory is deleted and recreated each run
3. **Sequential**: Exercises must pass in order; failure stops the run
4. **Student perspective**: Execute exactly what the exercise instructs, no shortcuts
5. **State persistence**: Virtual environments and files persist between exercises (cumulative)
6. **Capture everything**: Log all command outputs for debugging failed exercises
