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

The default language is served at the root, every other one under its primary subtag (`/hu/`, `/sr/`) or its
`meta.x-path`, which also names its downloads (`…_SR.pdf`). The build
checks every file and compares the languages: identifiers, dates, company names, URLs, technologies, levels and the
number of items must match the default language's, so a translation cannot drift. Plain JSON Resume (schema v1.2.1) works (see
[`samples/plain-json-resume`](samples/plain-json-resume)); the optional `x-` extensions add identifiers, a tagline,
a contact link, availability, strengths, position profiles (`?focus=` and tailored documents), the one-page view and
aliases for job ad matching. They are documented on the types in `Sipos.Resume.Core.Content`.
Projects whose names differ across languages must provide the same explicit `x-id` in every language; otherwise
their identifiers are generated from their names and start dates.
Standard sections preserved in the JSON downloads are validated too, including volunteer work, publications,
interests, references and education courses and scores.
HTTP and HTTPS content URLs must not contain user-info or credentials, including in preserved and extension fields.
This check also covers URLs embedded in prose and Markdown. A language's optional `meta.x-path` is limited to
32 lowercase ASCII letters, digits and hyphens.

**No e-mail address in the content.** A file with an e-mail address anywhere is refused: a published address is
harvested. An address may appear only in the PDFs, drawn as an image, from the `RESUME_CONTACT_EMAIL` environment
variable; the build checks that no other file carries it.
PDFs without an e-mail image declare PDF/UA-1 and PDF/A-3a conformance. When the address is drawn as an image,
the PDF declares PDF/A-3b only: keeping the address out of text also prevents screen readers from reading it,
so these files do not claim PDF/UA accessibility. The contact link remains available as text.

**Packages.** The engine's packages are published to GitHub Packages, next to the operandor design system's
(`Operandor.*`) that the theme depends on; that feed needs a token with `read:packages` even to read, and the operandor
packages need read access granted to your repository. Map both prefixes to it and keep everything else on nuget.org:

```xml
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/sipos-andor/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="github">
      <package pattern="Sipos.Resume.*" />
      <package pattern="Operandor.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

NuGet reads the token from `NuGetPackageSourceCredentials_github` (`Username=<you>;Password=<token>`); in GitHub Actions
that is `Username=${{ github.actor }};Password=${{ secrets.GITHUB_TOKEN }}`.

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

Builds reject symbolic links and junctions in output and protected folder paths (content, assets, program, working
and home folders), including linked parent components, before preparing the output.
Asset copying skips nested symbolic links and junctions, including links to individual files, so linked content
outside the asset tree is not published and directory-link cycles are not followed.

`RESUME_ANALYTICS_TOKEN` turns on Cloudflare Web Analytics (cookieless). The output is a static folder for any host;
the pages carry their Content-Security-Policy as a `meta` element, so GitHub Pages needs no headers. The build ends
by verifying the site: every page, download and machine-readable file, every local link, each page's language,
canonical URL, `hreflang` alternates and policy, and that no file holds an e-mail address as text.

## Developing

The operandor design system comes from GitHub Packages, which needs a token even to read. Give NuGet a personal
access token with `read:packages` through an environment variable, so it is never written into `nuget.config` or any
other file of the repository (read without echo, it stays out of the shell history too):

```sh
read -rs GITHUB_PACKAGES_TOKEN
export NuGetPackageSourceCredentials_github="Username=<you>;Password=$GITHUB_PACKAGES_TOKEN"
```

`dotnet test Sipos.Resume.slnx` runs every test; the theme's script is tested with Jint against the C# on shared
vectors. CI builds the sample site end to end. A tag `vX.Y.Z` on `main` publishes the packages (MinVer).

## Licence

MIT. `Sipos.Resume.Documents.Pdf` uses [QuestPDF](https://www.questpdf.com), which has its own licence: its Community
licence is free for individuals and organisations under USD 1M annual revenue; check it before you use the package. The bundled IBM Plex fonts
(SIL Open Font License) and the tests' copy of the JSON Resume schema (MIT) keep their own licences, listed in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
