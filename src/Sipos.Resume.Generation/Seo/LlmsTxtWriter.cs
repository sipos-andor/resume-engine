using System.Text;
using Sipos.Resume.Core.Localization;
using Sipos.Resume.Core.Site;
using Sipos.Resume.Generation.Documents.Markdown;

namespace Sipos.Resume.Generation.Seo;

/// <summary>
/// Writes the <c>llms.txt</c> files (llmstxt.org): one per language that points a language model to the CV's
/// Markdown, JSON Resume and downloads, and <c>llms-full.txt</c> with the whole CV in every language.
/// </summary>
/// <remarks>
/// Decision: the root's <c>llms.txt</c> is the default language's, and each language has its own under its path.
/// Why: a model that starts at the root finds the CV in the site's main language and every other version one link
/// away, in the same shape as the pages' hreflang links.
/// </remarks>
internal static class LlmsTxtWriter
{
    /// <summary>The path of the file with the whole CV in every language.</summary>
    public const string FullPath = "/llms-full.txt";

    /// <summary>Returns the llms.txt of one language's page.</summary>
    /// <param name="page">The page.</param>
    /// <param name="pages">Every page of the site, the default language first.</param>
    public static string Write(SitePage page, IReadOnlyList<SitePage> pages)
    {
        ArgumentNullException.ThrowIfNull(page);
        var person = page.Document.Person;
        var labels = new ResumeLabels(page.Document.Language.Culture);
        var settings = page.Settings;
        var text = new StringBuilder();

        text.Append("# ").AppendLine(E(person.Name)).AppendLine();
        var title = string.Join(" · ", new[] { person.Title, person.Tagline }.OfType<string>().Select(E));
        text.Append("> ").AppendLine(title.Length > 0 ? title : E(labels.CurriculumVitae)).AppendLine();
        if (person.Summary is { } summary)
        {
            text.AppendLine(E(summary)).AppendLine();
        }

        text.Append("## ").AppendLine(E(labels.CurriculumVitae)).AppendLine();
        text.Append("- [").Append(E(person.Name)).Append(" – ").Append(E(labels.CurriculumVitae)).Append(" (Markdown)](").Append(settings.Url(page.MarkdownPath).AbsoluteUri).AppendLine(")");
        text.Append("- [JSON Resume](").Append(settings.Url(page.JsonResumePath).AbsoluteUri).AppendLine(")");
        text.Append("- [HTML](").Append(settings.Url(page.Path).AbsoluteUri).AppendLine(")");
        if (page.Document.Language.IsDefault)
        {
            text.Append("- [").Append(E(labels.CurriculumVitae)).Append(", ").Append(string.Join(", ", pages.Select(other => E(other.Document.Language.Endonym))))
                .Append(" (Markdown)](").Append(settings.Url(FullPath).AbsoluteUri).AppendLine(")");
        }

        text.AppendLine();
        if (page.Downloads.Count > 0)
        {
            text.Append("## ").AppendLine(E(labels.Downloads)).AppendLine();
            foreach (var download in page.Downloads)
            {
                var focus = download.FocusId is null ? null : page.Document.FocusProfiles.FirstOrDefault(profile => profile.Id == download.FocusId)?.Label;
                text.Append("- [").Append(E(download.Describe(focus))).Append("](").Append(settings.Url(download.Path).AbsoluteUri).AppendLine(")");
            }

            text.AppendLine();
        }

        if (person.Contact is not null || person.Profiles.Count > 0)
        {
            text.Append("## ").AppendLine(E(labels.Contact)).AppendLine();
            if (person.Contact is { } contact)
            {
                text.Append("- [").Append(E(contact.Label)).Append("](").Append(MarkdownText.Destination(contact.Url)).AppendLine(")");
            }

            foreach (var profile in person.Profiles)
            {
                text.Append("- [").Append(E(profile.Network)).Append("](").Append(MarkdownText.Destination(profile.Url)).AppendLine(")");
            }

            text.AppendLine();
        }

        var others = pages.Where(other => other.Path != page.Path).ToList();
        if (others.Count > 0)
        {
            text.Append("## ").AppendLine(E(labels.OtherLanguages)).AppendLine();
            foreach (var other in others)
            {
                text.Append("- [").Append(E(other.Document.Language.Endonym)).Append("](").Append(settings.Url(other.LlmsTxtPath).AbsoluteUri).AppendLine(")");
            }

            text.AppendLine();
        }

        // AppendLine writes the system's line ending; a line feed everywhere gives every build the same bytes.
        return text.ToString().ReplaceLineEndings("\n").TrimEnd('\n') + "\n";
    }

    /// <summary>Returns llms-full.txt: the Markdown of every language, the default first, separated by rules.</summary>
    /// <param name="markdowns">Each language's Markdown, the default first.</param>
    public static string WriteFull(IEnumerable<string> markdowns) => string.Join("\n---\n\n", markdowns);

    private static string E(string text) => MarkdownText.Escape(text);
}
