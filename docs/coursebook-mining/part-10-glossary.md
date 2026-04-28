# Part X — Glossary

Terminology contract for the five chapters of Part X — Collaboration and Process.

## Terms owned by this Part

### Git
- **Owner chapter**: `1-version-control-with-git`
- **Canonical definition**: **Git** is a distributed version control system that tracks changes to files over time by creating immutable snapshots (commits) linked in a directed acyclic graph; every developer has a complete copy of the repository history, enabling offline work and efficient collaboration.
- **Used by chapters**: 1 (owner), 2, 3, 5

### Repository
- **Owner chapter**: `1-version-control-with-git`
- **Canonical definition**: A **repository** (or repo) is a `.git` directory and its contents that stores all commits, branches, tags, and configuration for a project; it serves as the persistent storage of a project's complete history and can be local (on your machine) or remote (on a server like GitHub).
- **Used by chapters**: 1 (owner), 2, 5

### Commit
- **Owner chapter**: `1-version-control-with-git`
- **Canonical definition**: A **commit** is an immutable snapshot of a project's files at a specific point in time, identified by a unique hash; it includes a message, author, timestamp, and a pointer to its parent commit(s), forming a linked chain that represents the project's history.
- **Used by chapters**: 1 (owner), 2, 3, 5

### Branch
- **Owner chapter**: `1-version-control-with-git`
- **Canonical definition**: A **branch** is a named pointer to a commit that allows parallel lines of development; creating a branch duplicates the pointer from the current commit, enabling isolated work without affecting other branches until an explicit merge reunites the changes.
- **Used by chapters**: 1 (owner), 2, 3, 5

### Working tree
- **Owner chapter**: `1-version-control-with-git`
- **Canonical definition**: The **working tree** (or working directory) is the visible, editable folder on your filesystem where project files reside; changes you make in the working tree are not automatically tracked by Git until you stage and commit them.
- **Used by chapters**: 1 (owner), 2

### Staging area
- **Owner chapter**: `1-version-control-with-git`
- **Canonical definition**: The **staging area** (also called the index) is an intermediate area between the working tree and the repository where you prepare files for the next commit; `git add` moves files to the staging area, and `git commit` transforms the staged snapshot into a permanent commit.
- **Used by chapters**: 1 (owner), 2

### Remote
- **Owner chapter**: `1-version-control-with-git`
- **Canonical definition**: A **remote** is a reference to a version of the repository hosted on a server (typically called `origin` for the default remote on GitHub); git commands like `fetch`, `push`, and `pull` synchronize commits between your local repository and the remote.
- **Used by chapters**: 1 (owner), 2, 5

### Fetch
- **Owner chapter**: `1-version-control-with-git`
- **Canonical definition**: **Fetch** is a git command that downloads commits and branches from a remote repository to your local machine without modifying the working tree or your local branches; it updates remote-tracking branches (e.g., `origin/main`) to reflect the current state of the remote.
- **Used by chapters**: 1 (owner), 2

### Pull
- **Owner chapter**: `1-version-control-with-git`
- **Canonical definition**: **Pull** is a git command that combines `fetch` and `merge` (or `rebase`) in a single operation; it downloads commits from the remote and automatically integrates them into your current local branch, updating the working tree.
- **Used by chapters**: 1 (owner), 2

### Push
- **Owner chapter**: `1-version-control-with-git`
- **Canonical definition**: **Push** is a git command that uploads commits from your local branch to the corresponding remote branch, making your work visible to collaborators and updating the remote repository; it requires write permission on the remote.
- **Used by chapters**: 1 (owner), 2, 5

### Pull request (PR)
- **Owner chapter**: `2-branching-pull-requests-and-code-review`
- **Canonical definition**: A **pull request** (or merge request) is a proposal to merge changes from one branch into another (usually a feature branch into `main`); it opens a discussion thread where reviewers examine the code, comment on changes, request revisions, and ultimately approve or reject the merge.
- **Used by chapters**: 2 (owner), 5

### Code review
- **Owner chapter**: `2-branching-pull-requests-and-code-review`
- **Canonical definition**: **Code review** is the process of examining proposed code changes (typically via a pull request) before merging them into a shared branch; reviewers assess correctness, style, design, and security, providing feedback to the author and learning from the codebase.
- **Used by chapters**: 2 (owner), 5

### Merge strategy
- **Owner chapter**: `2-branching-pull-requests-and-code-review`
- **Canonical definition**: A **merge strategy** determines how Git combines two branches: a merge commit preserves both branches' histories, a squash merge combines all commits into one, and a rebase merge replays commits on top of the target branch for a linear history.
- **Used by chapters**: 2 (owner), 3

### Branch protection rule
- **Owner chapter**: `2-branching-pull-requests-and-code-review`
- **Canonical definition**: A **branch protection rule** is a repository setting that enforces policies on a branch (e.g., `main`), such as requiring pull request reviews, status checks, or up-to-date branches before allowing pushes or merges, preventing accidental or malicious direct edits.
- **Used by chapters**: 2 (owner), 5

### Conflict resolution
- **Owner chapter**: `2-branching-pull-requests-and-code-review`
- **Canonical definition**: **Conflict resolution** is the process of handling merge conflicts that occur when two branches modify the same lines of code; the developer manually edits the conflicting file to choose which changes to keep, then completes the merge.
- **Used by chapters**: 2 (owner)

### Draft PR
- **Owner chapter**: `2-branching-pull-requests-and-code-review`
- **Canonical definition**: A **draft PR** (or draft pull request) is a pull request marked as work-in-progress that suppresses notifications and prevents merging; it allows developers to share early feedback on incomplete changes without formally requesting review.
- **Used by chapters**: 2 (owner)

### Inner loop
- **Owner chapter**: `3-inner-loop-vs-outer-loop`
- **Canonical definition**: The **inner loop** is the fast, local development cycle where a developer edits code, runs it (via `dotnet run` or similar), and verifies changes instantly without waiting for CI/CD; it emphasizes rapid feedback and iteration.
- **Used by chapters**: 3 (owner), 4

### Outer loop
- **Owner chapter**: `3-inner-loop-vs-outer-loop`
- **Canonical definition**: The **outer loop** is the slower, automated validation cycle triggered by pushing code (e.g., to GitHub), where CI/CD pipelines build, test, and deploy changes; it provides confidence that changes work across all environments before reaching production.
- **Used by chapters**: 3 (owner)

### Dev container
- **Owner chapter**: `3-inner-loop-vs-outer-loop`
- **Canonical definition**: A **dev container** is a containerized development environment (e.g., via Docker and VS Code Dev Containers extension) that ensures consistent tooling, dependencies, and configuration across team members' machines, reducing "works on my machine" problems.
- **Used by chapters**: 3 (owner)

### Hot reload
- **Owner chapter**: `3-inner-loop-vs-outer-loop`
- **Canonical definition**: **Hot reload** is a development feature that automatically rebuilds and restarts the running application when source code changes, eliminating the manual `stop → edit → run` cycle and accelerating the inner feedback loop.
- **Used by chapters**: 3 (owner)

### Fast feedback
- **Owner chapter**: `3-inner-loop-vs-outer-loop`
- **Canonical definition**: **Fast feedback** is the principle of providing immediate, actionable results to developers (error messages, test failures, runtime behavior) so they can correct mistakes quickly; it is the core benefit of the inner loop.
- **Used by chapters**: 3 (owner)

### Agile
- **Owner chapter**: `4-agile-sprints-and-user-stories`
- **Canonical definition**: **Agile** is a family of iterative software development methodologies that emphasize flexible requirements, frequent delivery, continuous feedback, and team collaboration over rigid upfront planning, with the goal of responding quickly to change.
- **Used by chapters**: 4 (owner), 5

### Sprint
- **Owner chapter**: `4-agile-sprints-and-user-stories`
- **Canonical definition**: A **sprint** is a fixed time-boxed iteration (usually 1–2 weeks) in Scrum where a team commits to completing a set of work items; it includes planning, daily standups, review, and retrospective ceremonies.
- **Used by chapters**: 4 (owner), 5

### User story
- **Owner chapter**: `4-agile-sprints-and-user-stories`
- **Canonical definition**: A **user story** is a brief, user-centric description of a feature in the format "As a [user type], I want [goal] so that [benefit]"; it focuses on delivering value from the user's perspective rather than technical implementation details.
- **Used by chapters**: 4 (owner), 5

### Acceptance criteria
- **Owner chapter**: `4-agile-sprints-and-user-stories`
- **Canonical definition**: **Acceptance criteria** are specific, testable conditions that define when a user story is considered complete; they provide a shared understanding between the team and stakeholders about what "done" means for that story.
- **Used by chapters**: 4 (owner)

### Sprint planning
- **Owner chapter**: `4-agile-sprints-and-user-stories`
- **Canonical definition**: **Sprint planning** is a ceremony where the team estimates story points for backlog items, commits to a sprint goal, and selects work to complete in the upcoming sprint based on team velocity and priorities.
- **Used by chapters**: 4 (owner), 5

### Retrospective
- **Owner chapter**: `4-agile-sprints-and-user-stories`
- **Canonical definition**: A **retrospective** (or retro) is a ceremony at the end of a sprint where the team reflects on what went well, what didn't, and agrees on specific improvements to implement in the next sprint, fostering continuous process improvement.
- **Used by chapters**: 4 (owner)

### Definition of done
- **Owner chapter**: `4-agile-sprints-and-user-stories`
- **Canonical definition**: The **definition of done** (DoD) is a shared checklist of criteria (e.g., code reviewed, tests passing, documentation updated) that a user story must meet before it can be marked complete; it ensures consistent quality across all work.
- **Used by chapters**: 4 (owner)

### Jira
- **Owner chapter**: `5-working-with-jira`
- **Canonical definition**: **Jira** is an Atlassian project and issue tracking tool used by teams to plan work, manage backlogs, run sprints, track progress, and report on project metrics; it integrates with Git platforms like GitHub via webhooks and branch naming conventions.
- **Used by chapters**: 5 (owner)

### Epic
- **Owner chapter**: `5-working-with-jira`
- **Canonical definition**: An **epic** in Jira is a large body of work that is broken down into smaller user stories; epics typically span multiple sprints and represent major features or themes (e.g., "User authentication" or "Payment integration").
- **Used by chapters**: 5 (owner)

### Story (Jira)
- **Owner chapter**: `5-working-with-jira`
- **Canonical definition**: A **story** (in Jira) is a work item representing a user-facing feature or enhancement; it is smaller than an epic, fits within one or two sprints, and includes fields like description, story point estimate, and acceptance criteria.
- **Used by chapters**: 5 (owner)

### Task (Jira)
- **Owner chapter**: `5-working-with-jira`
- **Canonical definition**: A **task** (in Jira) is a work item representing internal work (refactoring, documentation, infrastructure) that does not directly deliver user value; tasks are often subtasks of stories and are tracked on the sprint board.
- **Used by chapters**: 5 (owner)

### Workflow (Jira)
- **Owner chapter**: `5-working-with-jira`
- **Canonical definition**: A **workflow** (in Jira) is a sequence of statuses and transitions that a work item passes through (e.g., TO DO → IN PROGRESS → IN REVIEW → DONE); teams customize workflows to match their development process.
- **Used by chapters**: 5 (owner)

### Sprint board
- **Owner chapter**: `5-working-with-jira`
- **Canonical definition**: The **sprint board** (in Jira) is a visual board displaying work items in the current sprint organized by status columns (e.g., TO DO, IN PROGRESS, DONE); it provides real-time visibility into sprint progress during daily standups.
- **Used by chapters**: 5 (owner)

### Branch naming convention
- **Owner chapter**: `5-working-with-jira`
- **Canonical definition**: A **branch naming convention** is a team standard for naming Git branches (e.g., `feature/JIRA-123-user-login` or `bugfix/issue-456`) that encodes the work item type and Jira ticket ID, enabling automatic linking between code and issues.
- **Used by chapters**: 5 (owner)

## Terms borrowed from earlier Parts

### CI / CD
- **Defined in**: Part VIII — DevOps and Delivery / `2-continuous-integration-and-deployment`
- **Reference link**: `/course-book/8-devops-and-delivery/2-continuous-integration-and-deployment/`

### Pipelines as code
- **Defined in**: Part VIII — DevOps and Delivery / `3-pipelines-as-code`
- **Reference link**: `/course-book/8-devops-and-delivery/3-pipelines-as-code/`

### DevOps
- **Defined in**: Part VIII — DevOps and Delivery / `1-introducing-devops`
- **Reference link**: `/course-book/8-devops-and-delivery/1-introducing-devops/`

### Trunk-based development
- **Defined in**: Part VIII — DevOps and Delivery / `2-continuous-integration-and-deployment`
- **Reference link**: `/course-book/8-devops-and-delivery/2-continuous-integration-and-deployment/`

### ASP.NET Core
- **Defined in**: Part III — Application Development / `2-the-dotnet-platform`
- **Reference link**: `/course-book/3-application-development/2-the-dotnet-platform/`
