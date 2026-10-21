using System.Globalization;
using System.Text.RegularExpressions;
using Sipos.Resume.Core.Insights;
using Sipos.Resume.Theme.Operandor.Rendering;

namespace Sipos.Resume.Theme.Operandor.Tests.Rendering;

public class TimelineSvgDraw
{
    // A Hungarian page's culture writes a decimal comma, which would break every coordinate.
    [Fact]
    public void WritesInvariantNumbersGivenCultureWithDecimalComma()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("hu-HU");
        try
        {
            var timeline = ResumeInsights.Analyze(SampleDocuments.English(), SampleDocuments.Today).Timeline;

            var svg = TimelineSvg.Draw(timeline, id => "#" + id, period => period.ToString());

            svg.ShouldNotMatch(@"=""\d+,\d");
            svg.ShouldContain("data-cv-bar=\"acme\"");
            svg.ShouldContain("aria-hidden=\"true\"");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void EscapesLabelsGivenMarkupInName()
    {
        var timeline = ResumeInsights.Analyze(SampleDocuments.English(), SampleDocuments.Today).Timeline;
        timeline = timeline with { Bars = [timeline.Bars[0] with { Label = "<b>&</b>" }] };

        TimelineSvg.Draw(timeline, id => "#" + id, period => "x").ShouldContain("&lt;b&gt;&amp;&lt;/b&gt;");
    }

    // The bar's text formats the CV's own period, so a year alone gains no invented month.
    [Fact]
    public void TitlesBarWithCvPeriodGivenYearOnlyDates()
    {
        var timeline = ResumeInsights.Analyze(SampleDocuments.English(), SampleDocuments.Today).Timeline;
        var period = new Sipos.Resume.Core.Dates.DateRange(Sipos.Resume.Core.Dates.PartialDate.Parse("2015"), Sipos.Resume.Core.Dates.PartialDate.Parse("2018"));
        timeline = timeline with { Bars = [timeline.Bars[0] with { Period = period }] };

        TimelineSvg.Draw(timeline, id => "#" + id, given => given == period ? "2015 – 2018" : "invented").ShouldContain(": 2015 – 2018</title>");
    }

    [Fact]
    public void DrawsOngoingBarGivenMaximumDate()
    {
        var timeline = ResumeInsights.Analyze(SampleDocuments.English(), DateOnly.MaxValue).Timeline;

        var svg = TimelineSvg.Draw(timeline, id => "#" + id, period => period.ToString());

        timeline.Bars.ShouldContain(bar => bar.End == DateOnly.MaxValue);
        svg.ShouldContain("cv-timeline__bar--ongoing");
    }

    // A viewBox would scale the text with the chart, down to 4px on a phone; percentages stretch only the bars.
    [Fact]
    public void PlacesBarsInPercentGivenAnyWidth()
    {
        var timeline = ResumeInsights.Analyze(SampleDocuments.English(), SampleDocuments.Today).Timeline;

        var svg = TimelineSvg.Draw(timeline, id => "#" + id, period => period.ToString());

        svg.ShouldNotContain("viewBox");
        svg.ShouldMatch(@"<svg [^>]*height=""\d+(\.\d)?""");
        var bars = Regex.Matches(svg, @"<rect [^>]*x=""([^""]+)""[^>]*width=""([^""]+)""");
        bars.ShouldNotBeEmpty();
        bars.ShouldAllBe(bar => bar.Groups[1].Value.EndsWith('%') && bar.Groups[2].Value.EndsWith('%'));
    }

    // A label shows only where it fits its bar on the narrowest chart that shows labels; a project of two months has none.
    [Fact]
    public void LabelsOnlyBarsWideEnoughGivenNarrowestLabelledChart()
    {
        var timeline = ResumeInsights.Analyze(SampleDocuments.English(), SampleDocuments.Today).Timeline;

        var svg = TimelineSvg.Draw(timeline, id => "#" + id, period => period.ToString());

        svg.ShouldContain(">Initech</text>");
        svg.ShouldNotContain(">Portal</text>");
    }
}
