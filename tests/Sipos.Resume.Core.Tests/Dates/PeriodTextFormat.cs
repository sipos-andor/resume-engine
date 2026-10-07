using System.Globalization;
using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Localization;

namespace Sipos.Resume.Core.Tests.Dates;

public class PeriodTextFormat
{
    // Each language writes a month the way its readers do.
    [Theory]
    [InlineData("en", "November 2022")]
    [InlineData("hu", "2022. november")]
    [InlineData("hr", "studeni 2022.")]
    [InlineData("sr-Latn", "novembar 2022.")]
    public void WritesMonthByNameGivenLongStyle(string language, string expected) =>
        PeriodText.Format(new PartialDate(2022, 11), CultureInfo.CreateSpecificCulture(language), DateStyle.Long).ShouldBe(expected);

    [Fact]
    public void WritesDigitsMonthFirstGivenNumericStyle() =>
        PeriodText.Format(new PartialDate(2015, 3, 14), CultureInfo.CreateSpecificCulture("hu"), DateStyle.Numeric).ShouldBe("03/2015");

    [Fact]
    public void WritesYearAloneGivenYearOnly() =>
        PeriodText.Format(new PartialDate(2012), CultureInfo.CreateSpecificCulture("hu"), DateStyle.Long).ShouldBe("2012");

    [Fact]
    public void EndsWithWordForPresentGivenOngoingPeriod() =>
        PeriodText.Format(new DateRange(new PartialDate(2022, 11)), new ResumeLabels(CultureInfo.CreateSpecificCulture("hu")), DateStyle.Long)
            .ShouldBe("2022. november – jelenleg");

    [Fact]
    public void JoinsStartAndEndGivenFinishedPeriod() =>
        PeriodText.Format(new DateRange(new PartialDate(2015, 3), new PartialDate(2022, 10)), new ResumeLabels(CultureInfo.CreateSpecificCulture("en")), DateStyle.Numeric)
            .ShouldBe("03/2015 – 10/2022");

    // A period within one month or year reads as that month or year, not as "2012 – 2012".
    [Fact]
    public void WritesOneDateGivenSameStartAndEnd() =>
        PeriodText.Format(new DateRange(new PartialDate(2012), new PartialDate(2012)), new ResumeLabels(CultureInfo.CreateSpecificCulture("en")), DateStyle.Long)
            .ShouldBe("2012");
}
