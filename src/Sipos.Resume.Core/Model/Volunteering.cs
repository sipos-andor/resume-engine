using Sipos.Resume.Core.Dates;

namespace Sipos.Resume.Core.Model;

/// <summary>Volunteer work.</summary>
/// <param name="Position">The role held, or <see langword="null"/>; the work has a position or an organization.</param>
/// <param name="Organization">The organization, or <see langword="null"/> for work done on one's own.</param>
/// <param name="Period">When, or <see langword="null"/>.</param>
/// <param name="Url">The organization's site, or <see langword="null"/>.</param>
/// <param name="Summary">What the work was, or <see langword="null"/>.</param>
/// <param name="Highlights">The achievements, in the CV's order.</param>
public sealed record Volunteering(string? Position, string? Organization, DateRange? Period, string? Url, string? Summary, IReadOnlyList<string> Highlights)
{
    /// <summary>What the work is named by: the position, or the organization when the CV gives no position.</summary>
    public string Title => Position ?? Organization ?? "";
}
