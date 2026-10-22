using Sipos.Resume.Core.Dates;

namespace Sipos.Resume.Core.Model;

/// <summary>A publication.</summary>
/// <param name="Name">The title.</param>
/// <param name="Publisher">The publisher, or <see langword="null"/>.</param>
/// <param name="Released">When it was published, or <see langword="null"/>.</param>
/// <param name="Url">Where it can be found, or <see langword="null"/>.</param>
/// <param name="Summary">What it is or the person's part in it, or <see langword="null"/>.</param>
public sealed record Publication(string Name, string? Publisher, PartialDate? Released, string? Url, string? Summary);
