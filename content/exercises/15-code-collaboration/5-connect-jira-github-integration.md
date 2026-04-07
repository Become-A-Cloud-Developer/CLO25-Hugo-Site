+++
title = "Connect Jira to GitHub"
program = "CLO"
cohort = "25"
courses = ["BCD"]
description = "Connect your Jira project to GitHub to track commits, branches, and pull requests against Jira issues"
weight = 5
+++

# Connect Jira to GitHub

## Goal

Connect your Jira project to GitHub to enable automatic tracking of commits, branches, and pull requests against Jira issues.

> **What you'll learn:**
>
> - How to install the GitHub for Atlassian integration
> - How to authorize Jira to access your GitHub organization
> - How Jira issue keys link code to project management
> - The complete development lifecycle from planning to code tracking

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ A Jira workspace with a Scrum project at `yourworkspace.atlassian.net`
> - ✓ A GitHub account with at least one organization where you have **owner** permissions
> - ✓ Web browser with access to both Jira and GitHub

To verify your setup:

1. Log in to Jira and confirm you can access your project's settings
2. Log in to GitHub and confirm you see at least one organization in your profile

## Exercise Steps

### Overview

1. **Navigate to the Code Tab**
2. **Install GitHub for Atlassian App**
3. **Configure the Integration**
4. **Select GitHub Cloud**
5. **Authorize GitHub Access**
6. **Connect Your GitHub Organization**
7. **Verify the Connection**

### **Step 1:** Navigate to the Code Tab

Before you can link code to Jira issues, you need to set up the integration between the two platforms. Jira provides a dedicated Code tab in each project where you can connect your version control system.

1. **Open** your Jira project in your browser

2. **Locate** the project navigation tabs at the top of the page (Summary, Timeline, Backlog, Board, etc.)

3. **Click** on the **Code** tab

4. **Observe** the "Connect your code to Jira" page with options for GitHub, GitLab, and Bitbucket

> ℹ **Concept Deep Dive**
>
> The Code tab is where Jira displays all development activity linked to your project. Once connected, you'll see:
>
> - Commits that reference Jira issue keys
> - Branches created for specific issues
> - Pull requests linked to your work items
>
> This creates a complete audit trail from "why we built this" (the Jira issue) to "how we built it" (the code changes).
>
> ✓ **Quick check:** You see the "Connect your code to Jira" page with the GitHub option

### **Step 2:** Install GitHub for Atlassian App

Jira connects to GitHub through an official integration app from the Atlassian Marketplace. This app is free and maintained by Atlassian, ensuring reliable synchronization between the platforms.

1. **Click** the **Connect GitHub** button

2. **Review** the "Connect GitHub to Atlassian" page that appears

3. **Verify** the prerequisites shown:
   - A GitHub account
   - Owner permission for a GitHub organization

4. **Click** **Continue** to proceed with the installation

5. **Select** your Atlassian site from the dropdown (e.g., `yourworkspace.atlassian.net`)

6. **Review** the app details:
   - **Name:** GitHub for Atlassian
   - **Publisher:** Atlassian
   - **Price:** Free

7. **Click** **Get it now** to install the app

8. **Wait** for the installation to complete

9. **Observe** the success message: "GitHub for Atlassian is now installed"

10. **Click** **Configure app** to continue setup

> ℹ **Concept Deep Dive**
>
> The GitHub for Atlassian app installs at the site level, not the project level. This means:
>
> - One installation covers all projects in your Jira site
> - Any project can connect to any authorized GitHub repository
> - Site administrators control which organizations are connected
>
> The app uses OAuth for secure authentication, meaning Jira never stores your GitHub password.
>
> ⚠ **Common Mistakes**
>
> - Clicking "Maybe later" instead of "Configure app" leaves the setup incomplete
> - If you're not a site administrator, you may need to request installation from your admin
>
> ✓ **Quick check:** You see "GitHub for Atlassian is now installed" and have clicked Configure app

### **Step 3:** Configure the Integration

With the app installed, you now need to connect it to your GitHub account. This step determines which GitHub product you're using and initiates the authorization flow.

1. **Observe** the "Connect GitHub to Atlassian" configuration page

2. **Review** the "Select your GitHub product" options:
   - **GitHub Cloud** - The standard github.com service
   - **GitHub Enterprise Server** - Self-hosted GitHub for enterprises

3. **Prepare** to select GitHub Cloud (the default for most users)

> ℹ **Concept Deep Dive**
>
> GitHub offers two main products:
>
> | Product | URL | Use Case |
> |---------|-----|----------|
> | **GitHub Cloud** | github.com | Personal accounts, small teams, most organizations |
> | **GitHub Enterprise Server** | your-company.github.com | Large enterprises with on-premises requirements |
>
> Unless your organization specifically uses GitHub Enterprise Server, you'll use GitHub Cloud.
>
> ✓ **Quick check:** You see the product selection screen with GitHub Cloud and Enterprise Server options

### **Step 4:** Select GitHub Cloud

Most developers and teams use GitHub Cloud (github.com). This is the standard choice unless your organization has specifically deployed GitHub Enterprise Server on their own infrastructure.

1. **Ensure** that **GitHub Cloud** is selected (it's usually the default)

2. **Click** **Next** to proceed

3. **Observe** that you're redirected to GitHub's login page

> ℹ **Concept Deep Dive**
>
> Selecting GitHub Cloud tells the integration to connect to github.com using GitHub's public OAuth service. The integration will:
>
> 1. Redirect you to GitHub for authentication
> 2. Ask for permission to access your organizations
> 3. Return you to Jira with an access token
>
> This token allows Jira to read your repositories and watch for activity that mentions Jira issue keys.
>
> ✓ **Quick check:** You're now on GitHub's login or authorization page

### **Step 5:** Authorize GitHub Access

GitHub needs to verify your identity and get your permission before sharing any data with Jira. This OAuth authorization flow is a secure, standard way for applications to connect without sharing passwords. During this step, you'll also choose which repositories Jira can access.

1. **Sign in** to GitHub if prompted (you may already be signed in)

2. **Review** the authorization request from Atlassian

3. **Observe** what permissions Atlassian is requesting:
   - Read access to your organization membership
   - Access to repositories you authorize

4. **Select repository access** when prompted:
   - **Choose "All repositories"** to grant Jira access to all current and future repositories
   - This ensures all your team's code activity can be tracked in Jira

5. **Click** **Authorize** or **Install** to grant permission

6. **Wait** for the redirect back to Jira

> ℹ **Concept Deep Dive**
>
> **Repository Access Options**
>
> GitHub presents two choices for repository access:
>
> | Option | What it means |
> |--------|---------------|
> | **All repositories** | Jira can see all repos now and any created in the future |
> | **Only select repositories** | Jira can only see the specific repos you choose |
>
> For this course, select **All repositories** because:
>
> - You won't need to reconfigure when creating new project repos
> - All team members' commits will be tracked automatically
> - It simplifies project setup
>
> You can change this setting later in GitHub under Settings → Applications → Installed GitHub Apps.
>
> **OAuth Security**
>
> OAuth authorization is a three-party handshake:
>
> 1. **Jira** asks GitHub: "Can this user connect their account?"
> 2. **GitHub** asks you: "Do you authorize Jira to access your data?"
> 3. **You** approve, and GitHub gives Jira a limited-access token
>
> This token never contains your password and can be revoked at any time from GitHub settings.
>
> You can review and revoke authorized apps at: `<https://github.com/settings/applications>`
>
> ⚠ **Common Mistakes**
>
> - Selecting "Only select repositories" and forgetting to add new repos later
> - Denying the authorization prevents the integration from working
> - Using a different GitHub account than intended connects the wrong repositories
>
> ✓ **Quick check:** You selected "All repositories" and clicked Authorize

### **Step 6:** Connect Your GitHub Organization

After authorization, you'll select which GitHub organization to connect to Jira. This determines which repositories can be linked to your Jira project.

1. **Review** the list of organizations shown

2. **Locate** the organization you want to connect (this should be an organization where you have owner permissions)

3. **Click** **Connect** next to your chosen organization

4. **Wait** for the connection to be established

5. **Observe** the success message: "[Your account] is now connected!"

6. **Note** the important message about issue keys:

   > "Your team needs to add issue keys in GitHub"
   > To import development work into Jira and track it in your issues, add issue keys to branches, pull request titles, and commit messages.

7. **Click** **Exit set up** or **Add Teamwork Graph connector** (the connector is optional for this exercise)

> ℹ **Concept Deep Dive**
>
> **Organization Ownership Matters**
>
> You can only connect organizations where you have owner permissions. If you see a message like "You're not an owner for this organization," you have two options:
>
> 1. Ask the organization owner to connect it
> 2. Use a different organization where you are the owner
>
> **Issue Keys Are the Link**
>
> The integration works by scanning for Jira issue keys in your Git activity:
>
> - Branch names: `feature/PROJ-123-add-login`
> - Commit messages: `PROJ-123 Add user authentication`
> - Pull request titles: `[PROJ-123] Implement login form`
>
> Where `PROJ` is your project key and `123` is the issue number. Jira automatically finds these references and links them to the corresponding issues.
>
> ⚠ **Common Mistakes**
>
> - Connecting a personal account instead of an organization limits team collaboration
> - Forgetting to include issue keys in commits means Jira won't track the work
>
> ✓ **Quick check:** You see the success message showing your account is connected

### **Step 7:** Verify the Connection

With the integration complete, you should verify that everything is working correctly and understand how to use the connection going forward.

1. **Navigate to** the GitHub configuration page (you may be redirected automatically, or access it via Apps in Jira settings)

2. **Review** the connection status:
   - **Connected organization** - Your GitHub organization name
   - **Repository access** - "All repos" or a specific count
   - **Organization sync status** - Should show "FINISHED"
   - **Backfill status** - May show "IN PROGRESS" initially
   - **Permissions** - "FULL ACCESS" or configured level

3. **Return** to your Jira project's **Code** tab

4. **Verify** the Code tab now shows your connected GitHub organization

5. **Understand** how to use the integration going forward:

   To link a commit to a Jira issue, include the issue key in your commit message:

   ```bash
   git commit -m "SCRUM-1 Add homepage welcome message"
   ```

   To link a branch to a Jira issue, include the issue key in the branch name:

   ```bash
   git checkout -b feature/SCRUM-1-welcome-message
   ```

   To link a pull request, include the issue key in the PR title:

   ```text
   [SCRUM-1] Add homepage welcome message
   ```

> ℹ **Concept Deep Dive**
>
> **The Backfill Process**
>
> When you first connect, Jira performs a "backfill" - scanning your existing repositories for any commits, branches, or PRs that contain issue keys. This can take time for large repositories. New activity is tracked in real-time after the initial sync.
>
> **Repository Access Settings**
>
> You can configure which repositories Jira can access:
>
> - **All repos** - Jira watches all repositories in the organization
> - **Selected repos** - Jira only watches specific repositories you choose
>
> For team projects, "All repos" is usually fine. For organizations with sensitive repositories, you might want to restrict access.
>
> **The Development Panel**
>
> Once connected, each Jira issue gets a "Development" section showing:
>
> - Linked commits with messages and authors
> - Related branches
> - Associated pull requests with status (open, merged, closed)
>
> This closes the loop between planning (the issue) and execution (the code).
>
> ✓ **Quick check:** The GitHub configuration shows your organization as connected with "FINISHED" sync status

## Common Issues

> **If you encounter problems:**
>
> **"You're not an owner for this organization":** You need owner-level permissions in the GitHub organization. Either ask an existing owner to connect it, or use an organization where you have owner access.
>
> **Authorization fails or times out:** Try clearing your browser cache, or use an incognito window to start fresh. Ensure you're logged into the correct GitHub account.
>
> **Backfill status stuck on "IN PROGRESS":** Large repositories take longer to scan. Wait up to an hour for initial sync to complete. You can still use the integration while backfill is running.
>
> **Commits not appearing in Jira:** Verify you're using the exact issue key format (PROJECT-NUMBER). The key is case-insensitive but must match your project key exactly.
>
> **Can't find the Code tab:** The Code tab appears in Jira Software projects. If you're using Jira Work Management or a different product, the tab may not be available.
>
> **Still stuck?** Check the Atlassian documentation at `<https://support.atlassian.com/jira-cloud-administration/docs/integrate-with-github/>`

## Summary

You've successfully connected Jira to GitHub which:

- ✓ Installed the GitHub for Atlassian integration
- ✓ Authorized secure OAuth access between platforms
- ✓ Connected your GitHub organization to your Jira site
- ✓ Enabled automatic tracking of commits, branches, and PRs

> **Key takeaway:** The Jira-GitHub integration closes the development feedback loop. When a developer includes an issue key like `PROJ-123` in their commit message, branch name, or pull request, Jira automatically links that code activity to the planning artifact. This creates full traceability from "why we're building this" to "how we built it" - essential for project management, compliance, and team coordination.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Create a test commit with an issue key and verify it appears in the Jira issue's Development section
> - Explore the "smart commits" feature to transition issues directly from commit messages (e.g., `PROJ-123 #done Fixed the bug`)
> - Set up branch naming conventions for your team that always include issue keys
> - Configure automation rules in Jira to move issues when PRs are merged
> - Explore the Teamwork Graph connector for additional team insights

## Done! 🎉

You've connected Jira to GitHub. Your development workflow now has full visibility: user stories in Jira are automatically linked to the branches, commits, and pull requests that implement them. This integration is used by professional software teams to maintain traceability and keep project managers informed of development progress without requiring manual status updates.
