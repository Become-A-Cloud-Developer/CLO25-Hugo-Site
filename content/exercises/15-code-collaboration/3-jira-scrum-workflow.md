+++
title = "Jira Scrum Workflow"
program = "CLO"
cohort = "25"
courses = ["BCD"]
description = "Practice the complete Scrum workflow in Jira: create a user story, estimate, sprint, and deliver"
weight = 3
+++

# Jira Scrum Workflow

## Goal

Practice the complete Scrum workflow in Jira by creating a user story, estimating it, running a sprint, and completing the delivery cycle.

> **What you'll learn:**
>
> - How to create and structure user stories in Jira
> - What story points are and how to estimate work
> - The sprint lifecycle from planning to completion
> - How to use the Scrum board to track progress

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ A Jira account with a Scrum project (from the Jira Setup tutorial)
> - ✓ Access to your Jira workspace at `yourworkspace.atlassian.net`
> - ✓ Web browser

To verify your setup, log in to Jira and confirm you can see your Scrum board with Backlog and Board tabs.

## Exercise Steps

### Overview

1. **Create a User Story**
2. **Estimate the Story with Story Points**
3. **Create and Plan a Sprint**
4. **Start the Sprint**
5. **Move the Story Through the Board**
6. **Complete the Sprint**
7. **Review Sprint Completion Data**

### **Step 1:** Create a User Story

A user story describes a feature from the end user's perspective. It helps teams focus on delivering value rather than just completing tasks. You'll create a user story for a simple feature that you might build in your project.

1. **Navigate to** the **Backlog** tab in the left sidebar

2. **Click** the **+ Create work item** button at the bottom of the backlog

3. **Select** "Story" as the work item type

4. **Enter** a title using the user story format:

   ```text
   As a visitor, I can see a welcome message on the homepage
   ```

5. **Press Enter** or click **Create** to add the story

6. **Verify** the story appears in your Backlog section

> ℹ **Concept Deep Dive**
>
> The user story format follows a pattern:
>
> **As a [type of user], I want [goal] so that [benefit]**
>
> This format keeps the focus on:
>
> - **Who** wants the feature (the user role)
> - **What** they want to do (the goal)
> - **Why** it matters (the business value)
>
> In your project, you'll write stories from the perspective of your application's users. For a contact form app, you might write: "As a website visitor, I can submit a contact form so that I can reach the company."
>
> ⚠ **Common Mistakes**
>
> - Writing technical tasks instead of user-focused stories ("Add database table" vs "User can save their profile")
> - Making stories too large to complete in a sprint
>
> ✓ **Quick check:** Your story appears in the Backlog and describes value from a user's perspective

### **Step 2:** Estimate the Story with Story Points

Story points measure the relative effort, complexity, and uncertainty of a task. Teams use them to plan how much work fits in a sprint without getting stuck on exact time estimates.

1. **Click** on your story in the Backlog to open its details panel

2. **Locate** the **Story point estimate** field in the details panel (usually on the right side)

3. **Enter** a story point value:

   ```text
   3
   ```

4. **Press Enter** or click away to save the estimate

5. **Verify** the story point value appears next to your story in the Backlog

> ℹ **Concept Deep Dive**
>
> Story points use a relative scale, often based on the Fibonacci sequence: 1, 2, 3, 5, 8, 13, 21...
>
> - **1 point** - Trivial task, minimal complexity
> - **3 points** - Small task with some complexity
> - **5 points** - Medium task, typical feature
> - **8 points** - Large task, consider splitting
> - **13+ points** - Too large, definitely split
>
> The key insight is that humans are better at comparing sizes than estimating exact effort. "Is this task bigger or smaller than that one?" is easier to answer than "How many hours will this take?"
>
> Teams calibrate their scale by picking a well-understood task as a reference point. All other estimates are relative to that baseline.
>
> ⚠ **Common Mistakes**
>
> - Treating story points as hours (they're not)
> - Spending too much time debating exact estimates (rough consensus is fine)
>
> ✓ **Quick check:** The story shows "3" (or your chosen value) as its estimate

### **Step 3:** Create and Plan a Sprint

A sprint is a fixed time period (usually 1-2 weeks) where the team commits to completing a set of stories. You'll create a sprint and add your story to it.

1. **Stay in** the Backlog view

2. **Click** the **Create sprint** button (above your backlog items)

3. **Observe** that a new sprint section appears above the Backlog

4. **Drag** your user story from the Backlog into the new sprint section

5. **Optionally rename** the sprint by clicking on its name:

   ```text
   Sprint 1: Welcome Feature
   ```

> ℹ **Concept Deep Dive**
>
> Sprint planning involves deciding:
>
> - **Sprint goal** - What the team wants to achieve
> - **Sprint backlog** - Which stories to include
> - **Capacity** - How much work the team can handle
>
> The total story points in a sprint should match the team's historical velocity (how many points they typically complete). For a new team, start conservatively and adjust based on experience.
>
> In this exercise, you have one story. In real projects, a sprint typically contains 5-15 stories depending on team size and sprint length.
>
> ✓ **Quick check:** Your story is in the sprint section, not the Backlog section

### **Step 4:** Start the Sprint

Starting a sprint activates the time box and moves the work to the active board. This is a commitment: the team agrees to focus on completing the sprint backlog during the sprint duration.

1. **Click** the **Start sprint** button in the sprint section

2. **Review** the sprint configuration dialog:
   - **Sprint name** - Keep or modify
   - **Duration** - Select 1 week (or custom)
   - **Start date** - Today's date
   - **End date** - Calculated from duration

3. **Click** **Start** to begin the sprint

4. **Observe** that Jira navigates to the Board view automatically

5. **Verify** your story appears in the **TO DO** column

> ℹ **Concept Deep Dive**
>
> Once a sprint starts:
>
> - The sprint backlog is "locked" (changes should be avoided)
> - The team focuses on completing committed work
> - Daily standups track progress and blockers
> - The sprint end date is fixed
>
> Jira enforces some of these rules by default. You can still add or remove items from an active sprint, but the practice is discouraged in Scrum methodology.
>
> ⚠ **Common Mistakes**
>
> - Starting a sprint without a clear sprint goal
> - Overcommitting (putting too many points in the sprint)
>
> ✓ **Quick check:** You see the Board view with your story in the TO DO column

### **Step 5:** Move the Story Through the Board

The Scrum board visualizes work flowing through stages. As you work on a story, you move it across the board to reflect its current status. This provides transparency to the whole team.

1. **Locate** your story in the **TO DO** column

2. **Drag** the story card to the **IN PROGRESS** column

   This simulates starting work on the feature.

3. **Observe** the card is now in the IN PROGRESS column

4. **Drag** the story card to the **DONE** column

   This simulates completing the feature.

5. **Verify** the story shows as complete (it may change color or show a checkmark)

> ℹ **Concept Deep Dive**
>
> The default Scrum board has three columns:
>
> | Column | Meaning |
> |--------|---------|
> | **TO DO** | Work not yet started |
> | **IN PROGRESS** | Work actively being done |
> | **DONE** | Work completed and verified |
>
> In real projects, teams often customize their boards with additional columns like "In Review" or "Testing" to match their workflow.
>
> Moving cards during daily standups helps teams discuss:
>
> - What did I complete yesterday?
> - What will I work on today?
> - Are there any blockers?
>
> ✓ **Quick check:** Your story is in the DONE column

### **Step 6:** Complete the Sprint

When the sprint time box ends (or all work is done), you complete the sprint. This triggers Jira to generate reports and prompts you to handle any incomplete work.

1. **Click** the **Complete sprint** button (usually in the top-right of the Board view)

2. **Review** the completion dialog:
   - Jira shows how many issues were completed
   - If there were incomplete issues, you'd choose where to move them (Backlog or next sprint)

3. **Click** **Complete** to finish the sprint

4. **Observe** that Jira confirms the sprint is complete and may automatically create a new sprint for future planning

> ℹ **Concept Deep Dive**
>
> Sprint completion is a key moment in Scrum:
>
> - **Sprint Review** - Demonstrate completed work to stakeholders
> - **Sprint Retrospective** - Discuss what went well and what to improve
> - **Data Review** - Use completion metrics to inform the retrospective
>
> After completion, Jira tracks:
>
> - How many items were committed vs completed
> - Any scope changes during the sprint
> - Velocity trends over multiple sprints
>
> Teams use this data to improve their estimation and planning over time.
>
> ⚠ **Common Mistakes**
>
> - Marking incomplete work as "done" just to finish the sprint
> - Skipping the retrospective discussion
>
> ✓ **Quick check:** The sprint is marked as complete and you see the completion confirmation

### **Step 7:** Review Sprint Completion Data

After completing a sprint, Jira provides data to inform your team's retrospective discussion. In the current Jira Cloud interface, sprint metrics are integrated into the project Summary rather than a separate Sprint Report page.

1. **Navigate to** the **Summary** tab in the left sidebar

2. **Review** the sprint completion data:
   - Look for completed work items count
   - Note the status distribution across your project
   - Observe any velocity or progress indicators

3. **Alternatively**, explore other reporting options:
   - **Timeline** - See work items on a calendar view
   - **List** - View all items with their status and details
   - **Insights** (if available) - Project-level analytics

4. **Consider** these retrospective questions:
   - Was the sprint goal achieved?
   - Were the estimates accurate?
   - What could improve next sprint?

> ℹ **Concept Deep Dive**
>
> The Sprint Retrospective typically follows a format like:
>
> - **What went well?** - Keep doing these things
> - **What didn't go well?** - Address these issues
> - **What will we try next sprint?** - Concrete action items
>
> Jira Cloud's reporting features vary by plan and configuration. Some workspaces have dedicated Sprint Reports with burndown charts, while others integrate metrics into the Summary dashboard.
>
> Teams often conduct retrospectives using:
>
> - Confluence pages or whiteboards (linked from Jira)
> - Dedicated retrospective tools (Jira Marketplace has several)
> - Physical sticky notes (for co-located teams)
> - Simple documents or shared notes
>
> The key is using actual data (what was completed, what wasn't) to ground the discussion in facts rather than feelings.
>
> ✓ **Quick check:** You can see that your sprint was completed and the work item is marked as done

## Common Issues

> **If you encounter problems:**
>
> **Story point field not visible:** Click on a story, then look for "Story point estimate" in the details panel. If missing, the estimation feature may need to be enabled in project settings.
>
> **Can't find the Create sprint button:** Make sure you're in the Backlog view, not the Board view. The Create sprint button appears above the backlog items.
>
> **Sprint won't complete:** You need the "Manage Sprints" permission. If you're the project creator, you should have this by default.
>
> **Board shows no columns:** Your project may have been set up with Kanban instead of Scrum. Create a new project and select the Scrum template.
>
> **Still stuck?** Check that you're using the correct project (Scrum template) at `yourworkspace.atlassian.net`

## Summary

You've successfully completed a full Scrum cycle in Jira which:

- ✓ Created a user story following best practices
- ✓ Estimated work using story points
- ✓ Planned and executed a sprint
- ✓ Tracked progress on the Scrum board
- ✓ Completed the sprint and reviewed the report

> **Key takeaway:** Scrum provides a structured rhythm for delivering work: plan the sprint, work the sprint, review the sprint, improve, repeat. Jira implements this cycle with its Backlog, Board, and Reports features. The real value comes not from the tool, but from the team practices: regular planning, daily coordination, and continuous improvement through retrospectives.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Create multiple stories and practice prioritizing them by dragging in the Backlog
> - Add a second team member to your Jira workspace and assign stories to them
> - Explore the Velocity Chart in Reports to see how much work your team completes per sprint
> - Install a retrospective app from the Atlassian Marketplace for structured retros
> - Create epics to group related stories together

## Done! 🎉

You've mastered the Jira Scrum workflow. You can now create user stories, estimate work, run sprints, and use the board to track progress. This is the same workflow that professional software teams use to deliver products iteratively.
