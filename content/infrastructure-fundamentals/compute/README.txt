Compute Infrastructure Fundamentals - Presentation Architecture
===============================================================

This folder contains articles and presentations for the Compute section
of Infrastructure Fundamentals.

PRESENTATION SYSTEMS
--------------------

1. NEW: Standalone HTML Presentations (Active)
   Location: /static/presentations/infrastructure-fundamentals/compute/

   Modern reveal.js presentations with Swedish Tech branding.
   - 4 English presentations (*.html)
   - 4 Swedish presentations (*-swe.html)

   Articles currently link to these presentations.

2. LEGACY: DocDock Inline Slides (Preserved)
   Location: Within each topic subfolder (*-slides.md, *-slides-swe.md)

   Original Hugo-processed markdown slides using DocDock theme.
   These are kept for backwards compatibility. If issues arise with
   the new presentations, articles can be reverted to link to these
   by changing the link format back to:

   [Watch the presentation]({{< relref "topic-slides.md" >}})
   [Se presentationen på svenska]({{< relref "topic-slides-swe.md" >}})

FOLDER STRUCTURE
----------------

compute/
├── _index.md                    # Section index
├── README.txt                   # This file
├── legacy/                      # Old v1 articles (not slides)
├── 5-azure-vm-sizing-and-cost.md # Standalone article (no presentation)
├── 1-what-is-a-server/
│   ├── what-is-a-server.md            # Article (links to new HTML)
│   ├── what-is-a-server-slides.md     # Legacy DocDock slides (EN)
│   └── what-is-a-server-slides-swe.md # Legacy DocDock slides (SE)
├── 2-common-server-roles/
│   └── [same pattern]
├── 3-inside-a-physical-server/
│   └── [same pattern]
└── 4-inside-a-virtual-server/
    └── [same pattern]

ROLLBACK PROCEDURE
------------------

If new presentations have issues, edit each article and change:

FROM (new):
  [Watch the presentation](/presentations/infrastructure-fundamentals/compute/X-topic.html)

TO (legacy):
  [Watch the presentation]({{< relref "topic-slides.md" >}})

Last updated: 2025-12-01
