using System.Text.Encodings.Web;
using Microsoft.Extensions.DependencyInjection;
using Operandor.SharedKernel.UI;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Site;
using Sipos.Resume.Theme.Operandor.Pages;
using Sipos.Resume.Theme.Operandor.Rendering;

namespace Sipos.Resume.Theme.Operandor;

/// <summary>
/// The operandor look of a CV: the pages on the operandor design system with its light and dark themes, and the
/// designed documents in its colours and IBM Plex.
/// </summary>
public sealed class OperandorTheme : IResumeTheme
{
    /// <summary>The path the theme's static assets are published under.</summary>
    public const string AssetRoot = "/_content/Sipos.Resume.Theme.Operandor";

    /// <summary>
    /// The designed documents' colours: the design system's light theme (heading n-950, body n-800, muted n-600,
    /// subtle n-500, accent green-600, rules n-300) and IBM Plex.
    /// </summary>
    public static DocumentTheme DocumentColors { get; } = new("14181C", "333833", "636A61", "7F857C", "2D7612", "D6D9D3", "IBM Plex Sans", "IBM Plex Mono");

    /// <inheritdoc />
    public Type PageComponent => typeof(ResumePage);

    /// <inheritdoc />
    public Type NotFoundComponent => typeof(NotFoundPage);

    /// <inheritdoc />
    public DocumentTheme Documents => DocumentColors;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredAssets { get; } =
    [
        "/_content/Operandor.DesignSystem/operandor.css",
        "/_content/Operandor.SharedKernel.UI/theme.js",
        "/_content/Operandor.SharedKernel.UI/language.js",
        "/_content/Operandor.SharedKernel.UI/analytics/cloudflare-web-analytics.js",
        AssetRoot + "/css/cv.css",
        AssetRoot + "/js/cv.js",
        AssetRoot + "/js/text.js",
        AssetRoot + "/favicon.svg",
    ];

    /// <inheritdoc />
    /// <remarks>
    /// Decision: the shared kernel's scripts read their settings from <c>/site-config.js</c>, a file, not an inline
    /// script.
    /// Why: the policy allows no inline script; the file carries the storage keys and the analytics token, which the
    /// build takes from its secrets.
    /// </remarks>
    public IReadOnlyList<ThemeFile> Files(SiteSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        string Js(string value) => "\"" + JavaScriptEncoder.Default.Encode(value) + "\"";
        var token = settings.AnalyticsToken is { } value ? $",\n        webAnalyticsToken: {Js(value)}" : "";
        return
        [
            new ThemeFile("/site-config.js", $$"""
                // Written by the build: the settings of the shared kernel's scripts (theme.js, language.js, analytics).
                window.OperandorSharedKernelUI = {
                    config: {
                        themeKey: {{Js(settings.ThemeStorageKey)}},
                        languageKey: {{Js(settings.LanguageStorageKey)}}{{token}}
                    }
                };

                """),
        ];
    }

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IReadOnlyList<SitePage> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);
        services.AddSharedKernelUi(ResumeSeo.Options(pages));
    }
}
