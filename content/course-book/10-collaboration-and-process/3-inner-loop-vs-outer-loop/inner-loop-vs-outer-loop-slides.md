+++
title = "Inner Loop vs Outer Loop"
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

## Inner Loop vs Outer Loop
Part X — Collaboration and Process

---

## Productivity is feedback latency
- Every code change waits for an answer — that wait governs the day
- Two distinct cycles produce that answer at very different speeds
- The fast one runs on the laptop; the slow one runs on shared infrastructure
- Both are necessary; each catches problems the other cannot
- Knowing which loop owns which check is an engineering judgement

---

## The inner loop
- The **edit-build-run-test** cycle on the developer's own machine
- Runs in **seconds to single-digit minutes**, ideally invisible
- Answers narrow questions: does it compile, does the test pass, is the JSON right
- Runs hundreds of times a day per developer
- Nothing leaves the laptop until the developer is satisfied

---

## The outer loop
- The **commit-push-CI-deploy** cycle on shared infrastructure
- Runs in **minutes**, triggered by `git push` or a pull request
- Answers integrative questions: clean build, real database, deploy succeeds
- Defined as code in the repository; gates the merge to `main`
- The team's shared safety net, not a substitute for local work

---

## Why optimizing the inner loop matters
- Saved seconds compound across **thousands of iterations a week**
- Slow cycles break concentration — the real cost is re-orientation, not the wait
- Fast feedback enables test-first discipline; slow feedback quietly erodes it
- Cheap iteration encourages exploration — three approaches instead of one
- Inner-loop speed is, indirectly, code-quality investment

---

## Hot reload
- `dotnet watch run` monitors the source tree and reloads on save
- Collapses **save → stop → run** into one step
- The browser becomes a live mirror of the source file
- Falls back to a full rebuild for changes it cannot patch in place
- Best return on effort for UI-heavy iteration

---

## Fast unit tests
- A thousand fast tests can run in **under five seconds**
- Stay fast by avoiding I/O — no disk, no network, no real database
- Run the whole suite on save; treat any red as an immediate signal
- Cannot validate integration — that is the outer loop's job
- The inner loop owns correctness of individual units

---

## In-memory databases for tests
- EF Core `InMemoryDatabase` or SQLite `:memory:` — fast enough to run on save
- Catches **query-shape bugs**: wrong joins, missing `Include`, bad LINQ
- Not the production database — JSON columns and vendor SQL behave differently
- A useful intermediate signal, not a substitute for a real-DB outer-loop test
- Promotes some integration coverage into the inner loop

---

## Dev containers
- Declare the entire toolchain — runtimes, DBs, linters — in one repo file
- Eliminates "works on my machine" by making the container *be* the machine
- Onboarding shrinks from a day of installs to a Reopen-in-Container click
- Brings inner-loop tooling to any IDE that speaks the protocol
- Trade-off: the indirection layer can add latency on slow machines

---

## The boundary conversation
- Default: push every check toward the inner loop until something forces it out
- Outer loop earns its slot when a check needs **shared infra**, runs **too slow** for save, or validates a **production-like property**
- Re-visit the boundary as the codebase grows — slow tests creep inward
- Practice: [/exercises/15-code-collaboration/](/exercises/15-code-collaboration/)
- The team's shared judgement is itself a piece of engineering practice

---

## Questions?
