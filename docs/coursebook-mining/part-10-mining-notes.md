# Part X — Mining Notes

## Part Scope

Part X covers Git-based collaboration workflows, agile planning and process, code review discipline, and Jira integration for the ACD track. Focus: professional team development practices.

## Week 1 Content (v.15)

**"Agilt arbetssätt och utvecklingsmiljö"** covers:
- Jira account setup, agile fundamentals, user stories
- Git fundamentals: init, staging area, commits, `.gitignore`
- Branching, feature branches, pull request lifecycle
- Code review workflow, merge strategies
- GitHub CLI (`gh`) for PR automation
- Inner loop: `dotnet run`, local development, fast feedback

## Key Observations

1. **Version Control**: Exercises 1–2 ground students in local Git (`git init`, staging, commits) before moving to remote workflows.
2. **Pull Request Discipline**: Exercise 4 emphasizes full PR cycle including review and approval before merge.
3. **Jira + Git Integration**: Exercise 5 bridges Jira and GitHub; Exercise 6 shows how to link branch naming and sprint tracking.
4. **Agile Concepts**: User stories (Exercise 3) follow "As a [user]..." format; story points for estimation; sprint planning, board movement, retrospectives.
5. **Branch Strategy**: Exercises use `feature/*` prefix; trunk-based naming convention referenced; merges preserve history vs squash tradeoff mentioned.
6. **GitHub CLI**: Exercise 4 shows `gh pr create`, `gh pr view`, `gh pr merge` for scriptable workflows.
7. **Terminology Gaps**: Inner loop vs outer loop not explicitly defined in exercises; DevOps/CI/CD referenced but not detailed (belongs in Part VIII).

## Owned Terms by Chapter

- **Ch 1**: Git, repository, commit, branch, working tree, staging area, remote, fetch/pull/push
- **Ch 2**: pull request (PR), code review, merge strategy, branch protection rule, conflict resolution, draft PR
- **Ch 3**: inner loop, outer loop, dev container, hot reload, fast feedback
- **Ch 4**: agile, sprint, user story, acceptance criteria, sprint planning, retrospective, definition of done
- **Ch 5**: Jira, epic, story (Jira), task (Jira), workflow (Jira), sprint board, branch naming convention

## Borrowed Terms (Part VIII and III)

CI/CD, Pipelines as code, DevOps, Trunk-based development, ASP.NET Core — linked but not redefined.

## Suggested Chapter Structure

1. **Version Control with Git** — `git init`, staging, commits, branches, remotes; `.gitignore` best practice
2. **Branching, PRs, Code Review** — feature branching, PR creation/review/merge, merge strategies, protection rules
3. **Inner Loop vs Outer Loop** — local dev loop (`dotnet run`, hot reload), CI/CD outer loop, dev containers
4. **Agile, Sprints, User Stories** — user story format, story points, sprint ceremony, retrospectives, DoD
5. **Working with Jira** — project setup, backlog, board, epics/stories/tasks, GitHub integration, velocity

## Cross-Part Consistency Notes

- Part III (Fundamentals) covers HTTP, ASP.NET Core fundamentals
- Part VIII (DevOps) covers CI/CD pipelines and trunk-based development
- Part IX (Operations) covers observability and monitoring
- Careful not to duplicate DevOps concepts; Part X focuses on *process and collaboration*, not pipeline mechanics
