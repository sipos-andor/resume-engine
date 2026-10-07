# ADR 0002: Clean Architecture principles with a minimal number of projects

- Status: accepted
- Date: 2026-10-07

## Context

The engine turns JSON Resume files into a static site, documents and machine-readable files. It is built once per
commit, in CI, and runs no server; nothing is stored or changed after the build. It is shipped as packages so other
CVs can use it.

Ardalis' Clean Architecture template (Core, UseCases, Infrastructure, Web, with Mediator, Specification and EF Core)
and its Minimal Clean Architecture template (one web project, vertical slices, folder rules checked by NsDepCop) were
both considered. Both are built for ASP.NET Core APIs with a database.

## Decision

Follow the principles, not the templates:

- **Dependency inversion.** `Sipos.Resume.Core` holds the domain, its rules and the ports (`IResumeSource`,
  `IPageRenderer`, `IDocumentWriter`, `IArtifactSink`) and depends on nothing else.
- **Use cases** live in `Sipos.Resume.Generation` (`ValidateContent`, `BuildSite`, `ExportDocuments`) with the adapters
  that have no licence or platform reason to live apart.
- **Adapters** get a project of their own only where a package boundary pays for itself:
  `Sipos.Resume.Documents.Pdf` (QuestPDF's licence must not reach a consumer who renders no PDF) and
  `Sipos.Resume.Theme.Operandor` (a look built on the private operandor design system, replaceable by another theme).
- **Inside a project, feature folders** (vertical slices: `Timeline/`, `Search/`, `Matching/`, `Documents/`, `Seo/`…)
  instead of technical layers.
- **No Mediator, Specification, repositories or EF Core**: there is no request pipeline, no query and no database.
- **Architecture tests** (ArchUnitNET) check the dependency direction, so the boundaries hold without relying on
  folder discipline alone.

## Consequences

- Four packages instead of one, each with a reason: the Core runs in a browser (the Studio) as well as at build time;
  PDF output carries its own licence; the theme carries the operandor brand.
- A consumer composes the engine in a few lines (`ResumeGenerator.Create(args).UseOperandorTheme().UsePdf()`).
- New output formats are new `IDocumentWriter` adapters; a new delivery mode is a new `IPageRenderer`.

## Considered

- The full Clean Architecture template: Mediator, Specification and a UseCases project add ceremony a build-time
  transformation has no use for.
- The Minimal template as is: one project would ship QuestPDF and OpenXml to every consumer and could not run the
  Core in WebAssembly without them.
