using System.Text;
using Microsoft.Extensions.Logging;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Insights;
using Sipos.Resume.Core.Site;
using Sipos.Resume.Generation.Assets;
using Sipos.Resume.Generation.Output;
using Sipos.Resume.Generation.Pages;
using Sipos.Resume.Generation.Seo;

namespace Sipos.Resume.Generation.Building;

/// <summary>
/// Builds the site of a checked CV: every download, the pages and the not-found page, the theme's files and assets,
/// the Markdown and JSON Resume of each page, the share images, the sitemap, robots.txt and the llms.txt files.
/// </summary>
/// <param name="theme">The look of the pages and the designed documents.</param>
/// <param name="writers">The document writers; a download is offered only when a writer can write it.</param>
/// <param name="shareImages">The share image writer, or <see langword="null"/> for pages without one.</param>
/// <param name="options">The build's options.</param>
/// <param name="loggerFactory">The build's logging.</param>
internal sealed partial class SiteBuilder(IResumeTheme theme, IReadOnlyList<IDocumentWriter> writers, IShareImageWriter? shareImages, BuildOptions options, ILoggerFactory loggerFactory)
{
    /// <summary>The path of the not-found page, which GitHub Pages serves for every missing address.</summary>
    public const string NotFoundPath = "/404.html";

    private readonly ILogger _logger = loggerFactory.CreateLogger<SiteBuilder>();

    /// <summary>Writes the whole site.</summary>
    /// <param name="set">The checked content.</param>
    /// <param name="sink">Where the site is written.</param>
    /// <param name="cancellationToken">Stops the build.</param>
    /// <returns>The pages that were written, the default language first.</returns>
    public async Task<IReadOnlyList<SitePage>> BuildAsync(ResumeSet set, IArtifactSink sink, CancellationToken cancellationToken)
    {
        var pages = Plan(set);
        var markdowns = new List<string>();
        foreach (var (page, edition) in pages.Zip(set.Editions))
        {
            foreach (var download in page.Downloads)
            {
                var bytes = Write(WriterFor(download)!, Context(page, edition, download));
                await sink.WriteAsync(download.Path, bytes, cancellationToken).ConfigureAwait(false);
                switch (download.Format, download.Variant, download.FocusId)
                {
                    case (DownloadFormat.Markdown, DocumentVariant.Designed, null):
                        await sink.WriteAsync(page.MarkdownPath, bytes, cancellationToken).ConfigureAwait(false);
                        markdowns.Add(Encoding.UTF8.GetString(bytes));
                        break;
                    case (DownloadFormat.JsonResume, DocumentVariant.Designed, null):
                        await sink.WriteAsync(page.JsonResumePath, bytes, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        break;
                }
            }

            if (shareImages is not null && page.ShareImagePath is { } image)
            {
                await sink.WriteAsync(image, shareImages.Write(page.Document, theme.Documents, page.Settings.Origin), cancellationToken).ConfigureAwait(false);
            }

            await WriteTextAsync(sink, page.LlmsTxtPath, LlmsTxtWriter.Write(page, pages), cancellationToken).ConfigureAwait(false);
            LogLanguage(page.Document.Language.Tag, page.Downloads.Count);
        }

        var renderer = new StaticPageRenderer(theme, pages, loggerFactory);
        foreach (var page in pages)
        {
            var html = await renderer.RenderAsync(theme.PageComponent, new Dictionary<string, object?> { ["Page"] = page }, page.Path, page.Document.Language.Culture).ConfigureAwait(false);
            await WriteTextAsync(sink, SitePaths.IndexOf(page.Path), html, cancellationToken).ConfigureAwait(false);
        }

        var notFound = await renderer.RenderAsync(theme.NotFoundComponent, new Dictionary<string, object?> { ["Pages"] = pages }, NotFoundPath, pages[0].Document.Language.Culture).ConfigureAwait(false);
        await WriteTextAsync(sink, NotFoundPath, notFound, cancellationToken).ConfigureAwait(false);

        foreach (var file in theme.Files(set.Settings))
        {
            await WriteTextAsync(sink, file.Path, file.Content, cancellationToken).ConfigureAwait(false);
        }

        var assets = await AssetCopier.CopyAsync(options.AssetsRoot, sink, cancellationToken).ConfigureAwait(false);
        LogAssets(assets, options.AssetsRoot);

        await WriteTextAsync(sink, "/sitemap.xml", SitemapWriter.Write(pages), cancellationToken).ConfigureAwait(false);
        await WriteTextAsync(sink, "/robots.txt", RobotsWriter.Write(set.Settings), cancellationToken).ConfigureAwait(false);
        await WriteTextAsync(sink, LlmsTxtWriter.FullPath, LlmsTxtWriter.WriteFull(markdowns), cancellationToken).ConfigureAwait(false);

        // Decision: an empty .nojekyll at the root.
        // Why: a site deployed from a branch instead of an Actions artifact goes through Jekyll, which drops every
        // folder that starts with an underscore, such as the packages' _content/.
        await sink.WriteAsync("/.nojekyll", ReadOnlyMemory<byte>.Empty, cancellationToken).ConfigureAwait(false);
        return pages;
    }

    /// <summary>Lays out the pages: each language's insights, the downloads a writer can write and its share image.</summary>
    /// <param name="set">The checked content.</param>
    public IReadOnlyList<SitePage> Plan(ResumeSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        var languages = set.Languages;
        return [.. set.Editions.Select(edition =>
        {
            var document = edition.Document;
            var downloads = DownloadCatalog.For(document.Language, set.Settings.DownloadPrefix, document.FocusProfiles.Select(profile => profile.Id))
                .Where(download => WriterFor(download) is not null)
                .ToList();
            var image = shareImages is null ? null : $"/og/{document.Language.Tag.ToLowerInvariant()}.png";
            return new SitePage(document, ResumeInsights.Analyze(document, options.Today), languages, downloads, set.Settings, image);
        })];
    }

    private DocumentContext Context(SitePage page, ResumeEdition edition, DownloadSpec download) => new(
        page.Document,
        page.Insights,
        download,
        download.FocusId is null ? null : page.Insights.Focus.Single(view => view.Profile.Id == download.FocusId),
        page.Settings.Url(page.Path),
        theme.Documents,
        // Decision: the e-mail address reaches the PDF writer only.
        // Why: the PDF draws it as an image; every other format would carry it as text a crawler can read.
        download.Format == DownloadFormat.Pdf ? options.ContactEmail : null,
        edition.Source);

    private IDocumentWriter? WriterFor(DownloadSpec download) =>
        writers.FirstOrDefault(writer => writer.Format == download.Format && writer.Supports(download.Variant));

    private static byte[] Write(IDocumentWriter writer, DocumentContext context)
    {
        using var output = new MemoryStream();
        writer.Write(context, output);
        return output.ToArray();
    }

    private static Task WriteTextAsync(IArtifactSink sink, string path, string text, CancellationToken cancellationToken) =>
        sink.WriteAsync(path, Encoding.UTF8.GetBytes(text), cancellationToken);

    [LoggerMessage(Level = LogLevel.Information, Message = "Wrote the {Language} page's {Count} downloads.")]
    private partial void LogLanguage(string language, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Copied {Count} static assets from {Folder}.")]
    private partial void LogAssets(int count, string folder);
}
