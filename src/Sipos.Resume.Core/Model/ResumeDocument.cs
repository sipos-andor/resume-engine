using Sipos.Resume.Core.Languages;

namespace Sipos.Resume.Core.Model;

/// <summary>A CV in one language, read, checked and ready to render.</summary>
/// <param name="Language">The language the CV is written in.</param>
/// <param name="Person">Who the CV is about.</param>
/// <param name="Strengths">The selected strengths, in the CV's order.</param>
/// <param name="Positions">The positions, in the CV's order, each with its projects.</param>
/// <param name="Projects">Projects that belong to no position.</param>
/// <param name="Education">The studies.</param>
/// <param name="Certificates">The certificates.</param>
/// <param name="Awards">The awards.</param>
/// <param name="Languages">The spoken languages.</param>
/// <param name="SkillGroups">The skills by group, in the CV's order.</param>
/// <param name="FocusProfiles">The position profiles a reader can focus the CV on.</param>
/// <param name="Aliases">Other spellings of skills and keywords, by their CV spelling.</param>
/// <param name="LastModified">When the content last changed, or <see langword="null"/> when the file does not say.</param>
public sealed record ResumeDocument(
    ResumeLanguage Language,
    Person Person,
    IReadOnlyList<Strength> Strengths,
    IReadOnlyList<Position> Positions,
    IReadOnlyList<Engagement> Projects,
    IReadOnlyList<Study> Education,
    IReadOnlyList<Certificate> Certificates,
    IReadOnlyList<Award> Awards,
    IReadOnlyList<SpokenLanguage> Languages,
    IReadOnlyList<SkillGroup> SkillGroups,
    IReadOnlyList<FocusProfile> FocusProfiles,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Aliases,
    DateOnly? LastModified)
{
    /// <summary>Every project, those of the positions first, in the CV's order.</summary>
    public IEnumerable<Engagement> AllEngagements => Positions.SelectMany(position => position.Engagements).Concat(Projects);
}
