Network Infrastructure Fundamentals - Presentation Architecture
==============================================================

This folder contains articles and presentations for the Network section
of Infrastructure Fundamentals.

PRESENTATION SYSTEMS
--------------------

1. NEW: Standalone HTML Presentations (Active)
   Location: /static/presentations/infrastructure-fundamentals/network/

   Modern reveal.js presentations with Swedish Tech branding.
   - 6 English presentations (*.html)
   - 6 Swedish presentations (*-swe.html)

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

network/
├── _index.md                    # Section index
├── README.txt                   # This file
├── legacy/                      # Old v1 articles (not slides)
├── 1-what-is-a-network/
│   ├── what-is-a-network.md           # Article (links to new HTML)
│   ├── what-is-a-network-slides.md    # Legacy DocDock slides (EN)
│   └── what-is-a-network-slides-swe.md # Legacy DocDock slides (SE)
├── 2-ip-addresses-and-cidr-ranges/
│   └── [same pattern]
├── 3-private-and-public-networks/
│   └── [same pattern]
├── 4-firewalls/
│   └── [same pattern]
├── 5-the-osi-model/
│   └── [same pattern]
└── 6-network-intermediaries/
    └── [same pattern]

ROLLBACK PROCEDURE
------------------

If new presentations have issues, edit each article and change:

FROM (new):
  [Watch the presentation](/presentations/infrastructure-fundamentals/network/X-topic.html)

TO (legacy):
  [Watch the presentation]({{< relref "topic-slides.md" >}})

Last updated: 2025-12-01
