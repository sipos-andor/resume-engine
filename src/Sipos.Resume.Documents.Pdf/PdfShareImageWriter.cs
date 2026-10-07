using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Model;
using Sipos.Resume.Documents.Pdf.Fonts;
using Sipos.Resume.Documents.Pdf.Marks;

namespace Sipos.Resume.Documents.Pdf;

/// <summary>
/// Draws the Open Graph image of a CV with QuestPDF: 1200 by 630 pixels, the name, title and tagline in the theme's
/// colours and fonts, and the site's host name.
/// </summary>
/// <remarks>
/// Decision: the image is a 1200 by 630 point page rasterised at 72 DPI, so one point is one pixel.
/// Why: the PDF writer already has the fonts, colours and marks; a second drawing library for one image would be a
/// second way for the brand to drift.
/// </remarks>
public sealed class PdfShareImageWriter : IShareImageWriter
{
    private const float Width = 1200;
    private const float Height = 630;
    private const float Inset = 88;

    /// <summary>Creates the writer, applies the QuestPDF licence and registers the embedded fonts.</summary>
    /// <param name="options">The options, with the QuestPDF licence the host renders under.</param>
    public PdfShareImageWriter(PdfWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        QuestPDF.Settings.License = options.License;
        PlexFonts.EnsureRegistered();
    }

    /// <inheritdoc/>
    public byte[] Write(ResumeDocument document, DocumentTheme theme, Uri site)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(site);
        var look = PlexFonts.Resolve(theme);
        var person = document.Person;
        return Document
            .Create(container => container.Page(page =>
            {
                page.Size(Width, Height, Unit.Point);
                page.Margin(0);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style.FontFamily(look.SansFamily).FontColor(Color.FromHex(look.Body)).DisableFontFeature(FontFeatures.StandardLigatures));
                page.Background().AlignBottom().Height(14).Background(Color.FromHex(look.Accent));
                page.Content().PaddingHorizontal(Inset).PaddingTop(92).Column(column =>
                {
                    column.Item().Width(57).Height(57 / VectorMarks.ChevronRatio).Svg(VectorMarks.Chevron(look.Accent));
                    column.Item().PaddingTop(36).Text(person.Name).FontSize(70).SemiBold().LineHeight(1.1f).FontColor(Color.FromHex(look.Heading)).ClampLines(1);
                    if (person.Title is { } title)
                    {
                        column.Item().PaddingTop(14).Text(title).FontSize(34).Medium().LineHeight(1.2f).ClampLines(2);
                    }

                    if (person.Tagline is { } tagline)
                    {
                        column.Item().PaddingTop(12).Text(tagline).FontSize(25).LineHeight(1.3f).FontColor(Color.FromHex(look.Muted)).ClampLines(2);
                    }
                });
                page.Footer().PaddingHorizontal(Inset).PaddingBottom(52).Text(site.Host).FontSize(26).Medium().FontColor(Color.FromHex(look.Accent));
            }))
            .GenerateImages(new ImageGenerationSettings { ImageFormat = ImageFormat.Png, RasterDpi = 72 })
            .First();
    }
}
