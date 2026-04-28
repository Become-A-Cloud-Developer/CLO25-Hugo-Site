+++
title = "Version Control with Git"
program = "CLO"
cohort = "25"
courses = ["ACD"]
type = "slide"
date = 2026-04-28
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls = true
progress = true
history = true
center = true
+++

## Version Control with Git
Part X — Collaboration and Process

---

## Why version control
- A folder of code becomes unmanageable as soon as **two people** edit it
- Need: who changed what, when, and **why**
- Need: return to any earlier state without losing later work
- Need: parallel edits without **destructive overwrite**
- Solution: change is a **first-class object**, not a side-effect

---

## Centralized vs. distributed
- **Centralized (SVN, CVS)**: one server holds the history
- **Distributed (Git)**: every developer has a **full copy**
- Local commits are cheap; network is needed only to **share**
- Branching and history-viewing are local and fast
- Git won because the data model is simple and durable

---

## What a repository is
- A **`.git/` directory** at the project root
- **Object database**: content-addressed snapshots (SHA-1 hashes)
- **Refs**: short names like `main` pointing to commits
- Branches and tags are nothing more than **refs**
- Delete `.git/` and the project is no longer versioned

---

## The three states
- **Working tree** — visible, editable files on disk
- **Staging area** (index) — prepared changes for the next commit
- **Committed** — recorded permanently in the repository
- `git add` crosses the first boundary
- `git commit` crosses the second

---

## Commits and branches
- A **commit** is an immutable snapshot identified by a **SHA hash**
- Each commit points to its **parent(s)** — a directed acyclic graph
- A **branch** is a movable pointer to a commit
- Creating a branch writes one tiny ref file; switching is fast
- `HEAD` names the branch you are currently on

---

## Remotes and synchronization
- A **remote** is another copy of the repository (typically `origin`)
- `git fetch` — download remote commits, do **not** modify local branches
- `git pull` — fetch **plus** merge/rebase into the current branch
- `git push` — upload local commits to the remote
- Symmetric pair: nothing is automatic, which enables **offline work**

---

## The daily workflow
- `git status` — what is modified, staged, untracked
- `git add <files>` — stage selectively
- `git commit -m "..."` — record on the current branch
- `git push` — share with the team
- Small, frequent commits with descriptive messages

---

## Worked example
- `git init` — create `.git/`, turn folder into a repo
- `git add .` — stage every file
- `git commit -m "Initial commit"` — first snapshot, no parent
- `git remote add origin <url>` — register GitHub as `origin`
- `git push -u origin main` — upload and set upstream

---

## Where Git fits next
- **Branches** become the basis for pull requests and code review
- **Remotes** become the link between local work and CI/CD
- **Commit discipline** underpins clean history and reverts
- The CLI is rough; the **data model** is simple and correct
- Companion exercise: `/exercises/15-code-collaboration/`

---

## Questions?
