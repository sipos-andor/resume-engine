namespace Sipos.Resume.Generation.Building;

/// <summary>What a build reads, where it writes and what it may put into the documents.</summary>
public sealed record BuildOptions
{
    /// <summary>The content folder with <c>site.json</c> and the <c>resume.{language}.json</c> files.</summary>
    public required string ContentFolder { get; init; }

    /// <summary>The folder the site is written to.</summary>
    public required string OutputFolder { get; init; }

    /// <summary>
    /// The published web root whose static assets (the theme's and its packages' <c>_content</c>) are copied to the
    /// site; <see langword="null"/> for the <c>wwwroot</c> next to the running program.
    /// </summary>
    public string? AssetsFolder { get; init; }

    /// <summary>The date that stands for the present: the end of ongoing periods on the timeline.</summary>
    public required DateOnly Today { get; init; }

    /// <summary>
    /// An e-mail address only the PDFs show, drawn as an image; <see langword="null"/> for none. It comes from the
    /// environment, never from the content or the command line, and is never logged.
    /// </summary>
    public string? ContactEmail { get; init; }

    /// <summary>A Cloudflare Web Analytics token, or <see langword="null"/> for no analytics.</summary>
    public string? AnalyticsToken { get; init; }

    /// <summary>Whether to empty the output folder first; without it a folder that holds files stops the build.</summary>
    public bool Clean { get; init; }

    /// <summary>Whether a missing e-mail address stops the build, as it should for a deployment.</summary>
    public bool RequireContactEmail { get; init; }

    /// <summary>Whether to check the content only, without writing anything.</summary>
    public bool ValidateOnly { get; init; }

    /// <summary>The web root assets are copied from.</summary>
    public string AssetsRoot => AssetsFolder ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
}
