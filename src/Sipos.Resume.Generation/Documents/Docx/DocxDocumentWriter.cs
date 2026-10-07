using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Sipos.Resume.Core.Artifacts;

namespace Sipos.Resume.Generation.Documents.Docx;

/// <summary>
/// Writes the CV as a Word document in the designed or the ATS layout: real paragraph styles, a bound bullet
/// numbering, the CV's language for spell checking, hyperlinks for the online CV, the contact page and the profiles.
/// </summary>
/// <remarks>
/// <para>
/// The same context always gives the same bytes: relationship identifiers are fixed, the core properties are dated
/// by the content's last change, and the package is zipped again in a fixed order with a fixed timestamp.
/// </para>
/// <para>
/// A DOCX never carries an e-mail address: <see cref="DocumentContext.ContactEmail"/> is ignored, and only web
/// addresses become links.
/// </para>
/// </remarks>
public sealed class DocxDocumentWriter : IDocumentWriter
{
    private const string MainContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";

    /// <summary>How many technologies the keywords property lists.</summary>
    private const int KeywordCount = 12;

    /// <inheritdoc/>
    public DownloadFormat Format => DownloadFormat.Docx;

    /// <inheritdoc/>
    public bool Supports(DocumentVariant variant) => variant is DocumentVariant.Designed or DocumentVariant.Ats;

    /// <inheritdoc/>
    public void Write(DocumentContext context, Stream output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);
        if (!Supports(context.Download.Variant))
        {
            throw new ArgumentException($"The DOCX writer has no {context.Download.Variant} layout.", nameof(context));
        }

        var outline = DocumentOutline.Of(context);
        var look = outline.Variant == DocumentVariant.Ats ? DocxLook.Ats : DocxLook.Designed(context.Theme);
        var language = context.Document.Language.Culture.Name;
        var stamp = DocxArchive.StampOf(context.Document.LastModified);

        using var package = new MemoryStream();
        using (var document = WordprocessingDocument.Create(package, WordprocessingDocumentType.Document))
        {
            var main = document.AddNewPart<MainDocumentPart>(MainContentType, "rId1");
            main.AddNewPart<StyleDefinitionsPart>("rId1").Styles = DocxStyles.Build(look, language);
            main.AddNewPart<NumberingDefinitionsPart>("rId2").Numbering = DocxStyles.Bullets(look);
            main.AddNewPart<DocumentSettingsPart>("rId3").Settings = DocxParts.Settings();
            main.AddNewPart<FontTablePart>("rId4").Fonts = DocxParts.FontTable(look);

            var body = new Body();
            var composer = new DocxComposer(main, body, firstLink: 5);
            DocxLayout layout = look.IsDesigned
                ? new DesignedDocxLayout(composer, context, outline, look)
                : new AtsDocxLayout(composer, context, outline);
            layout.Write();
            body.Append(new SectionProperties(
                new PageSize { Width = DocxLook.PageWidth, Height = DocxLook.PageHeight },
                new PageMargin
                {
                    Top = look.VerticalMargin,
                    Bottom = look.VerticalMargin,
                    Left = (uint)look.Margin,
                    Right = (uint)look.Margin,
                    Header = 567,
                    Footer = 567,
                    Gutter = 0,
                }));

            var root = new Document(body);
            root.AddNamespaceDeclaration("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
            root.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            main.Document = root;

            using (var core = document.AddNewPart<CoreFilePropertiesPart>("rId2").GetStream(FileMode.Create))
            {
                DocxParts.WriteCoreProperties(core, CoreProperties(context, outline, language, stamp));
            }

            document.AddNewPart<ExtendedFilePropertiesPart>("rId3").Properties = DocxParts.AppProperties();
        }

        DocxArchive.Normalize(package, output, stamp);
    }

    private static DocxCoreProperties CoreProperties(DocumentContext context, DocumentOutline outline, string language, DateOnly stamp)
    {
        var person = context.Document.Person;
        var title = $"{person.Name} – {outline.Labels.CurriculumVitae}";
        if (outline.Focus is { } focus)
        {
            title += $" – {focus.Profile.Label}";
        }

        var keywords = context.Insights.Technologies.Technologies.Take(KeywordCount).Select(technology => technology.Name).ToList();
        return new DocxCoreProperties(title, person.Name, person.Title, keywords, language, stamp);
    }
}
