# Reference Project Pattern

The lightweight pattern used by chapter reference projects in this site. Modelled on `reference/CloudSoft-Auth/` — **not** the heavier `reference/CloudSoft-Recruitment/` style, which is reserved for the multi-chapter capstone project.

## Directory layout

```text
reference/<ProjectName>/
├── README.md                          # Human-facing overview
├── CLAUDE.md                          # Index for future agents working on this project
├── .gitignore                         # Standard for the source's runtime
├── src/<InnerProject>/                # The application (mirrored from live execution)
│   ├── ...                            # Source files (matches what students build)
│   ├── Dockerfile                     # If the chapter containerises
│   └── .dockerignore                  # If the chapter containerises
├── .github/workflows/<workflow>.yml   # Final state of the workflow if CI/CD is taught
├── scripts/                           # Optional: validation tooling
│   ├── validate.mjs                   # If the chapter validates a web app via Playwright
│   ├── package.json
│   └── .gitignore                     # Excludes node_modules/ and package-lock.json
└── docs/
    ├── EXERCISE-VALIDATION-REPORT.md  # Live-execution validation record
    └── screenshots/                   # Playwright captures
        └── .gitkeep
```

**Important:** the reference project does **not** have its own `.git/`. It lives as a subdirectory of the Hugo site repo. Live execution happens in a *separate* working directory (e.g. `~/Developer/CLO_Development/<project>/`) which has its own `.git`, and the final state is rsync'd into here.

## Naming

- **Reference project directory** follows `CloudSoft-X/` (`CloudSoft-Auth`, `CloudSoft-Pipeline`, `CloudSoft-Recruitment`). The `X` is the chapter's domain or theme.
- **Inner project name** matches what students build in the exercises. Often shorter and more concrete (e.g. exercises use `CloudCi` while the reference project directory is `CloudSoft-Pipeline`).

## Templates

### `README.md`

```markdown
# <ProjectName>

Reference project for the ACD course's **<Chapter Title>** exercises under `content/exercises/<path>/`.

## Purpose

<One paragraph: what kind of app this is, why it exists, what the laboratory surface is — typically a small visible cue that changes per revision so students can SEE state propagating.>

- **Exercise 1** — <one-line description>
- **Exercise 2** — <one-line description>
- **Exercise 3** — <one-line description>

## Layout

<code block matching the actual on-disk tree>

## Running locally

<the most common run command, with expected output>

## Building the container locally

<if the chapter containerises>

## Exercise progression

Each exercise corresponds to one or more commits in the live GitHub repository (`<owner>/<repo>`). The state in this directory represents the **final** state after all exercises are complete.

## Live deployment

See `docs/EXERCISE-VALIDATION-REPORT.md` for the live URL, resource names, GitHub Actions run links, and manual verification steps.

## Validation

<if the chapter has automated validation tooling — link to scripts/>
```

### `CLAUDE.md`

```markdown
# <ProjectName>

ACD course reference implementation for the **<Chapter Title>** exercise series. <One paragraph: what the project teaches, where its laboratory surface lives, what the final state captures.>

## Key files

| File | Purpose |
|------|---------|
| `README.md` | Human-facing overview |
| `src/<InnerProject>/` | The application code |
| `.github/workflows/<workflow>.yml` | Final pipeline state |
| `scripts/<script>.mjs` | Validation tooling |
| `docs/EXERCISE-VALIDATION-REPORT.md` | Live-execution record |
| `docs/screenshots/` | Validation captures |

## Reference to exercise files

- `content/exercises/<path>/_index.md` — subsection landing
- `content/exercises/<path>/1-<slug>.md` — Exercise 1
- `content/exercises/<path>/2-<slug>.md` — Exercise 2
- `content/exercises/<path>/3-<slug>.md` — Exercise 3

## Live resources

| Resource | Value |
|----------|-------|
| GitHub repository | `<URL>` |
| Azure subscription | `<id>` |
| Resource group | `<name>` |
| <other resources as relevant> | `<value>` |
| Live URL | `<URL>` |

(Populated during Phase 5 from live-execution capture.)
```

### `docs/EXERCISE-VALIDATION-REPORT.md` skeleton

```markdown
# Exercise Validation Report — <ProjectName>

## Overview

<One paragraph describing the live-execution run that validated the exercises, the date, and the overall outcome.>

## Resources Provisioned

| Resource | Name | Region | Notes |
|----------|------|--------|-------|
| <table rows> | | | |

## Live URL

<URL>

<Description of the visible cue students should look for.>

## GitHub Repository

- Repo: `<URL>`
- Final secret list: `<list>`
- Final workflow file: `<path>` — <one-line summary of what it does>

## Workflow Run History (validating each exercise)

| Stage | Commit | Run ID | Outcome |
|-------|--------|--------|---------|

## Build SHA Progression

<Table showing how the visible cue changed across exercises.>

## Screenshots

<Filenames under `docs/screenshots/`.>

## Manual Verification Steps

<Numbered list of copy-paste commands the user can run.>

## Deviations from Exercise Text

<Numbered list. Each entry: what diverged, why, what the fix was. Or "None.">

## Status

**Validated end-to-end on YYYY-MM-DD.**
```

### `.gitignore` (standard .NET if the chapter uses .NET)

Use `dotnet new gitignore` in the live working directory and rsync the result. Otherwise the equivalent for Node, Python, etc.

### `scripts/.gitignore`

```text
node_modules/
package-lock.json
```

### `docs/screenshots/.gitkeep`

Empty file. Ensures the directory exists in git even before the first screenshot lands.

## Common pitfalls when mirroring live state

- **Nested `.github/workflows/`**: if the live source repo's working directory is `<live>/<InnerProject>/` and you rsync that whole directory into `reference/<ProjectName>/src/<InnerProject>/`, you'll end up with `src/<InnerProject>/.github/workflows/` *and* the workflow at the top level. Pick one location (top-level is cleaner) and remove the duplicate.
- **`obj/`, `bin/`, `node_modules/`, `__pycache__/`, lock files**: rely on the source's `.gitignore` after `git add --dry-run` to confirm none get staged.
- **Editor caches** like `.lscache`, `.vs/`, `.idea/`: clean before committing.
