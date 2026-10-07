using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Insights;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Generation.Tests.Helpers;

/// <summary>Document contexts of the sample CVs.</summary>
internal static class Contexts
{
    public static DocumentContext For(ResumeDocument document, DownloadFormat format, DocumentVariant variant, string? focus = null, string? email = null, JsonResume? source = null)
    {
        var insights = ResumeInsights.Analyze(document, SampleDocuments.Today);
        var download = DownloadCatalog.For(document.Language, "Ann_Example_CV", document.FocusProfiles.Select(profile => profile.Id))
            .FirstOrDefault(spec => spec.Format == format && spec.Variant == variant && spec.FocusId == focus)
            ?? new DownloadSpec(format, variant, focus, $"Ann_Example_CV_{focus}.{format}");
        return new DocumentContext(
            document,
            insights,
            download,
            focus is null ? null : insights.Focus.Single(view => view.Profile.Id == focus),
            new Uri("https://cv.example.com" + document.Language.HomePath),
            DocumentTheme.Neutral,
            email,
            source);
    }
}
