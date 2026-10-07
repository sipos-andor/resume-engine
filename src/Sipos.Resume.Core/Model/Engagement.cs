using Sipos.Resume.Core.Dates;

namespace Sipos.Resume.Core.Model;

/// <summary>A project, for a client or within a position.</summary>
/// <param name="Id">The identifier, the same in every language; the page's anchor.</param>
/// <param name="Name">The project's name.</param>
/// <param name="Client">The client or organization the project was for, or <see langword="null"/>.</param>
/// <param name="Roles">The roles held.</param>
/// <param name="Description">What the project was, or <see langword="null"/>.</param>
/// <param name="Period">When, or <see langword="null"/> when the CV gives no dates.</param>
/// <param name="Highlights">The achievements.</param>
/// <param name="Keywords">The technologies used.</param>
/// <param name="Url">The project's site, or <see langword="null"/>.</param>
/// <param name="IsShort">Whether the one-page view shows it.</param>
/// <param name="ShortHighlights">How many highlights the one-page view shows; all when <see langword="null"/>.</param>
/// <param name="Focus">The focus profiles that put it first.</param>
public sealed record Engagement(
    string Id,
    string Name,
    string? Client,
    IReadOnlyList<string> Roles,
    string? Description,
    DateRange? Period,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Keywords,
    string? Url,
    bool IsShort,
    int? ShortHighlights,
    IReadOnlyList<string> Focus);
