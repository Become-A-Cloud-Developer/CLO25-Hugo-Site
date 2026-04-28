# Glossary Protocol

The Part-level glossary is the single mechanism that prevents terminology drift across parallel chapter workers. Read this before running Phase B1.

## Why a glossary

Six parallel agents authoring six chapters cannot see each other's prose. Without a contract, each agent will independently define **container**, **DTO**, **structured logging**, etc. as if introducing them for the first time. A reader moving from chapter 3 to chapter 4 then encounters "DTO" defined twice with subtly different language and feels the seams.

The glossary fixes this by deciding upfront which chapter owns the first-mention of each term, and what the canonical definition string is.

## What a term is

A term qualifies for the glossary if **all** of the following hold:

- It is **bolded on first use** in the chapter that introduces it. Bold is the explicit signal that a term is being defined.
- It is **referenced by at least one other chapter** in the same Part, OR it is referenced from a later Part. (Single-use terms do not need glossary entries — they introduce and define inline without contention.)
- It is **specific to the domain**. Do not glossarize "the system" or "a request"; do glossarize "control plane", "idempotency", "rolling deployment".

## Producing the glossary

The leader produces the glossary in B1 by:

1. **Enumerating candidate terms** from three sources:
   - The "Owns terms" and "Borrows terms" lists in the mining notes per chapter.
   - Bolded phrases in the companion exercise files: `grep -hoE '\*\*[A-Z][^*]+\*\*' <exercise files> | sort -u`.
   - Bolded phrases in any Part I or II chapter that this Part will link to.

2. **Assigning ownership**:
   - Reading order is the tiebreaker. The chapter that appears earliest in the Part's reading order owns the term.
   - If two chapters have equal claim and similar weight, prefer the chapter whose mining notes already list the term in "Owns terms".
   - A term used by only one chapter in the Part is owned by that chapter (no contention).

3. **Writing canonical definitions**:
   - 1 to 2 sentences each.
   - Third-person voice (no "we", no "you" except for action choices).
   - Lead with motivation when possible: "An **API key** identifies the caller of an HTTP API by attaching a shared secret to the request, allowing the server to gate access without authenticating an end user."
   - The canonical definition string is the contract — workers may reword it by no more than 5 words of edit distance.

4. **Cross-Part borrowing**:
   - If a term is owned by an earlier Part, write the entry in the "Terms borrowed from earlier Parts" section with the relative URL pointing to the defining chapter.
   - Borrowed terms must NOT be redefined in this Part. Workers link to the defining chapter using the supplied URL.

## File location

`docs/coursebook-mining/<part>-glossary.md`

Format:

```markdown
# Part <N> — Glossary

## Terms owned by this Part

### <Term in Title Case>
- **Owner chapter**: <slug>
- **Canonical definition**: <1–2 sentences>
- **Used by chapters**: <list of slugs that reference this term>

### <Next Term>
...

## Terms borrowed from earlier Parts

### <Term>
- **Defined in**: Part <N> chapter <slug>
- **Reference link**: /course-book/<part>/<section>/<slug>/
```

## Worker contract

Each B2 worker receives the entire glossary verbatim in its prompt with the following hard rules:

- **If your chapter owns a term**: introduce it in your chapter using the canonical definition string. You may reword by no more than 5 words of edit distance to fit your sentence rhythm. Bold the term on first use.
- **If a different chapter in this Part owns a term**: do NOT redefine it. Link to the owning chapter using a relative URL. Do NOT bold it on use in your chapter (only the owner bolds).
- **If a term is borrowed from an earlier Part**: do NOT redefine. Link to the supplied path.

## Reconciliation in B4 review

The Phase B4 review agent checks glossary compliance per chapter:

- For each owned term, verify the canonical definition appears in the owner's chapter within edit distance ≤5 words. If not, flag as Critical.
- For each borrowed term, grep all chapters for the bold-and-define pattern (`\*\*<term>\*\*[^.]*\bis\b`) outside the owner. Any match is a violation. Flag as Critical.
- Report violations in the Critical section of the punch list.

## Reconciliation in B7 validation

The `voice-check.sh` includes a glossary-compliance check (gate #9) that runs the same edit-distance check programmatically. The B4 review agent is a redundant safety net for cases where the heuristic misses a paraphrase.

## When to update the glossary

Once a Part's glossary is committed (after B7 of that Part), it becomes immutable. Later Parts read it as "Terms borrowed from earlier Parts" but do not modify it.

If a later Part discovers that an earlier Part's glossary has an error or a worse-than-intended definition, the fix is a follow-up edit pass on that earlier Part — never an inline override in the later Part.

## Anti-patterns

- ❌ Letting workers decide ownership. Always leader-driven.
- ❌ Glossarizing every bolded phrase. Single-use terms do not need entries.
- ❌ Long canonical definitions. 1–2 sentences max. The chapter prose develops the concept; the glossary holds the seed.
- ❌ Modifying an earlier Part's glossary from a later Part. Always fix at source.
