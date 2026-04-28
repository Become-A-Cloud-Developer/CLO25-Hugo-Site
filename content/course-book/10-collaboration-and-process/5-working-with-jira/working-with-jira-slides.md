+++
title = "Working with Jira"
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

## Working with Jira
Part X — Collaboration and Process

---

## Why a digital tracker
- A whiteboard works only when everyone sits in **one room**
- Remote and split-office teams cannot share a physical board
- The tracker has to be readable and editable from anywhere
- Jira is the standard answer across professional software teams

---

## What Jira is
- A **database of work items** with a project structure on top
- Project → issues → workflow status (the three layers)
- Each issue has a key like `PROJ-123` — the load-bearing identifier
- Views like sprint board and backlog are filters over the database

---

## The issue type hierarchy
- **Epic** — quarter-scale theme, owned by product
- **Story** — user-facing feature, fits in one or two sprints
- **Task** — internal work that does not deliver user value directly
- **Sub-task** — the day-or-two unit a developer picks up

---

## The workflow as a state machine
- **Workflow** — sequence of statuses an issue moves through
- Default: `TO DO → IN PROGRESS → IN REVIEW → DONE`
- Teams customise — QA gates, deployment gates, etc.
- Every added state is added friction; most teams stop at four to six

---

## The sprint board
- A visual board with columns per workflow status
- Makes **work-in-progress visible** during the daily standup
- Long `IN PROGRESS` column → too much in flight
- Stuck `IN REVIEW` column → reviews are not getting done

---

## Git integration through branch names
- **Branch naming convention** encodes the issue ID into the branch
- Pattern: `feature/PROJ-123-add-login`
- Jira watches commits and PRs for matching keys, auto-links them
- Workflow rule transitions the issue to `DONE` when the PR merges

---

## Worked example: epic to merged PR
- Epic `PROJ-100: User authentication` → 2 stories → 6 tasks
- Developer picks `PROJ-104`, drags card to `IN PROGRESS`
- Branch: `feature/PROJ-104-login-endpoint`
- PR title `PROJ-104: Implement login endpoint` — Jira links everything

---

## Comment hygiene and estimation
- One issue is **one conversation** — durable record beats Slack threads
- **Story points** for stories — abstract effort, calibrated to velocity
- **Hours** for tasks where the duration is already known
- Mixing the two on the same backlog produces meaningless aggregates

---

## The honest truth about Jira
- Jira works only as well as the team's **hygiene** around it
- A board that lies is worse than no board — false confidence
- Daily updates, branch IDs, PR linkage = a tracker that tells the truth
- The discipline is the value, not the tool itself

---

## Questions?
