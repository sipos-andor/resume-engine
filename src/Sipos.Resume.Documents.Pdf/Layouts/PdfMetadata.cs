using QuestPDF.Infrastructure;
using Sipos.Resume.Core.Artifacts;

namespace Sipos.Resume.Documents.Pdf.Layouts;

/// <summary>The document information and conformance settings both layouts share.</summary>
internal static class PdfMetadata
{
    /// <summary>How many technologies the keywords name.</summary>
    public const int KeywordCount = 12;

    /// <summary>The date a document carries when its content does not say when it last changed.</summary>
    /// <remarks>
    /// Decision: a fixed date, not the clock.
    /// Why: the same content must give the same file, so a build that changed nothing publishes nothing new.
    /// </remarks>
    public static readonly DateTimeOffset UndatedContent = DateTimeOffset.UnixEpoch;

    /// <summary>The software named in the document information.</summary>
    public const string Creator = "Sipos.Resume.Documents.Pdf";

    /// <summary>
    /// PDF/UA-1 and PDF/A-3a without an e-mail image; PDF/A-3b when the address must stay image-only.
    /// </summary>
    /// <param name="hasEmailImage">Whether the PDF includes an address that cannot have equivalent alternative text.</param>
    public static DocumentSettings Settings(bool hasEmailImage)
    {
        var settings = new DocumentSettings
        {
            PDFA_Conformance = hasEmailImage ? PDFA_Conformance.PDFA_3B : PDFA_Conformance.PDFA_3A,
            CompressDocument = true,
        };
        if (!hasEmailImage)
        {
            settings.PDFUA_Conformance = PDFUA_Conformance.PDFUA_1;
        }

        return settings;
    }

    /// <summary>The document information of a CV: title, author, subject, keywords, language and dates.</summary>
    /// <param name="context">The document's context.</param>
    /// <param name="outline">The document's outline.</param>
    public static DocumentMetadata Of(DocumentContext context, DocumentOutline outline)
    {
        var person = context.Document.Person;
        var date = context.Document.LastModified is { } modified ? new DateTimeOffset(modified.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : UndatedContent;
        return new DocumentMetadata
        {
            Title = Title(context, outline),
            Author = person.Name,
            Subject = person.Title,
            Keywords = string.Join(", ", Keywords(context)),
            Creator = Creator,
            Producer = "QuestPDF",
            Language = context.Document.Language.Tag,
            CreationDate = date,
            ModifiedDate = date,
        };
    }

    /// <summary>The title, such as <c>Andor Sípos – CV</c>, with the focus for a tailored document: <c>Andor Sípos – CV – Software architect</c>.</summary>
    /// <param name="context">The document's context.</param>
    /// <param name="outline">The document's outline.</param>
    public static string Title(DocumentContext context, DocumentOutline outline)
    {
        var title = $"{context.Document.Person.Name} – {outline.Labels.CurriculumVitae}";
        return outline.Focus is { } focus ? $"{title} – {focus.Profile.Label}" : title;
    }

    // The focus's skills first for a tailored document, then the technologies the CV names most.
    private static IEnumerable<string> Keywords(DocumentContext context) =>
        (context.Focus?.Profile.Skills ?? [])
            .Concat(context.Insights.Technologies.Technologies.Select(technology => technology.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(KeywordCount);
}
