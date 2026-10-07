using Microsoft.JSInterop;

namespace Sipos.Resume.Generation.Pages;

/// <summary>The JavaScript runtime of a page rendered to a file: there is none, and a call says which component made it.</summary>
/// <remarks>
/// Decision: a call fails the build instead of returning a default.
/// Why: the theme's components must render the same markup without a browser; a component that calls JavaScript while
/// rendering would show something else in the browser than in the file, and that is a bug to find at build time.
/// </remarks>
internal sealed class UnavailableJSRuntime : IJSRuntime
{
    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        throw new InvalidOperationException($"A component called '{identifier}' while the page was rendered to a file, where there is no JavaScript.");

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);
}
