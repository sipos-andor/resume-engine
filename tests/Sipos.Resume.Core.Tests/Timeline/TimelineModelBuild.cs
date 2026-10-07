using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Model;
using Sipos.Resume.Core.Timeline;

namespace Sipos.Resume.Core.Tests.Timeline;

public class TimelineModelBuild
{
    [Fact]
    public void SpansFromFirstYearToToday()
    {
        var timeline = TimelineModel.Build(SampleDocuments.English(), SampleDocuments.Today);

        timeline.Start.ShouldBe(new DateOnly(2015, 1, 1));
        timeline.End.ShouldBe(SampleDocuments.Today);
    }

    [Fact]
    public void EndsOngoingPositionTodayAndSkipsUndatedProject()
    {
        var bars = TimelineModel.Build(SampleDocuments.English(), SampleDocuments.Today).Bars;

        var acme = bars.Single(bar => bar.Id == "acme");
        acme.Ongoing.ShouldBeTrue();
        acme.End.ShouldBe(SampleDocuments.Today);
        bars.ShouldNotContain(bar => bar.Id == "hobby");
    }

    // Projects at the same time go to separate rows; one after another share a row.
    [Fact]
    public void PutsOverlappingProjectsOnSeparateRows()
    {
        var document = SampleDocuments.English();
        Engagement Project(string id, string start, string end) =>
            new(id, id, null, [], null, new DateRange(PartialDate.Parse(start), PartialDate.Parse(end)), [], [], null, false, null, []);
        var acme = document.Positions[0] with { Engagements = [Project("agco", "2022-11", "2024-09"), Project("regatta", "2023-03", "2023-05"), Project("supercharge", "2024-10", "2025-05")] };

        var timeline = TimelineModel.Build(document with { Positions = [acme, document.Positions[1]], Projects = [] }, SampleDocuments.Today);

        timeline.Bars.Where(bar => bar.Kind == TimelineBarKind.Engagement).Select(bar => (bar.Id, bar.Row)).ShouldBe([("agco", 0), ("regatta", 1), ("supercharge", 0)]);
        timeline.EngagementRows.ShouldBe(2);
        timeline.PositionRows.ShouldBe(1);
    }

    // The axis ends at the present: an item that starts later would get a bar that ends before it starts.
    [Fact]
    public void LeavesOutItemGivenStartAfterToday()
    {
        var document = SampleDocuments.English();
        var later = document.Positions[1] with { Id = "later", Period = new DateRange(PartialDate.Parse("2026-12")) };

        TimelineModel.Build(document with { Positions = [document.Positions[0], later] }, SampleDocuments.Today).Bars.ShouldNotContain(bar => bar.Id == "later");
    }

    // A year given alone stays a year in the bar's text.
    [Fact]
    public void KeepsCvPeriodGivenYearOnlyDates()
    {
        var document = SampleDocuments.English();
        var period = new DateRange(PartialDate.Parse("2015"), PartialDate.Parse("2018"));

        var bar = TimelineModel.Build(document with { Positions = [document.Positions[1] with { Period = period }] }, SampleDocuments.Today).Bars.Single(bar => bar.Id == "initech");

        bar.Period.ShouldBe(period);
        (bar.Start, bar.End).ShouldBe((new DateOnly(2015, 1, 1), new DateOnly(2018, 12, 31)));
    }
}
