using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Model;
using Sipos.Resume.Core.Tests.Helpers;
using Sipos.Resume.Core.Timeline;

namespace Sipos.Resume.Core.Tests.Timeline;

public class TimelineModelBuild
{
    [Fact]
    public void SpansFromFirstYearToToday()
    {
        var timeline = TimelineModel.Build(Documents.English(), Documents.Today);

        timeline.Start.ShouldBe(new DateOnly(2015, 1, 1));
        timeline.End.ShouldBe(Documents.Today);
    }

    [Fact]
    public void EndsOngoingPositionTodayAndSkipsUndatedProject()
    {
        var bars = TimelineModel.Build(Documents.English(), Documents.Today).Bars;

        var acme = bars.Single(bar => bar.Id == "acme");
        acme.Ongoing.ShouldBeTrue();
        acme.End.ShouldBe(Documents.Today);
        bars.ShouldNotContain(bar => bar.Id == "hobby");
    }

    // Projects at the same time go to separate rows; one after another share a row.
    [Fact]
    public void PutsOverlappingProjectsOnSeparateRows()
    {
        var document = Documents.English();
        Engagement Project(string id, string start, string end) =>
            new(id, id, null, [], null, new DateRange(PartialDate.Parse(start), PartialDate.Parse(end)), [], [], null, false, null, []);
        var acme = document.Positions[0] with { Engagements = [Project("agco", "2022-11", "2024-09"), Project("regatta", "2023-03", "2023-05"), Project("supercharge", "2024-10", "2025-05")] };

        var timeline = TimelineModel.Build(document with { Positions = [acme, document.Positions[1]], Projects = [] }, Documents.Today);

        timeline.Bars.Where(bar => bar.Kind == TimelineBarKind.Engagement).Select(bar => (bar.Id, bar.Row)).ShouldBe([("agco", 0), ("regatta", 1), ("supercharge", 0)]);
        timeline.EngagementRows.ShouldBe(2);
        timeline.PositionRows.ShouldBe(1);
    }
}
