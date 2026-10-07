using Microsoft.AspNetCore.Components;

namespace Sipos.Resume.Generation.Pages;

/// <summary>The navigation manager of a page rendered to a file: it knows the page's address and cannot navigate.</summary>
internal sealed class StaticNavigationManager : NavigationManager
{
    /// <summary>Creates the manager of one page.</summary>
    /// <param name="baseUri">The site's origin with a trailing slash.</param>
    /// <param name="uri">The page's absolute URL.</param>
    public StaticNavigationManager(string baseUri, string uri) => Initialize(baseUri, uri);

    /// <inheritdoc />
    protected override void NavigateToCore(string uri, NavigationOptions options) =>
        throw new NotSupportedException($"A page rendered to a file cannot navigate (to '{uri}').");
}
