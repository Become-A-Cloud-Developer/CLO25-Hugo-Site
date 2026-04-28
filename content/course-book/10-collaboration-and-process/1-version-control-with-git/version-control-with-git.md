+++
title = "Version Control with Git"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 10
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/10-collaboration-and-process/1-version-control-with-git.html)

[Se presentationen på svenska](/presentations/course-book/10-collaboration-and-process/1-version-control-with-git-swe.html)

---

A folder of source code becomes unmanageable as soon as more than one person changes it. Files diverge, edits collide, and last week's working version disappears under "final_v3_really_final.zip". The problem is not bad discipline — it is that change itself has no first-class representation. Without a system that records who changed what, when, and why, every modification is a destructive overwrite. Version control solves this by treating change as the primary object and the current files as merely the latest view of an accumulated history. Git is the system used to track that history in almost every professional codebase, and the rest of Part X depends on its core vocabulary.

## Why version control matters

Source code evolves continuously. A feature is started, abandoned, restarted, refactored, and shipped — all on top of work other contributors are doing in parallel. Three needs follow directly from this reality. The team must be able to answer "what changed and who changed it" for any line in the codebase. Anyone must be able to return the project to any earlier state without losing later work. And multiple developers must be able to edit the same files without overwriting each other.

A version control system meets all three needs by storing a complete history of named, immutable snapshots. Files in the working folder are no longer the source of truth — the recorded history is, and the current files are reconstituted from it on demand. Once a project is under version control, "undo" stops meaning "press Ctrl-Z fast enough" and starts meaning "check out the commit from before the bug appeared".

### Centralized vs. distributed systems

Two architectural families implement these ideas. **Centralized version control** systems such as Subversion (SVN) and CVS keep the authoritative history on a single server. Developers check out a working copy, edit it, and commit changes back to the server. The server is the only place the history lives in full; without network access, the developer cannot view past versions, compare branches, or commit work.

**Distributed version control** systems, of which Git is the dominant example, give every developer a complete copy of the entire history. Commits happen locally and are later synchronized with other copies. There is no single authoritative server in the protocol itself — although teams almost always nominate one (a GitHub repository, for instance) as the convention for collaboration. The practical consequence is that operations like viewing history, branching, and committing are local and fast, and network connectivity is needed only when sharing work with others.

This distinction matters because it shapes the daily workflow. With Git, commits are cheap and frequent; branches are created for any unit of work; and synchronization with the team is a deliberate, separate step. The rest of this chapter assumes the distributed model.

## What a Git repository actually is

A **repository** is a `.git` directory and its contents that stores all commits, branches, tags, and configuration for a project. Everything Git knows about a project lives inside that one hidden folder at the project root. Delete `.git/` and the surrounding files revert to ordinary, untracked text files; copy `.git/` somewhere else and the entire history travels with it.

Inside `.git/` are two structures worth understanding at a high level. The **object database** stores every snapshot Git has ever recorded as a content-addressed blob — a piece of data whose identifier is a SHA-1 hash of its contents. Identical content produces identical hashes, so duplicate files across commits are stored only once. The **refs** are short, human-readable names (such as `main` or `feature/login`) that point to specific commits in the object database. Branches and tags are nothing more than refs.

The clean implication is that Git's history is not a sequence of patches layered on top of each other. It is a graph of complete snapshots, and every name a developer uses — branch names, tag names, even `HEAD` — is just a label that points to one node in that graph.

## The three states of a file

Git tracks every file through three distinct states. Understanding the boundary between them eliminates most early confusion about why `git status` says what it says.

The **working tree** (or working directory) is the visible, editable folder on the filesystem where project files reside; changes you make in the working tree are not automatically tracked by Git until you stage and commit them. Edit a file in an editor and the change exists only in the working tree. Git sees the file as "modified" but has not yet recorded the change anywhere durable.

The **staging area** (also called the index) is an intermediate area between the working tree and the repository where you prepare files for the next commit; `git add` moves files to the staging area, and `git commit` transforms the staged snapshot into a permanent commit. The staging area is what makes Git commits selective. A developer who has touched ten files can stage only the three that belong to the current logical change and commit them together, leaving the rest unstaged for a later, separate commit.

The third state is **committed** — the change is now part of the repository's history, stored in the object database, and reachable through some ref. Once committed, content is immutable. The commit can be hidden, abandoned, or rewritten into a new commit, but the original object remains in the database until garbage collection removes unreachable objects.

The mental model to carry forward: edit in the working tree, prepare with `git add`, record permanently with `git commit`.

## Commits as immutable snapshots

A **commit** is an immutable snapshot of a project's files at a specific point in time, identified by a unique hash; it includes a message, author, timestamp, and a pointer to its parent commit(s), forming a linked chain that represents the project's history. The hash — typically displayed as a 40-character SHA-1 string, abbreviated to the first 7 in most output — is computed from the snapshot's content plus its metadata. Change anything about the commit and the hash changes. This is what makes Git history tamper-evident: a commit cannot be silently altered without invalidating every commit that descends from it.

Each commit points back to one or more parent commits, building a directed acyclic graph (DAG). A normal commit has one parent. A merge commit has two or more, marking the point where divergent histories were brought back together. The very first commit in a repository has no parent — it is the root of the graph.

Commit messages are how the team understands intent later. A commit titled `Fix bug` is almost worthless six months on; a commit titled `Fix off-by-one in pagination when page count is zero` lets the reader scan history without opening every diff. The convention this course uses is a short imperative subject line, optionally followed by a blank line and a longer body explaining the *why*.

## Branches as movable pointers

A **branch** is a named pointer to a commit that allows parallel lines of development; creating a branch duplicates the pointer from the current commit, enabling isolated work without affecting other branches until an explicit merge reunites the changes. The branch itself is just a ref — a 41-byte file inside `.git/refs/heads/` containing a commit hash. Creating a branch is therefore cheap (it writes one file); switching branches is fast (it updates `HEAD` and reshapes the working tree); and there is no architectural reason to be parsimonious about branch creation.

The default branch in a new Git repository is conventionally named `main` (older repositories use `master`). When a commit is made on a branch, Git advances that branch's pointer to the new commit. When a branch is merged into another, the receiving branch's pointer moves forward to incorporate the work — sometimes by fast-forwarding (the pointer just moves up the chain) and sometimes by creating a merge commit that ties the two histories together.

`HEAD` is a special ref that names the branch the developer is currently working on. `git switch feature/login` makes `HEAD` point to `feature/login`, and subsequent commits will advance that branch.

## Remotes and synchronization

A **remote** is a reference to a version of the repository hosted on a server (typically called `origin` for the default remote on GitHub); git commands like `fetch`, `push`, and `pull` synchronize commits between your local repository and the remote. A repository can have multiple remotes — one for the team's GitHub copy, one for an internal mirror, one for a personal fork — but the overwhelmingly common case is a single remote named `origin`.

Remotes do not change Git's distributed nature. Each remote is itself a full repository; what makes one of them "the team's repository" is convention, not protocol. Three commands move commits between local and remote.

**Fetch** is a git command that downloads commits and branches from a remote repository to your local machine without modifying the working tree or your local branches; it updates remote-tracking branches (e.g., `origin/main`) to reflect the current state of the remote. After a fetch, the developer can inspect what teammates have done before deciding whether to integrate it.

**Pull** is a git command that combines `fetch` and `merge` (or `rebase`) in a single operation; it downloads commits from the remote and automatically integrates them into your current local branch, updating the working tree. Pull is convenient when the developer trusts the incoming history; fetch followed by an explicit merge or rebase gives finer control.

**Push** is a git command that uploads commits from your local branch to the corresponding remote branch, making your work visible to collaborators and updating the remote repository; it requires write permission on the remote. Push is the symmetric counterpart of fetch — fetch downloads what teammates have done, push uploads what the local developer has done.

The symmetry is worth pausing on: every commit has to be pushed somewhere to be visible to anyone else, and every commit produced elsewhere has to be fetched to be visible locally. Nothing is automatic, which is what allows offline work in the first place.

## The daily workflow

Most days, a developer touches four commands in a tight loop.

`git status` reports the state of the working tree and staging area: which files have been modified, which are staged, which are untracked. It is the first command to run when something feels off; most "I lost my work" panic ends with `git status` showing the work is right there, just not committed.

`git add <files>` moves changes from the working tree into the staging area. Pass specific paths to stage selectively, or `git add .` to stage everything in the current directory tree.

`git commit -m "<message>"` records the staged snapshot as a new commit on the current branch.

`git push` uploads new commits on the current branch to the matching branch on the remote.

A typical exchange looks like this:

```bash
git status
git add src/Auth/LoginController.cs src/Auth/LoginController.Tests.cs
git commit -m "Reject login attempts with empty password"
git push
```

The cycle is short on purpose. Small, frequent commits with descriptive messages produce a history that is easy to read, easy to revert, and easy to review.

## Worked example: from empty folder to remote repository

Consider a fresh project on a developer's machine. Nothing is under version control yet. The session below takes it from empty folder to a repository pushed to GitHub.

```bash
mkdir clo-demo && cd clo-demo
echo "# CLO Demo" > README.md

git init
git add .
git commit -m "Initial commit"

git remote add origin https://github.com/student/clo-demo.git
git branch -M main
git push -u origin main
```

Six commands move the project through every concept in this chapter. `git init` creates the `.git/` directory, turning the folder into a repository. `git add .` stages every file in the working tree. `git commit -m "Initial commit"` records the first snapshot — a commit with no parent, the root of the new history. `git remote add origin <url>` registers the GitHub repository as a remote named `origin`; nothing is sent yet, only the address is recorded. `git branch -M main` ensures the local branch is named `main` (older Git defaults emit `master`). `git push -u origin main` uploads the commit and, thanks to `-u`, sets `origin/main` as the upstream for the local `main` so that future `git push` and `git pull` know where to go without arguments.

After this sequence the same history exists in two places: the local `.git/` directory and the GitHub-hosted repository. Either could now be cloned by a teammate, who would receive the full commit graph and could begin contributing. The companion exercise [Code Collaboration](/exercises/15-code-collaboration/) walks through this initialization and a first round of remote synchronization step by step.

## Where Git fits in the larger picture

Git is the foundation, not the whole story. The rest of Part X builds directly on the vocabulary established here: branches become the basis for pull requests and code review, remotes become the connection point between local development and CI/CD, and the discipline of small, descriptive commits underpins both. None of those higher-level practices make sense without a clear picture of what a commit is, what a branch is, and how local and remote histories synchronize.

The reason Git won the version control market is not that its command-line interface is friendly — it is famously not. It won because the underlying data model is simple, durable, and correct: a content-addressed graph of immutable snapshots, with cheap, movable names pointing into it. Once that model is in mind, even the rough edges of the CLI become predictable.

## Summary

Version control treats change as a first-class entity instead of a side-effect of editing files. Git is a distributed version control system: every developer holds a complete copy of the repository, stored in the `.git/` directory as a graph of immutable, content-addressed snapshots. A file passes through three states — working tree, staging area, committed — and the boundaries between them are what `git add` and `git commit` cross. Commits are identified by SHA hashes and chained through parent pointers; branches are cheap, movable refs into that chain; remotes are other copies of the repository, synchronized through the symmetric pair `fetch`/`pull` and `push`. The daily loop of `git status`, `git add`, `git commit`, and `git push` covers most working hours. With this foundation in place, the rest of Part X can address the collaborative practices — pull requests, code review, agile process — that depend on it.
