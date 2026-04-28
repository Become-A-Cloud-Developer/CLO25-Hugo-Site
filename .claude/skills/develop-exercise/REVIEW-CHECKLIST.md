# Cross-Review Checklist

Focus areas for the Phase 2 cross-review agent. The reviewer reads all newly-written content files and returns a punch list using these axes. The main agent then fixes findings directly.

## A. Narrative continuity (most important)

This is the single most valuable thing the cross-review can catch. Verify:

1. **State at the start of Ex N+1 == state at the end of Ex N.** Read the prerequisites and intro of each exercise. Does it assume what the previous exercise actually leaves behind?
2. **Resource naming consistency.** Same placeholders for project name, image name, Container App, registry, resource group, identity name across all exercises in the chapter.
3. **File naming consistency.** Workflow filename (e.g. `ci.yml`), Dockerfile location, configuration file names — same across exercises.
4. **No duplicated setup.** Exercise N+1 should not redo what Exercise N already did.
5. **Forward references.** Closing reflections should set up the next exercise without referencing it by *number* ("the next exercise" is fine; "Exercise 4.2" is not — the file numbering is the only place numbers appear).
6. **Backward references.** Opening intros should refer to "the previous exercise" not "Ex 4.1."

## B. Technical correctness

Spot-check the actual commands and configuration. Common defect classes:

1. **CLI flag mismatches**. E.g. `az role assignment create --assignee` accepts an appId; `--assignee-principal-type` requires `--assignee-object-id` — these don't compose freely.
2. **Action version drift**. Reasonable defaults: `actions/checkout@v4`, `azure/login@v2`, `docker/login-action@v3`, `docker/build-push-action@v6`. Verify all three (or relevant set) appear and are uniformly versioned.
3. **OIDC subject claim format**: exact string `repo:<org>/<repo>:ref:refs/heads/<branch>`. Common typos: `head` vs `heads`, missing colons, leftover angle brackets.
4. **Workflow `permissions:`**: federated OIDC needs `id-token: write` AND `contents: read`. Without the second, checkout fails.
5. **Container port consistency**: if the Dockerfile listens on 8080, the Container App ingress target port must match.
6. **Framework version consistency**: e.g. `.NET 10` Dockerfile base images and `dotnet new --framework net10.0`. Mismatches cause restore failures.
7. **Build args propagating**: if Ex 1 introduces `--build-arg BUILD_SHA=…`, Ex 2's workflow rewrite must keep passing it. Otherwise the visible cue silently breaks.
8. **Role-assignment scoping**: scopes should be specific resource IDs, not subscription scope. Flag `Contributor` at subscription scope.
9. **Secret-handling commands**: `gh secret set` should pipe via stdin, not pass as argv. `creds.json` should be `rm`d after `gh secret set`.
10. **Branch-trigger filters**: if an exercise uses `gh workflow run --ref <branch>` for testing, the workflow must have `workflow_dispatch:` in its triggers. Otherwise the dispatch is silently rejected.

## C. Skill-template compliance

For each content file, verify against `create-exercise/TEMPLATE.md`:

1. TOML frontmatter complete: `title`, `program`, `cohort`, `courses`, `description`, `weight`, `draft`.
2. H1 matches the title in frontmatter and contains **no exercise number** (`First Pipeline:`, not `Exercise 1: First Pipeline`).
3. Mandatory sections in order: Goal → Prerequisites → Exercise Steps (with Overview → numbered Steps) → Test Your Implementation → Common Issues → Summary → Going Deeper → Done.
4. Icons placed correctly: `> ℹ`, `> ⚠`, `> ✓`. No alternative markers.
5. File paths shown as `> \`path/to/file.ext\`` blockquotes (not bare inline).
6. URLs wrapped in angle brackets `<https://example.com>` (no bare URLs).
7. Code-block language tags everywhere (`bash`, `yaml`, `dockerfile`, `csharp`, `html`, `toml`, `json`).
8. Blank lines around code blocks, lists, and headings (MD031, MD032, MD022).
9. No cross-references to other exercises by NUMBER.
10. Bold action verbs in numbered steps (**Run**, **Add**, **Open**, **Replace**).

## D. Subsection `_index.md`

1. Frontmatter with leading number in title (e.g. `"9. CI/CD to Azure Container Apps"`), `program/cohort/courses/weight` correct.
2. Intro paragraphs walk the arc with the technologies bolded for scannability.
3. A `> ℹ Where this fits` blockquote situates the chapter relative to surrounding chapters.
4. Ends with `{{< children />}}`.

## E. Pedagogical quality

1. **Common Mistakes** sections list specific, real-world failures (with concrete error strings where possible) — not generic warnings ("be careful").
2. **Concept Deep Dive** sections deepen understanding (explain *why*) rather than restate the step (*what*).
3. **Reflection questions** are open-ended enough to provoke thinking, not yes/no closures.
4. The exercise can be completed by a student reading **only the bold text**. Supplementary detail belongs in blockquotes.
5. **Cleanup** of cloud resources is part of the chapter's final exercise — students should never be left with orphaned resources.

## Output format expected from the reviewer

```text
## Critical issues (must fix)
- File: <name>, line ~N: "<offending text>" — <one-sentence fix>
- ...

## Significant issues (should fix)
- ...

## Minor issues (nice to fix)
- ...

## Cross-cutting issues
- ...

## Strengths to preserve
- ...
```

Cap the report at ~800 words. The main agent will use this directly to make edits.
