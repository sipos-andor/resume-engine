using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Insights;
using Sipos.Resume.Core.Model;
using Sipos.Resume.Core.Site;

namespace Sipos.Resume.Theme.Operandor.Tests.Helpers;

/// <summary>The sample site's pages, rendered with the theme as the build renders them: HtmlRenderer, one scope per page.</summary>
internal static class ThemePages
{
    public static readonly SiteSettings Settings = new(new Uri("https://cv.example.com"), "en", ["hu"], "Ann_Example_CV", "cv-theme", "cv-language", null);

    /// <param name="settings">The site's settings, or the sample's.</param>
    /// <param name="change">Changes both documents, such as leaving out a section.</param>
    /// <param name="today">The build's present, or the sample's.</param>
    public static IReadOnlyList<SitePage> Sample(SiteSettings? settings = null, Func<ResumeDocument, ResumeDocument>? change = null, DateOnly? today = null)
    {
        settings ??= Settings;
        change ??= document => document;
        ResumeDocument[] documents = [change(SampleDocuments.English()), change(SampleDocuments.Hungarian())];
        var languages = documents.Select(document => document.Language).ToList();
        return [.. documents.Select(document => new SitePage(
            document,
            ResumeInsights.Analyze(document, today ?? SampleDocuments.Today),
            languages,
            DownloadCatalog.For(document.Language, settings.DownloadPrefix, document.FocusProfiles.Select(profile => profile.Id)),
            settings,
            $"/og/{document.Language.Tag}.png"))];
    }

    public static async Task<IHtmlDocument> RenderAsync(IReadOnlyList<SitePage> pages, int index)
    {
        var page = pages[index];
        return await RenderAsync(pages, typeof(Operandor.Pages.ResumePage), new Dictionary<string, object?> { ["Page"] = page }, page.Path, page.Document.Language.Culture);
    }

    public static Task<IHtmlDocument> RenderNotFoundAsync(IReadOnlyList<SitePage> pages) =>
        RenderAsync(pages, typeof(Operandor.Pages.NotFoundPage), new Dictionary<string, object?> { ["Pages"] = pages }, "/404.html", pages[0].Document.Language.Culture);

    private static async Task<IHtmlDocument> RenderAsync(IReadOnlyList<SitePage> pages, Type component, Dictionary<string, object?> parameters, string path, CultureInfo culture)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        new OperandorTheme().ConfigureServices(services, pages);
        services.AddScoped<NavigationManager>(_ => new TestNavigationManager(pages[0].Settings.Url("/").AbsoluteUri, pages[0].Settings.Url(path).AbsoluteUri));
        services.AddScoped<IJSRuntime, NoJSRuntime>();
        services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var (previous, previousUi) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = culture;
        try
        {
            await using var renderer = new HtmlRenderer(scope.ServiceProvider, NullLoggerFactory.Instance);
            var html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync(component, ParameterView.FromDictionary(parameters))).ToHtmlString());
            return new HtmlParser().ParseDocument(html);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string baseUri, string uri) => Initialize(baseUri, uri);
    }

    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new InvalidOperationException(identifier);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new InvalidOperationException(identifier);
    }
}
