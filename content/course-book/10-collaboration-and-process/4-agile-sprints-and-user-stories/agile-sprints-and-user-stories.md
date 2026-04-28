+++
title = "Agile, Sprints, and User Stories"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 40
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/10-collaboration-and-process/4-agile-sprints-and-user-stories.html)

[Se presentationen på svenska](/presentations/course-book/10-collaboration-and-process/4-agile-sprints-and-user-stories-swe.html)

---

Software requirements rarely survive contact with real users. A team that locks down a 200-page specification, builds against it for nine months, and only then ships to production routinely discovers that priorities shifted, assumptions were wrong, or the market moved. Waterfall planning collapses under requirement change because it treats change as an exception. Agile methods invert that assumption — change is the default, and the process exists to absorb it through short, frequent delivery cycles. A working team operates that way through a small set of shared concepts: the values agile rests on, the Scrum cadence that most teams adopt, the user-story format that frames work in user-visible terms, and the acceptance criteria and definition-of-done contracts that make "done" mean something specific.

## Why iterative delivery replaces big-bang planning

A waterfall project sequences phases — requirements, design, implementation, testing, release — and treats each as a gate. The plan only works if the requirements gathered at the start remain accurate when the product ships. In practice they almost never do. Stakeholders learn what they want by seeing partial systems running. Regulations change. Competitors release features that reshape the priority list. By the time a waterfall release lands, parts of it are already obsolete.

Iterative delivery accepts this and shrinks the cycle. Instead of one delivery in nine months, a team delivers something useful every one or two weeks. Each delivery is a real chance to learn — to confirm an assumption, to discover a bad one, to re-prioritize the next iteration based on what was just observed. The plan becomes a living artifact rather than a contract, and the cost of a wrong direction is bounded by the length of one iteration rather than the length of the project.

## The four values of the Agile Manifesto

**Agile** is a family of iterative software development methodologies that emphasize flexible requirements, frequent delivery, continuous feedback, and team collaboration over rigid upfront planning, with the goal of responding quickly to change. The 2001 Agile Manifesto distilled the movement into four value pairs. Each pair acknowledges that both sides have value, then states that the left side has more.

The first pair is individuals and interactions over processes and tools. A team of skilled people communicating well will out-deliver a more rigid team using better tools. Process exists to support people, not the other way around — when a process consistently obstructs good work, the team should reshape it.

The second pair is working software over comprehensive documentation. Working software is the only honest measure of progress. A 50-page design document does not run, cannot be exercised by a user, and does not validate any assumption. Some documentation is necessary; the value statement says it should never become a substitute for running code.

The third pair is customer collaboration over contract negotiation. A fixed-scope contract drafted before either party fully understands the problem will protect the wrong things. Continuous collaboration with the customer — letting them see partial work, adjusting based on their feedback — produces better outcomes than enforcing a contract that was wrong on the day it was signed.

The fourth pair is responding to change over following a plan. Plans are useful as snapshots of current intent, but the project must be able to incorporate new information without procedural friction. The plan is a tool, not a commitment.

These values do not constitute a method on their own. They are the criteria a team uses when choosing how to work.

## Scrum as the dominant framework

Most agile teams adopt Scrum, a lightweight framework that turns the manifesto values into a concrete weekly rhythm. Scrum defines roles (product owner, Scrum master, development team), artifacts (product backlog, sprint backlog, increment), and a small set of recurring ceremonies. The framework's core mechanism is the sprint.

A **sprint** is a fixed time-boxed iteration (usually 1–2 weeks) in Scrum where a team commits to completing a set of work items; it includes planning, daily standups, review, and retrospective ceremonies. The time-box is non-negotiable. If the team estimates poorly and cannot finish everything, the sprint still ends on the planned date and unfinished work returns to the backlog. This rule is what gives sprints their feedback value: every two weeks the team gets an honest signal about its capacity.

### Sprint planning

**Sprint planning** is a ceremony where the team estimates story points for backlog items, commits to a sprint goal, and selects work to complete in the upcoming sprint based on team velocity and priorities. The product owner brings a prioritized backlog. The team reviews the top items, asks clarifying questions, and decides how many it can credibly complete given its observed velocity from previous sprints. The output is a sprint backlog — a concrete list of work the team has committed to — and a single-sentence sprint goal that explains what the sprint is for.

### The daily sync

Once the sprint is running, the team meets briefly each day — fifteen minutes is typical — to coordinate. Each member answers three implicit questions: what was completed since the last sync, what is planned for today, and what is blocked. The daily sync is not a status report to a manager; it is the team aligning with itself. Blockers surface immediately so someone can clear them rather than discovering at the end of the sprint that a story stalled on day three.

### Sprint review and retrospective

At the end of the sprint the team holds two ceremonies. The sprint review demonstrates the increment to stakeholders. Working software is shown, not described. Stakeholders react, ask questions, and the product owner adjusts the backlog based on what they learn.

The **retrospective** (or retro) is a ceremony at the end of a sprint where the team reflects on what went well, what didn't, and agrees on specific improvements to implement in the next sprint, fostering continuous process improvement. The retrospective focuses on the process, not the product. It produces a small number of concrete actions — usually two or three — that the team will try in the next sprint. Without this ceremony the framework becomes mechanical and stops adapting.

## The user story format

A **user story** is a brief, user-centric description of a feature in the format "As a [user type], I want [goal] so that [benefit]"; it focuses on delivering value from the user's perspective rather than technical implementation details. The format is deliberately constrained. The role names a specific user type, the capability names what they want to do, and the outcome explains why it matters to them.

The format works because each part forces a distinct conversation. Naming the role exposes whether the team understands which user is being served. Naming the capability keeps the story at the level of what the user does, not how the system implements it. Naming the outcome links the work to user value, which is what the team uses when prioritizing or descoping under pressure.

A user story is not a specification. It is a placeholder for a conversation that the team will have when the story enters a sprint. The card reminds the team that a conversation is needed; the conversation produces the shared understanding; the acceptance criteria record the outcome of that conversation.

## Acceptance criteria as the "how do we know it's done" answer

A story without acceptance criteria invites endless interpretation. **Acceptance criteria** are specific, testable conditions that define when a user story is considered complete; they provide a shared understanding between the team and stakeholders about what "done" means for that story. Each criterion is concrete enough to verify by observation — either the behavior is present or it is not.

Acceptance criteria are story-specific. They answer "what must be true about this story before we accept it" and nothing more. They do not include cross-cutting concerns like test coverage or deployment status — those belong in the definition of done.

## Definition of done as the team's quality contract

The **definition of done** (DoD) is a shared checklist of criteria (e.g., code reviewed, tests passing, documentation updated) that a user story must meet before it can be marked complete; it ensures consistent quality across all work. The DoD applies to every story regardless of subject matter. It encodes what the team has agreed counts as professional output. A typical DoD includes items like: unit tests pass, the change is reviewed and merged via [pull request](/course-book/10-collaboration-and-process/2-branching-pull-requests-and-code-review/), the build is green on the main branch, the change is deployed to a staging environment, and any user-facing change has updated documentation.

The acceptance criteria and the DoD work together. Acceptance criteria say "this specific story does what it claims." The DoD says "this work is in the shape we ship." A story is only complete when both are satisfied.

## Worked example: a sign-up form

Consider a story for a new user sign-up form on a web application. Written in user-story format, it reads:

```text
As a prospective customer, I want to create an account with email and password,
so that I can save my preferences and place orders.
```

The team agrees on three acceptance criteria during sprint planning:

- The form rejects an email address that is already registered, with a visible error message naming the conflict.
- A password shorter than 10 characters is rejected before the form submits, with a message explaining the minimum length.
- A successful sign-up sends a verification email within 60 seconds and redirects the browser to a "check your email" page.

These criteria are observable: a tester can drive a browser, attempt each path, and report pass or fail. They do not say anything about how the email is sent, which library hashes the password, or which framework renders the form — those decisions belong to the developer.

The team's DoD adds the contract that every story must satisfy on top of its acceptance criteria. For the sign-up story, the DoD might require: unit tests cover the email-uniqueness and password-length checks; the change is merged via a reviewed pull request; an integration test exercises the full sign-up flow against a staging database; the staging deployment is healthy after the merge; and a release note mentions the new sign-up flow. A story that passes all three acceptance criteria but ships without the integration test is not done. A story with the integration test but with a broken staging deployment is not done either. The DoD is a gate, not a wish list.

## Scrum compared with Kanban

Scrum is not the only agile framework. Kanban organizes work as a continuous flow rather than as time-boxed sprints. A Kanban team maintains a board with columns representing workflow stages (e.g., To Do, In Progress, In Review, Done) and applies work-in-progress (WIP) limits to each column. New work pulls into the system whenever capacity opens; work is delivered as soon as it finishes rather than at the end of a fixed iteration.

| Dimension | Scrum | Kanban |
|-----------|-------|--------|
| Cadence | Fixed-length sprints (1–2 weeks) | Continuous flow |
| Commitment | Sprint backlog committed at planning | No commitment to a fixed batch |
| Ceremonies | Planning, daily sync, review, retrospective | Daily sync optional; flow review periodic |
| Capacity control | Sprint capacity from velocity | WIP limits per column |
| Best fit | Feature work with discoverable scope | Operational work, support, mixed-priority queues |

Many real teams blend the two — running sprint planning and retrospectives while letting work flow continuously inside the sprint, or applying WIP limits within a Scrum board. The framework is a tool. Teams adopt the parts that produce useful feedback and adjust the rest.

## Connecting to practice

The Scrum cadence becomes concrete only once a team practices it on a real backlog with real code. The collaboration exercises at [/exercises/15-code-collaboration/](/exercises/15-code-collaboration/) walk through writing user stories, opening pull requests against them, and integrating with [DevOps](/course-book/8-devops-and-delivery/1-the-devops-philosophy/) automation that enforces the definition of done. Working through the exercises is where the vocabulary in this chapter stops being abstract.

## Summary

Iterative delivery replaces big-bang waterfall planning because it treats requirement change as the default rather than as an exception. The Agile Manifesto's four values — individuals over process, working software over documentation, customer collaboration over contract, and responding to change over following a plan — give a team the criteria for deciding how to work. Scrum operationalizes those values through fixed-length sprints with planning, a daily sync, a review, and a retrospective. User stories frame work in the format "As a role, I want a capability, so that an outcome," keeping the team focused on user-visible value. Acceptance criteria define when a specific story is done; the definition of done is the cross-cutting quality contract that every story must satisfy. Kanban offers an alternative to time-boxed sprints when continuous flow fits the work better, and many teams blend the two.
