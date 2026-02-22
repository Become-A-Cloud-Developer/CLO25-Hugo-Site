---
name: repo-download
version: 1.0.0
description: Download student GitHub repositories as ZIP archives for grading. Extracts repo URLs from student report PDFs, downloads via gh CLI, and updates STUDENT-LIST.md with repo info. Run after report-preparation skill.
allowed-tools: Read, Write, Edit, Glob, Grep, Bash, Task
triggers:
  - download repos
  - student repositories
  - download source code
  - fetch repos
---

# Repo Download Skill

Download student GitHub repositories as ZIP archives for grading. Extracts repo URLs from report PDFs, downloads via `gh` CLI, and updates STUDENT-LIST.md.

**Run after:** `report-preparation` skill (requires renamed PDFs and STUDENT-LIST.md)

## Required Inputs

When invoking this skill, provide:

1. **Reports folder path** - Location of the processed student reports
   - Example: `docs/assignments/assignment-1/student-reports/`

## Processing Workflow

### Step 1: Verify Prerequisites

Before any processing, confirm:

```bash
# 1. Verify gh CLI is authenticated
gh auth status

# 2. Verify STUDENT-LIST.md exists (report-preparation has been run)
ls [reports_folder]/STUDENT-LIST.md

# 3. Verify renamed PDFs exist
ls [reports_folder]/*.pdf

# 4. Verify folder is gitignored
git check-ignore [reports_folder]
```

**STOP and warn user if:**
- `gh` is not authenticated
- STUDENT-LIST.md does not exist (run report-preparation first)
- No renamed PDFs found
- Folder is not gitignored

### Step 2: Extract GitHub URLs from PDFs (Parallel Subagents)

**Spawn one subagent per PDF** to avoid context overflow:

For each PDF file, create a Task with prompt:

```
Read the PDF at [path] and extract any GitHub repository URL.

Look for URLs in:
1. Title page or cover
2. Document body (links, references)
3. Headers or footers
4. Appendices

URL patterns to match:
- https://github.com/user/repo
- github.com/user/repo
- https://github.com/user/repo.git
- http://github.com/user/repo

Return ONLY:
- repo_url: The full GitHub URL (normalized to https://github.com/owner/repo)
- confidence: high/medium/low
- location: Where in the document the URL was found (e.g., "title page", "body text", "link")
- notes: Any relevant context (e.g., "multiple URLs found, selected main repo")

If multiple URLs are found:
- Prefer the one that appears to be the student's own project repository
- Prefer URLs on the title/cover page
- Exclude URLs that are clearly references to documentation or external libraries

If no GitHub URL found, return:
- repo_url: NOT_FOUND
- confidence: high
- notes: Reason (e.g., "no GitHub URLs in document")
```

Collect results and map each to the student's file prefix (from the PDF filename).

### Step 3: Validate and Normalize URLs

For each extracted URL:

1. **Parse owner/repo** from the URL
   - `https://github.com/owner/repo` → owner, repo
   - Strip trailing `.git`, `/`, `/tree/...`, `/blob/...`

2. **Verify repo exists and is accessible:**
   ```bash
   gh repo view owner/repo --json name,visibility 2>&1
   ```

3. **Categorize result:**

   | Result | Action |
   |--------|--------|
   | Public repo, accessible | Proceed to download |
   | Private repo | Record as "Private", skip download, flag for review |
   | Repo not found (404) | Record as "Not found (404)", skip download, flag for review |
   | URL parsing failed | Record as "Invalid URL", skip download, flag for review |

4. **Report validation summary:**
   ```
   ✓ Validated: X repos accessible
   ⚠ Private: Y repos (manual review needed)
   ✗ Not found: Z repos
   ```

### Step 4: Download as ZIP (Parallel)

For each validated, accessible repository:

1. **Check if ZIP already exists** (student may have submitted source code manually):
   ```bash
   ls [reports_folder]/[prefix]_source-code.zip 2>/dev/null
   ```
   If exists → skip download, note "ZIP already present (manual submission)"

2. **Download ZIP archive:**
   ```bash
   gh api repos/{owner}/{repo}/zipball > [reports_folder]/[prefix]_source-code.zip
   ```
   - Naming convention: `lastname_firstname_source-code.zip` (matches PDF prefix)

3. **Verify download:**
   ```bash
   # Check file exists and is non-empty
   ls -la [reports_folder]/[prefix]_source-code.zip
   # Verify it's a valid ZIP
   file [reports_folder]/[prefix]_source-code.zip
   ```

4. **If download fails:**
   - Log the error
   - Record as "Download failed" in STUDENT-LIST.md
   - Continue with remaining students

### Step 5: Update STUDENT-LIST.md

Read the existing STUDENT-LIST.md and update it:

1. **Add `Repo` column to the main table:**

   ```markdown
   | Full Name | File Prefix | Report Submitted | Repo | Betyg |
   |-----------|-------------|------------------|------|-------|
   | Tom Ekstrand | `ekstrand_tom` | Yes | [Link](https://github.com/user/repo) | |
   | Claes Fransson | `fransson_claes` | Yes | Not found | |
   | Anna Svensson | `svensson_anna` | Yes | Private | |
   | Missing Student | `student_missing` | No | — | |
   ```

   Repo column values:
   - `[Link](url)` — accessible repo with URL
   - `Not found` — no URL in report
   - `Private` — repo exists but is private
   - `Not found (404)` — URL found but repo doesn't exist
   - `—` — no report submitted

2. **Add a `## Repositories` section** after existing sections:

   ```markdown
   ## Repositories

   | Student | Repository | ZIP File |
   |---------|-----------|----------|
   | Tom Ekstrand | https://github.com/user/repo | `ekstrand_tom_source-code.zip` |
   | Claes Fransson | Not found in report | — |
   | Anna Svensson | Private: https://github.com/user/repo | — |
   | Erik Johansson | https://github.com/user/repo | `johansson_erik_source-code.zip` (manual) |
   ```

### Step 6: Report Statistics

Display final summary:

```
## Repo Download Complete

✓ PDFs scanned: X
✓ Repos found: Y
✓ Repos downloaded: Z
✓ Already had ZIP (manual): N
⚠ Not found in report: N
⚠ Private repos: N
⚠ Download failures: N
```

## Edge Cases

### Student Submitted ZIP Manually

If `[prefix]_source-code.zip` already exists before downloading:
- Skip the download
- Still extract and record the URL from the PDF
- Note as "manual submission" in the Repositories table

### Multiple URLs in Report

When a PDF contains multiple GitHub URLs:
- Prefer the URL on the title/cover page
- Prefer URLs that look like project repos (not forks of course templates)
- Prefer URLs under the student's own GitHub account
- If still ambiguous, pick the first one and note "multiple URLs found" in the log

### Private Repository

- Record the URL in STUDENT-LIST.md as `Private: url`
- Do not attempt download
- Flag for manual review (instructor may need to request access)

### Repository URL Points to Fork or Organization

- Handle normally — download the ZIP regardless of owner type
- Note in log if it appears to be a fork of the course template repo

### No URL Found in Report

- Record as "Not found in report"
- Flag for manual review
- The student may have submitted code via other means or forgotten to include the link

### ZIP Filename Conflicts

If a manually-submitted ZIP has a different naming pattern:
- Do not overwrite or rename existing files
- Skip download for that student
- Note the existing file in the log

### URL Variations

All of these should be normalized to `https://github.com/owner/repo`:
- `github.com/owner/repo`
- `https://github.com/owner/repo.git`
- `https://github.com/owner/repo/`
- `https://github.com/owner/repo/tree/main`
- `https://github.com/owner/repo/blob/main/README.md`
- `http://github.com/owner/repo`

## Privacy Checklist

Before completing, verify:

- [ ] All downloaded ZIPs are in a gitignored folder
- [ ] No student repository URLs leaked to git
- [ ] STUDENT-LIST.md is gitignored

```bash
# Final verification
git check-ignore [reports_folder]
git status | grep student-reports
```

## Output

After successful processing:
1. **One ZIP per accessible repo** — named `lastname_firstname_source-code.zip`
2. **STUDENT-LIST.md updated** with Repo column and Repositories section
3. **Statistics displayed** in terminal
4. **Flagged items** listed for manual review (private, not found, failures)

---

## Changelog

### 1.0.0 — 2026-02-22

- Initial release
- Extract GitHub URLs from student report PDFs using parallel subagents
- Validate and normalize repository URLs via `gh repo view`
- Download repositories as ZIP archives via `gh api repos/{owner}/{repo}/zipball`
- Skip students with existing manual ZIP submissions
- Update STUDENT-LIST.md with Repo column and Repositories section
- Handle edge cases: private repos, missing URLs, multiple URLs, URL variations
- Privacy protection checks for gitignored folders
