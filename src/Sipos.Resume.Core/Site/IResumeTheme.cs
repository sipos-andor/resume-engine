using Microsoft.Extensions.DependencyInjection;
using Sipos.Resume.Core.Artifacts;

namespace Sipos.Resume.Core.Site;

/// <summary>A look of the CV: the Razor components of its pages, the services they need and the documents' colours.</summary>
/// <remarks>
/// Decision: a theme is a set of component types and a service registration, not a dependency of the build.
/// Why: the build renders whatever page component the theme names with Blazor's HtmlRenderer, and the Studio (in the
/// browser) renders the same components; neither needs to know the brand.
/// </remarks>
public interface IResumeTheme
{
    /// <summary>The component of a CV's page; it takes a <see cref="SitePage"/> parameter named <c>Page</c>.</summary>
    Type PageComponent { get; }

    /// <summary>
    /// The component of the not-found page; it takes every language's <see cref="SitePage"/>, the default first, as a
    /// parameter named <c>Pages</c>, so it can lead to each.
    /// </summary>
    Type NotFoundComponent { get; }

    /// <summary>The colours and fonts of the designed documents.</summary>
    DocumentTheme Documents { get; }

    /// <summary>The theme's static assets the pages reference, as paths relative to the published web root.</summary>
    IReadOnlyList<string> RequiredAssets { get; }

    /// <summary>The files the theme writes for a site, such as a script with the site's storage keys.</summary>
    /// <param name="settings">The site's settings.</param>
    IReadOnlyList<ThemeFile> Files(SiteSettings settings);

    /// <summary>Registers the services the theme's components inject.</summary>
    /// <param name="services">The render scope's services.</param>
    /// <param name="pages">Every language's page, the default first, with the site's settings.</param>
    void ConfigureServices(IServiceCollection services, IReadOnlyList<SitePage> pages);
}
