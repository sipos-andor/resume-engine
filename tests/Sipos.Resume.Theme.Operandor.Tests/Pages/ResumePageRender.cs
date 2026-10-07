using System.Text.Json;
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
        document.QuerySelector("link[rel=alternate][type='text/markdown']")!.GetAttribute("href").ShouldBe("hu/index.md");
        document.QuerySelector("meta[property='og:type']")!.GetAttribute("content").ShouldBe("profile");
        document.QuerySelector("meta[property='og:image']")!.GetAttribute("content").ShouldBe("https://cv.example.com/og/hu.png");
        var graph = JsonDocument.Parse(document.QuerySelector("script[type='application/ld+json']")!.TextContent).RootElement.GetProperty("@graph");
        var person = graph.EnumerateArray().Single(node => node.GetProperty("@type").GetString() == "Person");
        person.GetProperty("@id").GetString().ShouldBe("https://cv.example.com/#person");
        person.TryGetProperty("email", out _).ShouldBeFalse();
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

    // Under <base href="/"> a bare "#experience" would lead to the root page.
    [Fact]
    public async Task LinksSectionsThroughPagePathGivenOtherLanguage()
    {
        var document = await ThemePages.RenderAsync(ThemePages.Sample(), 1);

        document.QuerySelector("#experience .cv-anchor")!.GetAttribute("href").ShouldBe("hu/#experience");
        document.QuerySelectorAll("a[href^='#']").ShouldBeEmpty();
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
            .ShouldBe(pages[1].Downloads.Select(download => download.Path.TrimStart('/')), ignoreOrder: true);
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
        document.QuerySelectorAll(".op-notfound__languages a").Select(link => (link.GetAttribute("href"), link.GetAttribute("hreflang"))).ShouldBe([("", "en"), ("hu/", "hu")]);
    }
}
