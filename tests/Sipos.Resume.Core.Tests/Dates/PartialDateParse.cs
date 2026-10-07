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
    [InlineData("0000")]
    [InlineData("2025-8")]
    [InlineData("2025-13")]
    [InlineData("2023-02-29")]
    [InlineData("2025-08-14T10:00")]
    [InlineData("٢٠٢٥-٠٨-١٤")]
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

    [Theory]
    [InlineData(0, null, null, "Year")]
    [InlineData(10000, null, null, "Year")]
    [InlineData(2025, 0, null, "Month")]
    [InlineData(2025, 13, null, "Month")]
    [InlineData(2025, 2, 0, "Day")]
    [InlineData(2025, 2, 30, "Day")]
    [InlineData(2025, 4, 31, "Day")]
    [InlineData(1, null, null, null)]
    [InlineData(9999, null, null, null)]
    [InlineData(2024, 2, 29, null)]
    [InlineData(9999, 12, 31, null)]
    public void ValidatesComponentsGivenDirectConstruction(int year, int? month, int? day, string? parameterName)
    {
        if (parameterName is null)
        {
            var date = new PartialDate(year, month, day);
            date.Year.ShouldBe(year);
            date.Month.ShouldBe(month);
            date.Day.ShouldBe(day);
            return;
        }

        Should.Throw<ArgumentOutOfRangeException>(() => new PartialDate(year, month, day))
            .ParamName.ShouldBe(parameterName);
    }

    [Fact]
    public void RejectsDayGivenNoMonth()
    {
        Should.Throw<ArgumentException>(() => new PartialDate(2025, Day: 1))
            .ParamName.ShouldBe("Day");
    }
}
