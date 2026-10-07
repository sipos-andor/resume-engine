# ADR 0001: Deliver CV pages as static HTML, keep WebAssembly for the Studio

- Status: accepted
- Date: 2026-10-07

## Context

The engine renders a CV from JSON Resume files into a site hosted on GitHub Pages. Two delivery modes were
considered for the CV pages:

- **A, static:** a console generator renders the Razor components with Blazor's `HtmlRenderer` into plain HTML.
  Interactivity comes from small JavaScript modules that apply data the generator computed in C#.
- **B, WebAssembly with prerendering:** the same components run in a Blazor WebAssembly app; the generator
  prerenders every page into the published `index.html` and the runtime takes the page over after it starts.

The owner's priorities are a runtime engine (a "Studio" that renders any JSON Resume in the browser) and speed
on mobile, measured by Lighthouse.

## Spike

The same page, built from the operandor design system packages with the CV's full text (header, language menu,
stat tiles, cards, tags, footer, theme toggle, SEO head), was produced both ways and served locally. GitHub Pages
negotiates no Brotli and its compression of `.wasm` files is not documented, so B was measured with and without
gzip on the runtime files.

Mobile profile (4x CPU slowdown, 1.6 Mbit/s, 150 ms round trip, as Lighthouse's mobile preset):

| Variant | Transferred | First contentful paint | Interactive | Layout shift |
|---|---|---|---|---|
| A static | 97 KB | 0.92 s | 1.65 s | 0.002 |
| B, `.wasm` not compressed | 7.4 MB | 0.71 s | 40.6 s | 0.001 |
| B, everything gzip | 3.0 MB | 0.72 s | 18.6 s | 0.001 |

Desktop profile (50 Mbit/s, 20 ms, no CPU slowdown): A interactive at 0.22 s, B at 1.5 s.

Lighthouse 13.5, mobile (B with everything gzip, its best case):

| Category / metric | A | B |
|---|---|---|
| Performance | 100 | 45 |
| Accessibility | 100 | 100 |
| Best practices | 96 | 96 |
| SEO | 100 | 100 |
| Agentic browsing | 100 | 100 |
| Largest contentful paint | 1.1 s | 18.1 s |
| Total blocking time | 0 ms | 3,290 ms |
| Time to interactive | 1.1 s | 21.3 s |

B's largest contentful paint lands after the runtime starts because Blazor WebAssembly renders the page again
instead of hydrating the prerendered markup, so the browser counts the new element. Brotli through a JavaScript
decoder would cut the runtime from about 2.9 MB (gzip) to about 2.3 MB, which does not change the picture.

The spike also confirmed:

- `HtmlRenderer` renders the design system components, `SeoHead` through `HeadOutlet` (title, description,
  canonical, hreflang, Open Graph) and a native `<html lang>` per page, with no web server; four pages took 179 ms.
- Publishing a console project with `Microsoft.NET.Sdk.Web` lays out the packages' static web assets under
  `wwwroot/_content/...`, ready to copy.
- QuestPDF 2026.9.1 on SDK 10.0.401 produces PDF/UA-1 and PDF/A-3a output with IBM Plex embedded and subset,
  extractable Hungarian, Croatian and Serbian letters, and an e-mail address drawn as an image with no text.

## Decision

Deliver the CV pages as static HTML (A). Build the Studio as a separate WebAssembly app under `/studio/`,
where only visitors who open it download the runtime (about 1.5 s on desktop, about 18 s on a throttled phone).
Keep the components render-mode agnostic and the data behind `IResumeSource`, so a WebAssembly mode for the CV
pages stays a contained change.

## Consequences

- The CV pages keep Lighthouse performance at 100 on mobile and a strict Content-Security-Policy without
  `'wasm-unsafe-eval'` or an import map hash.
- Interactive features need JavaScript modules; their shared logic is checked against the C# implementation
  with common test vectors.
- The design system's `ThemeToggle` and `LanguageMenu` need static counterparts for their scripted parts.

## Considered

- B for the CV pages: full operandor parity and C# for every feature, at Lighthouse performance 45 and 18–40 s
  to interactive on a throttled phone.
- Lazily started WebAssembly islands on the CV pages: the same CSP cost on every page and two programming
  models on one page, for features that only look up a few kilobytes of data.
