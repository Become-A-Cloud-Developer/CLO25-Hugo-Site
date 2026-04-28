# Chapter Template

The structure every `<slug>.md` chapter follows. Workers in B2 write to this template. The gold-standard reference is `content/course-book/2-infrastructure/network/2-ip-addresses-and-cidr-ranges/ip-addresses-and-cidr-ranges.md` (after the migration, currently at `content/infrastructure-fundamentals/network/2-ip-addresses-and-cidr-ranges/ip-addresses-and-cidr-ranges.md`).

## File path

```
content/course-book/<part-N>-<part-slug>/<section>/<chapter-N>-<slug>/<slug>.md
```

Example:

```
content/course-book/3-application-development/1-http-and-mvc/2-the-mvc-pattern/the-mvc-pattern.md
```

## Frontmatter (TOML)

```toml
+++
title = "<Title in Title Case>"
program = "CLO"
cohort = "25"
courses = [...]   # ["BCD"], ["ACD"], or ["BCD", "ACD"]
weight = N        # integer; 10, 20, 30 ... within the section
date = YYYY-MM-DD
draft = false
+++
```

Optional `aliases` only for migrated pages:

```toml
aliases = ["/old-path/", "/another-old-path/"]
```

`description` is optional and rarely needed for chapter pages — the section `_index.md` carries the description shown on hub pages.

## Body skeleton

```markdown
[Watch the presentation](/presentations/course-book/<part>/<section>/<slug>.html)

[Se presentationen på svenska](/presentations/course-book/<part>/<section>/<slug>-swe.html)

---

<Opening paragraph: motivation. 2–4 sentences. State the problem this concept addresses, before defining the concept. End by previewing what the chapter will develop.>

## <First major section heading>

<Concept introduction. Define the load-bearing term in **bold** on first use, with the canonical definition string from the glossary (within edit-distance 5).>

<Mechanism: how does it work?>

### <Sub-section if needed>

<Drill into a specific aspect.>

## <Second major section heading>

<Continue the concept's development.>

### <Worked example>

<A concrete example tied to the companion exercise. Code block, CLI command, or config fragment.>

```language
// well-named code, sparing comments
```

<One paragraph interpreting the example.>

## <Third major section heading>

<Trade-offs, decision guidance, or comparison if applicable. Use a table when comparison genuinely helps decision-making.>

| Option | Strength | Trade-off |
|--------|----------|-----------|
| ... | ... | ... |

## Summary

<3–6 sentences recapping the chapter's load-bearing claims. The reader should be able to pick up cold from the Summary alone and know what the chapter taught.>
```

## Mandatory elements

- **First two lines after frontmatter** are the EN and SWE presentation links. The `---` horizontal rule is the visual break before prose begins.
- **At least one link to the companion exercise** somewhere in the body, formatted as `[text](/exercises/<path>/)`.
- **`## Summary` section** as the final section.
- **All bolded terms on first use** match the canonical glossary definition (within edit-distance 5) when this chapter owns the term.
- **No H1 in body** — the H1 comes from the frontmatter `title` via Hugo. Body headings start at H2 (`##`).
- **All code fences have language tags** (`bash`, `csharp`, `yaml`, `json`, `dockerfile`, `sql`, `text`, `toml`).

## Forbidden elements

- ❌ First-person plural (`we`, `our`, `us`, `let's`).
- ❌ Rhetorical questions in prose blocks.
- ❌ Temporal filler (`modern`, `today's`).
- ❌ Analogies to non-technical domains.
- ❌ Marketing language (`powerful`, `simply`, `easy`, `seamless`).
- ❌ Cross-references to other chapters by number (link by path, never "Chapter 3 of this Part").
- ❌ Duplicate definitions of glossary terms owned by other chapters.

## Length target

| Length | Outcome |
|--------|---------|
| 1500–3500 words | Target |
| 1400–1499 or 3501–3600 | Acceptable; soft warning |
| 1200–1399 or 3601–4000 | Stretching the bounds; soft warning |
| <1200 or >4000 | Hard fail in B7 |

Word count is body prose only — frontmatter, code blocks, and tables are excluded.

## Anti-patterns observed in early drafts

- **Definition opener** ("A container is a lightweight..."). Replace with motivation opener ("Running an application reproducibly across machines requires..."). The reader is more engaged when the problem precedes the definition.
- **List soup** (every paragraph is a bullet list). Prose carries narrative; lists carry parallel options. Mix them.
- **Re-defining** a glossary-owned term because the worker forgot to read the glossary. Cause for the worker prompt to fail loudly.
- **Missing trade-off** ("Containers are lightweight."). Pair every claim with its trade-off ("...but share the host kernel.").
- **Worked example dump** without interpretation. Always one paragraph of interpretation after a code block.
