namespace Sipos.Resume.Core.Model;

/// <summary>The skills' legend: what their ratings mean, as the CV explains them.</summary>
/// <param name="Steps">The ratings with their words, the highest first.</param>
/// <param name="Note">What the ratings mean and do not mean, or <see langword="null"/>.</param>
public sealed record SkillScale(IReadOnlyList<SkillScaleStep> Steps, string? Note);

/// <summary>A step of the skills' scale.</summary>
/// <param name="Rating">The rating from 1 to 5.</param>
/// <param name="Label">The rating in words, in the CV's language.</param>
public sealed record SkillScaleStep(int Rating, string Label);
