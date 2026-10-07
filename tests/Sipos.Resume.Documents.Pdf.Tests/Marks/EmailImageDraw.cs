using QuestPDF.Infrastructure;
using Sipos.Resume.Documents.Pdf.Fonts;
using Sipos.Resume.Documents.Pdf.Marks;

namespace Sipos.Resume.Documents.Pdf.Tests.Marks;

public class EmailImageDraw
{
    public EmailImageDraw()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        PlexFonts.EnsureRegistered();
    }

    [Fact]
    public void DrawsOneLineAtFourPixelsPerPointGivenAddress()
    {
        var image = EmailImage.Draw(PdfSamples.Address, PlexFonts.Sans, 10, 1.3f, "222222");

        var (width, height) = EmailImage.PngSize(image.Png);
        image.Width.ShouldBe(width / 4f);
        image.Height.ShouldBe(height / 4f);
        image.Height.ShouldBe(13, 0.5);
        image.Width.ShouldBeInRange(80, 160);
    }

    // QuestPDF quotes the text it fails to draw; the address must not reach a log through the exception.
    [Fact]
    public void ThrowsWithoutAddressGivenUnknownFont()
    {
        var exception = Should.Throw<InvalidOperationException>(() => EmailImage.Draw(PdfSamples.Address, "No Such Sans", 10, 1.3f, "222222"));

        exception.ToString().ShouldNotContain("privacy.probe");
        exception.InnerException.ShouldBeNull();
    }
}
