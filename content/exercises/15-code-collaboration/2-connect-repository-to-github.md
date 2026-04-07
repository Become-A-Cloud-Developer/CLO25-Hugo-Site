+++
title = "Connect Your Repository to GitHub"
program = "CLO"
cohort = "25"
courses = ["BCD"]
description = "Push your local repository to GitHub for backup and collaboration"
weight = 2
+++

# Connect Your Repository to GitHub

## Goal

Push your local Git repository to GitHub so your code is backed up in the cloud and ready for collaboration with others.

> **What you'll learn:**
>
> - The difference between local and remote repositories
> - How to create a GitHub repository from the command line
> - How to push commits to GitHub
> - The complete local-to-remote Git workflow

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ A local Git repository with at least one commit
> - ✓ A GitHub account
> - ✓ GitHub CLI (`gh`) installed and authenticated

To verify GitHub CLI is installed and authenticated:

```bash
gh auth status
```

You should see output indicating you're logged in. If not, run:

```bash
gh auth login
```

Follow the prompts to authenticate with your GitHub account.

## Exercise Steps

### Overview

1. **Understand Local vs Remote Repositories**
2. **Create GitHub Repository and Push**
3. **Verify on GitHub**
4. **Make a Change and Push Again**

### **Step 1:** Understand Local vs Remote Repositories

So far, your Git repository exists only on your computer. A **remote repository** is a copy hosted on a server like GitHub that enables:

- **Backup** - Your code is safe even if your computer fails
- **Collaboration** - Others can access, clone, and contribute to your project
- **Deployment** - Many hosting services deploy directly from GitHub
- **Portfolio** - Showcase your work to potential employers

> ℹ **Concept Deep Dive**
>
> In Git terminology:
>
> - **Local** - The repository on your machine
> - **Remote** - A repository hosted elsewhere (GitHub, GitLab, etc.)
> - **Origin** - The conventional name for your primary remote repository
> - **Push** - Send commits from local to remote
> - **Pull** - Get commits from remote to local
>
> A typical workflow:
>
> ```
> Local: edit → add → commit → push → Remote (GitHub)
> ```
>
> ✓ **Quick check:** You understand that "remote" means a copy of your repository on GitHub's servers

### **Step 2:** Create GitHub Repository and Push

The GitHub CLI can create a repository and push your code in a single command. This is the simplest way to get started.

1. **Navigate to** your project directory:

   ```bash
   cd ~/your-workspace-directory/HelloMvc
   ```

2. **Create the repository and push:**

   ```bash
   gh repo create HelloMvc --public --source=. --push
   ```

   You should see output like:

   ```text
   ✓ Created repository yourusername/HelloMvc on GitHub
   ✓ Added remote https://github.com/yourusername/HelloMvc.git
   ✓ Pushed commits to https://github.com/yourusername/HelloMvc.git
   ```

> ℹ **Concept Deep Dive**
>
> This single command does three things:
>
> 1. Creates a new repository called `HelloMvc` on your GitHub account
> 2. Adds that repository as a remote named `origin`
> 3. Pushes all your commits to GitHub
>
> The flags:
>
> - `--public` makes the repository visible to everyone (use `--private` for private repos)
> - `--source=.` tells `gh` to use the current directory as the source
> - `--push` pushes the commits immediately after creating the repo
>
> ⚠ **Common Mistakes**
>
> - Running the command outside your project directory
> - Using a repository name that already exists on your GitHub account
> - Not being authenticated with `gh auth login`
>
> ✓ **Quick check:** The command completes successfully with three checkmarks

### **Step 3:** Verify on GitHub

Confirm that your code is now on GitHub by viewing it in a browser.

1. **Open** the repository in your browser:

   ```bash
   gh repo view --web
   ```

   This opens your repository on GitHub in your default browser.

2. **Verify** you see:

   - Your project files (`Program.cs`, `HelloMvc.csproj`, `.gitignore`)
   - Your commit history (click "commits" or the commit count)
   - The README prompt (GitHub suggests adding a README)

3. **Browse** the commit history:

   - Click on a commit to see what changed
   - Notice the commit messages you wrote earlier

> ✓ **Quick check:** Your files and commit history are visible on GitHub

### **Step 4:** Make a Change and Push Again

Practice the full workflow by making a local change, committing it, and pushing to GitHub.

1. **Create** a README file for your project:

   > `README.md`

   ```markdown
   # Hello MVC

   A minimal ASP.NET Core MVC application demonstrating the basics of .NET web development.

   ## Setup

   1. Restore dependencies: `dotnet restore`
   2. Build the project: `dotnet build`
   3. Run the app: `dotnet run`

   ## Usage

   Open http://localhost:5000 in your browser (or the URL shown in the terminal).
   ```

2. **Stage and commit** the new file:

   ```bash
   git add README.md
   git commit -m "Add README with setup instructions"
   ```

3. **Push** to GitHub:

   ```bash
   git push
   ```

   You should see output indicating the push succeeded:

   ```text
   Enumerating objects: 4, done.
   Counting objects: 100% (4/4), done.
   Delta compression using up to 8 threads
   Compressing objects: 100% (3/3), done.
   Writing objects: 100% (3/3), 456 bytes | 456.00 KiB/s, done.
   Total 3 (delta 0), reused 0 (delta 0)
   To https://github.com/yourusername/HelloMvc.git
      abc1234..def5678  main -> main
   ```

4. **Refresh** the GitHub page in your browser

5. **Verify** the README is now displayed on your repository's main page

> ℹ **Concept Deep Dive**
>
> The workflow you just completed:
>
> 1. **Edit** - Create or modify files
> 2. **Add** - Stage changes with `git add`
> 3. **Commit** - Save changes locally with `git commit`
> 4. **Push** - Send changes to GitHub with `git push`
>
> This is the core Git workflow you'll use for every change going forward.
>
> ✓ **Quick check:** README appears on your GitHub repository page

## Common Issues

> **If you encounter problems:**
>
> **"gh: command not found":** GitHub CLI is not installed. Install it from [cli.github.com](https://cli.github.com/)
>
> **"authentication required" or "not logged in":** Run `gh auth login` and follow the prompts to authenticate
>
> **"repository already exists":** A repository with that name already exists on your GitHub account. Either delete it on GitHub or choose a different name: `gh repo create different-name --public --source=. --push`
>
> **"error: src refspec main does not match any":** You have no commits yet. Create a commit first: `git add . && git commit -m "Initial commit"`
>
> **Branch is "master" instead of "main":** Older Git versions default to `master`. Rename it with: `git branch -M main`
>
> **"failed to push some refs":** The remote has changes you don't have locally. For a new repository this shouldn't happen, but if it does, run: `git pull --rebase origin main` then try pushing again
>
> **Still stuck?** Run `gh auth status` to verify authentication, and `git remote -v` to verify the remote is configured

## Summary

You've successfully connected your local repository to GitHub which:

- ✓ Backs up your code to GitHub's servers
- ✓ Makes your project accessible from anywhere
- ✓ Prepares your repository for collaboration with others

> **Key takeaway:** The complete Git workflow is `edit → add → commit → push`. Once your repository is on GitHub, this cycle repeats for every change you make. Your code is now version-controlled locally and backed up remotely.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Try cloning your repository to a different location: `git clone https://github.com/USERNAME/HelloMvc.git`
> - Learn about `git pull` to download changes from GitHub to your local machine
> - Explore GitHub features like Issues, Pull Requests, and Actions
> - Research branching strategies for working on features without affecting the main code
> - Set up SSH authentication for passwordless pushing: [GitHub SSH docs](https://docs.github.com/en/authentication/connecting-to-github-with-ssh)

### Alternative Methods for Connecting to GitHub

The GitHub CLI (`gh`) is the recommended approach, but there are other ways to connect a repository to GitHub.

**VS Code with GitHub Extension:**

1. Open the Source Control panel (Ctrl+Shift+G or Cmd+Shift+G)
2. Click "Publish to GitHub"
3. Choose public or private
4. VS Code creates the repository and pushes automatically

**Firebase Studio (Project IDX):**

Firebase Studio includes Git integration in its Source Control panel. Use the integrated terminal to run the `gh repo create` command as shown in the main instructions. Alternatively, create the repository on github.com first, then connect it manually.

**Manual Approach:**

If you prefer to create the repository on GitHub's website:

1. Go to [github.com/new](https://github.com/new)
2. Create an empty repository (don't initialize with README)
3. Copy the repository URL
4. Add the remote and push:

   ```bash
   git remote add origin https://github.com/USERNAME/HelloMvc.git
   git push -u origin main
   ```

The `-u` flag sets up tracking so future `git push` commands know where to push.

## Done! 🎉

You've established a complete Git workflow from local development to GitHub. Your project is now version-controlled, backed up, and ready for collaboration. This foundation - local commits pushed to a remote repository - is how professional developers work on every project.
