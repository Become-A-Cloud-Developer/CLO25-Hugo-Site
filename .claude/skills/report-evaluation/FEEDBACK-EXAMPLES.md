# Feedback Examples

This document defines the tone and style for student feedback. All feedback is written in Swedish using "du/din".

## Fundamental Rules

1. **All feedback must be framed positively.** Never mention what's missing or could be improved.
2. **Keep it dry.** Minimal adjectives, no exclamation marks, no superlatives.
3. **Stay vague on specifics.** Do not reference specific filenames, code snippets, section names, or tools from the report unless you are 100% certain the claim is accurate. It is better to be general than to make an incorrect claim.
4. **Do not address the student by first name** in the feedback text.
5. **Do not translate established English tech terms into Swedish.** Words like deploy, commit, push, pull request, reverse proxy, Infrastructure as Code, CI/CD, pipeline stay in English — that is how they are used in Swedish. Only translate when a natural Swedish term is well-established (e.g., "servicefil", "skärmdumpar").
6. **The tone must subtly reflect the grade** so the instructor can identify the grade from the first sentence alone, but the student should not notice the pattern.

## Three-Sentence Structure

Every feedback consists of exactly **three sentences**. No filler sentences like "Bra jobbat!" — each sentence must carry meaning.

| Sentence | Purpose |
|----------|---------|
| **Sentence 1** | Grade signal — a vague, general statement that subtly indicates the grade level (see below) |
| **Sentence 2** | One safe observation about the work — kept general enough to be verifiable |
| **Sentence 3** | A closing remark that rounds out the picture |

## Grade-Signal Words (Sentence 1)

The first sentence uses different vocabulary depending on the grade. This is the primary mechanism for the instructor to read the grade from the tone.

### VG signals (depth, understanding, coherence)

Use words like: **förståelse**, **resonemang**, **sammanhängande**, **genomtänkt**, **röd tråd**, **djup**, **hänger ihop**

These words signal that the student demonstrates understanding beyond the surface level.

### G signals (functional, complete, basics covered)

Use words like: **fungerande**, **alla delmoment**, **på plats**, **fått ihop**, **täcker**, **behandlade**, **redovisade**

These words signal that the student has completed the work and it functions, without implying deeper understanding.

**Important:** The distinction must be subtle. A student reading their own G feedback should feel acknowledged, not "lesser." A student reading their VG feedback should not think "oh, this is the VG template."

## VG Examples

> "Rapporten visar god förståelse för hela kedjan från applikation till driftsättning. Automation och säkerhet löper som en röd tråd genom arbetet. Det märks att du har tänkt igenom helheten."

> "Det finns en tydlig röd tråd genom rapporten som visar förståelse för hela processen. Infrastructure as Code och automatiserad deploy ger en sammanhängande lösning. Rapporten håller ihop väl."

> "Rapporten visar en sammanhängande förståelse för sambandet mellan infrastruktur, konfiguration och säkerhet. Det märks att du förstår varför varje steg behövs, inte bara hur. Genomtänkt arbete."

> "Rapporten visar en djup förståelse för hela kedjan från infrastruktur till säkerhetskonfiguration. Resonemangen kring IaC och säkerhet visar att du har grävt under ytan. Det hänger ihop som helhet."

> "Rapporten visar ett genomtänkt arbetssätt med tydliga resonemang genom hela processen. Säkerhetsaspekterna är väl integrerade i lösningen. Det hänger ihop."

> "Rapporten har en tydlig röd tråd med genomtänkta resonemang kring varje steg. Infrastruktur som kod och säkerhet löper naturligt genom texten. Det märks att du förstår helheten."

## G Examples

> "Du har fått ihop alla delar och visar en fungerande lösning. Automatiseringen av processen visar ett strukturerat tillvägagångssätt. Alla delmoment är behandlade."

> "Alla delmoment är på plats och lösningen fungerar i Azure. Säkerhetsdelen visar att du tänkt på grundläggande skydd. Du har redovisat alla steg i processen."

> "Rapporten täcker alla delmoment och visar en fungerande deploy-kedja. Provisioneringen visar att du har tänkt på automatisering. Processen är tydligt redovisad."

> "Du har fått ihop alla steg och visar en fungerande lösning. Det syns att du har arbetat dig igenom uppgiften. Alla delmoment är redovisade."

## Phrases to AVOID

| Avoid | Why |
|-------|-----|
| "Riktigt snyggt!", "Kul att se..." | Too enthusiastic |
| "Bra jobbat!" | Filler — not a real sentence |
| "imponerande" / "imponerad" | Superlative |
| "genomarbetad" as standalone praise | Too enthusiastic for this tone |
| Student's first name | Do not address by name |
| Specific filenames (setup_server.sh) | Risk of inaccuracy |
| Specific code (`set -euo pipefail`) | Risk of inaccuracy |
| Specific error messages (ZonalAllocationFailed) | Risk of inaccuracy |
| Specific section titles from the report | Risk of inaccuracy |
| Direct quotes from the report | Risk of inaccuracy |
| "men" statements | No "but" clauses |
| "dock", "emellertid" | No contrast words |
| "saknar", "saknas" | Don't describe what's missing |
| "VG kräver...", "För VG..." | Never mention grades |
| "Nästa steg vore..." | No improvement suggestions |
| Invented Swedish tech words | Keep English terms as-is |

## Tech Terminology

Swedish IT professionals mix Swedish and English freely. Follow established usage:

| Keep in English | Natural Swedish equivalent exists |
|-----------------|-----------------------------------|
| deploy, deployment | driftsättning (both are fine) |
| commit, push, pull request | — (always English) |
| reverse proxy | — (always English) |
| Infrastructure as Code, IaC | — (always English) |
| CI/CD, pipeline | — (always English) |
| SSH, NSG, VM | — (always English abbreviations) |
| branch, merge | — (always English) |
| container, image | — (always English) |

| Use Swedish | Avoid |
|-------------|-------|
| servicefil | tjänstfil |
| skärmdumpar | skärmbilder (both OK, but skärmdumpar more common) |
| provisionering | — (established Swedish) |
| säkerhetshärdning | — (established Swedish) |

**Rule of thumb:** If a Swedish developer would say the English word in a conversation, keep it in English. Do not invent translations.

## Testing Your Feedback

Before finalizing, verify:

1. Can you tell the grade from sentence 1 alone? (instructor test)
2. Would a student notice the G/VG pattern? (subtlety test)
3. Are there any specific claims that could be wrong? (accuracy test)
4. Is the student addressed by first name? (should not be)
5. Are there any invented Swedish tech terms? (should not be)
6. Are there more than three sentences? (should not be)
7. Is there an exclamation mark? (should not be)
8. Are there filler phrases like "Bra jobbat"? (should not be)
