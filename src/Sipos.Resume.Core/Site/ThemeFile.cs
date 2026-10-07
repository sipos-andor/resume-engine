namespace Sipos.Resume.Core.Site;

/// <summary>A file a theme writes for a site, such as the script that hands the site's settings to its other scripts.</summary>
/// <param name="Path">The site path, such as <c>/site-config.js</c>.</param>
/// <param name="Content">The file's text, written as UTF-8.</param>
public sealed record ThemeFile(string Path, string Content);
