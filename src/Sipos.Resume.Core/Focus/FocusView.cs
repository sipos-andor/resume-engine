using Sipos.Resume.Core.Evidence;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Focus;

/// <summary>
/// The CV seen through a position profile: which items it puts forward and in which order the strengths and skill
/// groups come. The page applies it for <c>?focus=</c>; the tailored documents are rendered with it.
/// </summary>
/// <param name="Profile">The profile.</param>
/// <param name="StrengthOrder">The strengths' identifiers, those of the profile first.</param>
/// <param name="SkillGroupOrder">The skill groups' indexes in the CV, those with the profile's skills first.</param>
/// <param name="Emphasized">The identifiers of the positions, projects and strengths the profile puts forward.</param>
/// <param name="EmphasizedSkills">The keys of the profile's skills, an alias by its term's key.</param>
public sealed record FocusView(
    FocusProfile Profile,
    IReadOnlyList<string> StrengthOrder,
    IReadOnlyList<int> SkillGroupOrder,
    IReadOnlySet<string> Emphasized,
    IReadOnlySet<string> EmphasizedSkills)
{
    /// <summary>Builds the view of every profile of a CV.</summary>
    /// <param name="document">The CV.</param>
    /// <remarks>
    /// An item is put forward when its <c>x-focus</c> names the profile, or, for positions and projects, when it uses
    /// one of the profile's skills. Order is stable: within each half the CV's order stays.
    /// </remarks>
    public static IReadOnlyList<FocusView> Build(ResumeDocument document) => [.. document.FocusProfiles.Select(profile => Build(document, profile))];

    private static FocusView Build(ResumeDocument document, FocusProfile profile)
    {
        // Every name through the aliases, as the technology index and the skill evidence fold them: a profile asking for
        // Kubernetes finds the position that names K8s.
        var canonical = TechnologyIndex.Canonical(document);
        string Key(string name) => canonical.TryGetValue(Keys.Of(name), out var term) ? Keys.Of(term) : Keys.Of(name);
        var skills = profile.Skills.Select(Key).ToHashSet(StringComparer.Ordinal);
        bool Uses(IEnumerable<string> keywords) => keywords.Any(keyword => skills.Contains(Key(keyword)));
        bool Tagged(IReadOnlyList<string> focus) => focus.Contains(profile.Id, StringComparer.Ordinal);

        var emphasized = new HashSet<string>(StringComparer.Ordinal);
        foreach (var position in document.Positions)
        {
            if (Tagged(position.Focus) || Uses(position.Keywords))
            {
                emphasized.Add(position.Id);
            }

            emphasized.UnionWith(position.Engagements.Where(e => Tagged(e.Focus) || Uses(e.Keywords)).Select(e => e.Id));
        }

        emphasized.UnionWith(document.Projects.Where(e => Tagged(e.Focus) || Uses(e.Keywords)).Select(e => e.Id));
        emphasized.UnionWith(document.Strengths.Where(s => Tagged(s.Focus)).Select(s => s.Id));

        var strengths = document.Strengths.OrderBy(s => emphasized.Contains(s.Id) ? 0 : 1).Select(s => s.Id).ToList();
        var groups = document.SkillGroups
            .Select((group, index) => (Index: index, Matches: group.Skills.Count(skill => skills.Contains(Key(skill.Name)))))
            .OrderByDescending(group => group.Matches > 0)
            .ThenByDescending(group => group.Matches)
            .Select(group => group.Index)
            .ToList();

        return new FocusView(profile, strengths, groups, emphasized, skills);
    }
}
