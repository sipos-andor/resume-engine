using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Evidence;

/// <summary>An item that shows a skill in use.</summary>
/// <param name="Id">The item's anchor.</param>
/// <param name="Title">The item's name, such as <c>Portal – Globex</c>.</param>
/// <param name="Period">When, or <see langword="null"/>.</param>
public sealed record EvidenceItem(string Id, string Title, DateRange? Period);

/// <summary>Where and when a skill was used: the evidence behind its level.</summary>
/// <param name="Skill">The skill as the skills section names it.</param>
/// <param name="Items">The positions and projects that name it, in the CV's order.</param>
/// <param name="FirstYear">The first year it was used, or <see langword="null"/> when no item has dates.</param>
/// <param name="LastYear">The last year it was used, or <see langword="null"/> while in use or without dates.</param>
/// <param name="Ongoing">Whether an item that uses it is still going on.</param>
public sealed record SkillEvidence(string Skill, IReadOnlyList<EvidenceItem> Items, int? FirstYear, int? LastYear, bool Ongoing)
{
    /// <summary>Collects the evidence of every skill in the skills section.</summary>
    /// <param name="document">The CV.</param>
    public static IReadOnlyDictionary<string, SkillEvidence> Build(ResumeDocument document)
    {
        var canonical = TechnologyIndex.Canonical(document);
        string Key(string name) => canonical.TryGetValue(Keys.Of(name), out var target) ? Keys.Of(target) : Keys.Of(name);

        var items = document.Positions
            .SelectMany(position => new[] { (Id: position.Id, Title: position.Role is null ? position.Organization : $"{position.Organization} – {position.Role}", Period: (DateRange?)position.Period, Keywords: position.Keywords) }
                .Concat(position.Engagements.Select(e => (e.Id, Title: Title(e), e.Period, e.Keywords))))
            .Concat(document.Projects.Select(e => (e.Id, Title: Title(e), e.Period, e.Keywords)))
            .Select(item => (item.Id, item.Title, item.Period, Keys: item.Keywords.Select(Key).ToHashSet(StringComparer.Ordinal)))
            .ToList();

        var evidence = new Dictionary<string, SkillEvidence>(StringComparer.Ordinal);
        foreach (var skill in document.SkillGroups.SelectMany(group => group.Skills))
        {
            var key = Key(skill.Name);
            var used = items.Where(item => item.Keys.Contains(key)).ToList();
            var periods = used.Where(item => item.Period is not null).Select(item => item.Period!.Value).ToList();
            evidence[skill.Name] = new SkillEvidence(
                skill.Name,
                [.. used.Select(item => new EvidenceItem(item.Id, item.Title, item.Period))],
                periods.Count == 0 ? null : periods.Min(period => period.Start.Year),
                periods.Count == 0 || periods.Any(period => period.IsOngoing) ? null : periods.Max(period => period.End!.Value.Year),
                periods.Any(period => period.IsOngoing));
        }

        return evidence;
    }

    private static string Title(Engagement engagement) => engagement.Client is null ? engagement.Name : $"{engagement.Name} – {engagement.Client}";
}
