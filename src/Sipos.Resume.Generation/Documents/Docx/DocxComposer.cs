using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Sipos.Resume.Generation.Documents.Docx;

/// <summary>Appends styled paragraphs, runs and links to a document's body.</summary>
/// <param name="main">The main document part, which holds the hyperlink relationships.</param>
/// <param name="body">The body the paragraphs go to.</param>
/// <param name="firstLink">The number of the first hyperlink relationship; lower numbers belong to the other parts.</param>
internal sealed class DocxComposer(MainDocumentPart main, Body body, int firstLink)
{
    private int _nextLink = firstLink;

    public Body Body => body;

    /// <summary>Appends a paragraph of a style; <see langword="null"/> items are left out.</summary>
    /// <remarks>
    /// The content is an array, not a <c>params IEnumerable</c>: an <see cref="OpenXmlElement"/> is itself an
    /// enumerable of its children, so a single run would bind as the collection of its own text.
    /// </remarks>
    public Paragraph Add(string style, params OpenXmlElement?[] content)
    {
        var paragraph = Paragraph(style, content);
        body.Append(paragraph);
        return paragraph;
    }

    /// <summary>Appends any block, such as a table.</summary>
    public void Add(OpenXmlElement block) => body.Append(block);

    /// <summary>Appends one paragraph for every line of a text; a text from a content file may hold several.</summary>
    public void AddText(string style, string? text)
    {
        foreach (var line in Lines(text))
        {
            Add(style, Text(line));
        }
    }

    /// <summary>A paragraph of a style that is not appended yet, such as one for a table cell.</summary>
    public static Paragraph Paragraph(string style, params OpenXmlElement?[] content)
    {
        var paragraph = new Paragraph(new ParagraphProperties(new ParagraphStyleId { Val = style }));
        paragraph.Append(content.OfType<OpenXmlElement>());
        return paragraph;
    }

    /// <summary>A run of text, optionally in a character style.</summary>
    public static Run Text(string text, string? style = null)
    {
        var run = new Run();
        if (style is not null)
        {
            run.Append(new RunProperties(new RunStyle { Val = style }));
        }

        run.Append(new Text(text) { Space = text.Length > 0 && (char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1])) ? SpaceProcessingModeValues.Preserve : null });
        return run;
    }

    public static Run Tab() => new(new TabChar());

    public static Run Break() => new(new Break());

    /// <summary>A link to an address, or plain text when the address is not a web address.</summary>
    /// <remarks>
    /// Decision: only <c>http</c> and <c>https</c> addresses become hyperlinks.
    /// Why: a document must never carry a <c>mailto:</c> link, and no other scheme belongs in a CV.
    /// </remarks>
    public OpenXmlElement Link(string url, string text)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return Text(text);
        }

        var id = $"rId{_nextLink++}";
        main.AddHyperlinkRelationship(uri, true, id);
        return new Hyperlink(Text(text, StyleIds.Hyperlink)) { Id = id, History = true };
    }

    /// <summary>An address as a reader writes it: without the scheme and the trailing slash.</summary>
    public static string Display(string url)
    {
        var text = url;
        foreach (var scheme in new[] { "https://", "http://" })
        {
            if (text.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            {
                text = text[scheme.Length..];
                break;
            }
        }

        return text.TrimEnd('/');
    }

    /// <summary>The non-empty lines of a text.</summary>
    public static IEnumerable<string> Lines(string? text) =>
        (text ?? "").Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0);
}
