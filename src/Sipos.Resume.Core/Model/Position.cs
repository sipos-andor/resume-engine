using Sipos.Resume.Core.Dates;

namespace Sipos.Resume.Core.Model;

/// <summary>A position held at an organization, with the projects done in it.</summary>
/// <param name="Id">The identifier, the same in every language; the page's anchor.</param>
/// <param name="Organization">The organization's name.</param>
/// <param name="Note">A note on the organization, such as <c>formerly a sole proprietorship</c>, or <see langword="null"/>.</param>
/// <param name="Role">The role held, or <see langword="null"/>.</param>
/// <param name="Url">The organization's site, or <see langword="null"/>.</param>
/// <param name="Location">Where the work was done, or <see langword="null"/>.</param>
/// <param name="Period">When.</param>
/// <param name="Summary">What the position was about, or <see langword="null"/>.</param>
/// <param name="Highlights">The achievements.</param>
/// <param name="Keywords">The technologies used directly in the position.</param>
/// <param name="Engagements">The projects done in the position.</param>
/// <param name="IsShort">Whether the one-page view shows it.</param>
/// <param name="ShortHighlights">How many highlights the one-page view shows; all when <see langword="null"/>.</param>
/// <param name="Focus">The focus profiles that put it first.</param>
public sealed record Position(
    string Id,
    string Organization,
    string? Note,
    string? Role,
    string? Url,
    string? Location,
    DateRange Period,
    string? Summary,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<Engagement> Engagements,
    bool IsShort,
    int? ShortHighlights,
    IReadOnlyList<string> Focus);
