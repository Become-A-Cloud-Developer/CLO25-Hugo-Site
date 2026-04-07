+++
title = "Pull Request Workflow"
program = "CLO"
cohort = "25"
courses = ["BCD"]
description = "Learn feature branching and pull requests: the foundation of collaborative development"
weight = 4
+++

# Pull Request Workflow

## Goal

Learn the pull request workflow that professional development teams use to collaborate on code. You'll create a feature branch, make changes, submit a pull request, experience the review process, and clean up after merging.

> **What you'll learn:**
>
> - Why branches exist and how they enable parallel work
> - How to create and work on feature branches
> - The pull request lifecycle from creation to merge
> - How to review changes before they're merged
> - Best practices for branch cleanup

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ A GitHub repository with your .NET MVC project (from exercise 2)
> - ✓ Your local repository synced with GitHub (main branch up to date)
> - ✓ GitHub CLI (`gh`) installed and authenticated

To verify your setup:

```bash
# Check you're on main and up to date
git checkout main
git pull

# Verify GitHub CLI authentication
gh auth status
```

## Exercise Steps

### Overview

1. **Understand Branching Concepts**
2. **Create a Feature Branch**
3. **Make Changes on the Feature Branch**
4. **Push the Feature Branch to GitHub**
5. **Create a Pull Request**
6. **Review the Pull Request**
7. **Merge and Clean Up**

### **Step 1:** Understand Branching Concepts

Branches allow you to work on changes in isolation without affecting the main codebase. Think of them as parallel timelines: you can experiment freely on a branch, and only merge your changes back when they're ready.

This workflow is the foundation of team collaboration. While you work on one feature, teammates can work on other features in their own branches. Everyone's work stays separate until it's ready to combine.

> ℹ **Concept Deep Dive**
>
> In Git, a branch is simply a pointer to a commit. When you create a branch, Git creates a new pointer that you can move independently of the main branch.
>
> ```
> main:    A --- B --- C
>                       \
> feature:               D --- E
> ```
>
> In this diagram:
>
> - Commits A, B, C are on the main branch
> - You created a feature branch from commit C
> - Commits D and E exist only on the feature branch
> - The main branch is unaffected by your work until you merge
>
> **Why this matters:**
>
> - **Safety** - If your experiment fails, main is untouched
> - **Parallel work** - Multiple features can develop simultaneously
> - **Review** - Changes can be reviewed before affecting production code
>
> ✓ **Quick check:** You understand that branches let you work without affecting main

### **Step 2:** Create a Feature Branch

Create a new branch for your feature. The branch name should describe what you're working on.

1. **Ensure** you're on the main branch:

   ```bash
   git checkout main
   ```

2. **Create and switch** to a new feature branch:

   ```bash
   git checkout -b feature/update-greeting
   ```

   You should see:

   ```
   Switched to a new branch 'feature/update-greeting'
   ```

3. **Verify** you're on the new branch:

   ```bash
   git branch
   ```

   You should see:

   ```
     main
   * feature/update-greeting
   ```

   The asterisk (*) indicates your current branch.

> ℹ **Concept Deep Dive**
>
> Branch naming conventions help teams understand the purpose of each branch at a glance:
>
> | Prefix | Purpose | Example |
> |--------|---------|---------|
> | `feature/` | New functionality | `feature/user-login` |
> | `bugfix/` | Bug repairs | `bugfix/fix-typo` |
> | `hotfix/` | Urgent production fixes | `hotfix/security-patch` |
>
> The command `git checkout -b` is a shortcut that combines two operations:
>
> - `git branch feature/update-greeting` (create the branch)
> - `git checkout feature/update-greeting` (switch to it)
>
> ⚠ **Common Mistakes**
>
> - Creating a branch from the wrong starting point (always branch from an up-to-date main)
> - Using spaces in branch names (use hyphens instead)
>
> ✓ **Quick check:** Running `git branch` shows you're on `feature/update-greeting`

### **Step 3:** Make Changes on the Feature Branch

Now make a change to your code. You'll modify the greeting message in your .NET MVC application.

1. **Open** `Views/Home/Index.cshtml` in your editor

2. **Change** the greeting message. Find the heading and update it:

   ```html
   <h1 class="display-4">Hello, Code Review!</h1>
   ```

3. **Save** the file

4. **Check** what changed:

   ```bash
   git status
   ```

   You should see `Views/Home/Index.cshtml` listed as modified.

5. **Stage and commit** the change:

   ```bash
   git add Views/Home/Index.cshtml
   git commit -m "Update greeting message for code review demo"
   ```

6. **Verify** the commit exists on your branch:

   ```bash
   git log --oneline -3
   ```

   You should see your new commit at the top.

> ℹ **Concept Deep Dive**
>
> Your commit now exists only on the `feature/update-greeting` branch. If you switch back to main, you won't see this change:
>
> ```bash
> git checkout main
> cat Views/Home/Index.cshtml    # Shows old greeting
> git checkout feature/update-greeting
> cat Views/Home/Index.cshtml    # Shows new greeting
> ```
>
> This isolation is the power of branching. You can experiment freely, and main remains stable until you explicitly merge your changes.
>
> ✓ **Quick check:** Your commit appears in `git log --oneline`

### **Step 4:** Push the Feature Branch to GitHub

Push your feature branch to GitHub so others can see your work and you can create a pull request.

1. **Push** the branch to GitHub:

   ```bash
   git push -u origin feature/update-greeting
   ```

   You should see output indicating the branch was pushed:

   ```
   Enumerating objects: 5, done.
   ...
   To https://github.com/yourusername/HelloMvc.git
    * [new branch]      feature/update-greeting -> feature/update-greeting
   branch 'feature/update-greeting' set up to track 'origin/feature/update-greeting'.
   ```

2. **Verify** the branch exists on GitHub:

   ```bash
   gh repo view --web
   ```

   On GitHub, click the branch dropdown (shows "main") and you should see `feature/update-greeting` listed.

> ℹ **Concept Deep Dive**
>
> The `-u` flag (or `--set-upstream`) establishes a tracking relationship between your local branch and the remote branch. After this, you can simply use `git push` and `git pull` without specifying the branch name.
>
> Before pushing:
>
> ```
> Local:  feature/update-greeting
> Remote: (doesn't exist)
> ```
>
> After pushing:
>
> ```
> Local:  feature/update-greeting → tracks → origin/feature/update-greeting
> Remote: feature/update-greeting (new!)
> ```
>
> ✓ **Quick check:** Your feature branch is visible on GitHub

### **Step 5:** Create a Pull Request

A pull request (PR) is a proposal to merge your changes into another branch. It's called a "pull request" because you're requesting that the maintainers pull your changes into the main branch.

1. **Create** a pull request using GitHub CLI:

   ```bash
   gh pr create --title "Update greeting message" --body "Change the greeting to demonstrate the code review workflow.

   ## Changes
   - Modified the greeting in Views/Home/Index.cshtml

   ## Testing
   - Run dotnet run and visit the application URL to see the new greeting"
   ```

   You should see:

   ```
   Creating pull request for feature/update-greeting into main in yourusername/HelloMvc

   https://github.com/yourusername/HelloMvc/pull/1
   ```

2. **Note** the PR URL that was created - you'll use it in the next step.

> ℹ **Concept Deep Dive**
>
> A good pull request includes:
>
> | Component | Purpose |
> |-----------|---------|
> | **Title** | Brief summary of the change |
> | **Description** | Context: why this change? what does it do? |
> | **Changes** | What files/features were modified |
> | **Testing** | How to verify the change works |
>
> The PR description becomes documentation. Months later, someone investigating why code changed can read the PR to understand the reasoning.
>
> **Alternative:** You can also create PRs through the GitHub web interface by clicking "Compare & pull request" after pushing a branch.
>
> ✓ **Quick check:** The `gh pr create` command succeeded and showed a PR URL

### **Step 6:** Review the Pull Request

This is the most important step. Code review is where teams catch bugs, share knowledge, and maintain code quality. Even when working alone, reviewing your own changes before merging helps catch mistakes.

1. **Open** the pull request in your browser:

   ```bash
   gh pr view --web
   ```

2. **Explore** the PR interface:

   - **Conversation tab** - Discussion, comments, and status
   - **Commits tab** - List of commits in this PR
   - **Files changed tab** - The actual code diff (most important!)

3. **Click** on the **Files changed** tab

4. **Study** the diff:

   - Green lines with `+` are additions
   - Red lines with `-` are deletions
   - You can see exactly what changed and in what context

5. **Add a review comment** (practice even on your own PR):

   - Hover over a line number in the diff
   - Click the blue `+` button that appears
   - Write a comment like: "Changed greeting to demonstrate PR workflow"
   - Click **Add single comment**

6. **Submit a review:**

   - Click the green **Review changes** button (top right of Files changed)
   - Write a summary: "Changes look good. Ready to merge."
   - Select **Approve**
   - Click **Submit review**

> ℹ **Concept Deep Dive**
>
> **Why code review matters:**
>
> - **Quality** - A second pair of eyes catches bugs you missed
> - **Knowledge sharing** - Reviewers learn about parts of the codebase they didn't write
> - **Documentation** - PR discussions explain *why* changes were made
> - **Standards** - Teams maintain consistent coding practices
>
> **Review mindset - ask yourself:**
>
> - Does the code do what the PR description says?
> - Is the change easy to understand?
> - Are there edge cases that weren't considered?
> - Would I understand this code in 6 months?
>
> Even reviewing your own code is valuable. The act of looking at changes in a different context (the GitHub diff view vs your editor) often reveals issues you didn't notice while coding.
>
> ⚠ **Common Mistakes**
>
> - Approving without actually reading the changes
> - Focusing only on style instead of logic
> - Being harsh rather than constructive in feedback
>
> ✓ **Quick check:** You've explored the diff, added a comment, and submitted a review

### **Step 7:** Merge and Clean Up

After review, merge the pull request and clean up the feature branch. Keeping branches tidy prevents confusion about what's active work versus old experiments.

1. **Merge** the pull request:

   ```bash
   gh pr merge --merge
   ```

   The `--merge` flag creates a merge commit. You'll be prompted to confirm.

   Alternatively, click the green **Merge pull request** button on GitHub.

2. **Delete the remote branch** when prompted (or manually):

   ```bash
   git push origin --delete feature/update-greeting
   ```

   GitHub often offers to delete the branch automatically after merging - click that button if you see it.

3. **Switch** back to main:

   ```bash
   git checkout main
   ```

4. **Pull** the merged changes:

   ```bash
   git pull
   ```

   You should see the merge commit being downloaded.

5. **Delete** the local feature branch:

   ```bash
   git branch -d feature/update-greeting
   ```

   You should see:

   ```
   Deleted branch feature/update-greeting (was abc1234).
   ```

6. **Verify** everything is clean:

   ```bash
   git branch
   ```

   You should see only `main`.

   ```bash
   git log --oneline -3
   ```

   You should see the merge commit and your feature commit in the history.

> ℹ **Concept Deep Dive**
>
> **Why delete branches?**
>
> - **Clarity** - Active branches represent work in progress; old branches create confusion
> - **Hygiene** - A repository with hundreds of stale branches is hard to navigate
> - **Git history** - The commits are preserved in main; the branch pointer is no longer needed
>
> **Merge strategies** (brief overview):
>
> | Strategy | Result | When to use |
> |----------|--------|-------------|
> | **Merge** | Creates a merge commit | Default, preserves full history |
> | **Squash** | Combines all commits into one | Clean up messy commit history |
> | **Rebase** | Replays commits on top of main | Linear history, advanced |
>
> For now, regular merge commits are fine. Teams develop preferences over time.
>
> ✓ **Quick check:** Only `main` branch exists locally, and `git log` shows your merged changes

## Common Issues

> **If you encounter problems:**
>
> **"Branch already exists":** Use a different branch name, or delete the old branch: `git branch -d old-branch-name`
>
> **"Your branch is behind":** Your main branch is outdated. Run: `git checkout main && git pull` before creating a new feature branch
>
> **"Pull request has conflicts":** Your changes conflict with other changes to main. For this exercise, the simplest fix is to start over with a fresh branch from an updated main
>
> **"Permission denied" when pushing:** Check your GitHub CLI authentication: `gh auth status`
>
> **Branch not found on remote:** You forgot to push the feature branch. Run: `git push -u origin feature/update-greeting`
>
> **Can't delete branch (has unmerged changes):** The branch wasn't merged. Either merge it first, or force delete with `git branch -D branch-name` (careful - this discards unmerged work)
>
> **Still stuck?** Run `git status` and `git branch -a` to understand your current state

## Summary

You've successfully completed the pull request workflow which:

- ✓ Created a feature branch to isolate your work
- ✓ Made changes and committed them on the feature branch
- ✓ Pushed to GitHub and created a pull request
- ✓ Reviewed the changes and approved the PR
- ✓ Merged and cleaned up both local and remote branches

> **Key takeaway:** The pull request workflow - branch, commit, push, PR, review, merge - is how professional teams collaborate on code. The review step is where quality happens: it catches bugs, spreads knowledge, and documents decisions. Always review changes (even your own) before merging to main.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Create a **draft PR** to share work in progress without requesting review: `gh pr create --draft`
> - Learn about **branch protection rules** that require reviews before merging
> - Explore **squash merging** to combine many commits into one clean commit
> - Research **rebasing** to keep a linear commit history
> - Set up a **PR template** in your repository (`.github/pull_request_template.md`)
> - Practice requesting specific **reviewers** when working with a team

## Done! 🎉

You've mastered the pull request workflow. This cycle - create branch, make changes, push, create PR, review, merge, clean up - is what you'll repeat for every feature you build. The review step transforms solo coding into collaborative development, even when you're the only reviewer.
