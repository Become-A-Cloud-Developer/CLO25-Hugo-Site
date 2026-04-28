+++
title = "Working with Jira"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 50
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/10-collaboration-and-process/5-working-with-jira.html)

[Se presentationen på svenska](/presentations/course-book/10-collaboration-and-process/5-working-with-jira-swe.html)

---

A sprint board drawn on a whiteboard works for a team that sits in one room. The moment the team works across two offices, two time zones, or a mix of office and remote, that whiteboard stops being a shared instrument — half the team can no longer see it, and the other half stops trusting it because they cannot verify it from their desk. Software teams need a digital work-tracker that every member can read and update from anywhere, and that integrates with the version control and review tools the developers already use. Jira has become the standard answer to this need across professional software organisations, and the discipline of using it well — not the tool itself — is what separates teams that ship from teams that argue about what they were supposed to be shipping. The pieces that matter for a working developer are the issue-type hierarchy, the workflow as a state machine, the sprint board, and the Git integration that hooks branches and pull requests back to the issues they implement.

## What Jira is

**Jira** is an Atlassian project and issue tracking tool used by teams to plan work, manage backlogs, run sprints, track progress, and report on project metrics; it integrates with Git platforms like GitHub via webhooks and branch naming conventions. Strip away the marketing surface and Jira is a database of work items with a project structure on top: a project owns a backlog of issues, each issue has a type and a workflow status, and views like the sprint board and the backlog list are filters over that database.

The model has three layers. The project is the container — typically one project per product or per team, identified by a short key like `PROJ` or `WEB`. Inside the project sit individual issues, every one identified by the project key plus a number — `PROJ-123`, `WEB-47`. The issue ID is the load-bearing identifier in the rest of this chapter; it is what the [agile](/course-book/10-collaboration-and-process/4-agile-sprints-and-user-stories/) board displays, what the developer puts in a branch name, and what the [pull request](/course-book/10-collaboration-and-process/2-branching-pull-requests-and-code-review/) description references. The third layer is the workflow that each issue moves through as work progresses, which is described later in the chapter.

## The issue type hierarchy

Jira issues are organised in a deliberate hierarchy that lets a team plan at multiple levels of granularity at once. Four types matter for the standard agile setup: epic, story, task, and sub-task.

An **epic** in Jira is a large body of work that is broken down into smaller user stories; epics typically span multiple sprints and represent major features or themes (e.g., "User authentication" or "Payment integration"). Epics are the planning unit for product managers and tech leads. They sit above the [sprint](/course-book/10-collaboration-and-process/4-agile-sprints-and-user-stories/) horizon — a single epic is rarely completed in one sprint and often runs across an entire quarter — and they serve as the answer to "what major work is in flight right now."

A **story** (in Jira) is a work item representing a user-facing feature or enhancement; it is smaller than an epic, fits within one or two sprints, and includes fields like description, story point estimate, and acceptance criteria. The story is the unit the team commits to in sprint planning. Each story belongs to one epic — its parent — and the relationship lets the product manager track the epic's progress as the percentage of its child stories completed. The [user story](/course-book/10-collaboration-and-process/4-agile-sprints-and-user-stories/) format ("As a [user], I want [goal] so that [benefit]") fills the description field; the story-point estimate fills the estimate field; the acceptance criteria fill a checklist field.

A **task** (in Jira) is a work item representing internal work (refactoring, documentation, infrastructure) that does not directly deliver user value; tasks are often subtasks of stories and are tracked on the sprint board. Tasks fill the gap that user stories cannot — work that has to happen but does not map to a user-facing outcome. Migrating a database schema, upgrading a dependency, writing a runbook, or refactoring a module are all tasks. They appear on the sprint board alongside stories and consume the team's capacity in the same way.

Below stories and tasks sit sub-tasks, which decompose a story or task into the concrete pieces of work that individual developers pick up. A story like "User can reset their password" might decompose into sub-tasks for the database migration, the API endpoint, the email-sending integration, and the frontend form. Sub-tasks are how a story shared by the team becomes work owned by an individual.

| Level | Typical span | Owner | Example |
|-------|--------------|-------|---------|
| Epic | Quarter | Product manager | User authentication |
| Story | Sprint | Team | User can reset their password via email |
| Task | Sprint | Team | Upgrade database driver to v8 |
| Sub-task | A day or two | Developer | Add `PasswordResetToken` migration |

The hierarchy is not pedantic — it lets a single backlog answer different questions for different roles. The product manager looks at epic progress; the team looks at sprint stories and tasks; the developer looks at the sub-task they are picking up next.

## The workflow as a state machine

A **workflow** (in Jira) is a sequence of statuses and transitions that a work item passes through (e.g., TO DO → IN PROGRESS → IN REVIEW → DONE); teams customize workflows to match their development process. The workflow is the answer to "where is this issue right now," and it is the thing that turns the issue tracker into a process tool rather than just a list of work.

The default workflow that most teams start with has four states. `TO DO` is the staging area — the issue has been planned into the sprint but no one has started it. `IN PROGRESS` means a developer has picked the issue up and is actively working on it. `IN REVIEW` means the work is complete from the developer's side and is now waiting for a [code review](/course-book/10-collaboration-and-process/2-branching-pull-requests-and-code-review/), typically via a pull request. `DONE` means the work has been merged and meets the team's [definition of done](/course-book/10-collaboration-and-process/4-agile-sprints-and-user-stories/) — reviewed, merged, deployed, and verified.

Teams customise this workflow when their process demands it. A team with a manual QA stage adds a `READY FOR QA` and `QA` state between `IN REVIEW` and `DONE`. A team with a deployment gate adds a `DEPLOYED TO STAGING` state. The customisation is straightforward in Jira's workflow editor, but every added state is also added friction — the more granular the workflow, the more transitions someone has to remember to make, and the more likely that the board lies about reality because someone forgot to advance an issue. Most teams settle on four to six states and resist adding more.

## The sprint board

The **sprint board** (in Jira) is a visual board displaying work items in the current sprint organized by status columns (e.g., TO DO, IN PROGRESS, DONE); it provides real-time visibility into sprint progress during daily standups. The board makes work-in-progress visible, which is the single most useful thing a process tool can do — a long `IN PROGRESS` column is a signal that the team has too much in flight, a stuck `IN REVIEW` column is a signal that reviews are not getting done, and an empty `TO DO` column with the sprint half over is a signal that the team underestimated.

The board is the artefact the team gathers around at the daily standup. Each developer walks through their cards, says what is moving forward, and flags anything stuck. The conversation is grounded in the board state — "this card has been in review for three days, who can pick it up" — instead of in vague status reports. When a team's board accurately reflects reality, the standup takes ten minutes; when the board lies, the standup is people remembering what they did yesterday and the manager taking notes.

The board also visualises work-in-progress limits. A team that decides "no more than three issues in `IN PROGRESS` per developer" enforces that limit on the board, and the limit makes the team finish work before starting new work. The pull-system discipline that limits encourage is the antidote to the failure mode where a team starts ten things, finishes none of them, and ends the sprint with everything still half-done.

## Git integration through branch naming

The connection between the issue tracker and the source code is what makes Jira useful for developers, and it is implemented through a convention rather than a magical integration. A **branch naming convention** is a team standard for naming Git branches (e.g., `feature/JIRA-123-user-login` or `bugfix/issue-456`) that encodes the work item type and Jira ticket ID, enabling automatic linking between code and issues.

The convention has three parts. A prefix indicates the type of work — `feature/` for new functionality, `bugfix/` for fixes, `chore/` for maintenance. The Jira issue ID identifies which specific work item the branch implements — `PROJ-123`. A short slug describes the change in human-readable form — `add-login`. The full branch name reads `feature/PROJ-123-add-login`, and from that single string a teammate can tell what kind of work it is, where to find the full context in Jira, and what the change does in plain language.

```bash
git checkout -b feature/PROJ-123-add-login
```

The Jira-GitHub integration watches commits and pull request titles for matching issue keys and auto-links the activity back to the issue. When the developer pushes the branch, Jira shows the branch as connected to `PROJ-123`. When a pull request is opened, Jira shows the PR. When the PR is merged, Jira can transition the issue to `DONE` automatically based on a workflow rule. The developer never opens Jira to update the status — the act of doing the work updates the issue, which is the only way status accuracy survives at scale.

### A worked example

Consider an epic and the work that comes out of it. The product manager creates `PROJ-100: User authentication` as the epic. The team breaks it into two stories during refinement: `PROJ-101: User can sign in with email and password` and `PROJ-102: User can reset their password`. Each story decomposes into three tasks during sprint planning. `PROJ-101` becomes `PROJ-103: Add user table migration`, `PROJ-104: Implement login endpoint`, and `PROJ-105: Add login form to UI`. `PROJ-102` becomes `PROJ-106: Add password-reset-token table migration`, `PROJ-107: Implement reset-request endpoint`, and `PROJ-108: Add reset form to UI`.

A developer picks up `PROJ-104`. They drag the card to `IN PROGRESS` on the sprint board, then create a branch:

```bash
git checkout -b feature/PROJ-104-login-endpoint
```

They commit progress as they go, with messages that reference the issue:

```text
PROJ-104: Add LoginRequest DTO and validation
PROJ-104: Wire login endpoint to authentication service
PROJ-104: Add integration test for valid credentials
```

When the work is ready, they open a pull request titled `PROJ-104: Implement login endpoint` with a description that links to the Jira issue. The Jira-GitHub integration sees `PROJ-104` in the branch name, the commit messages, and the PR title, and attaches all of them to the issue's activity log. The developer drags the card to `IN REVIEW`. A teammate reviews and approves; the PR is merged; the workflow rule transitions `PROJ-104` to `DONE` automatically. The epic `PROJ-100` shows progress without anyone updating its fields directly — its progress bar is computed from its children.

## Comment hygiene and estimating discipline

Two practices separate teams that get value from Jira from teams that find it bureaucratic. The first is comment hygiene. An issue is one conversation about one piece of work, and the comments on it are the durable record of what was discussed and decided. Putting design discussions in Slack and decisions in Jira comments — rather than burying them in a Slack thread that the team will lose track of by next sprint — means that someone joining the team next quarter can read the issue and understand why the code looks the way it does.

The second is estimation. Jira supports estimating in story points (an abstract unit reflecting effort and complexity, calibrated against the team's historical baseline) and in hours. Story points work for stories — a five-point story is roughly twice as much work as a two-point story, regardless of who picks it up, and the team's average velocity in points per sprint is the planning instrument. Hours work for tasks where the estimator already knows roughly how long the work will take and the team needs a deadline-style number for scheduling. Mixing the two on the same backlog produces meaningless aggregates; pick one for stories and stick with it.

The honest summary is that Jira works only as well as the team's hygiene around it. A team that updates the board daily, writes comments on issues, names branches with issue IDs, and reviews the board at every standup gets a tool that tells the truth about the project. A team that does the work but neglects the tracker gets a tool that lies, and a lying tracker is worse than no tracker — it produces false confidence that erodes the moment a stakeholder asks a question the board cannot answer. The companion exercise [Code Collaboration](/exercises/15-code-collaboration/) walks through the Jira-GitHub integration end to end, including the branch naming, the commit message format, and the PR linkage that makes the workflow function.

## Summary

Jira is a database of work items organised under a project, displayed through views like the sprint board and the backlog list. The issue-type hierarchy — epic, story, task, sub-task — lets a single backlog serve product managers, teams, and individual developers at the level of granularity each role needs. The workflow is a state machine, typically `TO DO → IN PROGRESS → IN REVIEW → DONE`, and the sprint board makes work-in-progress visible so the team can see when too much is in flight or when reviews are stuck. The Git integration is implemented through a branch naming convention like `feature/PROJ-123-add-login`, which lets commits, pull requests, and issue status flow through one identifier and removes the need for developers to update Jira separately. Estimating in story points works for stories; estimating in hours works for tasks; mixing them produces nonsense. The deeper truth is that Jira is only as honest as the team's discipline around it. With this chapter the Course Book closes — the path from cloud foundations through application development, data, identity, services, containers, delivery, observability, and team process is now in place, and what remains is the practice of doing the work.
