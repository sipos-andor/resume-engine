using Sipos.Resume.Core.Dates;

namespace Sipos.Resume.Core.Model;

/// <summary>The availability indicator.</summary>
/// <param name="Status">Whether and when the person is available.</param>
/// <param name="From">The date from which, for <see cref="AvailabilityStatus.From"/>.</param>
/// <param name="Url">A page that explains the offer, or <see langword="null"/>.</param>
/// <param name="Label">The indicator's text in the CV's language.</param>
public sealed record Availability(AvailabilityStatus Status, PartialDate? From, string? Url, string Label);
