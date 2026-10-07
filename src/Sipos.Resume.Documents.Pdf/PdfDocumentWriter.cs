using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Validation;
using Sipos.Resume.Documents.Pdf.Fonts;
using Sipos.Resume.Documents.Pdf.Layouts;

namespace Sipos.Resume.Documents.Pdf;

/// <summary>
/// Writes the designed and the ATS PDF of a CV with QuestPDF: A4, PDF/UA-1 and PDF/A-3a without an e-mail image
/// (PDF/A-3b with one), IBM Plex embedded,
/// the same bytes for the same content.
/// </summary>
/// <remarks>
/// What the document says comes from <see cref="DocumentOutline"/>; this writer decides only how it looks. An e-mail
/// address in <see cref="DocumentContext.ContactEmail"/> is drawn as an image and is not written anywhere as text.
/// </remarks>
public sealed class PdfDocumentWriter : IDocumentWriter, IContentCheck
{
    /// <summary>Creates the writer, applies the QuestPDF licence and registers the embedded fonts.</summary>
    /// <param name="options">The options, with the QuestPDF licence the host renders under.</param>
    public PdfDocumentWriter(PdfWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        QuestPDF.Settings.License = options.License;
        PlexFonts.EnsureRegistered();
    }

    /// <inheritdoc/>
    public DownloadFormat Format => DownloadFormat.Pdf;

    /// <inheritdoc/>
    public bool Supports(DocumentVariant variant) => variant is DocumentVariant.Designed or DocumentVariant.Ats;

    /// <inheritdoc/>
    public IEnumerable<ValidationIssue> Check(ResumeEdition edition, DocumentTheme? theme, string? contactEmail) => GlyphCheck.Check(edition, theme, contactEmail);

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">The download is not a PDF.</exception>
    /// <exception cref="InvalidOperationException">The e-mail address could not be drawn, or the identifier could not be made reproducible.</exception>
    public void Write(DocumentContext context, Stream output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);
        if (context.Download.Format != DownloadFormat.Pdf)
        {
            throw new ArgumentException($"The PDF writer cannot write {context.Download.FileName}.", nameof(context));
        }

        var outline = DocumentOutline.Of(context);
        IDocument document = outline.Variant == DocumentVariant.Ats ? new AtsLayout(context, outline) : new DesignedLayout(context, outline);
        // The same content must give the same bytes; a QuestPDF that writes the identifier otherwise fails the build
        // here rather than publishing changed downloads on every run.
        var pdf = ReproducibleId.Apply(document.GeneratePdf())
            ?? throw new InvalidOperationException($"QuestPDF wrote {context.Download.FileName} with an identifier in a form the engine cannot make reproducible.");
        output.Write(pdf);
    }
}
