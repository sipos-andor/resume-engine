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
    /// <summary>
    /// The phone number as a <c>tel:</c> link dials it: <c>+</c> and the digits for a number written internationally,
    /// such as <c>+36309036622</c>; the digits alone for a local one, such as <c>02079460000</c>; or
    /// <see langword="null"/> when it includes an extension or characters other than number formatting.
    /// </summary>
    /// <remarks>
    /// Decision: a <c>+</c> only when the CV writes one.
    /// Why: adding it to a local number such as <c>020 7946 0000</c> would dial a different, invalid number.
    /// </remarks>
    public string? PhoneDial
    {
        get
        {
            if (Phone is null)
            {
                return null;
            }

            var number = Phone.Trim();
            var international = number.StartsWith('+');
            var body = international ? number[1..] : number;
            if (body.Any(character => !char.IsAsciiDigit(character) && !char.IsWhiteSpace(character)
                && character is not ('(' or ')' or '-' or '.' or '/')))
            {
                return null;
            }

            var digits = new string([.. body.Where(char.IsAsciiDigit)]);
            return digits.Length == 0 ? null : international ? "+" + digits : digits;
        }
    }
}
