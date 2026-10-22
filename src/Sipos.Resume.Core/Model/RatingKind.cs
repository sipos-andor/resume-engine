namespace Sipos.Resume.Core.Model;

/// <summary>What a skill's rating measures.</summary>
public enum RatingKind
{
    /// <summary>How well the person masters the skill; the default.</summary>
    Proficiency,

    /// <summary>How often the skill was used across the CV's work, which says nothing about how well.</summary>
    Usage,
}
