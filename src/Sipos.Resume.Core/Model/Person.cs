namespace Sipos.Resume.Core.Model;

/// <summary>The person a CV is about.</summary>
/// <param name="Name">The name as the CV writes it in its language.</param>
/// <param name="GivenName">The given name, or <see langword="null"/>.</param>
/// <param name="FamilyName">The family name, or <see langword="null"/>.</param>
/// <param name="Title">The title, such as <c>Software Architect</c>, or <see langword="null"/>.</param>
/// <param name="Tagline">A line under the title, or <see langword="null"/>.</param>
/// <param name="Summary">The profile paragraph, or <see langword="null"/>.</param>
/// <param name="Phone">A public phone number as the reader sees it, or <see langword="null"/>.</param>
/// <param name="Url">The CV's address in this language, or <see langword="null"/>.</param>
/// <param name="Location">Where the person works from, as the CV writes it, or <see langword="null"/>.</param>
/// <param name="Contact">The way to get in touch, such as a contact form, or <see langword="null"/>.</param>
/// <param name="Availability">Whether the person takes new work, or <see langword="null"/>.</param>
/// <param name="Profiles">Profiles on other sites.</param>
/// <param name="Image">A picture's URL, or <see langword="null"/>.</param>
public sealed record Person(
    string Name,
    string? GivenName,
    string? FamilyName,
    string? Title,
    string? Tagline,
    string? Summary,
    string? Phone,
    string? Url,
    string? Location,
    Link? Contact,
    Availability? Availability,
    IReadOnlyList<Profile> Profiles,
    string? Image)
{
    /// <summary>The phone number in E.164 form for <c>tel:</c> links, such as <c>+36309036622</c>, or <see langword="null"/>.</summary>
    public string? PhoneE164 => Phone is null ? null : "+" + new string([.. Phone.Where(char.IsAsciiDigit)]);
}
