# Slides Template

Both `<slug>-slides.md` (English) and `<slug>-slides-swe.md` (Swedish) follow the same structure. The Hugo content layer hosts the markdown; the rendered standalone HTML lives at `static/presentations/course-book/<part>/<section>/<slug>.html` (and `-swe.html`).

## File paths

```
content/course-book/<part>/<section>/<slug>/<slug>-slides.md         # English
content/course-book/<part>/<section>/<slug>/<slug>-slides-swe.md     # Swedish
```

Both rendered HTML files are produced in Phase B3 by the leader using the `revealjs-skill` patterns.

## Frontmatter (TOML)

```toml
+++
title = "<Title in Title Case>"   # English title or Swedish translation
program = "CLO"
cohort = "25"
courses = [...]
type = "slide"
date = YYYY-MM-DD
draft = false
hidden = true                     # do not appear in DocDock navigation

theme = "sky"
[revealOptions]
controls = true
progress = true
history = true
center = true
+++
```

`hidden = true` keeps these markdown source files out of the navigation tree — students reach the slides via the `[Watch the presentation]` link in the prose chapter.

## Body skeleton (markdown source)

The body is markdown-style slide content that mirrors the rendered HTML structure. Each `## Heading` becomes one slide; bullets within a heading become info-boxes; `---` separates slides.

```markdown
## <Slide 1: chapter title>
<chapter title in display form>

---

## <Slide 2: First concept>
- <Bullet 1, with **bolded keyword** for highlight>
- <Bullet 2>
- <Bullet 3>

---

## <Slide 3: Next concept>
- <Bullet 1>
- <Bullet 2>

---

## <Slide N: Closing>
- <One closing bullet, or "Questions?" / "Frågor?">
```

## Per-slide guidance

| Slide type | When to use | Bullet count |
|------------|-------------|--------------|
| Hero (slide 1) | Always — chapter title + Part subtitle | 0 (the title carries) |
| Bullet | Concept slides — most of the deck | 3–5 info-boxes |
| Closing (last slide) | Always — "Frågor?" / "Questions?" | 0 or 1 |

For chapters with a key diagram (e.g. 3-tier architecture, OAuth flow, container layer model), include one Diagram slide. The slide markdown should reference the diagram with a placeholder; the actual SVG is hand-written in B3 when rendering the HTML.

## Swedish translation guidance

The Swedish deck preserves the structure of the English deck (same slide count, same bullet count) but translates content into Swedish. Use established translations from existing infrastructure-fundamentals SWE pages where they exist:

| English | Swedish |
|---------|---------|
| Server | Server (also "tjänst" depending on context) |
| Container | Behållare (in formal/educational), often "container" in industry |
| Network | Nätverk |
| Storage | Lagring |
| Database | Databas |
| Health check | Hälsokontroll |
| Pipeline | Pipeline (loanword, no Swedish translation in industry) |
| Authentication | Autentisering |
| Authorization | Auktorisering |
| Cookie | Cookie (loanword) |
| Token | Token (loanword) |
| Build | Build (loanword in CI/CD context) |
| Deployment | Driftsättning |
| Monitoring | Övervakning |
| Logging | Loggning |
| Pull request | Pull request (loanword) |

When in doubt, prefer the loanword used in the existing infrastructure-fundamentals SWE pages over a literal translation.

## Rendered HTML output

The HTML deck is produced in B3, not B2. The renderer (using `revealjs-skill` patterns) maps the markdown source to the appropriate slide types:

- First slide markdown block → Hero slide section in HTML
- `## Heading` markdown blocks → Bullet slide sections
- Last slide markdown block → Closing slide section

Output HTML structure (skeleton):

```html
<!DOCTYPE html>
<html lang="en">   <!-- "sv" for SWE -->
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta name="robots" content="noindex, nofollow">
    <title><Chapter Title></title>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/reveal.js/5.0.4/reveal.min.css">
    <link rel="stylesheet" href="../../../../swedish-tech-slides.css">  <!-- 4 levels up -->
</head>
<body>
    <div class="reveal">
        <div class="slides">
            <!-- Hero, Bullet*, Closing -->
        </div>
    </div>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/reveal.js/5.0.4/reveal.min.js"></script>
    <script>
        Reveal.initialize({
            hash: true, controls: true, progress: true, center: false,
            transition: 'slide', backgroundTransition: 'fade',
            slideNumber: 'c/t', history: true,
            width: 1920, height: 1080, margin: 0.04,
            minScale: 0.2, maxScale: 1.0
        });
    </script>
</body>
</html>
```

## Quality bar for slides (B7 gate 6)

- EN and SWE decks have the same number of `<section>` blocks ±1.
- Every Hero slide has the chapter title and a Part-naming subtitle.
- Every Bullet slide has 3–5 info-boxes (extremes flagged in B4 review).
- Every Closing slide is "Questions?" / "Frågor?".
- The `swedish-tech-slides.css` reference resolves (path math is correct).

## Anti-patterns

- ❌ Translating loanwords that the industry uses untranslated (e.g. "pipeline" → "rörledning").
- ❌ Bulleted slides with paragraph-length bullets. Each bullet ≤15 words.
- ❌ Diverging slide counts between EN and SWE.
- ❌ Embedding code blocks inside the markdown source (the HTML renderer will produce code blocks via `revealjs-skill`'s `<pre class="code-block">` pattern; the markdown source describes intent, not literal HTML).
