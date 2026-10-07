using System.Text.Json;
using Sipos.Resume.Core.Mapping;
using Sipos.Resume.Core.Site;
using Sipos.Resume.Core.Validation;
using Sipos.Resume.Theme.Operandor.Tests.Helpers;

namespace Sipos.Resume.Theme.Operandor.Tests.Pages;

public class ResumePageRender
{
    [Fact]
    public async Task DeclaresLanguageAndPolicyFirstGivenHungarianPage()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 1);

        document.DocumentElement.GetAttribute("lang").ShouldBe("hu");
        var head = document.Head!.Children.ToList();
        head[0].GetAttribute("charset").ShouldBe("utf-8");
        document.QuerySelector("meta[http-equiv='Content-Security-Policy']")!.GetAttribute("content").ShouldBe(ContentSecurityPolicy.For(ThemePages.Settings));
        head.IndexOf(document.QuerySelector("meta[http-equiv='Content-Security-Policy']")!).ShouldBeLessThan(head.IndexOf(document.QuerySelector("script")!));
    }

    // The page's head names it, its versions, its Markdown and its person, as the build's verifier expects.
    [Fact]
    public async Task DescribesPageAndPersonInHeadGivenPage()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 1);

        document.Title.ShouldBe("Example Ann – Önéletrajz: Szoftverarchitekt");
        document.QuerySelector("link[rel=canonical]")!.GetAttribute("href").ShouldBe("https://cv.example.com/hu/");
        document.QuerySelectorAll("link[rel=alternate][hreflang]").Select(link => link.GetAttribute("hreflang")).ShouldBe(["en", "hu", "x-default"]);
        document.QuerySelector("link[rel=alternate][type='text/markdown']")!.GetAttribute("href").ShouldBe("/hu/index.md");
        document.QuerySelector("meta[property='og:type']")!.GetAttribute("content").ShouldBe("profile");
        document.QuerySelector("meta[property='og:image']")!.GetAttribute("content").ShouldBe("https://cv.example.com/og/hu.png");
        var graph = JsonDocument.Parse(document.QuerySelector("script[type='application/ld+json']")!.TextContent).RootElement.GetProperty("@graph");
        var person = graph.EnumerateArray().Single(node => node.GetProperty("@type").GetString() == "Person");
        person.GetProperty("@id").GetString().ShouldBe("https://cv.example.com/#person");
        person.TryGetProperty("email", out _).ShouldBeFalse();
    }

    // The header shows operandor's wide and compact marks, never its name, and the person's name as the home link's text.
    [Fact]
    public async Task ShowsOperandorMarksWithoutItsNameInHeaderGivenPage()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 1);

        var home = document.QuerySelector(".op-header__home")!;
        home.GetAttribute("aria-label").ShouldBe("Example Ann");
        home.QuerySelector(".cv-brand__mark--wide svg")!.GetAttribute("viewBox").ShouldBe("0 0 278 100");
        home.QuerySelector(".cv-brand__mark--compact svg")!.GetAttribute("viewBox").ShouldBe("0 0 100 100");
        home.QuerySelectorAll(".cv-brand__mark").ShouldAllBe(mark => mark.GetAttribute("aria-hidden") == "true");
        home.QuerySelector(".op-wordmark").ShouldBeNull();
        home.TextContent.Trim().ShouldBe("Example Ann");
    }

    // The policy allows no inline script; the JSON blocks are data the browser does not run.
    [Fact]
    public async Task RunsNoInlineScriptGivenPage()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        document.Scripts.Where(script => string.IsNullOrEmpty(script.Source)).Select(script => script.Type).Order()
            .ShouldBe(["application/json", "application/ld+json"]);
        document.QuerySelectorAll("[onclick], [onload], [onerror]").ShouldBeEmpty();
    }

    [Fact]
    public async Task GivesEveryElementUniqueIdGivenPage()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        var ids = document.QuerySelectorAll("[id]").Select(element => element.Id).ToList();
        ids.ShouldBeUnique();
        ids.ShouldContain("main");
        ids.ShouldContain("experience");
        ids.ShouldContain("acme");
        ids.ShouldContain("portal");
        ids.ShouldContain("skills-1");
    }

    // Anchors.IsReserved must cover every id the theme writes for itself, or an item's x-id could take one.
    [Fact]
    public async Task UsesOnlyReservedIdsBesideItemIdentifiersGivenPage()
    {
        var pages = ThemePages.Sample();
        var document = await ThemePages.RenderAsync(pages, 0);
        var cv = pages[0].Document;
        var items = cv.Positions.Select(position => position.Id).Concat(cv.AllEngagements.Select(engagement => engagement.Id)).Concat(cv.Strengths.Select(strength => strength.Id)).ToHashSet();

        var own = document.QuerySelectorAll("[id]").Select(element => element.Id!).Where(id => !items.Contains(id)).ToList();
        own.ShouldNotBeEmpty();
        own.ShouldAllBe(id => Anchors.IsReserved(id));
    }

    // x-strengths is an extension and a CV may have no skills, positions or profiles: no empty section or row.
    [Fact]
    public async Task LeavesOutEmptySectionsGivenCvWithoutThem()
    {
        var pages = ThemePages.Sample(change: cv => cv with { Strengths = [], SkillGroups = [], Positions = [], Projects = [], Person = cv.Person with { Profiles = [] } });
        var document = await ThemePages.RenderAsync(pages, 0);

        document.GetElementById("strengths").ShouldBeNull();
        document.GetElementById("skills").ShouldBeNull();
        document.GetElementById("experience").ShouldBeNull();
        document.QuerySelectorAll(".cv-facts dd").ShouldAllBe(row => row.TextContent.Trim().Length > 0);
    }

    // Only the CV's own sections are landmarks, so a skill group named "Languages" does not repeat the languages section.
    [Fact]
    public async Task KeepsSkillGroupsOutOfLandmarksGivenSkills()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        var groups = document.QuerySelectorAll(".cv-skill-group");
        groups.ShouldNotBeEmpty();
        groups.ShouldAllBe(group => group.LocalName == "div" && !group.HasAttribute("aria-labelledby"));
    }

    // The profiles row names what it lists, not the online CV, which the documents' "Web" label means.
    [Fact]
    public async Task LabelsProfilesAsOnlineGivenProfiles()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        document.QuerySelector(".cv-facts__profile")!.ParentElement!.PreviousElementSibling!.TextContent.Trim().ShouldBe("Online");
    }

    // The script drives the theme toggle; without it, as on the not-found page, the toggle would do nothing.
    [Fact]
    public async Task HidesThemeToggleUntilScriptGivenAnyPage()
    {
        var pages = ThemePages.Sample();

        (await ThemePages.RenderAsync(pages, 0)).QuerySelector("[data-cv-theme-toggle]")!.HasAttribute("hidden").ShouldBeTrue();
        (await ThemePages.RenderNotFoundAsync(pages)).QuerySelector("[data-cv-theme-toggle]")!.HasAttribute("hidden").ShouldBeTrue();
    }

    // The same content and build date give the same page, whatever the clock says.
    [Fact]
    public async Task TakesCopyrightYearFromBuildDateGivenNoLastModified()
    {
        var pages = ThemePages.Sample(change: cv => cv with { LastModified = null }, today: new DateOnly(2024, 3, 1));

        (await ThemePages.RenderAsync(pages, 0)).QuerySelector(".op-footer")!.TextContent.ShouldContain("© 2024 Ann Example");
        (await ThemePages.RenderNotFoundAsync(pages)).QuerySelector(".op-footer")!.TextContent.ShouldContain("© 2024 Ann Example");
    }

    // A position's projects sit under its "client projects" heading (h4); a project of its own is an h3 like a position.
    [Fact]
    public async Task NestsHeadingsInOrderGivenProjectsWithAndWithoutPosition()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        document.QuerySelector("#acme .cv-item__title")!.LocalName.ShouldBe("h3");
        document.QuerySelector("#acme .cv-engagements__title")!.LocalName.ShouldBe("h4");
        document.QuerySelector("#portal .cv-item__title")!.LocalName.ShouldBe("h5");
        document.QuerySelector("#hobby .cv-item__title")!.LocalName.ShouldBe("h3");
    }

    // The timeline repeats the list beside it, so screen readers and the keyboard skip all of it, caption included.
    [Fact]
    public async Task HidesTimelineFromAssistiveTechnologyGivenPage()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        var figure = document.QuerySelector("figure.cv-timeline")!;
        figure.GetAttribute("aria-hidden").ShouldBe("true");
        var links = figure.QuerySelectorAll("a");
        links.ShouldNotBeEmpty();
        links.ShouldAllBe(link => link.GetAttribute("tabindex") == "-1");
    }

    // A bare #anchor keeps the address's query (?focus=, ?tech=, ?view=), so following it scrolls instead of loading
    // the page again without the reader's view; every other link is root-relative, as the page has no <base>.
    [Fact]
    public async Task LinksWithinPageByAnchorAndElsewhereByRootPathGivenOtherLanguage()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 1);

        document.QuerySelector("base").ShouldBeNull();
        document.QuerySelector("#experience .cv-anchor")!.GetAttribute("href").ShouldBe("#experience");
        var references = document.QuerySelectorAll("[href], [src]").Select(element => element.GetAttribute("href") ?? element.GetAttribute("src")!).ToList();
        references.ShouldNotBeEmpty();
        references.ShouldAllBe(reference => reference.StartsWith('/') || reference.StartsWith('#') || reference.Contains(':'));
    }

    [Fact]
    public async Task MarksItemsForScriptGivenPositionsAndProjects()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        var acme = document.GetElementById("acme")!;
        acme.GetAttribute("data-cv-short").ShouldBe("true");
        document.GetElementById("initech")!.GetAttribute("data-cv-short").ShouldBe("false");
        document.GetElementById("portal")!.GetAttribute("data-cv-tech")!.Split('|').ShouldBe(["c#", "azure"], ignoreOrder: true);
        acme.QuerySelector("#portal").ShouldNotBeNull();
    }

    // The tools need the script; without it they stay hidden and the whole CV shows.
    [Fact]
    public async Task HidesToolsUntilScriptGivenPage()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        document.QuerySelector("[data-cv-tools]")!.HasAttribute("hidden").ShouldBeTrue();
        document.QuerySelectorAll("[data-cv-focus-download]").ShouldAllBe(button => button.HasAttribute("hidden"));
    }

    [Fact]
    public async Task OffersEveryDownloadOfLanguageGivenDownloads()
    {
        var pages = ThemePages.Sample();
        var document = await ThemePages.RenderAsync(pages, 1);

        document.QuerySelectorAll("#downloads a.cv-download").Select(link => link.GetAttribute("href"))
            .ShouldBe(pages[1].Downloads.Select(download => download.Path), ignoreOrder: true);
    }

    [Fact]
    public async Task WritesLevelsInWordsAndMetersGivenSkills()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 1);

        var skill = document.QuerySelector("[data-cv-skill='c#']")!;
        skill.QuerySelector(".cv-skill__level")!.TextContent.ShouldBe("Expert");
        skill.QuerySelector(".cv-meter")!.ClassList.ShouldContain("cv-meter--5");
        skill.QuerySelector(".cv-meter")!.GetAttribute("aria-hidden").ShouldBe("true");
    }

    // Only the PDFs may show the address, as an image; the page never does.
    [Fact]
    public async Task ShowsNoEmailAddressGivenAnyPage()
    {
        foreach (var index in new[] { 0, 1 })
        {
            var document = await ThemePages.RenderAsync(ThemePages.Sample(), index);

            EmailGuard.ContainsAddress(document.DocumentElement.OuterHtml).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task LoadsAnalyticsOnlyGivenToken()
    {
        var without = await ThemePages.RenderAsync(ThemePages.Sample(), 0);
        var with = await ThemePages.RenderAsync(ThemePages.Sample(ThemePages.Settings with { AnalyticsToken = "token" }), 0);

        without.QuerySelectorAll("script[src*='cloudflare-web-analytics']").ShouldBeEmpty();
        with.QuerySelectorAll("script[src*='cloudflare-web-analytics']").Count().ShouldBe(1);
    }

    [Fact]
    public async Task LeadsToEveryLanguageAndAsksNotToIndexGivenNotFoundPage()
    {
        var document = await ThemePages.RenderNotFoundAsync(ThemePages.Sample());

        document.QuerySelector("meta[name=robots]")!.GetAttribute("content").ShouldBe("noindex, follow");
        document.QuerySelectorAll(".op-notfound__languages a").Select(link => (link.GetAttribute("href"), link.GetAttribute("hreflang"))).ShouldBe([("/", "en"), ("/hu/", "hu")]);
        document.Scripts.ShouldNotContain(script => script.Source != null && script.Source.EndsWith("cv.js", StringComparison.Ordinal));
    }
}
