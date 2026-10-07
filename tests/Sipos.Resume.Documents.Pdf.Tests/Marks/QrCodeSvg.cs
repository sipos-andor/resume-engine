using System.Globalization;
using System.Text.RegularExpressions;
using Sipos.Resume.Documents.Pdf.Marks;

namespace Sipos.Resume.Documents.Pdf.Tests.Marks;

public class QrCodeSvg
{
    [Fact]
    public void DrawsSymbolWithoutQuietZoneGivenUrl()
    {
        var svg = QrCode.Svg(new Uri("https://cv.example.com/"), "111111");

        var size = int.Parse(Regex.Match(svg, """viewBox="0 0 (\d+) \1" """.TrimEnd()).Groups[1].Value, CultureInfo.InvariantCulture);
        ((size - 21) % 4).ShouldBe(0);
        svg.ShouldStartWith("<svg");
        svg.ShouldContain("""fill="#111111" d="M0 0h7v1h-7z""");
    }

    [Fact]
    public void DrawsSamePathGivenSameUrl() =>
        QrCode.Svg(new Uri("https://cv.example.com/hu/"), "111111").ShouldBe(QrCode.Svg(new Uri("https://cv.example.com/hu/"), "111111"));
}
