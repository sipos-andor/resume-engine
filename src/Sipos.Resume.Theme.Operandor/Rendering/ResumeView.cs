using System.Globalization;
using Microsoft.Extensions.Localization;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Localization;
using Sipos.Resume.Core.Model;
using Sipos.Resume.Core.Site;
using Sipos.Resume.Theme.Operandor.Localization;

namespace Sipos.Resume.Theme.Operandor.Rendering;

/// <summary>What every section of a page renders from, cascaded from the page: the page, the words and the links.</summary>
/// <param name="Page">The page.</param>
/// <param name="Text">The theme's words in the page's language.</param>
internal sealed record ResumeView(SitePage Page, IStringLocalizer<ThemeText> Text)
{
    /// <summary>The engine's words in the page's language.</summary>
    public ResumeLabels Labels { get; } = new(Page.Document.Language.Culture);

    /// <summary>The CV.</summary>
    public ResumeDocument Document => Page.Document;

    /// <summary>The person.</summary>
    public Person Person => Page.Document.Person;

    /// <summary>The page's culture.</summary>
    public CultureInfo Culture => Page.Document.Language.Culture;

    /// <summary>
    /// A link to a site path, relative to the pages' <c>base href="/"</c>, such as <c>hu/</c> for <c>/hu/</c>.
    /// </summary>
    /// <param name="path">A site path starting with <c>/</c>.</param>
    public static string Href(string path) => path.TrimStart('/');

    /// <summary>A link to an element of this page, such as <c>hu/#experience</c>; a bare <c>#experience</c> would lead to the root under <c>base href="/"</c>.</summary>
    /// <param name="id">The element's id.</param>
    public string Anchor(string id) => Href(Page.Path) + "#" + id;

    /// <summary>A period in the page's words, such as <c>2022. november – jelenleg</c>.</summary>
    /// <param name="period">The period.</param>
    public string Period(DateRange period) => PeriodText.Format(period, Labels, DateStyle.Long);

    /// <summary>A date in the page's words.</summary>
    /// <param name="date">The date.</param>
    public string Date(PartialDate date) => PeriodText.Format(date, Culture, DateStyle.Long);

    /// <summary>The level of a skill in words: the CV's own word or the engine's word for the rating.</summary>
    /// <param name="skill">The skill.</param>
    public string? Level(Skill skill) => skill.Level ?? (skill.Rating is { } rating ? Labels.Rating(rating) : null);

    /// <summary>The download of a format and layout without a focus, or <see langword="null"/>.</summary>
    /// <param name="format">The format.</param>
    /// <param name="variant">The layout.</param>
    public DownloadSpec? Download(DownloadFormat format, DocumentVariant variant) =>
        Page.Downloads.FirstOrDefault(download => download.Format == format && download.Variant == variant && download.FocusId is null);

    /// <summary>The label of a position profile, or <see langword="null"/>.</summary>
    /// <param name="focusId">The profile's identifier.</param>
    public string? FocusLabel(string? focusId) => focusId is null ? null : Document.FocusProfiles.FirstOrDefault(profile => profile.Id == focusId)?.Label;

    /// <summary>The keys of an item's technologies, joined with <c>|</c> for the technology filter.</summary>
    /// <param name="itemId">The position's or project's identifier.</param>
    public string TechnologyKeys(string itemId) =>
        string.Join('|', Page.Insights.Technologies.Technologies.Where(technology => technology.ItemIds.Contains(itemId)).Select(technology => technology.Key));

    /// <summary>Writes a URL as people read it: without the scheme and the slash after a bare host.</summary>
    /// <param name="url">An absolute URL.</param>
    public static string Shorten(string url)
    {
        var text = url.StartsWith("https://", StringComparison.Ordinal) ? url[8..] : url.StartsWith("http://", StringComparison.Ordinal) ? url[7..] : url;
        return text.EndsWith('/') && text.IndexOf('/', StringComparison.Ordinal) == text.Length - 1 ? text[..^1] : text;
    }
}
