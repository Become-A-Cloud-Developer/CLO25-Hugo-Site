+++
title = "Development Workflow with Jira"
program = "CLO"
cohort = "25"
courses = ["BCD"]
description = "Complete the full development cycle: create a story, branch from Jira, commit with issue keys, merge, and close"
weight = 6
+++

# Development Workflow with Jira

## Goal

Experience the complete development workflow by implementing a feature from Jira story to merged code, using the Jira-GitHub integration to track every step.

> **What you'll learn:**
>
> - How to create branches directly from Jira issues
> - How issue keys link commits to stories automatically
> - How to track development progress in Jira's Development panel
> - The complete cycle: Story → Branch → Code → Commit → PR → Merge → Done

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Jira-GitHub integration configured (your GitHub organization connected to Jira)
> - ✓ A GitHub repository that Jira can access
> - ✓ Local clone of that repository on your machine
> - ✓ Git configured with your credentials

To verify your setup:

1. Open your Jira project and navigate to the **Code** tab
2. Confirm you see your connected GitHub organization
3. In your terminal, verify you can pull from the repository:

   ```bash
   git fetch origin
   ```

## Exercise Steps

### Overview

1. **Create a User Story**
2. **Start a Sprint**
3. **Move Story to In Progress**
4. **Create a Branch from Jira**
5. **Fetch the Branch Locally**
6. **Make Code Changes**
7. **Commit with Issue Key**
8. **Push and Verify in Jira**
9. **Create and Merge Pull Request**
10. **Move Story to Done**

### **Step 1:** Create a User Story

Every development task starts with a user story. This creates the artifact that will track all code changes related to this feature. The story's issue key becomes the link between planning and implementation.

1. **Navigate to** your Jira project's **Backlog** view

2. **Click** **+ Create** to add a new work item

3. **Select** "Story" as the work type

4. **Enter** a summary using the user story format:

   ```text
   As a user I want to see a welcome message on the homepage
   ```

5. **Click** **Create** to save the story

6. **Note** the issue key assigned (e.g., `SCRUM-4`)

> ℹ **Concept Deep Dive**
>
> The issue key (like `SCRUM-4`) is crucial for the integration. This key:
>
> - Uniquely identifies the work item across both Jira and GitHub
> - When included in branch names, commits, or PRs, automatically links them to this story
> - Enables full traceability from requirement to code
>
> The format is always `PROJECT-NUMBER` where PROJECT is your Jira project key.
>
> ✓ **Quick check:** Your story appears in the Backlog with an issue key like SCRUM-4

### **Step 2:** Start a Sprint

To work on a story, it needs to be in an active sprint. This associates the work with a time-boxed iteration and makes it visible on the Scrum board.

1. **Drag** your new story into a sprint section (or create a new sprint if needed)

2. **Click** **Start sprint** on the sprint section

3. **Configure** the sprint:
   - Sprint name: Keep the default or customize
   - Duration: 1 week
   - Start date: Today

4. **Click** **Start** to activate the sprint

5. **Observe** that Jira navigates to the Board view

> ℹ **Concept Deep Dive**
>
> Starting a sprint:
>
> - Makes the stories visible on the active board
> - Begins the time-box for this iteration
> - Enables tracking of work in progress
>
> In a real project, sprint planning would involve the whole team. For this exercise, you're simulating that process.
>
> ✓ **Quick check:** Your story appears in the TO DO column on the Board

### **Step 3:** Move Story to In Progress

Before starting development, update the story status to reflect that work has begun. This keeps the board accurate and communicates progress to the team.

1. **Locate** your story card in the **TO DO** column

2. **Drag** the card to the **IN PROGRESS** column

3. **Observe** the card now shows in the middle column

> ℹ **Concept Deep Dive**
>
> The Scrum board columns represent workflow states:
>
> | Column | Meaning |
> |--------|---------|
> | **TO DO** | Work ready to start but not yet begun |
> | **IN PROGRESS** | Work actively being developed |
> | **DONE** | Work completed and verified |
>
> Moving cards during daily standups helps teams answer: "What did I do? What will I do? Any blockers?"
>
> ✓ **Quick check:** Your story is in the IN PROGRESS column

### **Step 4:** Create a Branch from Jira

Instead of creating branches manually, Jira can create them directly on GitHub with the issue key already included. This ensures consistent naming and automatic linking.

1. **Click** on your story card to open the detail panel

2. **Locate** the **Development** section on the right side of the panel

3. **Click** **Create branch**

4. **Review** the branch creation dialog:
   - **Repository**: Select your connected GitHub repository
   - **Branch from**: `main` (or your default branch)
   - **Branch name**: Auto-generated with issue key (e.g., `SCRUM-4-As-a-user-I-want-to-see-a-welcome-message`)

5. **Click** **Create branch**

6. **Observe** the success message with a link to view the branch in GitHub

> ℹ **Concept Deep Dive**
>
> **Why create branches from Jira?**
>
> - Branch name automatically includes the issue key
> - Jira immediately knows about the branch (no sync delay)
> - Consistent naming convention across the team
> - Links are established before any code is written
>
> The auto-generated branch name follows the pattern: `ISSUE-KEY-story-title-in-kebab-case`
>
> You can also create branches manually in Git and include the issue key - Jira will still detect them. But creating from Jira is more reliable.
>
> ⚠ **Common Mistakes**
>
> - Selecting the wrong repository if you have multiple connected
> - Branching from a feature branch instead of main
>
> ✓ **Quick check:** You see "GitHub branch created" with the branch name containing your issue key

### **Step 5:** Fetch the Branch Locally

The branch now exists on GitHub, but you need to fetch it to your local machine to make changes. This is standard Git workflow for working with remote branches.

1. **Open** your terminal

2. **Navigate** to your local repository:

   ```bash
   cd path/to/your/repository
   ```

3. **Fetch** the latest branches from GitHub:

   ```bash
   git fetch origin
   ```

4. **List** remote branches to confirm:

   ```bash
   git branch -r
   ```

   You should see your new branch listed (e.g., `origin/SCRUM-4-As-a-user-I-want-to-see-a-welcome-message`)

5. **Checkout** the branch:

   ```bash
   git checkout SCRUM-4-As-a-user-I-want-to-see-a-welcome-message
   ```

   Or use the shorter form that creates a local tracking branch:

   ```bash
   git checkout -b SCRUM-4-As-a-user-I-want-to-see-a-welcome-message origin/SCRUM-4-As-a-user-I-want-to-see-a-welcome-message
   ```

> ℹ **Concept Deep Dive**
>
> **Git fetch vs pull:**
>
> - `git fetch` downloads remote changes without merging them
> - `git pull` fetches AND merges into your current branch
>
> Using `fetch` first is safer when working with new branches because it lets you see what exists before switching.
>
> **Tab completion tip:** Most terminals support tab completion for branch names. Type the first few characters and press Tab.
>
> ✓ **Quick check:** `git branch` shows your new branch with an asterisk (*) indicating it's checked out

### **Step 6:** Make Code Changes

Now implement the feature. For this exercise, make a simple change that demonstrates the workflow. In a real project, this would be the actual feature implementation.

1. **Open** a file in your repository (or create a new one)

2. **Make** a visible change. For example, edit `README.md`:

   ```markdown
   ## Welcome

   This feature was implemented as part of SCRUM-4.
   ```

   Or create a new file:

   ```bash
   echo "# Welcome Feature" > welcome.md
   ```

3. **Save** your changes

4. **Verify** your changes with git status:

   ```bash
   git status
   ```

> ℹ **Concept Deep Dive**
>
> In a real development workflow, this step would involve:
>
> - Writing application code
> - Adding or updating tests
> - Updating documentation
> - Running local tests to verify
>
> For this exercise, a simple file change is sufficient to demonstrate the integration. The important part is the commit message in the next step.
>
> ✓ **Quick check:** `git status` shows your modified or new files

### **Step 7:** Commit with Issue Key

The commit message must include the issue key for Jira to track it. This is the critical link that connects your code change to the planning artifact.

1. **Stage** your changes:

   ```bash
   git add .
   ```

2. **Commit** with the issue key at the start of the message:

   ```bash
   git commit -m "SCRUM-4 Add welcome message to homepage"
   ```

3. **Verify** the commit was created:

   ```bash
   git log --oneline -1
   ```

   You should see something like: `d3855f SCRUM-4 Add welcome message to homepage`

> ℹ **Concept Deep Dive**
>
> **Issue key placement in commit messages:**
>
> The integration recognizes issue keys anywhere in the commit message, but best practice is to put it at the beginning:
>
> ```text
> SCRUM-4 Add welcome message to homepage    ← Recommended
> Add welcome message (SCRUM-4)              ← Also works
> Fixed bug mentioned in SCRUM-4             ← Also works
> ```
>
> **Multiple issues:** You can reference multiple issues in one commit:
>
> ```text
> SCRUM-4 SCRUM-5 Implement shared component
> ```
>
> ⚠ **Common Mistakes**
>
> - Forgetting the issue key entirely (commit won't link to Jira)
> - Typo in the issue key (e.g., `SCURM-4` instead of `SCRUM-4`)
> - Wrong project key (e.g., `PROJ-4` when your project is `SCRUM`)
>
> ✓ **Quick check:** Your commit message starts with the correct issue key

### **Step 8:** Push and Verify in Jira

Push your commit to GitHub and verify that Jira automatically detects it. This demonstrates the real-time integration between the two systems.

1. **Push** your branch to GitHub:

   ```bash
   git push origin SCRUM-4-As-a-user-I-want-to-see-a-welcome-message
   ```

2. **Return** to Jira in your browser

3. **Open** your story (click on the card or find it in the backlog)

4. **Check** the Development section - you should see:
   - A commit indicator showing "1 commit"
   - The branch you created earlier

5. **Click** on the commit indicator or **Development** link to expand the details

6. **Verify** the Development panel shows:
   - **Commits tab**: Your commit with message and hash
   - **Branches tab**: Your feature branch

> ℹ **Concept Deep Dive**
>
> **How fast does Jira detect commits?**
>
> - Typically within 1-2 minutes of pushing
> - GitHub sends webhooks to Jira when repository events occur
> - If you don't see updates, wait a moment and refresh
>
> **The Development panel** aggregates all code activity:
>
> - Branches created for this issue
> - Commits referencing this issue
> - Pull requests linked to this issue
> - Build status (if CI/CD is connected)
> - Deployment status (if configured)
>
> This gives project managers visibility into development progress without needing GitHub access.
>
> ✓ **Quick check:** Jira shows your commit in the Development section

### **Step 9:** Create and Merge Pull Request

Complete the code review cycle by creating a pull request. You can initiate this from Jira or GitHub - both will be linked to the story.

1. **In Jira**, open the Development panel for your story

2. **Click** the **Branches** tab

3. **Click** **Create pull request** next to your branch

   This opens GitHub's pull request creation page.

4. **Review** the PR details on GitHub:
   - **Title**: Auto-filled with branch name (includes issue key)
   - **Base**: main
   - **Compare**: Your feature branch

5. **Add** a description if desired

6. **Click** **Create pull request**

7. **Review** the PR page - verify it shows:
   - "Able to merge" or "No conflicts"
   - Your commit listed

8. **Click** **Merge pull request**

9. **Click** **Confirm merge**

10. **Optionally** delete the branch when prompted

> ℹ **Concept Deep Dive**
>
> **PR title and issue linking:**
>
> Since your branch name contains the issue key, the PR title automatically includes it too. Jira will detect this and link the PR to the story.
>
> **In a team setting:**
>
> - Another team member would review your code before merging
> - You might need to address review comments
> - CI/CD checks might need to pass
>
> For this exercise, you're merging your own PR to complete the workflow demonstration.
>
> **After merging:**
>
> Jira will show the merge commit in addition to your feature commit, providing a complete history of the code change.
>
> ⚠ **Common Mistakes**
>
> - Merging to the wrong base branch
> - Not including the issue key in the PR title (if you create manually)
>
> ✓ **Quick check:** PR shows as "Merged" on GitHub

### **Step 10:** Move Story to Done

With the code merged, update the story status to reflect completion. This closes the development cycle and provides accurate metrics for sprint velocity.

1. **Return** to your Jira project's **Board** view

2. **Locate** your story in the **IN PROGRESS** column

3. **Drag** the card to the **DONE** column

4. **Verify** the card shows in DONE with a checkmark

5. **Click** on the card to open details

6. **Review** the Development section - you should now see:
   - Multiple commits (your feature commit + merge commit)
   - The merged PR
   - Branch may show as deleted (if you deleted it after merge)

> ℹ **Concept Deep Dive**
>
> **The complete picture:**
>
> Your Jira story now contains the full history:
>
> 1. **Story created** - The requirement was documented
> 2. **Sprint planned** - Work was scheduled
> 3. **Branch created** - Development started
> 4. **Commits made** - Code was written
> 5. **PR merged** - Code was reviewed and integrated
> 6. **Story completed** - Work was delivered
>
> This traceability is invaluable for:
>
> - Understanding why code was changed (link back to story)
> - Auditing what was delivered in each sprint
> - Debugging issues (which story introduced a change?)
> - Compliance requirements (proving work was properly tracked)
>
> ✓ **Quick check:** Story is in DONE column with commits visible in Development section

## Common Issues

> **If you encounter problems:**
>
> **"Create branch" option not visible:** Ensure your Jira-GitHub integration is fully configured. Check the Code tab to verify your organization is connected.
>
> **Branch not appearing in Jira:** Wait 1-2 minutes for sync. If still missing, verify you created it from the correct repository.
>
> **Commit not linking to story:** Check that the issue key in your commit message exactly matches the story (correct project key and number).
>
> **"Not authorized" when creating branch:** Your GitHub account may not have write access to the repository. Verify your permissions.
>
> **PR not showing in Jira:** Ensure the PR title or branch name contains the issue key. PRs without issue keys won't link automatically.
>
> **Still stuck?** Check the Development panel's "Other links" tab for troubleshooting options, or verify the integration status in Jira's Apps settings.

## Summary

You've successfully completed a full development cycle which:

- ✓ Created a user story to track the work
- ✓ Used Jira to create a properly-named branch on GitHub
- ✓ Made code changes and committed with issue key linking
- ✓ Verified real-time synchronization between Jira and GitHub
- ✓ Completed the code review cycle with a pull request
- ✓ Maintained full traceability from requirement to code

> **Key takeaway:** The Jira-GitHub integration transforms issue tracking from a separate administrative task into an integrated part of the development workflow. By including issue keys in branches, commits, and PRs, every code change automatically links back to its business justification. This creates the traceability that professional teams need for project management, compliance, and debugging.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Try using "smart commits" to transition issues: `SCRUM-4 #done Fixed the bug`
> - Set up automation rules in Jira to auto-transition stories when PRs are merged
> - Explore the Code tab to see aggregated development activity across all stories
> - Configure deployment tracking to see which environments have which code
> - Use Jira's built-in reports to analyze development metrics across sprints

## Done! 🎉

You've mastered the integrated development workflow. From now on, every feature you build can be tracked from the initial user story through to the deployed code. This is the same workflow used by professional software teams worldwide to maintain visibility, accountability, and traceability in their development process.
