# resume-engine

A content-agnostic engine that turns [JSON Resume](https://jsonresume.org) files into a fast, multilingual,
prerendered static CV site with downloadable documents.

From one `resume.<language>.json` per language it builds:

- **web pages**, rendered at build time with Blazor's `HtmlRenderer` (no server, no WebAssembly download), in the
  operandor design system with a light and a dark theme;
- **documents**: a designed PDF and DOCX, ATS-friendly PDF, DOCX and plain text, Markdown and JSON Resume, plus
  position-tailored PDFs and DOCX;
- **machine-readable files**: `sitemap.xml` with `hreflang`, `robots.txt`, `llms.txt` per language, `llms-full.txt` and
  schema.org `ProfilePage` data.

The languages are the content files: add `resume.hu.json` and the site gets `/hu/`.

## Packages

| Package | Purpose |
|---|---|
| `Sipos.Resume.Core` | Domain model, JSON Resume reading and validation, cross-language checks, derived data (timeline, skill evidence, focus views, search index), ports |
| `Sipos.Resume.Generation` | The build: pages, Markdown/text/JSON/DOCX documents, SEO and AI files, verification |
| `Sipos.Resume.Documents.Pdf` | Designed and ATS PDFs with QuestPDF |
| `Sipos.Resume.Theme.Operandor` | Razor components in the operandor design system |

Architecture decisions are in [`docs/adr`](docs/adr).

## Licence

MIT. `Sipos.Resume.Documents.Pdf` uses [QuestPDF](https://www.questpdf.com), which has its own licence: its Community
licence is free for individuals and organisations under USD 1M annual revenue; check it before you use the package.
