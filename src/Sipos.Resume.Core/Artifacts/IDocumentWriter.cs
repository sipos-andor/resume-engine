using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Focus;
using Sipos.Resume.Core.Insights;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Artifacts;

/// <summary>What a document writer needs to write one file.</summary>
/// <param name="Document">The CV in one language.</param>
/// <param name="Insights">What the engine derived from it.</param>
/// <param name="Download">The file to write: format, layout, focus and name.</param>
/// <param name="Focus">The position profile's view for a tailored document, or <see langword="null"/>.</param>
/// <param name="PageUrl">The CV's page in the document's language, printed in the document and encoded in its QR code.</param>
/// <param name="Theme">The look of a designed document.</param>
/// <param name="ContactEmail">
/// An e-mail address only a PDF shows, drawn as an image; <see langword="null"/> for every other format and when none
/// is configured. It comes from the build's secrets, never from the content.
/// </param>
/// <param name="Source">The content file as read, which the JSON Resume download republishes; <see langword="null"/> when unknown.</param>
public sealed record DocumentContext(
    ResumeDocument Document,
    ResumeInsights Insights,
    DownloadSpec Download,
    FocusView? Focus,
    Uri PageUrl,
    DocumentTheme Theme,
    string? ContactEmail,
    JsonResume? Source = null);

/// <summary>Writes one kind of document. The build calls every writer whose format a download needs.</summary>
public interface IDocumentWriter
{
    /// <summary>The format the writer produces.</summary>
    DocumentFormat Format { get; }

    /// <summary>Whether the writer produces a layout of its format.</summary>
    /// <param name="variant">The layout.</param>
    bool Supports(DocumentVariant variant);

    /// <summary>Writes the document.</summary>
    /// <param name="context">The CV, the file and the options.</param>
    /// <param name="output">Where to write the file's bytes.</param>
    void Write(DocumentContext context, Stream output);
}
