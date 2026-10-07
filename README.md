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

## Using it

A CV site is a content folder and a small build program.

**Content.** One `resume.<language>.json` per language (a BCP 47 tag: `resume.en.json`, `resume.hu.json`,
`resume.sr-Latn.json`), every file a complete CV, and a `site.json`:

```json
{
  "origin": "https://cv.example.com",
  "defaultLanguage": "en",
  "languageOrder": ["hu", "hr", "sr-Latn"],
  "downloadPrefix": "Ann_Example_CV",
  "themeStorageKey": "ann-cv-theme",
  "languageStorageKey": "ann-cv-language"
}
```

The default language is served at the root, every other one under its primary subtag (`/hu/`, `/sr/`). The build
checks every file and compares the languages: identifiers, dates, company names, URLs, technologies, levels and the
number of items must match the default language's, so a translation cannot drift. Plain JSON Resume works (see
[`samples/plain-json-resume`](samples/plain-json-resume)); the optional `x-` extensions add identifiers, a tagline,
a contact link, availability, strengths, position profiles (`?focus=` and tailored documents), the one-page view and
aliases for job ad matching. They are documented on the types in `Sipos.Resume.Core.Content`.

**No e-mail address in the content.** A file with an e-mail address anywhere is refused: a published address is
harvested. An address may appear only in the PDFs, drawn as an image, from the `RESUME_CONTACT_EMAIL` environment
variable; the build checks that no other file carries it.

**Build program.** A console on the Web SDK (publishing lays out the packages' static assets under `wwwroot/_content`):

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ScopedCssEnabled>false</ScopedCssEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Sipos.Resume.Generation" Version="0.1.0" />
    <PackageReference Include="Sipos.Resume.Theme.Operandor" Version="0.1.0" />
    <PackageReference Include="Sipos.Resume.Documents.Pdf" Version="0.1.0" />
  </ItemGroup>
</Project>
```

```csharp
var pdf = new PdfWriterOptions(LicenseType.Community); // the QuestPDF licence that applies to you
return await ResumeGenerator.Create(args)
    .UseTheme(new OperandorTheme())
    .UseWriter(new PdfDocumentWriter(pdf))
    .UseShareImages(new PdfShareImageWriter(pdf))
    .RunAsync();
```

```sh
dotnet publish -c Release -o build
RESUME_CONTACT_EMAIL=... dotnet build/YourCv.dll --content content --output site --clean --require-email
```

| Option | Meaning |
|---|---|
| `--content <folder>` | The content folder (default `content`). |
| `--output <folder>` | Where the site is written (default `dist`); a folder with files is emptied only with `--clean`. |
| `--assets <folder>` | The published `wwwroot` to copy assets from (default: next to the program). |
| `--today yyyy-MM-dd` | The present for ongoing periods (default: `SOURCE_DATE_EPOCH`, else today). |
| `--require-email` | Fail without `RESUME_CONTACT_EMAIL`, as a deployment should. |
| `--validate-only` | Check the content and stop. |

`RESUME_ANALYTICS_TOKEN` turns on Cloudflare Web Analytics (cookieless). The output is a static folder for any host;
the pages carry their Content-Security-Policy as a `meta` element, so GitHub Pages needs no headers. The build ends
by verifying the site: every page, download and machine-readable file, every local link, each page's language,
canonical URL, `hreflang` alternates and policy, and that no file holds an e-mail address as text.

## Developing

The operandor design system comes from GitHub Packages, which needs a token even to read: add a personal access
token with `read:packages` to the `github` source once:

```sh
dotnet nuget update source github --username <you> --password <token> --store-password-in-clear-text --configfile nuget.config
```

`dotnet test Sipos.Resume.slnx` runs every test; the theme's script is tested with Jint against the C# on shared
vectors. CI builds the sample site end to end. A tag `vX.Y.Z` on `main` publishes the packages (MinVer).

## Licence

MIT. `Sipos.Resume.Documents.Pdf` uses [QuestPDF](https://www.questpdf.com), which has its own licence: its Community
licence is free for individuals and organisations under USD 1M annual revenue; check it before you use the package.
