using QuestPDF.Infrastructure;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Documents.Pdf.Marks;

namespace Sipos.Resume.Documents.Pdf.Tests;

public class PdfShareImageWriterWrite
{
    private static readonly PdfShareImageWriter Writer = new(new PdfWriterOptions(LicenseType.Community));

    [Fact]
    public void DrawsPngOf1200By630PixelsGivenCv()
    {
        var png = Writer.Write(SampleDocuments.English(), DocumentTheme.Neutral, new Uri("https://cv.example.com/"));

        png[..8].ShouldBe(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        EmailImage.PngSize(png).ShouldBe((1200, 630));
    }

    [Fact]
    public void DrawsSameBytesGivenSameCv()
    {
        var site = new Uri("https://cv.example.com/");

        Writer.Write(SampleDocuments.Accented(), DocumentTheme.Neutral, site).ShouldBe(Writer.Write(SampleDocuments.Accented(), DocumentTheme.Neutral, site));
    }

    // A theme written for the web may name a font only the browser has.
    [Fact]
    public void FallsBackToPlexGivenUnknownFontFamily()
    {
        var theme = DocumentTheme.Neutral with { SansFamily = "No Such Sans", MonoFamily = "No Such Mono" };

        EmailImage.PngSize(Writer.Write(SampleDocuments.English(), theme, new Uri("https://cv.example.com/"))).ShouldBe((1200, 630));
    }
}
