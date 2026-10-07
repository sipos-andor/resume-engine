using System.Globalization;
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
}
