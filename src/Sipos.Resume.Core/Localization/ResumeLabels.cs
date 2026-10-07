using System.Globalization;
using System.Resources;

namespace Sipos.Resume.Core.Localization;

/// <summary>
/// The engine's own words in a CV's language: section headings for the page and the documents, the standard headings
/// of the ATS documents, the level words and a few labels. Content files hold the CV's texts; these are the frame.
/// </summary>
/// <remarks>
/// Decision: one set of satellite resources (<c>ResumeLabels.hu.resx</c> next to the English <c>ResumeLabels.resx</c>)
/// shared by the theme and every document writer.
/// Why: a heading must read the same on the page and in the PDF; and <see cref="Supports"/> lets the build fail for a
/// content language that has no labels, instead of falling back to English unnoticed.
/// </remarks>
public sealed class ResumeLabels
{
    private static readonly ResourceManager Resources = new("Sipos.Resume.Core.Localization.ResumeLabels", typeof(ResumeLabels).Assembly);
    private readonly CultureInfo _culture;

    /// <summary>Creates the labels of a culture.</summary>
    /// <param name="culture">The CV's culture, such as <c>hu-HU</c>.</param>
    public ResumeLabels(CultureInfo culture) => _culture = culture;

    /// <summary>The culture the labels are in, which also formats the CV's dates and numbers.</summary>
    public CultureInfo Culture => _culture;

    /// <summary>The languages the engine has labels for, by neutral culture name: <c>en</c>, <c>hu</c>, <c>hr</c>, <c>sr-Latn</c>.</summary>
    public static IReadOnlyList<string> SupportedLanguages { get; } = ["en", "hu", "hr", "sr-Latn"];

    /// <summary>Whether the engine has its own labels for a culture, itself or a parent, rather than the English fallback.</summary>
    /// <param name="culture">The culture.</param>
    public static bool Supports(CultureInfo culture)
    {
        for (var current = culture; !current.Equals(CultureInfo.InvariantCulture); current = current.Parent)
        {
            if (SupportedLanguages.Contains(current.Name, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the label of a key; the keys are the properties of this class.</summary>
    /// <param name="key">The key, such as <c>Experience</c>.</param>
    /// <exception cref="KeyNotFoundException">The resources have no such key.</exception>
    public string this[string key] => Resources.GetString(key, _culture) ?? throw new KeyNotFoundException($"The labels have no '{key}' key.");

    /// <summary>The word for a level from 1 to 5, such as <c>Expert</c> for 5.</summary>
    /// <param name="rating">The level.</param>
    public string Rating(int rating) => this[$"Rating{Math.Clamp(rating, 1, 5)}"];

    /// <summary>The level as a fraction, such as <c>4 of 5</c>, for screen readers.</summary>
    /// <param name="rating">The level.</param>
    public string RatingOf(int rating) => string.Format(_culture, this["RatingOf"], rating);

    /// <summary>The label <c>Profile</c>.</summary>
    public string Profile => this[nameof(Profile)];

    /// <summary>The label <c>Strengths</c>.</summary>
    public string Strengths => this[nameof(Strengths)];

    /// <summary>The label <c>Experience</c>.</summary>
    public string Experience => this[nameof(Experience)];

    /// <summary>The label <c>Projects</c>.</summary>
    public string Projects => this[nameof(Projects)];

    /// <summary>The label <c>ClientProjects</c>.</summary>
    public string ClientProjects => this[nameof(ClientProjects)];

    /// <summary>The label <c>Skills</c>.</summary>
    public string Skills => this[nameof(Skills)];

    /// <summary>The label <c>Education</c>.</summary>
    public string Education => this[nameof(Education)];

    /// <summary>The label <c>Certificates</c>.</summary>
    public string Certificates => this[nameof(Certificates)];

    /// <summary>The label <c>Awards</c>.</summary>
    public string Awards => this[nameof(Awards)];

    /// <summary>The label <c>Languages</c>.</summary>
    public string Languages => this[nameof(Languages)];

    /// <summary>The label <c>Timeline</c>.</summary>
    public string Timeline => this[nameof(Timeline)];

    /// <summary>The label <c>Contact</c>.</summary>
    public string Contact => this[nameof(Contact)];

    /// <summary>The label <c>Downloads</c>.</summary>
    public string Downloads => this[nameof(Downloads)];

    /// <summary>The label <c>AtsSummary</c>.</summary>
    public string AtsSummary => this[nameof(AtsSummary)];

    /// <summary>The label <c>AtsSkills</c>.</summary>
    public string AtsSkills => this[nameof(AtsSkills)];

    /// <summary>The label <c>AtsExperience</c>.</summary>
    public string AtsExperience => this[nameof(AtsExperience)];

    /// <summary>The label <c>AtsProjects</c>.</summary>
    public string AtsProjects => this[nameof(AtsProjects)];

    /// <summary>The label <c>AtsEducation</c>.</summary>
    public string AtsEducation => this[nameof(AtsEducation)];

    /// <summary>The label <c>AtsCertifications</c>.</summary>
    public string AtsCertifications => this[nameof(AtsCertifications)];

    /// <summary>The label <c>AtsAwards</c>.</summary>
    public string AtsAwards => this[nameof(AtsAwards)];

    /// <summary>The label <c>AtsLanguages</c>.</summary>
    public string AtsLanguages => this[nameof(AtsLanguages)];

    /// <summary>The label <c>Rating5</c>.</summary>
    public string Rating5 => this[nameof(Rating5)];

    /// <summary>The label <c>Rating4</c>.</summary>
    public string Rating4 => this[nameof(Rating4)];

    /// <summary>The label <c>Rating3</c>.</summary>
    public string Rating3 => this[nameof(Rating3)];

    /// <summary>The label <c>Rating2</c>.</summary>
    public string Rating2 => this[nameof(Rating2)];

    /// <summary>The label <c>Rating1</c>.</summary>
    public string Rating1 => this[nameof(Rating1)];

    /// <summary>The label <c>Present</c>.</summary>
    public string Present => this[nameof(Present)];

    /// <summary>The label <c>Client</c>.</summary>
    public string Client => this[nameof(Client)];

    /// <summary>The label <c>Role</c>.</summary>
    public string Role => this[nameof(Role)];

    /// <summary>The label <c>Technologies</c>.</summary>
    public string Technologies => this[nameof(Technologies)];

    /// <summary>The label <c>Phone</c>.</summary>
    public string Phone => this[nameof(Phone)];

    /// <summary>The label <c>Web</c>.</summary>
    public string Web => this[nameof(Web)];

    /// <summary>The label <c>Location</c>.</summary>
    public string Location => this[nameof(Location)];

    /// <summary>The label <c>EmailImage</c>.</summary>
    public string EmailImage => this[nameof(EmailImage)];

    /// <summary>The label <c>CurriculumVitae</c>.</summary>
    public string CurriculumVitae => this[nameof(CurriculumVitae)];

    /// <summary>The label <c>QrCaption</c>.</summary>
    public string QrCaption => this[nameof(QrCaption)];

    /// <summary>The label <c>Focus</c>.</summary>
    public string Focus => this[nameof(Focus)];

    /// <summary>The label <c>Highlights</c>.</summary>
    public string Highlights => this[nameof(Highlights)];

    /// <summary>The label <c>OtherLanguages</c>.</summary>
    public string OtherLanguages => this[nameof(OtherLanguages)];
}
