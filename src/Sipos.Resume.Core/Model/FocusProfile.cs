namespace Sipos.Resume.Core.Model;

/// <summary>A position profile the reader can focus the CV on, such as <c>architect</c>.</summary>
/// <param name="Id">The identifier, used in <c>?focus=</c> and in file names.</param>
/// <param name="Label">The profile's name in the CV's language.</param>
/// <param name="Summary">The profile paragraph for this focus, or <see langword="null"/> to keep the general one.</param>
/// <param name="Skills">The skills this focus puts first, by name.</param>
public sealed record FocusProfile(string Id, string Label, string? Summary, IReadOnlyList<string> Skills);
