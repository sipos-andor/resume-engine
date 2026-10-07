namespace Sipos.Resume.Core.Model;

/// <summary>Whether the person takes new work.</summary>
public enum AvailabilityStatus
{
    /// <summary>Available now.</summary>
    Available,

    /// <summary>Available from a date.</summary>
    From,

    /// <summary>Available by arrangement.</summary>
    OnRequest,
}
