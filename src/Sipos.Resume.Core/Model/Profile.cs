namespace Sipos.Resume.Core.Model;

/// <summary>A profile on another site.</summary>
/// <param name="Network">The site, such as <c>GitHub</c>.</param>
/// <param name="Url">The profile's absolute address.</param>
/// <param name="Username">The user name there, or <see langword="null"/>.</param>
public sealed record Profile(string Network, string Url, string? Username);
