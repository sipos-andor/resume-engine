using System.Buffers.Binary;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Sipos.Resume.Documents.Pdf.Marks;

/// <summary>An e-mail address drawn as a PNG, to be placed in a PDF at the size of the text around it.</summary>
/// <param name="Png">The image.</param>
/// <param name="Width">The width in points at which it matches the text it was drawn as.</param>
/// <param name="Height">The height in points, one line of that text.</param>
/// <remarks>
/// <para>
/// Decision: the address reaches the PDF only as pixels: drawn by QuestPDF into a separate PNG, embedded with an
/// alternative text that names what it is (<c>E-mail address</c>), never what it says. No text, link, metadata or
/// message of the document holds it.
/// Why: a PDF's text layer, links and metadata are what harvesters read; an address published as text is spam within
/// days. A reader still sees it and can type it.
/// Considered: a <c>mailto:</c> link (harvested first), obfuscated text such as <c>name [at] domain</c> (read by every
/// harvester), and leaving it out (a recruiter who prints the CV has no way to write).
/// </para>
/// <para>
/// Decision: 288 DPI and a transparent background.
/// Why: four pixels per point stays sharp when printed and zoomed; with an alpha channel QuestPDF keeps the image
/// lossless, while an opaque one would be JPEG-encoded, which smears thin strokes.
/// </para>
/// </remarks>
internal sealed record EmailImage(byte[] Png, float Width, float Height)
{
    private const int Dpi = 288;

    /// <summary>Draws an address in one line of text.</summary>
    /// <param name="address">The address; it appears in no exception this method throws.</param>
    /// <param name="family">The font family.</param>
    /// <param name="size">The font size in points.</param>
    /// <param name="lineHeight">The line height as a multiple of the font size.</param>
    /// <param name="color">The text colour, six hex digits.</param>
    /// <exception cref="InvalidOperationException">QuestPDF could not draw the address.</exception>
    public static EmailImage Draw(string address, string family, float size, float lineHeight, string color)
    {
        try
        {
            var png = Document
                .Create(document => document.Page(page =>
                {
                    page.MinSize(new PageSize(1, 1));
                    page.MaxSize(new PageSize(2000, 200));
                    page.Margin(0);
                    page.PageColor(Colors.Transparent);
                    page.Content().Text(address).FontFamily(family).FontSize(size).LineHeight(lineHeight).FontColor(Color.FromHex(color))
                        .DisableFontFeature(FontFeatures.StandardLigatures);
                }))
                .GenerateImages(new ImageGenerationSettings { ImageFormat = ImageFormat.Png, RasterDpi = Dpi, UseTransparentBackground = true })
                .Single();
            var (width, height) = PngSize(png);
            return new EmailImage(png, width * 72f / Dpi, height * 72f / Dpi);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // QuestPDF quotes the text it failed to draw; the address must not reach a log, so neither does its message.
            throw new InvalidOperationException($"The contact e-mail address could not be drawn as an image ({exception.GetType().Name}).");
        }
    }

    /// <summary>Reads the pixel size from a PNG's header chunk.</summary>
    /// <param name="png">The PNG's bytes.</param>
    public static (int Width, int Height) PngSize(ReadOnlySpan<byte> png) =>
        png.Length >= 24 && png[12..16].SequenceEqual("IHDR"u8)
            ? (BinaryPrimitives.ReadInt32BigEndian(png[16..20]), BinaryPrimitives.ReadInt32BigEndian(png[20..24]))
            : throw new ArgumentException("Not a PNG image.", nameof(png));
}
