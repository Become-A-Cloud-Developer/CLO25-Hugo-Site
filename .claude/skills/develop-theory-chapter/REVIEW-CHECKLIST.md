# Cross-Review Checklist

Focus areas for the Phase B4 cross-review agent. The reviewer reads every chapter in the Part and returns a punch list using these axes. The leader then fixes findings in B5.

## A. Glossary compliance (most important)

The single most valuable thing the cross-review can catch. Verify against `docs/coursebook-mining/<part>-glossary.md`:

1. **Owned terms** — for each term in the "Terms owned by this Part" section, the owner's chapter introduces it using the canonical definition within edit distance ≤5 words. The term is bolded on first use.
2. **Borrowed terms (Part-internal)** — for each term owned by chapter X, no other chapter in this Part redefines it. Other chapters link to the owner via relative URL.
3. **Borrowed terms (cross-Part)** — for each term in the "Terms borrowed from earlier Parts" section, no chapter in this Part redefines it. All references link to the supplied path.
4. **Stray bold-and-define patterns** — grep each chapter for the pattern `\*\*[A-Z][^*]+\*\*[^.]*\bis\b`. Every hit must correspond to a glossary entry where this chapter is the owner. Any other hit is a violation (flag Critical).

## B. Voice and tone

Spot-check every chapter against `student-technical-writer/SKILL.md`:

1. **No first-person plural** — search for `\bwe will\b`, `\bwe'll\b`, `\bwe can\b`, `\blet's\b`. Any hit is Critical.
2. **No rhetorical questions in prose blocks** — sentences that end in `?` outside of code, blockquotes, or quoted user inputs.
3. **No temporal filler** — `\b(modern|today's|in the digital age|in the current landscape|contemporary)\b`. Any hit is Significant.
4. **No analogies to non-technical domains** — sentences like "think of X as a traffic cop / a librarian / a restaurant".
5. **No hedging filler** — "It's worth noting that", "It's important to understand that", "you should be aware that".
6. **Paragraph rhythm** — flag any chapter where ≥80% of paragraphs are the same sentence count (all 3-sentence or all 7-sentence) — that's a candidate for B6 voice rewrite.

## C. Structural integrity

For each chapter, verify against `CHAPTER-TEMPLATE.md`:

1. **Frontmatter complete** — `title`, `program = "CLO"`, `cohort = "25"`, `courses = [...]`, `weight`, `draft = false`. `aliases` only if migration brief specified them.
2. **First two body lines are presentation links** — `[Watch the presentation](/presentations/...)` followed by blank line then `[Se presentationen på svenska](/presentations/...-swe.html)`.
3. **At least one cross-link to a companion exercise** under `/exercises/...`.
4. **Closing `## Summary` section** present, 3–6 sentences, summarizing load-bearing claims.
5. **Heading hierarchy** — H1 only as the chapter title (auto from frontmatter); body uses H2 (`##`) and H3 (`###`); no H4+ unless genuinely necessary.
6. **No exercise numbers in cross-references** — link by path (`/exercises/4-services-and-apis/1-rest-api-and-dtos/`), not by number.
7. **Slide pair present and parallel** — `*-slides.md` and `*-slides-swe.md` exist, structurally parallel (same `## Heading` count ±1).

## D. Pedagogical quality

1. **Motivation before definition** — does the chapter open with a problem or need, not a definition? "Container orchestration manages..." is a definition opener; "Running multiple isolated applications on a single physical server requires..." is a motivation opener.
2. **Worked example present** — chapter contains at least one concrete example tied to the companion exercise (a code snippet, a CLI command, a config fragment).
3. **Decision guidance where applicable** — if the chapter covers multiple options (cookies vs JWT, Docker Hub vs ACR), it includes a decision-framing paragraph or table comparing trade-offs.
4. **Trade-offs named** — every named technique has at least one trade-off acknowledged. "Containers are lightweight..." should be paired with "...but share the host kernel, so a kernel CVE affects all containers on that host."
5. **No marketing language** — "powerful", "robust", "seamless", "intuitive", "easy", "simply", "just".

## E. Cross-chapter consistency (Part-level)

Read all chapters in sequence, then check:

1. **Reading order makes sense** — does chapter N+1 build on chapter N's foundation, or could a reader hit it cold?
2. **No backwards forward-references** — chapter 2 should not reference a term that chapter 5 introduces.
3. **Cross-links resolve** — every `[term](/course-book/<part>/<section>/<slug>/)` link points to a chapter that exists in this Part or an earlier Part.
4. **No duplicated worked examples** — if chapter 1 worked through `dotnet new mvc` and chapter 4 also uses MVC scaffolding, chapter 4 should reference chapter 1 rather than redoing the worked example.
5. **Voice consistency** — read chapters 1, 3, 5 in sequence and chapters 2, 4, 6 in sequence. If they sound like different authors, B6 voice rewrite has work to do.

## F. Slide-pair quality

For each chapter's `*-slides.md` and `*-slides-swe.md`:

1. **First slide is Hero** — chapter title in `<h1>`, subtitle naming the Part, course badge.
2. **Each `## Heading` block** maps cleanly to a Bullet slide with 3–5 info-boxes.
3. **Last slide is Closing** — `?` symbol with "Frågor" / "Questions".
4. **Swedish translation is accurate** — terms preserve their Swedish counterparts where established (e.g. "containrar" not "containers", "hälsokontroll" not "health check"). Defer to existing infrastructure-fundamentals SWE pages for established translations.
5. **Section-count parity** — EN and SWE have the same number of `## Heading` blocks ±1.

## Output format expected from the reviewer

```text
## Critical issues (must fix)
- File: <chapter-slug>.md, line ~N: "<offending text>" — <one-sentence fix>

## Significant issues (should fix)
- ...

## Minor issues (nice to fix)
- ...

## Cross-cutting issues (Part-wide)
- <patterns observed across multiple chapters>

## Strengths to preserve
- <what is working — guide for the voice rewrite agent>
```

Cap the report at ~1000 words. The leader uses this directly to make B5 edits.
