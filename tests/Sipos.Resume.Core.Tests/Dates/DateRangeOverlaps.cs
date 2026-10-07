using Sipos.Resume.Core.Dates;

namespace Sipos.Resume.Core.Tests.Dates;

public class DateRangeOverlaps
{
    private static readonly DateOnly Today = new(2026, 10, 7);

    [Theory]
    [InlineData("2022-11", null, "2023-03", "2023-05", true)]
    [InlineData("2022-11", "2024-09", "2024-09", "2025-05", true)]
    [InlineData("2022-11", "2024-08", "2024-10", "2025-05", false)]
    [InlineData("2013-04", "2014-07", "2026-01", null, false)]
    public void SharesDayGivenPeriods(string start, string? end, string otherStart, string? otherEnd, bool overlaps)
    {
        var first = new DateRange(PartialDate.Parse(start), end is null ? null : PartialDate.Parse(end));
        var second = new DateRange(PartialDate.Parse(otherStart), otherEnd is null ? null : PartialDate.Parse(otherEnd));

        first.Overlaps(second, Today).ShouldBe(overlaps);
        second.Overlaps(first, Today).ShouldBe(overlaps);
    }

    [Fact]
    public void EndsTodayGivenOngoingPeriod() => new DateRange(PartialDate.Parse("2022-11")).LastDay(Today).ShouldBe(Today);
}
