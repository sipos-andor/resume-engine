using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Sipos.Resume.Core.Languages;
using Sipos.Resume.Core.Site;

namespace Sipos.Resume.Generation.Pages;

/// <summary>Renders a theme's Razor components to HTML files with Blazor's <see cref="HtmlRenderer"/>; no web server is involved.</summary>
/// <remarks>
/// <para>
/// Decision: a new service provider and scope for every page, with the page's address and culture.
/// Why: a theme's scoped services (the navigation manager, the localizers) must see one page only; and the culture
/// set for the render makes <c>@inject IStringLocalizer</c> and every date answer in the page's language, as in the
/// browser.
/// </para>
/// <para>
/// Decision: a static navigation manager and a JavaScript runtime that fails every call.
/// Why: the components are the same ones the Studio runs in the browser; rendering them to a file must not depend on
/// anything only a browser has.
/// </para>
/// </remarks>
/// <param name="theme">The theme whose components and services are rendered.</param>
/// <param name="settings">The site's settings.</param>
/// <param name="languages">The site's languages, the default first.</param>
/// <param name="loggerFactory">The build's logging.</param>
internal sealed class StaticPageRenderer(IResumeTheme theme, SiteSettings settings, IReadOnlyList<ResumeLanguage> languages, ILoggerFactory loggerFactory)
{
    /// <summary>Renders a component as a whole HTML document.</summary>
    /// <param name="component">The component type, such as the theme's page component.</param>
    /// <param name="parameters">The component's parameters by name.</param>
    /// <param name="path">The page's site path, such as <c>/hu/</c>.</param>
    /// <param name="culture">The page's culture.</param>
    public async Task<string> RenderAsync(Type component, IReadOnlyDictionary<string, object?> parameters, string path, CultureInfo culture)
    {
        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        theme.ConfigureServices(services, settings, languages);
        services.AddScoped<NavigationManager>(_ => new StaticNavigationManager(settings.Url("/").AbsoluteUri, settings.Url(path).AbsoluteUri));
        services.AddScoped<IJSRuntime, UnavailableJSRuntime>();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await using var scope = provider.CreateAsyncScope();
        var (culturePrevious, uiCulturePrevious) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        try
        {
            await using var renderer = new HtmlRenderer(scope.ServiceProvider, loggerFactory);
            // The render and the reading of its HTML stay on the renderer's dispatcher, so nothing here leaves its context.
            return await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var output = await renderer.RenderComponentAsync(component, ParameterView.FromDictionary(parameters.ToDictionary()));
                return output.ToHtmlString();
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = culturePrevious;
            CultureInfo.CurrentUICulture = uiCulturePrevious;
        }
    }
}
