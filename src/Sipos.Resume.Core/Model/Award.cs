using Sipos.Resume.Core.Dates;

namespace Sipos.Resume.Core.Model;

/// <summary>An award.</summary>
/// <param name="Title">The award's name.</param>
/// <param name="Date">When it was given, or <see langword="null"/>.</param>
/// <param name="Awarder">Who gave it, or <see langword="null"/>.</param>
/// <param name="Summary">What it was for, or <see langword="null"/>.</param>
public sealed record Award(string Title, PartialDate? Date, string? Awarder, string? Summary);
