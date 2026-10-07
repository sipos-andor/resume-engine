namespace Sipos.Resume.Core.Model;

/// <summary>A language the person speaks.</summary>
/// <param name="Name">The language's name in the CV's language.</param>
/// <param name="Fluency">How well, in the CV's language, or <see langword="null"/>.</param>
public sealed record SpokenLanguage(string Name, string? Fluency);
