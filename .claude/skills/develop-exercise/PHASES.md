# Execution Phases

Detailed runbook for Phase B of the `develop-exercise` skill. Read this in full before starting Phase 1.

## Phase 1 — Parallel authoring

**Goal:** produce one markdown file per exercise plus the subsection `_index.md` plus a scaffolded reference project, all in parallel.

**Mechanics:** dispatch one Agent per deliverable in **a single message** containing multiple `Agent` tool calls. Tasks are independent — agents never read each other's output.

**Standard agent prompts** (one per Agent call):

- **Per exercise (one Agent each):**
  - Read these four files first, in this order:
    - `.claude/skills/create-exercise/SKILL.md`
    - `.claude/skills/create-exercise/GUIDE.md`
    - `.claude/skills/create-exercise/TEMPLATE.md`
    - `.claude/skills/create-exercise/EXAMPLE.md`
  - Read one well-formed existing ACD exercise as a house-style reference (e.g. `content/exercises/10-webapp-development/4-authentication-authorization/1-cookie-authentication-and-whoami.md`).
  - Write to the agreed path with the agreed frontmatter (`program = "CLO"`, `cohort = "25"`, `courses = ["ACD"]` — adjust per chapter), agreed `weight`, `draft = false`.
  - Use ~12-step skeleton (often plus the mandatory Test Your Implementation step). Target 450–550 lines; if the topic genuinely demands more, document why.
  - Continuity: each exercise (except the first) opens by referencing the prior state without using exercise *numbers*. End by setting up the next without naming it by number.
  - Never write or commit anything outside the assigned file.

- **For `_index.md`:** mirror the shape of an existing subsection landing page (e.g. `content/exercises/10-webapp-development/4-authentication-authorization/_index.md`). Frontmatter, H1, 2–3 paragraph intro that walks through the arc, an `> ℹ Where this fits` blockquote that situates the chapter inside the broader section, ending with `{{< children />}}`.

- **For reference-project scaffold:** read `develop-exercise/REFERENCE-PROJECT.md` for the templates. Create only the human-facing skeleton (README, CLAUDE.md, .gitignore, EXERCISE-VALIDATION-REPORT.md skeleton, screenshots/.gitkeep). **Do NOT** scaffold the actual source code or Dockerfile here — those come from live execution in Phase 3 and get mirrored back in Phase 5.

**Gate criteria for Phase 1 completion:**

- All declared files exist on disk (verify with Glob).
- `hugo --gc --minify` builds cleanly with no errors.
- Mark each Phase 1 task `completed` as the corresponding agent returns.

## Phase 2 — Cross-review

**Goal:** catch narrative discontinuities, technical errors, and skill-template violations before live execution exposes them.

**Mechanics:** dispatch a **single** Agent (not multiple — the whole point is one mind reading all the files in sequence). Pass it `REVIEW-CHECKLIST.md` as the focus list.

**Agent prompt template:**

> You are reviewing N newly-written content files that form a tight progression. Read them in order, then return a punch list. Do NOT fix anything — the main agent will. Read these files: [...]. Apply the focus areas in `develop-exercise/REVIEW-CHECKLIST.md`. Return: Critical issues (must fix), Significant issues (should fix), Minor issues (nice to fix), Cross-cutting issues, Strengths to preserve. For each issue give file, approximate line, the offending text, and a one-sentence suggested fix. Cap at ~800 words.

**After the review returns:**

- The main agent applies fixes directly using `Edit` (preferring `replace_all` for systematic renames). After targeted renames using `replace_all`, double-check that substrings didn't get rewritten unintentionally — e.g., renaming `cloudsoft-web` → `cloudci` will also rewrite `ca-cloudsoft-web` to `ca-cloudci`, which is rarely what's wanted.
- Re-run `hugo --gc --minify` after fixes.

**Gate criteria for Phase 2 completion:**

- All "Critical" issues fixed.
- All "Significant" issues fixed.
- "Minor" issues either fixed or explicitly accepted.
- Hugo still builds cleanly.

## Phase 3 — Live execution

**Goal:** drive the published exercises end-to-end against the user's real cloud subscription / GitHub account / external services. Catch defects that paper review cannot.

**Mechanics:** **Sequential, leader-driven.** Do NOT use subagents for this — too much shared state (resource IDs, run IDs, FQDNs) for subagent isolation to help.

**Live working directory:** create a sibling to the Hugo site, e.g. `/Users/lasse/Developer/CLO_Development/<project-name>/`. This is its own git working copy, pushed to its own GitHub repo. **Not** inside the Hugo site (avoid nested `.git`).

**Standard sequence:**

1. **Scaffold the source repository** (e.g. `dotnet new mvc -o ProjectName`, `npm init`, `python -m venv`). Add the small visible cue the exercises ask for (build SHA badge, version string, hostname display).
2. **Initialise git locally**, then `gh repo create --public --source=. --push`.
3. **Walk through each exercise's CLI commands** as a real user would. Observe:
   - Command output for warnings, deprecations, missing flags.
   - Whether the next command in the exercise actually works given the previous step's state.
4. **Capture artifacts**: workflow run IDs (`gh run list --limit 5`), resource IDs (`az resource show ...`), FQDNs.
5. **Whenever live execution diverges from the exercise text**, record it in the validation report's "Deviations from Exercise Text" section *and* fold the fix back into the exercise file.

**Secret handling (load-bearing):**

- Docker Hub PATs and other tokens: pipe via `printf '%s' '<token>' | gh secret set NAME --repo <repo>`. Never `echo`. Never write to a temp file.
- Azure SP credentials JSON: `az ad sp create-for-rbac --json-auth 2>/dev/null > /tmp/creds.json`, then `gh secret set AZURE_CREDENTIALS < /tmp/creds.json`, then `rm /tmp/creds.json`. Never `cat` it to chat output.
- Final chat report redacts secrets to shape only (`dckr_pat_…<redacted>`).

**Skip cleanly** if the chapter has no deployment target — note this in the plan and proceed to Phase 5.

**Gate criteria for Phase 3 completion:**

- Every exercise's "happy path" succeeded against real services.
- The final state matches what the last exercise's "Test Your Implementation" section claims.
- All deviations recorded in the validation report.

## Phase 4 — Validation

**Goal:** an automated, repeatable check that the deployed artifact behaves as the chapter promises.

**Tooling by chapter type:**

- **Web app** → small Playwright Node script: launch Chromium → load FQDN → assert HTTP 200 → assert key elements present → save full-page screenshot to `<reference-project>/docs/screenshots/<label>.png`.
- **HTTP API** → `curl --fail` + `jq` assertions on responses; save the request/response transcript to `docs/validation/`.
- **CLI tool** → invoke the tool with sample inputs, capture stdout/stderr, diff against expected output saved as a fixture.
- **Docs-only chapter** → skip Phase 4. The Hugo build itself is the verification.

**Standard Playwright script shape:** see `reference/CloudSoft-Pipeline/scripts/validate.mjs` for an example. Drives `playwright install chromium` + `chromium.launch()` + `page.goto()` + assertions + `page.screenshot()`. Reads target via `FQDN` and `LABEL` env vars.

**Gate criteria for Phase 4 completion:**

- The validation script exits 0 against the live deployment.
- At least one durable artifact (screenshot, transcript, fixture) saved into the reference project.

## Phase 5 — Dual reports

**Goal:** produce two complementary records of what was built and how to verify it.

### 5a — Mirror live state into the reference project

`rsync` the live source directory into `reference/<ProjectName>/src/<...>/` (excluding `.git`, `bin`, `obj`, `node_modules`). Copy the final workflow file. Avoid duplicates (e.g. `.github/workflows/` should appear once at the reference project root, not also nested inside the source dir). Remove stray cache files (`.lscache`, `obj/`, `package-lock.json` — most are caught by the source's `.gitignore`).

### 5b — Populate the validation report

Open `reference/<ProjectName>/docs/EXERCISE-VALIDATION-REPORT.md` and fill in every section:

- Overview (one paragraph, with date).
- Resources Provisioned (table).
- Live URL.
- GitHub Repository (URL + final secret list).
- Workflow Run History (one row per stage that validates a different exercise).
- Build progression / version progression (how the visible cue changed across runs).
- Screenshots (filenames).
- Manual Verification Steps (numbered list of copy-paste commands).
- Deviations from Exercise Text.
- Status (date + green/red).

### 5c — Write the chat report

Structured user-facing summary including:

- Live URL.
- What was built (content files, reference project) — paths.
- Validation evidence (commit SHAs, run links, screenshot paths).
- Cross-review fixes applied (numbered).
- Manual verification commands.
- **Outstanding items requiring user decision**: commit/push approval, secret revocation, cleanup of cloud resources.

### 5d — Ask before commit and push

Per project `CLAUDE.md`. Stage the new content + reference project, dry-run `git add` to confirm nothing accidental gets included (no `obj/`, no `node_modules/`, no `package-lock.json`), then ask the user before committing. Never push without explicit approval.

**Gate criteria for Phase 5 completion:**

- Reference project mirrors the final state of live execution.
- Validation report has zero `<TBD>` markers.
- Chat report sent.
- User has approved or deferred commit/push.
