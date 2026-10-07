namespace Sipos.Resume.Core.Model;

/// <summary>A skill with its level.</summary>
/// <param name="Name">The skill, such as <c>C#</c>; technology names are not translated.</param>
/// <param name="Rating">The level from 1 to 5, or <see langword="null"/> when the CV gives none.</param>
/// <param name="Level">The level in words in the CV's language, or <see langword="null"/>.</param>
public sealed record Skill(string Name, int? Rating, string? Level);
