using System.Text.Json.Serialization;

namespace Sipos.Resume.Core.Content;

/// <summary>
/// A JSON Resume document (schema 1.0.0) with the engine's optional <c>x-</c> extensions. Every property is optional,
/// as in the schema; the validator decides what the engine needs.
/// </summary>
/// <remarks>
/// <para>
/// Decision: the extensions are additional properties with an <c>x-</c> prefix, never a change to a standard field.
/// Why: a file stays a valid JSON Resume that any other tool reads, and drops only what it does not know.
/// </para>
/// <para>
/// Decision: the lists have setters, the other properties are init-only.
/// Why: the source-generated serializer builds init-only properties in one object initializer and writes the default
/// (null) for a list the file leaves out; a settable list keeps its empty default instead.
/// </para>
/// </remarks>
public sealed record JsonResume
{
    /// <summary>The JSON schema the file declares, such as the JSON Resume schema's URL.</summary>
    [JsonPropertyName("$schema")] public string? Schema { get; init; }

    /// <summary>The person: name, title, contact channels, location and profiles.</summary>
    public JsonResumeBasics? Basics { get; init; }

    /// <summary>The positions held, in the order the CV shows them.</summary>
    public IReadOnlyList<JsonResumeWork> Work { get; set; } = [];

    /// <summary>The studies.</summary>
    public IReadOnlyList<JsonResumeEducation> Education { get; set; } = [];

    /// <summary>The awards.</summary>
    public IReadOnlyList<JsonResumeAward> Awards { get; set; } = [];

    /// <summary>The certificates.</summary>
    public IReadOnlyList<JsonResumeCertificate> Certificates { get; set; } = [];

    /// <summary>The skills; the engine reads one entry per group and level.</summary>
    public IReadOnlyList<JsonResumeSkill> Skills { get; set; } = [];

    /// <summary>The spoken languages.</summary>
    public IReadOnlyList<JsonResumeLanguage> Languages { get; set; } = [];

    /// <summary>The projects; with <c>x-work</c> a project belongs to a position.</summary>
    public IReadOnlyList<JsonResumeProject> Projects { get; set; } = [];

    /// <summary>Facts about the file itself.</summary>
    public JsonResumeMeta? Meta { get; init; }

    /// <summary>Extension: the selected strengths shown after the profile.</summary>
    [JsonPropertyName("x-strengths")] public IReadOnlyList<JsonResumeStrength> Strengths { get; set; } = [];

    /// <summary>Extension: the position profiles a reader can focus the CV on.</summary>
    [JsonPropertyName("x-focusProfiles")] public IReadOnlyList<JsonResumeFocusProfile> FocusProfiles { get; set; } = [];

    /// <summary>Extension: other spellings of a skill or keyword, such as <c>dotnet</c> for <c>.NET</c>, for matching a job ad.</summary>
    [JsonPropertyName("x-aliases")] public IReadOnlyDictionary<string, IReadOnlyList<string>> Aliases { get; set; } = new Dictionary<string, IReadOnlyList<string>>();
}

/// <summary>The person a JSON Resume is about.</summary>
public sealed record JsonResumeBasics
{
    /// <summary>The name as the CV writes it in its language.</summary>
    public string? Name { get; init; }

    /// <summary>The title, such as <c>Software Architect</c>.</summary>
    public string? Label { get; init; }

    /// <summary>A picture's URL.</summary>
    public string? Image { get; init; }

    /// <summary>An e-mail address. The engine refuses it: an address on a public page is harvested.</summary>
    public string? Email { get; init; }

    /// <summary>A public phone number, written as the reader should see it.</summary>
    public string? Phone { get; init; }

    /// <summary>The CV's own address in this language.</summary>
    public string? Url { get; init; }

    /// <summary>A short profile paragraph.</summary>
    public string? Summary { get; init; }

    /// <summary>Where the person works from.</summary>
    public JsonResumeLocation? Location { get; init; }

    /// <summary>Profiles on other sites.</summary>
    public IReadOnlyList<JsonResumeProfile> Profiles { get; set; } = [];

    /// <summary>Extension: the given name, for structured data and sharing.</summary>
    [JsonPropertyName("x-givenName")] public string? GivenName { get; init; }

    /// <summary>Extension: the family name, for structured data and sharing.</summary>
    [JsonPropertyName("x-familyName")] public string? FamilyName { get; init; }

    /// <summary>Extension: a line under the title, such as the fields of expertise.</summary>
    [JsonPropertyName("x-tagline")] public string? Tagline { get; init; }

    /// <summary>Extension: the way to get in touch instead of an e-mail address, such as a contact form.</summary>
    [JsonPropertyName("x-contact")] public JsonResumeLink? Contact { get; init; }

    /// <summary>Extension: whether and when the person is available for new work.</summary>
    [JsonPropertyName("x-availability")] public JsonResumeAvailability? Availability { get; init; }
}

/// <summary>Where a person or a position is.</summary>
public sealed record JsonResumeLocation
{
    /// <summary>The street address.</summary>
    public string? Address { get; init; }

    /// <summary>The postal code.</summary>
    public string? PostalCode { get; init; }

    /// <summary>The city.</summary>
    public string? City { get; init; }

    /// <summary>The two-letter country code.</summary>
    public string? CountryCode { get; init; }

    /// <summary>The region, such as a state or <c>Europe</c>.</summary>
    public string? Region { get; init; }

    /// <summary>Extension: the location as the CV writes it, such as <c>Remote from Europe</c>.</summary>
    [JsonPropertyName("x-label")] public string? Label { get; init; }
}

/// <summary>A profile on another site.</summary>
public sealed record JsonResumeProfile
{
    /// <summary>The site, such as <c>GitHub</c>.</summary>
    public string? Network { get; init; }

    /// <summary>The user name there.</summary>
    public string? Username { get; init; }

    /// <summary>The profile's address.</summary>
    public string? Url { get; init; }
}

/// <summary>Extension: a link with its text.</summary>
public sealed record JsonResumeLink
{
    /// <summary>The address.</summary>
    public string? Url { get; init; }

    /// <summary>The link's text in the CV's language.</summary>
    public string? Label { get; init; }
}

/// <summary>Extension: whether the person takes new work.</summary>
public sealed record JsonResumeAvailability
{
    /// <summary><c>available</c>, <c>from</c> (with <see cref="From"/>) or <c>on-request</c>.</summary>
    public string? Status { get; init; }

    /// <summary>The date from which the person is available, for status <c>from</c>.</summary>
    public string? From { get; init; }

    /// <summary>The page that explains the offer, such as a senior capacity page.</summary>
    public string? Url { get; init; }

    /// <summary>The indicator's text in the CV's language.</summary>
    public string? Label { get; init; }
}

/// <summary>A position.</summary>
public sealed record JsonResumeWork
{
    /// <summary>The organization's name.</summary>
    public string? Name { get; init; }

    /// <summary>Where the work was done.</summary>
    public string? Location { get; init; }

    /// <summary>A note on the organization, such as <c>formerly a sole proprietorship</c>.</summary>
    public string? Description { get; init; }

    /// <summary>The role held.</summary>
    public string? Position { get; init; }

    /// <summary>The organization's site.</summary>
    public string? Url { get; init; }

    /// <summary>The first date.</summary>
    public string? StartDate { get; init; }

    /// <summary>The last date; empty for a current position.</summary>
    public string? EndDate { get; init; }

    /// <summary>What the position was about.</summary>
    public string? Summary { get; init; }

    /// <summary>The achievements, most important first.</summary>
    public IReadOnlyList<string> Highlights { get; set; } = [];

    /// <summary>Extension: the position's identifier, the same in every language; anchors and projects use it.</summary>
    [JsonPropertyName("x-id")] public string? Id { get; init; }

    /// <summary>Extension: the technologies used directly in the position.</summary>
    [JsonPropertyName("x-keywords")] public IReadOnlyList<string> Keywords { get; set; } = [];

    /// <summary>Extension: whether the one-page view shows the position.</summary>
    [JsonPropertyName("x-short")] public bool Short { get; init; }

    /// <summary>Extension: how many highlights the one-page view shows; all when not set.</summary>
    [JsonPropertyName("x-shortHighlights")] public int? ShortHighlights { get; init; }

    /// <summary>Extension: the focus profiles that put this position first.</summary>
    [JsonPropertyName("x-focus")] public IReadOnlyList<string> Focus { get; set; } = [];
}

/// <summary>A study.</summary>
public sealed record JsonResumeEducation
{
    /// <summary>The school.</summary>
    public string? Institution { get; init; }

    /// <summary>The school's site.</summary>
    public string? Url { get; init; }

    /// <summary>The field of study.</summary>
    public string? Area { get; init; }

    /// <summary>The degree, such as <c>BSc</c>.</summary>
    public string? StudyType { get; init; }

    /// <summary>The first date.</summary>
    public string? StartDate { get; init; }

    /// <summary>The last date.</summary>
    public string? EndDate { get; init; }
}

/// <summary>An award.</summary>
public sealed record JsonResumeAward
{
    /// <summary>The award's name.</summary>
    public string? Title { get; init; }

    /// <summary>When it was given.</summary>
    public string? Date { get; init; }

    /// <summary>Who gave it.</summary>
    public string? Awarder { get; init; }

    /// <summary>What it was for.</summary>
    public string? Summary { get; init; }
}

/// <summary>A certificate.</summary>
public sealed record JsonResumeCertificate
{
    /// <summary>The certificate's name.</summary>
    public string? Name { get; init; }

    /// <summary>When it was issued.</summary>
    public string? Date { get; init; }

    /// <summary>Who issued it.</summary>
    public string? Issuer { get; init; }

    /// <summary>Where it can be verified.</summary>
    public string? Url { get; init; }
}

/// <summary>A group of skills at one level.</summary>
public sealed record JsonResumeSkill
{
    /// <summary>The group's name, such as <c>.NET platform</c>.</summary>
    public string? Name { get; init; }

    /// <summary>The level in words, in the CV's language.</summary>
    public string? Level { get; init; }

    /// <summary>The skills, such as <c>C#</c>.</summary>
    public IReadOnlyList<string> Keywords { get; set; } = [];

    /// <summary>Extension: the level as a number from 1 to 5, the same in every language.</summary>
    [JsonPropertyName("x-rating")] public int? Rating { get; init; }
}

/// <summary>A spoken language.</summary>
public sealed record JsonResumeLanguage
{
    /// <summary>The language's name in the CV's language.</summary>
    public string? Language { get; init; }

    /// <summary>How well, in the CV's language.</summary>
    public string? Fluency { get; init; }
}

/// <summary>A project, on its own or as part of a position.</summary>
public sealed record JsonResumeProject
{
    /// <summary>The project's name.</summary>
    public string? Name { get; init; }

    /// <summary>What the project was.</summary>
    public string? Description { get; init; }

    /// <summary>The achievements.</summary>
    public IReadOnlyList<string> Highlights { get; set; } = [];

    /// <summary>The technologies used.</summary>
    public IReadOnlyList<string> Keywords { get; set; } = [];

    /// <summary>The first date.</summary>
    public string? StartDate { get; init; }

    /// <summary>The last date.</summary>
    public string? EndDate { get; init; }

    /// <summary>The project's site.</summary>
    public string? Url { get; init; }

    /// <summary>The roles held.</summary>
    public IReadOnlyList<string> Roles { get; set; } = [];

    /// <summary>The client or organization the project was for.</summary>
    public string? Entity { get; init; }

    /// <summary>The kind of project, such as <c>application</c>.</summary>
    public string? Type { get; init; }

    /// <summary>Extension: the project's identifier, the same in every language.</summary>
    [JsonPropertyName("x-id")] public string? Id { get; init; }

    /// <summary>Extension: the identifier of the position the project belongs to.</summary>
    [JsonPropertyName("x-work")] public string? Work { get; init; }

    /// <summary>Extension: whether the one-page view shows the project.</summary>
    [JsonPropertyName("x-short")] public bool Short { get; init; }

    /// <summary>Extension: how many highlights the one-page view shows.</summary>
    [JsonPropertyName("x-shortHighlights")] public int? ShortHighlights { get; init; }

    /// <summary>Extension: the focus profiles that put this project first.</summary>
    [JsonPropertyName("x-focus")] public IReadOnlyList<string> Focus { get; set; } = [];
}

/// <summary>Facts about the file.</summary>
public sealed record JsonResumeMeta
{
    /// <summary>The canonical address of the JSON file.</summary>
    public string? Canonical { get; init; }

    /// <summary>The CV's version.</summary>
    public string? Version { get; init; }

    /// <summary>When the content last changed, as a date or date and time.</summary>
    public string? LastModified { get; init; }

    /// <summary>Extension: the culture for dates and numbers, such as <c>hu-HU</c>.</summary>
    [JsonPropertyName("x-culture")] public string? Culture { get; init; }

    /// <summary>Extension: the language's name in itself, such as <c>Magyar</c>.</summary>
    [JsonPropertyName("x-endonym")] public string? Endonym { get; init; }

    /// <summary>Extension: the Open Graph locale, such as <c>hu_HU</c>.</summary>
    [JsonPropertyName("x-ogLocale")] public string? OgLocale { get; init; }

    /// <summary>Extension: the path segment of the language's pages, such as <c>sr</c> for <c>sr-Latn</c>.</summary>
    [JsonPropertyName("x-path")] public string? Path { get; init; }
}

/// <summary>Extension: a selected strength.</summary>
public sealed record JsonResumeStrength
{
    /// <summary>The strength's identifier, the same in every language.</summary>
    public string? Id { get; init; }

    /// <summary>The strength in a few words.</summary>
    public string? Title { get; init; }

    /// <summary>The strength explained.</summary>
    public string? Summary { get; init; }

    /// <summary>Whether the one-page view shows it.</summary>
    public bool Short { get; init; }

    /// <summary>The focus profiles that put it first.</summary>
    public IReadOnlyList<string> Focus { get; set; } = [];
}

/// <summary>Extension: a position profile the reader can focus the CV on.</summary>
public sealed record JsonResumeFocusProfile
{
    /// <summary>The profile's identifier, used in <c>?focus=</c> and the same in every language.</summary>
    public string? Id { get; init; }

    /// <summary>The profile's name in the CV's language.</summary>
    public string? Label { get; init; }

    /// <summary>The profile paragraph for this focus, replacing the general one.</summary>
    public string? Summary { get; init; }

    /// <summary>The skills this focus puts first, by name.</summary>
    public IReadOnlyList<string> Skills { get; set; } = [];
}
