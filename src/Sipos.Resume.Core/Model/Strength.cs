namespace Sipos.Resume.Core.Model;

/// <summary>A selected strength.</summary>
/// <param name="Id">The identifier, the same in every language.</param>
/// <param name="Title">The strength in a few words.</param>
/// <param name="Summary">The strength explained, or <see langword="null"/>.</param>
/// <param name="IsShort">Whether the one-page view shows it.</param>
/// <param name="Focus">The focus profiles that put it first.</param>
public sealed record Strength(string Id, string Title, string? Summary, bool IsShort, IReadOnlyList<string> Focus);
