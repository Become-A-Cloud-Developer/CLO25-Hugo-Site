# Progressive Disclosure Improvements

Checklist for improving content findability and progressive disclosure across the Hugo site.
Update this document as items are completed.

**Analysis date:** 2026-02-23
**Status:** In progress

---

## 1. Enable descriptions on all `children` shortcode calls

The `{{< children />}}` shortcode supports `description="true"` but it is never used.
Enabling it surfaces each child page's `description` frontmatter field directly in the listing.
This is the smallest change with the most immediate impact.

**Files to update:**

- [ ] `content/infrastructure-fundamentals/_index.md` — change `{{< children />}}` to `{{< children description="true" >}}`
- [ ] `content/exercises/_index.md` — same change
- [ ] `content/tutorials/_index.md` — same change
- [ ] `content/intro-cloud-development/_index.md` — same change
- [ ] `content/exercises/1-server-foundation/_index.md` — same change
- [ ] `content/exercises/1-server-foundation/1-portal-interface/_index.md` — same change
- [ ] Audit all remaining `_index.md` files for bare `{{< children />}}` calls and update them

**Verify:** After updating, confirm that every child page has a meaningful `description` in its frontmatter. Add missing descriptions where needed.

---

## 2. Fix the homepage to match actual content

The homepage (`content/_index.md`) lists five conceptual categories that don't match the real section names. The "Getting Started" link may point to a nonexistent path. The homepage is the most important progressive disclosure layer and currently the weakest.

- [ ] Update the "What's Here" list to use the actual section names and link to them
- [ ] Remove or fix the "Getting Started" link
- [ ] Add one-sentence descriptions for each section that match the `description` frontmatter
- [ ] Consider using `{{< children description="true" >}}` here as well for automatic accuracy

---

## 3. Add cross-references between Theory and Exercises

Theory sections (compute, network, storage) and exercise tracks (server-foundation, network-foundation, cloud-databases) cover the same topics but have no links connecting them. Students must navigate by sidebar to discover related content.

- [ ] `content/infrastructure-fundamentals/compute/_index.md` — add a "Practice This" section at the bottom linking to `exercises/1-server-foundation/`
- [ ] `content/infrastructure-fundamentals/network/_index.md` — add a "Practice This" section linking to `exercises/2-network-foundation/`
- [ ] `content/infrastructure-fundamentals/storage/_index.md` — add a "Practice This" section linking to `exercises/5-cloud-databases/`
- [ ] `content/exercises/1-server-foundation/_index.md` — add a "Background Reading" section linking to `infrastructure-fundamentals/compute/`
- [ ] `content/exercises/2-network-foundation/_index.md` — add a "Background Reading" section linking to `infrastructure-fundamentals/network/`
- [ ] `content/exercises/5-cloud-databases/_index.md` — add a "Background Reading" section linking to `infrastructure-fundamentals/storage/`
- [ ] Verify all cross-reference links resolve correctly with `hugo server`

---

## 4. Standardize `_index.md` landing page structure

Currently there are two competing patterns: bare `{{< children />}}` (auto-generated, no context) and manually curated topic lists (good UX, maintenance burden). Standardize on a three-part structure for every section index:

1. One-paragraph orientation (what this section is and why it matters)
2. "What You Will Learn" bullet list (3-5 points)
3. `{{< children description="true" >}}` for the child listing

- [ ] Define the standard template and document it (can go in this file or in `archetypes/`)
- [ ] Update `content/infrastructure-fundamentals/_index.md`
- [ ] Update `content/exercises/_index.md`
- [ ] Update `content/tutorials/_index.md`
- [ ] Update `content/intro-cloud-development/_index.md`
- [ ] Update mid-level sections (`exercises/1-server-foundation/`, etc.)

---

## 5. Migrate curated topic lists to description-based children

`compute/_index.md` and `network/_index.md` have manually written numbered topic lists with hardcoded paths. These become stale when content is added or renamed.

- [ ] Move per-topic descriptions from the curated lists into each child page's `description` frontmatter
- [ ] Replace the hardcoded lists with `{{< children description="true" >}}`
- [ ] Keep the "What You Will Learn" section (it summarizes the arc, not individual pages)
- [ ] Verify descriptions render well and no information is lost
- [ ] Apply the same migration to any other sections using hardcoded topic lists

---

## 6. Override the `children` shortcode for better rendering

The theme's default `children` shortcode renders a plain `<li>` list. A layout override can render children as cards or description lists for better scannability.

- [ ] Create `layouts/shortcodes/children.html` to override the theme version
- [ ] Render each child as a block with title and description (not just a bullet link)
- [ ] Add CSS via `static/css/custom.css` and enable it in `hugo.toml` with `customCSS = ["css/custom.css"]`
- [ ] Test that the override works for all existing `children` calls (with and without `description="true"`)
- [ ] Verify no regressions in sidebar menu or search

---

## 7. Add content-type signaling

All pages look identical in navigation. Adding a `content_type` frontmatter field enables visual distinction between content types.

Proposed values: `theory`, `exercise`, `tutorial`, `slides`

- [ ] Define the `content_type` field and its allowed values
- [ ] Add `content_type` to representative pages across each category
- [ ] Update the custom `children` shortcode (item 6) to render a label or icon based on `content_type`
- [ ] Roll out `content_type` to all content files
- [ ] Update `archetypes/default.md` to include `content_type` as a field

---

## 8. Use `expand` shortcode for optional depth

DocDock includes `{{< expand >}}` for collapsible sections. Use it for prerequisite details, advanced notes, or slide links to keep pages scannable while preserving depth.

- [ ] Identify pages with long prerequisite sections or optional detail
- [ ] Wrap optional content in `{{< expand "title" >}}...{{< /expand >}}`
- [ ] Test that expand/collapse works correctly on the deployed site

---

## Things to avoid

These approaches would fight Hugo's static site behavior or add unnecessary complexity:

- **Don't** introduce JavaScript-based filtering or dynamic rendering
- **Don't** add Hugo taxonomies for navigation (they produce flat list pages, not hierarchical ones)
- **Don't** restructure the directory tree (the hierarchy is sound; the problem is presentation)
- **Don't** edit files in `themes/docdock/` directly (use layout overrides)
