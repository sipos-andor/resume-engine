using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Languages;
using Sipos.Resume.Core.Site;

namespace Sipos.Resume.Generation.Tests.Helpers;

/// <summary>A minimal theme: valid pages that link to everything the build writes, written without Razor.</summary>
internal sealed class FakeTheme : IResumeTheme
{
    public Type PageComponent => typeof(FakePage);

    public Type NotFoundComponent => typeof(FakeNotFound);

    public DocumentTheme Documents => DocumentTheme.Neutral;

    public IReadOnlyList<string> RequiredAssets => ["/js/site.js"];

    public IReadOnlyList<ThemeFile> Files(SiteSettings settings) => [new("/site-config.js", $"window.config = {{ themeKey: '{settings.ThemeStorageKey}' }};\n")];

    public void ConfigureServices(IServiceCollection services, SiteSettings settings, IReadOnlyList<ResumeLanguage> languages) =>
        services.AddSingleton(new FakeGreeting("rendered by the fake theme"));

    internal static string Head(SiteSettings settings, string title) =>
        $"<meta charset=\"utf-8\"><meta http-equiv=\"Content-Security-Policy\" content=\"{WebUtility.HtmlEncode(ContentSecurityPolicy.For(settings))}\"><title>{WebUtility.HtmlEncode(title)}</title><script src=\"/site-config.js\"></script>";
}

internal sealed record FakeGreeting(string Text);

internal sealed class FakePage : ComponentBase
{
    [Parameter, EditorRequired] public SitePage Page { get; set; } = default!;

    [Inject] public FakeGreeting Greeting { get; set; } = default!;

    [Inject] public NavigationManager Navigation { get; set; } = default!;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var settings = Page.Settings;
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html><html lang=\"").Append(Page.Document.Language.Tag).Append("\"><head>");
        html.Append(FakeTheme.Head(settings, Page.Document.Person.Name));
        html.Append("<link rel=\"canonical\" href=\"").Append(settings.Url(Page.Path).AbsoluteUri).Append("\">");
        foreach (var language in Page.Languages)
        {
            html.Append("<link rel=\"alternate\" hreflang=\"").Append(language.Tag).Append("\" href=\"").Append(settings.Url(language.HomePath).AbsoluteUri).Append("\">");
        }

        html.Append("<link rel=\"alternate\" hreflang=\"x-default\" href=\"").Append(settings.Url("/").AbsoluteUri).Append("\">");
        html.Append("<script type=\"application/ld+json\">{}</script></head><body>");
        html.Append("<p id=\"greeting\">").Append(Greeting.Text).Append("</p>");
        html.Append("<p id=\"uri\">").Append(Navigation.Uri).Append("</p>");
        html.Append("<p id=\"culture\">").Append(CultureInfo.CurrentUICulture.Name).Append("</p>");
        html.Append("<a href=\"").Append(Page.MarkdownPath).Append("\">md</a><a href=\"").Append(Page.JsonResumePath).Append("\">json</a>");
        foreach (var download in Page.Downloads)
        {
            html.Append("<a href=\"").Append(download.Path).Append("\">").Append(download.Describe()).Append("</a>");
        }

        html.Append("<a href=\"https://example.com/elsewhere\">away</a><a href=\"#top\">top</a>");
        html.Append("<script src=\"/js/site.js\"></script></body></html>");
        builder.AddMarkupContent(0, html.ToString());
    }
}

internal sealed class FakeNotFound : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<SitePage> Pages { get; set; } = default!;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var html = new StringBuilder("<!DOCTYPE html><html lang=\"en\"><head>");
        html.Append(FakeTheme.Head(Pages[0].Settings, "Not found")).Append("<meta name=\"robots\" content=\"noindex\"></head><body>");
        foreach (var page in Pages)
        {
            html.Append("<a href=\"").Append(page.Path).Append("\">").Append(page.Document.Language.Endonym).Append("</a>");
        }

        builder.AddMarkupContent(0, html.Append("</body></html>").ToString());
    }
}
