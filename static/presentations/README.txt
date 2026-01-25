Standalone HTML Presentations
=============================

This folder contains self-contained reveal.js presentations with
Swedish Tech branding. These are static HTML files served directly
by Hugo without processing.

CLAUDE CODE SKILL
-----------------

When creating new presentations, use the reveal.js skill:

  Skill location: /.claude/skills/revealjs-skill/SKILL.md

The skill defines:
  - 5 slide types (Hero, Profile Card, Bullet, Timeline, Closing)
  - Swedish Tech color palette and typography
  - Complete HTML template structure
  - Fragment animation patterns
  - Reveal.js configuration

SHARED RESOURCES
----------------

CSS: /static/presentations/swedish-tech-slides.css
  - All presentations link to this file for consistent styling
  - Contains the Swedish Tech design system

FOLDER STRUCTURE
----------------

presentations/
├── README.txt                    # This file
├── swedish-tech-slides.css       # Shared Swedish Tech styling
├── course-introduction.html      # Course overview presentation
├── project-assignment.html       # Project requirements
└── infrastructure-fundamentals/
    ├── compute/                  # 4 topics × 2 languages = 8 files
    └── network/                  # 6 topics × 2 languages = 12 files

NAMING CONVENTIONS
------------------

Files:
  - English: {number}-{topic-name}.html
  - Swedish: {number}-{topic-name}-swe.html
  - Number prefix matches article folder (e.g., 1-, 2-, 3-)

Bilingual pairs:
  - Always create both English and Swedish versions
  - Swedish version uses lang="sv" and Swedish text
  - Technical terms typically kept in English

CREATING NEW PRESENTATIONS
--------------------------

1. Read the skill guide: /.claude/skills/revealjs-skill/SKILL.md
2. Use an existing presentation as reference template
3. Create both English and Swedish versions
4. Update the corresponding article to link to the new presentation

LINKING FROM ARTICLES
---------------------

Articles in /content/ link to presentations using absolute paths:

  [Watch the presentation](/presentations/path/to/presentation.html)
  [Se presentationen på svenska](/presentations/path/to/presentation-swe.html)

  ---

  [Article content begins here...]

Last updated: 2025-12-01
