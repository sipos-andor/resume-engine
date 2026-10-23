using AngleSharp.Dom;
using Sipos.Resume.Core.Model;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    [Fact]
    public async Task RendersHiddenSummaryPlaceholderGivenOnlyFocusSummary()
    {
        var pages = ThemePages.Sample(change: cv => cv with
        {
            Person = cv.Person with { Summary = null },
            FocusProfiles = [cv.FocusProfiles[0] with { Summary = "Builds for the selected role." }],
        });
        var document = await ThemePages.RenderAsync(pages, 0);

        var summary = document.QuerySelector("[data-cv-summary]")!;
        summary.TextContent.ShouldBeEmpty();
        summary.GetAttribute("data-cv-summary-default").ShouldBe("");
        summary.HasAttribute("hidden").ShouldBeTrue();
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

    // The one-page view shows what the CV marks with x-short; a plain JSON Resume marks nothing and has no such view.
    [Fact]
    public async Task OffersOnePageViewOnlyGivenMarkedItems()
    {
        var plain = ThemePages.Sample(change: WithoutShortItems);

        (await ThemePages.RenderAsync(ThemePages.Sample(), 0)).QuerySelectorAll("[data-cv-view]").Length.ShouldBe(2);
        (await ThemePages.RenderAsync(plain, 0)).QuerySelectorAll("[data-cv-view]").ShouldBeEmpty();
    }

    private static ResumeDocument WithoutShortItems(ResumeDocument cv) => cv with
    {
        Strengths = [.. cv.Strengths.Select(strength => strength with { IsShort = false })],
        Positions = [.. cv.Positions.Select(position => position with { IsShort = false, Engagements = [.. position.Engagements.Select(engagement => engagement with { IsShort = false })] })],
        Projects = [.. cv.Projects.Select(project => project with { IsShort = false })],
    };

    // A section or a position's project list none of whose items is marked would show only its heading in that view.
    [Fact]
    public async Task MarksSectionsForOnePageViewGivenTheirItems()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        document.GetElementById("strengths")!.GetAttribute("data-cv-short").ShouldBe("true");
        document.GetElementById("experience")!.GetAttribute("data-cv-short").ShouldBe("true");
        document.GetElementById("projects")!.GetAttribute("data-cv-short").ShouldBe("false");
        document.GetElementById("skills")!.HasAttribute("data-cv-short").ShouldBeFalse();
        document.QuerySelector("#acme .cv-engagements")!.GetAttribute("data-cv-short").ShouldBe("false");
    }

    [Fact]
    public async Task KeepsPositionVisibleGivenOnlyShortEngagement()
    {
        var pages = ThemePages.Sample(change: cv => cv with
        {
            Positions = [.. cv.Positions.Select((position, index) => index == 0
                ? position with
                {
                    IsShort = false,
                    Engagements = [.. position.Engagements.Select((engagement, engagementIndex) => engagement with { IsShort = engagementIndex == 0 })],
                }
                : position)],
        });
        var document = await ThemePages.RenderAsync(pages, 0);

        document.GetElementById("experience")!.GetAttribute("data-cv-short").ShouldBe("true");
        document.GetElementById("acme")!.GetAttribute("data-cv-short").ShouldBe("true");
        document.QuerySelector("#acme .cv-engagements")!.GetAttribute("data-cv-short").ShouldBe("true");
    }

    // A tag spelled as an alias carries its term's key, which the technology filter and the job ad matcher compare.
    [Fact]
    public async Task KeysTagsByTermGivenAliasSpelling()
    {
        var pages = ThemePages.Sample(change: cv => cv with { Projects = [cv.Projects[0] with { Keywords = ["csharp", "Rust"] }] });
        var document = await ThemePages.RenderAsync(pages, 0);

        document.QuerySelectorAll("#hobby .op-tag").Select(tag => (tag.TextContent, tag.GetAttribute("data-cv-key"))).ShouldBe([("csharp", "c#"), ("Rust", "rust")]);
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

    // The tools need the script; without it the button and the note stay hidden, the panel closed and the whole CV shows.
    [Fact]
    public async Task HidesToolsUntilScriptGivenPage()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        document.QuerySelector("[data-cv-filter-toggle]")!.HasAttribute("hidden").ShouldBeTrue();
        var panel = document.QuerySelector("[data-cv-tools]")!;
        panel.TagName.ShouldBe("DIALOG");
        panel.HasAttribute("open").ShouldBeFalse();
        document.QuerySelector("[data-cv-view-note]")!.HasAttribute("hidden").ShouldBeTrue();
        document.QuerySelectorAll("[data-cv-focus-download]").ShouldAllBe(button => button.HasAttribute("hidden"));
        document.QuerySelector("[data-cv-copy-view]")!.HasAttribute("hidden").ShouldBeTrue();
    }

    // The panel opens from the header; it is no longer a box between the hero and the sections.
    [Fact]
    public async Task OpensToolsFromHeaderGivenPage()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        var toggle = document.QuerySelector(".op-header__right [data-cv-filter-toggle]")!;
        var pdf = document.QuerySelector(".op-header__right a[download]")!;
        toggle.CompareDocumentPosition(pdf).ShouldBe(DocumentPositions.Following);
        toggle.GetAttribute("aria-controls").ShouldBe("cv-panel");
        document.GetElementById("cv-panel")!.HasAttribute("data-cv-tools").ShouldBeTrue();
        document.QuerySelector("main [data-cv-tools]").ShouldBeNull();
    }

    // The design system hides header .op-btn at ≤360 px and their icons at ≤400 px.
    [Fact]
    public async Task KeepsFilterToggleOutOfDesignSystemButtonsGivenHeader()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        document.QuerySelector("[data-cv-filter-toggle]")!.ClassList.ShouldNotContain("op-btn");
    }

    [Fact]
    public async Task LeadsNoteToPlainPageGivenHungarianPage()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 1);

        var note = document.QuerySelector(".cv-hero__title")!.NextElementSibling!;
        note.HasAttribute("data-cv-view-note").ShouldBeTrue();
        var full = note.QuerySelector("a[data-cv-full]")!;
        full.GetAttribute("href").ShouldBe("/hu/");
        full.TextContent.ShouldContain("Teljes önéletrajz");
        document.QuerySelector("[data-cv-focus-note]").ShouldBeNull();
    }

    [Fact]
    public async Task HoldsEveryToolInPanelGivenPage()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        var panel = document.GetElementById("cv-panel")!;
        foreach (var selector in new[] { "#cv-search", "#cv-focus", "#cv-technology", "[data-cv-view]", "[data-cv-print]", "[data-cv-copy-view]", "details.cv-match", "[data-cv-clear]", "[data-cv-panel-close]" })
        {
            panel.QuerySelector(selector).ShouldNotBeNull(selector);
        }
        panel.QuerySelector("#cv-panel-title")!.TextContent.ShouldBe("Filter and view");
    }

    // A CV with neither position profiles nor a one-page view has no focus field and no view switch; the rest stays.
    [Fact]
    public async Task LeavesOutFocusAndViewInPanelGivenPlainResume()
    {
        var plain = ThemePages.Sample(change: cv => WithoutShortItems(cv) with { FocusProfiles = [] });

        var document = await ThemePages.RenderAsync(plain, 0);

        document.GetElementById("cv-focus").ShouldBeNull();
        document.QuerySelector("[data-cv-view]").ShouldBeNull();
        document.QuerySelector("[data-cv-filter-toggle]").ShouldNotBeNull();
        document.GetElementById("cv-technology").ShouldNotBeNull();
    }

    [Fact]
    public async Task OffersEveryDownloadOfLanguageGivenDownloads()
    {
        var pages = ThemePages.Sample();
        var document = await ThemePages.RenderAsync(pages, 1);

        document.QuerySelectorAll("#downloads a.cv-download").Select(link => link.GetAttribute("href"))
            .ShouldBe(pages[1].Downloads.Select(download => download.Path), ignoreOrder: true);
    }

    // A tailored version is a row: its profile once, then its formats side by side, each link still naming the profile.
    [Fact]
    public async Task ListsTailoredFilesInRowPerProfileGivenFocusProfile()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        var row = document.QuerySelectorAll("#downloads .cv-download-row").ShouldHaveSingleItem();
        row.QuerySelector(".cv-download-row__label")!.TextContent.ShouldBe("Software architect");
        row.QuerySelectorAll("a.cv-download").Select(link => Regex.Replace(link.TextContent, @"\s+", " ").Trim())
            .ShouldBe(["PDF · Software architect", "DOCX · Software architect"]);
    }

    // The legend names the chart's two colours, as the sample has positions and a dated project.
    [Fact]
    public async Task NamesBarKindsInLegendGivenPositionsAndProjects()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        document.QuerySelectorAll("figure.cv-timeline .cv-timeline__key").Select(key => key.TextContent)
            .ShouldBe(["Positions", "Projects"]);
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
        document.Scripts.ShouldNotContain(script => script.Source != null && script.Source.Contains("/js/cv.js", StringComparison.Ordinal));
    }

    // Every file the page loads carries the build's asset version, so a deploy that changed one reaches a returning
    // reader at once; without a version the addresses stay plain.
    [Fact]
    public async Task VersionsLoadedFilesGivenAssetVersion()
    {
        var pages = ThemePages.Sample().Select(page => page with { AssetVersion = "0123456789ab" }).ToList();

        var versioned = await ThemePages.RenderAsync(pages, 0);
        var plain = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        var addresses = versioned.QuerySelectorAll("link[rel=stylesheet], link[rel=icon], script[src]")
            .Select(element => element.GetAttribute("href") ?? element.GetAttribute("src")!).ToList();
        addresses.Count.ShouldBeGreaterThanOrEqualTo(6);
        addresses.ShouldAllBe(address => address.StartsWith('/') && address.EndsWith("?v=0123456789ab", StringComparison.Ordinal));
        plain.QuerySelector("link[rel=stylesheet][href$='cv.css']")!.GetAttribute("href").ShouldBe("/_content/Sipos.Resume.Theme.Operandor/css/cv.css");
    }

    // The stylesheet lets the not-found page's one band fill its main area, so no strip of the page's background shows
    // above the footer on a tall window.
    [Fact]
    public async Task MarksMainForFillingBandGivenNotFoundPage()
    {
        var document = await ThemePages.RenderNotFoundAsync(ThemePages.Sample());

        var main = document.QuerySelector("main.cv-main--notfound")!;
        main.Children.ShouldHaveSingleItem().ClassList.ShouldContain("op-band");
    }

    // The meters alone do not say what they measure: the scale's steps stand next to them as text, then the note, and
    // all of it stays out of the groups a focus reorders.
    [Fact]
    public async Task ShowsScaleStepsAndNoteBeforeGroupsGivenAssessment()
    {
        var scale = new SkillScale([new SkillScaleStep(5, "Usage: core and recurring"), new SkillScaleStep(3, "Usage: project-specific")], "Scores describe usage, not proficiency.");
        var document = await ThemePages.RenderAsync(ThemePages.Sample(change: cv => cv with { SkillScale = scale }), 0);

        var legend = document.QuerySelector("#skills .cv-scale")!;
        legend.QuerySelectorAll(".cv-scale__step").Select(step => step.TextContent.Trim()).ShouldBe(["Usage: core and recurring", "Usage: project-specific"]);
        legend.QuerySelector(".cv-scale__step .cv-meter")!.ClassList.ShouldContain("cv-meter--5");
        legend.QuerySelector(".cv-scale__note")!.TextContent.ShouldBe("Scores describe usage, not proficiency.");
        legend.NextElementSibling!.GetAttribute("data-cv-order").ShouldBe("skill-groups");
        (await ThemePages.RenderAsync(ThemePages.Sample(), 0)).QuerySelector(".cv-scale").ShouldBeNull();
    }

    [Fact]
    public async Task WidensLevelColumnGivenLevelInSeveralWords()
    {
        var pages = ThemePages.Sample(change: cv => cv with
        {
            SkillGroups = [.. cv.SkillGroups.Select(group => group with { Skills = [.. group.Skills.Select(skill => skill with { Level = "Usage: recurring across engagements" })] })],
        });

        (await ThemePages.RenderAsync(pages, 0)).QuerySelector(".cv-skill-groups")!.ClassList.ShouldContain("cv-skill-groups--long-levels");
        (await ThemePages.RenderAsync(ThemePages.Sample(), 0)).QuerySelector(".cv-skill-groups")!.ClassList.ShouldNotContain("cv-skill-groups--long-levels");
    }

    // The one-page view leaves publications and volunteer work out; their items are search targets, as awards are.
    [Fact]
    public async Task ListsPublicationsAndVolunteerWorkAfterAwardsOutsideOnePageViewGivenSample()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 0);

        var publications = document.GetElementById("publications")!;
        var volunteer = document.GetElementById("volunteer")!;
        publications.GetAttribute("data-cv-short").ShouldBe("false");
        volunteer.GetAttribute("data-cv-short").ShouldBe("false");
        publications.QuerySelector("#publication-1[data-cv-id='publication-1'] .cv-list__title")!.TextContent.ShouldBe("Practical .NET");
        publications.QuerySelector("#publication-1 .cv-list__detail")!.TextContent.ShouldBe("Example Press");
        volunteer.QuerySelector("#volunteer-1[data-cv-id='volunteer-1'] .cv-list__title")!.TextContent.ShouldBe("Volunteer");
        volunteer.QuerySelector("#volunteer-1 .cv-list__detail")!.TextContent.ShouldBe("Food Bank");
        document.GetElementById("awards")!.NextElementSibling.ShouldBe(publications);
        publications.NextElementSibling.ShouldBe(volunteer);
    }

    [Fact]
    public async Task ShowsGradeAndCoursesGivenStudy()
    {
        var study = (await ThemePages.RenderAsync(ThemePages.Sample(), 0)).GetElementById("education-1")!;

        study.QuerySelector(".cv-list__detail")!.TextContent.ShouldBe("BSc, Informatics · Grade: 4.5/5");
        study.QuerySelector(".cv-list__summary")!.TextContent.ShouldBe("Courses: Databases, Networks");
    }
}
