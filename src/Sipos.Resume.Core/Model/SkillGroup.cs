namespace Sipos.Resume.Core.Model;

/// <summary>A group of skills, such as <c>.NET platform</c>.</summary>
/// <param name="Name">The group's name in the CV's language.</param>
/// <param name="Skills">The skills, in the CV's order.</param>
public sealed record SkillGroup(string Name, IReadOnlyList<Skill> Skills);
