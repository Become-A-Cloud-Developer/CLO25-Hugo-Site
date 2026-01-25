# Reveal.js Swedish Tech Presentation Skill

## Overview
Expert skill for creating professional Reveal.js presentations with a distinctive Swedish tech aesthetic. Use standalone HTML presentations stored in `static/presentations/` and link to them from Hugo content pages.

## Core Capabilities
- Create multi-slide Reveal.js presentations with 5 predefined slide types
- Apply Swedish tech design system (blue/yellow/cyan color scheme)
- Generate interactive elements with fragment animations
- Maintain consistent typography and spacing optimized for large screens

## Design System

### Color Palette
```css
--swedish-blue: #006AA7
--swedish-blue-light: #0090E3
--swedish-blue-dark: #004B79
--swedish-yellow: #FECC00
--tech-cyan: #00D9FF
--dark-background: #0A0E27
--muted-text: #94A3B8
```

### Typography (Optimized for 1920x1080)
- **Headers**: Segoe UI, 800 weight, uppercase
- **Body**: Segoe UI, 400 weight
- **Title sizes**: 5em (h1/main-title), 3.6em (slide headers), 1.9em (body)
- **Minimum body**: 1.5em for readability

### File Structure
```
static/presentations/
├── swedish-tech-slides.css    # Shared CSS for all presentations
├── your-presentation.html     # Standalone presentation
└── images/                    # Presentation images
```

## The 5 Slide Types

### 1. Hero Slide
Opening slide with course badge, multi-line title, and decorative elements.

```html
<section class="hero-slide" data-background-color="#0A0E27">
    <div class="geometric-bg"></div>
    <div class="corner-accent"></div>

    <div class="course-badge">
        <span>BADGE TEXT</span>
    </div>

    <h1 class="main-title">
        LINE ONE<br>
        LINE TWO<br>
        LINE THREE
    </h1>

    <p class="subtitle">Subtitle in cyan</p>
    <p class="course-info">Additional info • Duration • Year</p>

    <div class="bottom-accent"></div>
</section>
```

### 2. Profile Card Slide
Card positioned on the right side for personal introductions or questions.

**With Profile Image:**
```html
<section class="profile-card-slide" data-background-color="#0A0E27">
    <div class="geometric-bg"></div>
    <div class="corner-accent"></div>

    <h2 class="slide-header">Vem är jag?</h2>

    <div class="card-container">
        <div class="profile-card">
            <div class="profile-image">
                <img src="profile-photo.jpg" alt="Name">
            </div>
            <h3 class="profile-name">Person Name</h3>
            <p class="profile-roles">
                Role 1, <span class="highlight">Role 2</span>,<br>
                Role 3, Role 4
            </p>
        </div>
    </div>

    <div class="bottom-accent"></div>
</section>
```

**With Question Mark:**
```html
<section class="profile-card-slide" data-background-color="#0A0E27">
    <div class="geometric-bg"></div>
    <div class="corner-accent"></div>

    <h2 class="slide-header">Vem är du?</h2>

    <div class="card-container">
        <div class="profile-card">
            <div class="question-mark">?</div>
            <p class="question-text">Presentera dig själv</p>
        </div>
    </div>

    <div class="bottom-accent"></div>
</section>
```

### 3. Bullet Slide
Information boxes that appear progressively with fragments.

```html
<section class="bullet-slide" data-background-color="#0A0E27">
    <h2 class="slide-header">Slide Title</h2>

    <div class="fragment info-box">
        <p>First point with <span class="highlight">highlighted text</span></p>
    </div>

    <div class="fragment info-box">
        <p>Second point with more <span class="highlight">emphasis</span></p>
    </div>

    <div class="fragment info-box">
        <p>Third point continues the <span class="highlight">pattern</span></p>
    </div>
</section>
```

### 4. Timeline Slide
Horizontal timeline with week/phase indicators and description boxes.

```html
<section class="timeline-slide" data-background-color="#0A0E27">
    <h2 class="slide-header">Timeline Title</h2>

    <div class="timeline">
        <div class="timeline-item fragment" data-fragment-index="1">
            <div class="timeline-week">1</div>
            <div class="timeline-label">Topic 1</div>
        </div>
        <div class="timeline-item fragment" data-fragment-index="2">
            <div class="timeline-week">2</div>
            <div class="timeline-label">Topic 2</div>
        </div>
        <div class="timeline-item fragment" data-fragment-index="3">
            <div class="timeline-week">3</div>
            <div class="timeline-label">Topic 3</div>
        </div>
        <!-- Add more items as needed -->
    </div>

    <div class="description-container">
        <div class="fragment fade-in-then-out description-box" data-fragment-index="1">
            <p><span class="highlight">Week 1 - Topic 1:</span> Description of what happens in week 1.</p>
        </div>
        <div class="fragment fade-in-then-out description-box" data-fragment-index="2">
            <p><span class="highlight">Week 2 - Topic 2:</span> Description of what happens in week 2.</p>
        </div>
        <div class="fragment fade-in description-box" data-fragment-index="3">
            <p><span class="highlight">Week 3 - Topic 3:</span> Description of what happens in week 3.</p>
        </div>
    </div>
</section>
```

### 5. Closing Slide
Centered content for questions or closing statements.

```html
<section class="closing-slide" data-background-color="#0A0E27">
    <div class="geometric-bg"></div>
    <div class="corner-accent"></div>

    <div class="centered-content">
        <div class="big-symbol">?</div>
        <p class="big-text">Frågor</p>
    </div>

    <div class="bottom-accent"></div>
</section>
```

## Complete HTML Template

```html
<!DOCTYPE html>
<html lang="sv">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Presentation Title</title>

    <!-- Reveal.js Core CSS -->
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/reveal.js/5.0.4/reveal.min.css">

    <!-- Swedish Tech Slides CSS -->
    <link rel="stylesheet" href="swedish-tech-slides.css">
</head>
<body>
    <div class="reveal">
        <div class="slides">
            <!-- Add slides here using the 5 slide types -->
        </div>
    </div>

    <!-- Reveal.js Core JS -->
    <script src="https://cdnjs.cloudflare.com/ajax/libs/reveal.js/5.0.4/reveal.min.js"></script>

    <script>
        Reveal.initialize({
            hash: true,
            controls: true,
            progress: true,
            center: false,
            transition: 'slide',
            backgroundTransition: 'fade',
            slideNumber: 'c/t',
            history: true,
            width: 1920,
            height: 1080,
            margin: 0.04,
            minScale: 0.2,
            maxScale: 1.0
        });
    </script>
</body>
</html>
```

## Hugo Integration

Create a content page that links to the presentation:

```markdown
+++
title = "Presentation Title"
description = "Brief description"
weight = 5
+++

# Presentation Title

Description of what the presentation covers.

**[Öppna presentationen](/presentations/your-presentation.html)**

## Innehåll

- Topic 1
- Topic 2
- Topic 3

*Presentationen använder reveal.js med Swedish Tech-tema.*
```

## Workflow

### 1. Analyze Requirements
- Identify presentation purpose and audience
- Determine which slide types are needed
- Plan content for each slide
- Gather any images needed

### 2. Create Files
- Copy `swedish-tech-slides.css` to `static/presentations/` (if not already there)
- Create new HTML file in `static/presentations/`
- Create Hugo content page linking to presentation

### 3. Build Slides
Use the appropriate slide type for each section:
- **Hero Slide**: Opening/title
- **Profile Card Slide**: Introductions, questions to audience
- **Bullet Slide**: Main content, lists, key points
- **Timeline Slide**: Schedules, phases, progression
- **Closing Slide**: Questions, thank you, contact info

### 4. Add Interactivity
- Use `class="fragment"` for sequential reveals
- Add `data-fragment-index="N"` for specific order
- Use `fade-in-then-out` for timeline descriptions
- Use `fade-in` for the last item that stays visible

## Best Practices

### Design Principles
1. **Bold choices** - Use strong contrasts and geometric shapes
2. **Visual hierarchy** - Clear size and color differences
3. **Consistent spacing** - Follow the spacing system
4. **Swedish identity** - Blue/yellow prominently featured
5. **Tech aesthetic** - Cyan accents, dark backgrounds

### Content Guidelines
1. **Bullet slides**: Maximum 5 info-boxes per slide
2. **Timeline slides**: 6-8 items maximum
3. **Text**: Keep concise, use highlights for emphasis
4. **Progressive disclosure**: Use fragments for complex info
5. **High contrast**: White/cyan/yellow on dark background

### Technical Requirements
1. **CDN links**: Use Reveal.js 5.0.4 from CDN
2. **Resolution**: Optimized for 1920x1080
3. **Scaling**: maxScale: 1.0 prevents overflow
4. **CSS file**: Always reference swedish-tech-slides.css

## Common Patterns

### Highlight Text
```html
<span class="highlight">Important text</span>
```

### Fragment Order
```html
<div class="fragment" data-fragment-index="1">First</div>
<div class="fragment" data-fragment-index="2">Second</div>
```

### Description Box with Fade
```html
<!-- Fades out when next appears -->
<div class="fragment fade-in-then-out description-box" data-fragment-index="1">

<!-- Stays visible (last item) -->
<div class="fragment fade-in description-box" data-fragment-index="2">
```

## Example Usage

```
User: "Create a Reveal.js presentation about Azure fundamentals with 5 slides"

Claude Code will:
1. Create hero slide with course title
2. Add profile card slide for instructor intro
3. Create bullet slide with key topics
4. Add timeline slide for course schedule
5. Create closing slide for questions
6. Save to static/presentations/azure-fundamentals.html
7. Create content page in content/presentations/azure-fundamentals.md
```

## Quality Checklist
- [ ] All text is readable (proper sizing)
- [ ] Colors follow Swedish tech palette
- [ ] Fragments appear in correct order
- [ ] Timeline descriptions use fade-in-then-out (except last)
- [ ] Headers have yellow left border
- [ ] Info-boxes have yellow background tint
- [ ] Scaling set to maxScale: 1.0
- [ ] Resolution set to 1920x1080
