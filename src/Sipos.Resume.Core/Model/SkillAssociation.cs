using Sipos.Resume.Core.Dates;

namespace Sipos.Resume.Core.Model;

/// <summary>A use of a skill the person confirmed in a position or project whose keywords may not show it.</summary>
/// <param name="Skill">The skill as the skills section names it.</param>
/// <param name="ItemId">The position's or project's identifier.</param>
/// <param name="Period">When the skill was used there, or <see langword="null"/> for the item's own period.</param>
public sealed record SkillAssociation(string Skill, string ItemId, DateRange? Period = null);
