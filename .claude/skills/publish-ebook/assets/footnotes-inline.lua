-- Inline footnotes for WeasyPrint's CSS Paged Media `float: footnote`.
--
-- By default, Pandoc's HTML writer collects all footnotes into a
-- <section class="footnotes"> at the very end of the document — i.e.
-- endnotes. WeasyPrint can lift inline elements to the page-bottom
-- footnote area only when the float lives at the call-site.
--
-- This filter rewrites every Note element into an inline
-- <span class="footnote"> placed exactly where the reference appears,
-- keeping inline formatting (em / strong / code / links) intact via
-- Pandoc's HTML writer rather than the lossy `pandoc.utils.stringify`.

function Note(elem)
  local html = pandoc.write(pandoc.Pandoc(elem.content), 'html')
  -- Pandoc wraps single-paragraph notes in <p>...</p>; strip outer
  -- block-level wrapping so the span isn't a block.
  html = html:gsub('^%s*<p[^>]*>%s*', '')
  html = html:gsub('%s*</p>%s*$', '')
  return pandoc.RawInline('html',
    '<span class="footnote">' .. html .. '</span>')
end
