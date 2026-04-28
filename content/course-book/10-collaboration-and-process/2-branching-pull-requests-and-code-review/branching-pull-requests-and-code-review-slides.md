+++
title = "Branching, Pull Requests, and Code Review"
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

## Branching, Pull Requests, and Code Review
Part X — Collaboration and Process

---

## Why not push to main
- No **second pair of eyes** before code lands
- No CI checkpoint between commit and shared history
- Knowledge stays trapped with the original author
- Rolling back requires rewriting public history

---

## The topic-branch flow
- **Branch** from an up-to-date `main`
- **Push** the topic branch to GitHub
- **Open** a pull request against `main`
- **Review** the diff, then **merge** and delete the branch

---

## What a pull request is
- A **proposal** to merge one branch into another
- A diff rendered with inline review controls
- A **discussion thread** attached to the proposal
- A **CI status panel** showing automated check results
- A durable artifact long after the branch is gone

---

## Branch protection rules
- Require a **pull request** before merging
- Require **N approving reviews**
- Require **status checks** (CI) to pass
- Require **linear history**
- Require the branch to be **up to date** with `main`

---

## Three merge strategies
- **Merge commit** — preserves topic commits and branch shape
- **Squash** — collapses the PR into one clean commit
- **Rebase** — replays commits linearly, no merge commit
- Pick one, enforce it, stop debating it

---

## Conflicts and draft PRs
- Git stops at the conflict markers; the **developer decides intent**
- Short branches conflict less than long ones
- A **draft PR** signals work-in-progress
- Same diff, same CI, but no merge button and no review pings

---

## Code review as a craft
- **Small PRs** get real reads; large PRs get rubber stamps
- **Ask, do not assert** — invite a conversation
- Separate **blocking** feedback from `nit:` style notes
- Be kind in tone, rigorous in standard

---

## Questions?
