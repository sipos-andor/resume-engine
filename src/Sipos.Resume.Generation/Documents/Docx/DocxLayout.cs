using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Localization;
using Sipos.Resume.Core.Model;
using static Sipos.Resume.Generation.Documents.Docx.DocxComposer;

namespace Sipos.Resume.Generation.Documents.Docx;

/// <summary>Writes the body of a document in one layout: the header block, then the outline's sections in order.</summary>
/// <remarks>
/// The outline decides what the document says; a layout only decides how each part looks. Neither layout ever writes
/// <see cref="DocumentContext.ContactEmail"/>: a DOCX is edited, forwarded and parsed, and an address in it is out of
/// the owner's hands, so the build's address reaches the PDF alone.
/// </remarks>
/// <param name="docx">Where the paragraphs go.</param>
/// <param name="context">The CV and the download.</param>
/// <param name="outline">What the document says.</param>
internal abstract class DocxLayout(DocxComposer docx, DocumentContext context, DocumentOutline outline)
{
    /// <summary>The dash between a name and its detail, the same as between the dates of a period.</summary>
    protected const string Dash = PeriodText.Separator;

    protected DocxComposer Docx => docx;

    protected DocumentContext Context => context;

    protected DocumentOutline Outline => outline;

    protected ResumeLabels Labels => outline.Labels;

    protected Person Person => context.Document.Person;

    public void Write()
    {
        Docx.Add(StyleIds.Title, Text(Person.Name));
        if (Person.Title is { } title)
        {
            Docx.Add(StyleIds.Subtitle, Text(title));
        }

        if (Person.Tagline is { } tagline)
        {
            Docx.Add(StyleIds.Tagline, Text(tagline));
        }

        if (outline.Focus is { } focus)
        {
            Docx.Add(StyleIds.FocusLine, Text($"{Labels.Focus}: ", StyleIds.Muted), Text(focus.Profile.Label, StyleIds.Strong));
        }

        WriteContact();
        foreach (var section in outline.Sections)
        {
            Docx.Add(StyleIds.Heading1, Text(outline.Heading(section)));
            WriteSection(section);
        }

        // Word repairs a body whose last block is a table, such as a skills grid at the end.
        if (Docx.Body.LastChild is Table)
        {
            Docx.Add(StyleIds.Normal);
        }
    }

    protected abstract void WriteContact();

    protected abstract void WriteSection(DocumentSection section);

    /// <summary>The online CV as a link whose text has no scheme.</summary>
    protected OpenXmlElement PageLink() => Docx.Link(context.PageUrl.AbsoluteUri, Display(context.PageUrl.ToString()));

    /// <summary>Joins inline items, each one or more runs, with a separator run.</summary>
    protected static OpenXmlElement[] Joined(IEnumerable<OpenXmlElement[]> items, Func<OpenXmlElement> separator) =>
        [.. items.SelectMany((item, index) => index == 0 ? item : item.Prepend(separator()))];

    protected string Period(DateRange? period) => period is { } value ? outline.Period(value) : "";

    /// <summary>The name of a project and its client, such as <c>Portal – Globex</c>.</summary>
    protected static string ProjectTitle(Engagement engagement) =>
        engagement.Client is null ? engagement.Name : $"{engagement.Name}{Dash}{engagement.Client}";

    /// <summary>The degree and field of a study, such as <c>BSc, Informatics</c>, or the school when neither is known.</summary>
    protected static string StudyTitle(Study study) =>
        string.Join(", ", new[] { study.StudyType, study.Area }.OfType<string>()) is { Length: > 0 } title ? title : study.Institution;
}
