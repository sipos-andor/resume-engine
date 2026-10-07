using Sipos.Resume.Core.Dates;

namespace Sipos.Resume.Core.Tests.Dates;

public class PartialDateDays
{
    [Theory]
    [InlineData("2025", "2025-01-01", "2025-12-31")]
    [InlineData("2024-02", "2024-02-01", "2024-02-29")]
    [InlineData("2025-08-14", "2025-08-14", "2025-08-14")]
    public void SpansWholeUnitGivenPrecision(string text, string first, string last)
    {
        var date = PartialDate.Parse(text);

        date.FirstDay.ShouldBe(DateOnly.Parse(first, System.Globalization.CultureInfo.InvariantCulture));
        date.LastDay.ShouldBe(DateOnly.Parse(last, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void OrdersByFirstDay() =>
        new[] { PartialDate.Parse("2025-08"), PartialDate.Parse("2013"), PartialDate.Parse("2025-01-15") }.Order()
            .Select(date => date.ToString()).ShouldBe(["2013", "2025-01-15", "2025-08"]);
}
