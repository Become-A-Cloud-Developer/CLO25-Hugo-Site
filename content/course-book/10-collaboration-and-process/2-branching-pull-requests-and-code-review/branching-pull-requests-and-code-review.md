+++
title = "Branching, Pull Requests, and Code Review"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 20
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/10-collaboration-and-process/2-branching-pull-requests-and-code-review.html)

[Se presentationen på svenska](/presentations/course-book/10-collaboration-and-process/2-branching-pull-requests-and-code-review-swe.html)

---

A solo developer can push every commit straight to the `main` branch and get away with it most of the time. The same workflow on a team breaks within a week. Two engineers overwrite each other's work, a half-finished refactor lands at the same moment a colleague is debugging production, and nobody ever read the code that just shipped. The remedy is a piece of social infrastructure layered on top of [Git](/course-book/10-collaboration-and-process/1-version-control-with-git/): every change moves through a short-lived branch, is proposed back to `main` through a pull request, and is reviewed by another human before it merges. The chapter develops that workflow — the branch, the pull request, the review, the protection rules that enforce the contract, and the merge strategies that decide what the history ends up looking like.

## Why pushing to main is risky

Direct commits to the shared branch skip every checkpoint a team relies on. There is no chance for a colleague to spot the off-by-one error. There is no run of the test suite before the change reaches everyone else. There is no record of *why* the change was made, separate from the diff itself. And if the change is wrong, rolling it back means rewriting public history — an operation Git allows but treats as hostile, because every other clone of the repository now disagrees with the remote about which commits exist.

The deeper cost is cultural. A team that pushes directly to `main` has no shared moment in which the code is held up to scrutiny. Knowledge stays trapped inside the head of whoever wrote each line. New team members have nowhere to learn from existing code, because no one is ever asked to explain it. The pull request workflow exists as much to create that scrutiny moment as it does to prevent any specific bug.

The fix is not to forbid sharing the `main` branch — every team needs a single trunk that represents the current state of the product. The fix is to require that changes arrive on `main` through a reviewable proposal rather than a unilateral push.

## The topic-branch flow

The standard rhythm for adding a feature, fixing a bug, or refactoring a module is a four-step loop: branch, push, propose, merge.

1. **Branch from `main`**. Starting from an up-to-date local `main`, the developer creates a short-lived topic branch — usually named after the work it contains (`feature/user-login`, `fix/null-pointer-on-checkout`). All commits for that piece of work land on this branch, isolating them from `main` and from anyone else's in-flight work.
2. **Push the branch to the [remote](/course-book/10-collaboration-and-process/1-version-control-with-git/)**. Once there is something worth showing — even an early sketch — the branch is pushed to GitHub. The remote copy is what reviewers will look at.
3. **Open a pull request**. The developer asks GitHub to create a proposal to merge the topic branch into `main`. The PR is the conversation surface for the change.
4. **Review and merge**. One or more reviewers read the diff, ask questions, request changes if needed, and approve. When all required checks pass, the branch is merged into `main` and the topic branch is deleted.

The flow is short by design. A topic branch that lives for an afternoon merges cleanly. A topic branch that lives for three weeks accumulates conflicts with every other change that landed on `main` in the meantime, and the cost of integration grows roughly with the square of the duration. This is the same pressure that motivates [trunk-based development](/course-book/8-devops-and-delivery/2-ci-vs-cd/) — keep branches short, integrate often, never let two parallel histories drift far apart.

## What a pull request actually is

A **pull request** (or merge request) is a proposal to merge changes from one branch into another (usually a feature branch into `main`); it opens a discussion thread where reviewers examine the code, comment on changes, request revisions, and ultimately approve or reject the merge. The name is literal: the author is requesting that the target branch *pull* in the commits from the source branch.

A PR is not a Git concept. Git itself only knows about branches, commits, and merges. The PR is a feature of the hosting platform — GitHub, GitLab, Azure DevOps, Bitbucket — and it bundles three things together that Git would otherwise leave separate:

- A diff between the source branch and the target branch, rendered file-by-file with inline review controls.
- A discussion thread attached to the proposal, where reviewers leave general comments and per-line comments that the author can resolve as they push fixes.
- A CI status panel showing the result of automated checks ([continuous integration](/course-book/8-devops-and-delivery/2-ci-vs-cd/) builds, test suites, linters, security scans) that ran against the proposed merge.

The PR is also a durable artifact. Long after the branch is deleted, the discussion, the diff, and the CI history remain attached to the merge commit on `main`. When someone six months later is reading a stack trace and `git blame` points them at a line, the PR is there to explain why the line looks the way it does.

### The lifecycle of a PR

A PR moves through a small set of states. It opens in *open* state, accumulates commits as the author pushes fixes in response to review, transitions to *approved* once enough reviewers have signed off, and ends in *merged* (or *closed without merging* if the work is abandoned). GitHub records every state change, every comment, and every commit that was added during review, so the entire decision is reconstructible.

## Branch protection: turning convention into enforcement

A team can agree to use the topic-branch flow and still have someone forget on a Friday afternoon and push a hotfix straight to `main`. Conventions break under pressure. The remedy is to make the convention impossible to violate.

A **branch protection rule** is a repository setting that enforces policies on a branch (e.g., `main`), such as requiring pull request reviews, status checks, or up-to-date branches before allowing pushes or merges, preventing accidental or malicious direct edits. Once enabled on `main`, the rule replaces "please don't" with "the platform refuses." The most common policies a team enables:

- **Require a pull request before merging.** Direct pushes to `main` are rejected at the server. The only path in is a reviewed PR.
- **Require N approving reviews.** The platform refuses to merge until at least N reviewers (often one or two) have approved the diff. Stale approvals — those given before the most recent push — can be auto-dismissed so a last-minute change cannot slip through unreviewed.
- **Require status checks to pass.** The merge button stays disabled until the CI pipeline reports green. A PR that breaks the test suite cannot be merged, no matter how many humans approve it.
- **Require a linear history.** The platform forbids merge commits, pushing the team toward squash or rebase merges (covered below) so that `main` is a straight line of commits rather than a tangle of parallel ribbons.
- **Require the branch to be up to date with `main`.** Before merging, the topic branch must include the latest commits from `main`, which forces conflicts to be resolved on the topic branch rather than discovered on `main` after the fact.

These rules turn the social workflow into a technical guarantee. A new contributor who has never read the team's wiki cannot accidentally merge an unreviewed change to production, because the platform itself will not let them.

## Merge strategies

Once a PR is approved and the checks are green, the platform offers a final choice: *how* should the topic branch's commits be combined into `main`? Git supports three answers, and each leaves the history looking different.

| Strategy | Result on `main` | When it fits |
|----------|-----------------|--------------|
| Merge commit | All topic commits land verbatim, plus a merge commit tying them together | Long-running feature branches where intermediate commits matter for archaeology |
| Squash | All topic commits collapse into one new commit on `main` | Most short-lived PRs — produces a clean, one-PR-per-commit history |
| Rebase | Topic commits are replayed on top of `main`, with no merge commit | Linear-history teams that want each commit on `main` to stand alone |

A **merge strategy** determines how Git combines two branches: a merge commit preserves both branches' histories, a squash merge combines all commits into one, and a rebase merge replays commits on top of the target branch for a linear history.

A merge commit preserves the full topic branch as it was developed. The history shows every "wip", "fix typo", and "address review feedback" commit the author made along the way. This is honest but noisy. The branch structure is also preserved — `main` becomes a tree, not a line. That structure is occasionally useful when reasoning about long-running parallel work, and unhelpful when reading the recent history to find the commit that introduced a bug.

A squash merge flattens the topic branch into one new commit on `main`, with a message the author writes once before merging. The intermediate commits are gone. The advantage is a clean `main` history where every commit corresponds to one reviewed PR. The disadvantage is that the granular history of how the work evolved is lost — though the original commits are still visible on the (now-deleted) branch's PR page if anyone needs them. Squash is the default for most product teams using GitHub.

A rebase merge replays each topic commit on top of the latest `main`, then fast-forwards `main` to include them. The result is a linear history with all the original commits intact, but no merge commit signaling that they were developed on a branch. This works well when the topic branch was kept tidy throughout development (each commit is meaningful and self-contained) and badly when it was not (the rebased commits include "wip" and "fix typo" forever).

The choice is a team decision, not a per-PR one. Most teams pick one strategy, configure the branch protection rule to enforce it, and stop debating it.

## Conflict resolution

Most merges are uneventful — Git applies the topic branch's diffs on top of `main` and the result is unambiguous. A merge conflict happens when two branches modified the same lines of the same file in incompatible ways: a colleague renamed `getUserName()` to `fetchUser()` while the topic branch added a new caller of the old name. Git cannot decide which version is correct; it stops, marks the conflicted regions in the file with `<<<<<<<` / `=======` / `>>>>>>>` markers, and hands the decision to the developer.

**Conflict resolution** is the process of handling merge conflicts that occur when two branches modify the same lines of code; the developer manually edits the conflicting file to choose which changes to keep, then completes the merge. The job is one of intent, not mechanics. The tool can show what each side did; only a human knows what the code is *supposed* to do after both changes are applied.

Conflicts are easier to resolve when topic branches are short and the team rebases or merges from `main` frequently. A branch that has been off doing its own thing for two weeks will conflict with everything; a branch that pulled `main` yesterday will conflict with almost nothing.

## Draft pull requests for work in progress

Sometimes a developer wants feedback on a direction before the work is done — a sketch of an API, a partial migration, an early stab at a refactor. Opening a normal PR for this signals "ready to merge," which is misleading. The remedy is a **draft PR**: a pull request marked as work-in-progress that suppresses notifications and prevents merging; it allows developers to share early feedback on incomplete changes without formally requesting review.

A draft PR is identical to a regular PR in every way that matters for collaboration — same diff, same discussion thread, same CI runs — except that the merge button is disabled and reviewers are not paged. The author marks the PR as ready when the work is finished, at which point it transitions to a normal review request.

Draft PRs are also a useful collaboration surface for paired work. Two developers can push to the same branch, and the draft PR shows the running state to both of them and to anyone they want to consult, without anyone treating the work as done.

## Code review as a craft

The mechanics of opening, approving, and merging a PR are the easy part. The skill is in the review itself. A good review catches bugs, levels up the team, and leaves the author feeling helped rather than judged. A bad review nitpicks variable names, ignores the actual logic, and corrodes trust over time.

A few habits that distinguish good review:

- **Keep PRs small.** A 50-line PR gets a careful read. A 2,000-line PR gets a rubber stamp. The author owes reviewers a reviewable diff; the reviewer owes the author a real review. Both halves break when the diff is too large to hold in one sitting.
- **Ask, do not assert.** "Why did you choose a `Dictionary` here instead of a `HashSet`?" invites a conversation. "This should be a `HashSet`" closes one. Often the author has a reason the reviewer did not see.
- **Distinguish blocking from non-blocking feedback.** A bug that will cause an outage blocks the merge. A preference about variable naming does not. Conventions like prefixing comments with `nit:` for minor stylistic suggestions help reviewers send both kinds of feedback without conflating them.
- **Be kind in the review and rigorous in the standard.** The author wrote the code at three in the afternoon under deadline pressure; the reviewer is reading it at ten in the morning with full attention. Tone matters because the asymmetry is real. Standards matter because the code will live in production for years.
- **Approve when it is good enough, not when it is perfect.** The point of review is to catch things the author missed, not to rewrite the PR in the reviewer's preferred style. Trust the author to make the small calls.

## Where this fits with the exercises

The companion exercise chapter, [Code Collaboration](/exercises/15-code-collaboration/), walks through this workflow end-to-end: creating a topic branch, opening a pull request via the GitHub UI and the `gh` CLI, requesting review, resolving conflicts, and merging through each of the three strategies. The theory in this chapter exists to make those steps feel motivated rather than ceremonial — every click in the exercise corresponds to one of the protections, signals, or design decisions described above.

## Summary

Pushing directly to `main` skips the social mechanism teams rely on to catch mistakes and share knowledge, which is why production codebases route every change through a topic branch and a pull request. A pull request is a platform-level proposal to merge one branch into another, bundling a diff, a discussion thread, and a CI status into a single durable artifact. Branch protection rules turn the convention into enforcement: required reviews, required green builds, and required linear history move the team's standards into the platform itself. The merge strategy — merge commit, squash, or rebase — decides what the resulting history on `main` looks like, and is a team-level choice rather than a per-PR one. Conflict resolution is the human's job, not the tool's, because only the developer knows what the merged code is supposed to mean. Draft PRs make work-in-progress collaboration explicit, and the craft of code review — small diffs, kind language, asking rather than asserting — is what turns the workflow from mechanical ceremony into a learning loop the whole team benefits from.
