+++
title = "Agile, Sprints, and User Stories"
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

## Agile, Sprints, and User Stories
Part X — Collaboration and Process

---

## Why iterative delivery
- Waterfall plans **collapse** when requirements change mid-project
- Iterative delivery treats **change as the default**, not an exception
- Short cycles bound the cost of a wrong direction
- Each iteration is a real chance to learn and re-prioritize

---

## The four agile values
- **Individuals and interactions** over processes and tools
- **Working software** over comprehensive documentation
- **Customer collaboration** over contract negotiation
- **Responding to change** over following a plan

---

## Scrum and the sprint
- A **sprint** is a fixed time-box, usually 1–2 weeks
- Time-box is non-negotiable — unfinished work returns to the backlog
- Honest signal each iteration about real team capacity
- Roles: product owner, Scrum master, development team

---

## Sprint ceremonies
- **Sprint planning** — commit to a goal and a backlog
- **Daily sync** — fifteen-minute alignment, not a status report
- **Sprint review** — demonstrate working software to stakeholders
- **Retrospective** — improve the process, not the product

---

## The user story format
- Format: "As a **role**, I want a **capability**, so that an **outcome**"
- Role exposes which user is being served
- Capability stays at user-visible behavior, not implementation
- Outcome links work to user value for prioritization

---

## Acceptance criteria
- **Story-specific**, testable conditions for "this story is done"
- Each criterion is observable — pass or fail
- Recorded outcome of the team's conversation about the story
- Does not include cross-cutting concerns like coverage or deploy

---

## Definition of done
- **Cross-cutting** quality contract for every story
- Typical items: tests pass, PR merged, deployed to staging
- Pairs with acceptance criteria — both must be satisfied to ship
- Encodes what the team has agreed counts as professional output

---

## Worked example: sign-up form
- Story: "As a prospective customer, I want to create an account..."
- AC1: duplicate email rejected with a visible error
- AC2: password under 10 characters rejected before submit
- AC3: verification email sent within 60 seconds
- DoD gate: tests, reviewed PR, healthy staging deploy

---

## Scrum compared with Kanban
- **Scrum**: fixed-length sprints, committed batch, full ceremony set
- **Kanban**: continuous flow, WIP limits per column
- Scrum fits feature work; Kanban fits operational queues
- Many teams blend the two — adopt what produces useful feedback

---

## Questions?
