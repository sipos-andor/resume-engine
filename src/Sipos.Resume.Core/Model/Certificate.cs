using Sipos.Resume.Core.Dates;

namespace Sipos.Resume.Core.Model;

/// <summary>A certificate or a course.</summary>
/// <param name="Name">The certificate's name.</param>
/// <param name="Date">When it was issued, or <see langword="null"/>.</param>
/// <param name="Issuer">Who issued it, or <see langword="null"/>.</param>
/// <param name="Url">Where it can be verified, or <see langword="null"/>.</param>
public sealed record Certificate(string Name, PartialDate? Date, string? Issuer, string? Url);
