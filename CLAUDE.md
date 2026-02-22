# Cloud Developers CLO25 - Hugo Documentation Site

## Project Overview

This repository contains the Hugo-based documentation site for the Cloud Developers CLO25 course. The site is deployed to GitHub Pages at <https://cloud-dev-25.educ8.se/>.

**Purpose:** Public-facing course documentation, exercises, tutorials, and presentations for the CLO25 Cloud Developers course.

**Technology Stack:**
- Static site generator: Hugo Extended (v0.128.0+)
- Theme: DocDock (with compatibility patches for modern Hugo)
- Deployment: GitHub Actions → GitHub Pages

## Reference Project

**Location:** `/Users/lasse/Developer/IPL_Development/IPL25-Hugo-Site`

**Important:** This is a READ-ONLY reference. Never modify files in this directory.

The IPL25 project serves as the template for this CLO25 site. It contains:
- Complete Hugo site structure
- DocDock theme configuration
- Layout overrides for Hugo 0.128+ compatibility
- GitHub Actions deployment workflow

## Course Taxonomy

**Program:** CLO (Cloud Developers)
**Current Cohort:** 25

| Tag | Full Name |
|-----|-----------|
| BCD | Basic Cloud Development |

*Note: BCD is the first of 4 courses in the Cloud Developers program.*

### Frontmatter Format

All content files include course taxonomy fields:

```toml
program = "CLO"
cohort = "25"
courses = ["BCD"]
```

## Hugo Site Structure

```
CLO25-Hugo-Site/
├── .git/                           # Git repository
├── .gitignore                      # Hugo + dev ignores
├── .gitmodules                     # DocDock theme reference
├── hugo.toml                       # Hugo configuration
├── CLAUDE.md                       # This file
├── archetypes/
│   └── default.md                  # Content archetype
├── content/
│   └── _index.md                   # Homepage
├── layouts/
│   ├── partials/
│   │   ├── custom-head.html        # SEO meta tags
│   │   ├── header.html             # Header fixes
│   │   ├── pagination.html         # Pagination fix
│   │   └── flex/
│   │       ├── body-aftercontent.html
│   │       └── scripts.html
│   └── _default/_markup/
│       └── render-codeblock-mermaid.html
├── static/
│   ├── CNAME                       # Custom domain
│   └── robots.txt                  # Search directives
├── themes/
│   └── docdock/                    # Git submodule
└── .github/
    └── workflows/
        └── hugo.yaml               # Deployment workflow
```

## Theme Overrides

The DocDock theme (2018) requires compatibility patches for Hugo 0.128+. All patches are in `layouts/partials/` as overrides - never edit the theme directly.

**Override Files:**
1. **custom-head.html** - robots meta (`noindex,nofollow`)
2. **header.html** - Header compatibility, nil pointer fixes
3. **pagination.html** - Hugo v0.148+ Pager API fix
4. **flex/body-aftercontent.html** - Page layout fixes
5. **flex/scripts.html** - Mermaid diagram support, menu collapse prevention

## Building and Testing

```bash
# Local development
hugo server

# Production build
hugo --gc --minify

# Site available at http://localhost:1313
```

## Deployment

Automatic deployment via GitHub Actions when pushing to `main` branch.

**GitHub Pages Settings:**
- Source: GitHub Actions
- Custom domain: cloud-dev-25.educ8.se

## Sensitive Data

**NEVER commit or push anything in `docs/student-reports/`.** This directory contains student personal data and is gitignored. Do not stage, commit, or include these files in any git operations.

## Git Workflow

### Before Committing
Always ask before committing or pushing changes.

### Commit Message Format
```
Brief summary of changes

- Detailed point 1
- Detailed point 2

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

### Branch Strategy
- `main` - Production, auto-deploys to GitHub Pages
- Feature branches for major changes

## Key Files Reference

| File/Directory | Purpose |
|----------------|---------|
| `hugo.toml` | Hugo site configuration (baseURL, theme, outputs) |
| `static/CNAME` | Custom domain (cloud-dev-25.educ8.se) |
| `static/robots.txt` | Search engine directives (Disallow: /) |
| `.github/workflows/hugo.yaml` | Deployment workflow (Hugo 0.128.0, GitHub Pages) |
| `.gitmodules` | DocDock theme submodule reference |
| `content/_index.md` | Homepage |
| `layouts/partials/pagination.html` | Hugo v0.148+ pagination fix |
| `layouts/partials/custom-head.html` | Robots meta |
| `themes/docdock/` | DocDock theme (git submodule, never edit directly) |

## Resources

- **Hugo Documentation:** <https://gohugo.io/documentation/>
- **DocDock Theme:** <https://github.com/vjeantet/hugo-theme-docdock>
- **GitHub Pages:** <https://docs.github.com/en/pages>
