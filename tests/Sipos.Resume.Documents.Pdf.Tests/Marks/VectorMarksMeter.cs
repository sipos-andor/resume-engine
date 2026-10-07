using System.Text.RegularExpressions;
using Sipos.Resume.Documents.Pdf.Marks;

namespace Sipos.Resume.Documents.Pdf.Tests.Marks;

public class VectorMarksMeter
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    [InlineData(7, 5)]
    [InlineData(0, 0)]
    public void FillsOneStepPerLevelGivenRating(int rating, int filled)
    {
        var svg = VectorMarks.Meter(rating, "2D7612", "D6D9D3");

        Regex.Count(svg, "<rect").ShouldBe(VectorMarks.MeterSteps);
        Regex.Count(svg, "fill=\"#2D7612\"").ShouldBe(filled);
    }
}
