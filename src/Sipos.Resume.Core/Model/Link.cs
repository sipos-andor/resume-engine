namespace Sipos.Resume.Core.Model;

/// <summary>A link with its text.</summary>
/// <param name="Url">The absolute address.</param>
/// <param name="Label">The text in the CV's language.</param>
public sealed record Link(string Url, string Label);
