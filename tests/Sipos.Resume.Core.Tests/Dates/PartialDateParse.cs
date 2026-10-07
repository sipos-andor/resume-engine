using Sipos.Resume.Core.Dates;

namespace Sipos.Resume.Core.Tests.Dates;

public class PartialDateParse
{
    [Theory]
    [InlineData("2025", 2025, null, null)]
    [InlineData("2025-08", 2025, 8, null)]
    [InlineData("2024-02-29", 2024, 2, 29)]
    public void ReadsEachFormGivenValidText(string text, int year, int? month, int? day) =>
        PartialDate.Parse(text).ShouldBe(new PartialDate(year, month, day));

    [Theory]
    [InlineData("25")]
    [InlineData("2025-8")]
    [InlineData("2025-13")]
    [InlineData("2023-02-29")]
    [InlineData("2025-08-14T10:00")]
    [InlineData("Aug 2025")]
    [InlineData("")]
    public void RefusesTextGivenOtherForm(string text)
    {
        PartialDate.TryParse(text, out _).ShouldBeFalse();
        Should.Throw<FormatException>(() => PartialDate.Parse(text));
    }

    [Theory]
    [InlineData("2025")]
    [InlineData("2025-08")]
    [InlineData("2025-08-14")]
    public void WritesSameTextGivenParsedDate(string text) => PartialDate.Parse(text).ToString().ShouldBe(text);
}
