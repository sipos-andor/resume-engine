using System.Diagnostics;
using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Evidence;
using Sipos.Resume.Core.Focus;
using Sipos.Resume.Core.Localization;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Artifacts;

/// <summary>A section of a document.</summary>
public enum DocumentSection
{
    /// <summary>The profile paragraph; the ATS layout calls it the summary and lists the strengths under it.</summary>
    Profile,

    /// <summary>The selected strengths; the designed layout only.</summary>
    Strengths,

    /// <summary>The positions, each with its projects.</summary>
    Experience,

    /// <summary>The projects that belong to no position.</summary>
    Projects,

    /// <summary>The skills by group, with levels.</summary>
    Skills,

    /// <summary>The studies; in the designed layout also the certificates, under one heading.</summary>
    Education,

    /// <summary>The certificates; the ATS layout only.</summary>
    Certificates,

    /// <summary>The awards.</summary>
    Awards,

    /// <summary>The publications.</summary>
    Publications,

    /// <summary>The volunteer work.</summary>
    Volunteering,

    /// <summary>The spoken languages.</summary>
    Languages,
}

/// <summary>
/// What a document says and in which order, the same for every writer: the sections of its layout, the summary and
/// the order of the strengths and skill groups under its focus, which highlights each item keeps, the headings and
/// the dates in the CV's language. A writer only decides how it looks.
/// </summary>
/// <remarks>
/// <para>
/// Decision: one outline for the PDF, DOCX, Markdown and text writers.
/// Why: a recruiter who opens the PDF and the DOCX of the same download expects the same CV; four writers that each
/// decide what to leave out and how to write a date drift apart with the first change.
/// </para>
/// <para>
/// Decision: the ATS layout puts the skills right after the summary, keeps certificates under their own heading and
/// lists the strengths as bullets under the summary.
/// Why: applicant tracking systems look for standard headings (Summary, Skills, Work Experience, Education,
/// Certifications) and score the keywords they find early; "Selected strengths" is no heading they know.
/// </para>
/// <para>
/// Decision: a tailored document keeps every position and project in date order and only shortens the ones its
/// focus does not put forward.
/// Why: a gap in the dates raises a question the reader cannot ask; a reordered history reads as hiding something.
/// </para>
/// </remarks>
public sealed class DocumentOutline
{
    /// <summary>How many highlights a tailored document keeps of a position or project its focus does not put forward.</summary>
    public const int UnfocusedHighlights = 2;

    /// <summary>The longest level in words that fits the layouts' usual level column, such as <c>Proficient</c>.</summary>
    public const int ShortLevelLength = 12;

    private static readonly DocumentSection[] DesignedSections =
    [
        DocumentSection.Profile, DocumentSection.Strengths, DocumentSection.Experience, DocumentSection.Projects,
        DocumentSection.Skills, DocumentSection.Education, DocumentSection.Awards, DocumentSection.Publications,
        DocumentSection.Volunteering, DocumentSection.Languages,
    ];

    private static readonly DocumentSection[] AtsSections =
    [
        DocumentSection.Profile, DocumentSection.Skills, DocumentSection.Experience, DocumentSection.Projects,
        DocumentSection.Education, DocumentSection.Certificates, DocumentSection.Awards, DocumentSection.Publications,
        DocumentSection.Volunteering, DocumentSection.Languages,
    ];

    private readonly IReadOnlyDictionary<string, SkillEvidence> _evidence;

    private DocumentOutline(ResumeDocument document, DocumentVariant variant, FocusView? focus)
    {
        _evidence = SkillEvidence.Build(document);
        Document = document;
        Variant = variant;
        Focus = focus;
        Labels = new ResumeLabels(document.Language.Culture);
        Summary = focus?.Profile.Summary ?? document.Person.Summary;
        Strengths = focus is null
            ? document.Strengths
            : [.. focus.StrengthOrder.Select(id => document.Strengths.First(strength => strength.Id == id))];
        SkillGroups = focus is null ? document.SkillGroups : [.. focus.SkillGroupOrder.Select(index => document.SkillGroups[index])];
        Sections = [.. (variant == DocumentVariant.Ats ? AtsSections : DesignedSections).Where(HasContent)];
        HasLongLevels = HasLongLevelsIn(document);
    }

    /// <summary>The CV.</summary>
    public ResumeDocument Document { get; }

    /// <summary>The layout.</summary>
    public DocumentVariant Variant { get; }

    /// <summary>The position profile the document is tailored to, or <see langword="null"/> for the full CV.</summary>
    public FocusView? Focus { get; }

    /// <summary>The engine's words in the CV's language.</summary>
    public ResumeLabels Labels { get; }

    /// <summary>The profile paragraph: the focus's own when it has one, otherwise the person's.</summary>
    public string? Summary { get; }

    /// <summary>The sections with content, in the layout's order.</summary>
    public IReadOnlyList<DocumentSection> Sections { get; }

    /// <summary>The strengths, those of the focus first.</summary>
    public IReadOnlyList<Strength> Strengths { get; }

    /// <summary>The skill groups, those with the focus's skills first.</summary>
    public IReadOnlyList<SkillGroup> SkillGroups { get; }

    /// <summary>Whether a level in words is longer than <see cref="ShortLevelLength"/>, so the layouts widen the level column.</summary>
    public bool HasLongLevels { get; }

    /// <summary>The style the layout writes dates in.</summary>
    public DateStyle DateStyle => Variant == DocumentVariant.Ats ? DateStyle.Numeric : DateStyle.Long;

    /// <summary>Returns the outline of the document a context asks for.</summary>
    /// <param name="context">The CV, the download and its focus.</param>
    public static DocumentOutline Of(DocumentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Of(context.Document, context.Download.Variant, context.Focus);
    }

    /// <summary>Returns the outline of a CV in a layout, optionally tailored to a position profile.</summary>
    /// <param name="document">The CV.</param>
    /// <param name="variant">The layout.</param>
    /// <param name="focus">The position profile's view, or <see langword="null"/> for the full CV.</param>
    public static DocumentOutline Of(ResumeDocument document, DocumentVariant variant, FocusView? focus)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new DocumentOutline(document, variant, focus);
    }

    /// <summary>The heading of a section in the layout's words, such as <c>Work Experience</c> for the ATS layout.</summary>
    /// <param name="section">The section.</param>
    public string Heading(DocumentSection section) => (Variant, section) switch
    {
        (DocumentVariant.Ats, DocumentSection.Profile) => Labels.AtsSummary,
        (DocumentVariant.Ats, DocumentSection.Skills) => Labels.AtsSkills,
        (DocumentVariant.Ats, DocumentSection.Experience) => Labels.AtsExperience,
        (DocumentVariant.Ats, DocumentSection.Projects) => Labels.AtsProjects,
        (DocumentVariant.Ats, DocumentSection.Education) => Labels.AtsEducation,
        (DocumentVariant.Ats, DocumentSection.Certificates) => Labels.AtsCertifications,
        (DocumentVariant.Ats, DocumentSection.Awards) => Labels.AtsAwards,
        (DocumentVariant.Ats, DocumentSection.Publications) => Labels.AtsPublications,
        (DocumentVariant.Ats, DocumentSection.Volunteering) => Labels.AtsVolunteering,
        (DocumentVariant.Ats, DocumentSection.Languages) => Labels.AtsLanguages,
        (_, DocumentSection.Profile) => Labels.Profile,
        (_, DocumentSection.Strengths) => Labels.Strengths,
        (_, DocumentSection.Experience) => Labels.Experience,
        (_, DocumentSection.Projects) => Labels.Projects,
        (_, DocumentSection.Skills) => Labels.Skills,
        (_, DocumentSection.Education) => Labels.Education,
        (_, DocumentSection.Certificates) => Labels.Certificates,
        (_, DocumentSection.Awards) => Labels.Awards,
        (_, DocumentSection.Publications) => Labels.Publications,
        (_, DocumentSection.Volunteering) => Labels.Volunteering,
        (_, DocumentSection.Languages) => Labels.Languages,
        _ => throw new UnreachableException($"No heading for the section {section}."),
    };

    /// <summary>Whether the focus puts a position, project or strength forward; always <see langword="false"/> without a focus.</summary>
    /// <param name="id">The item's identifier.</param>
    public bool IsEmphasized(string id) => Focus?.Emphasized.Contains(id) ?? false;

    /// <summary>The highlights a position keeps: all of them, or <see cref="UnfocusedHighlights"/> when the focus passes it by.</summary>
    /// <param name="position">The position.</param>
    public IReadOnlyList<string> Highlights(Position position)
    {
        ArgumentNullException.ThrowIfNull(position);
        return Keep(position.Id, position.Highlights);
    }

    /// <summary>The highlights a project keeps: all of them, or <see cref="UnfocusedHighlights"/> when the focus passes it by.</summary>
    /// <param name="engagement">The project.</param>
    public IReadOnlyList<string> Highlights(Engagement engagement)
    {
        ArgumentNullException.ThrowIfNull(engagement);
        return Keep(engagement.Id, engagement.Highlights);
    }

    /// <summary>Writes a period in the layout's style, such as <c>November 2022 – present</c> or <c>11/2022 – present</c>.</summary>
    /// <param name="period">The period.</param>
    public string Period(DateRange period) => PeriodText.Format(period, Labels, DateStyle);

    /// <summary>Writes a date in the layout's style.</summary>
    /// <param name="date">The date.</param>
    public string Date(PartialDate date) => PeriodText.Format(date, Labels.Culture, DateStyle);

    /// <summary>The level of a skill in words: the CV's own word, or the engine's word for its rating, or <see langword="null"/>.</summary>
    /// <param name="skill">The skill.</param>
    public string? Level(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        return skill.Level ?? (skill.Rating is { } rating ? Labels.Rating(rating) : null);
    }

    /// <summary>
    /// A skill's name as the documents write it: with the year it was used last, such as
    /// <c>Selenium (last used 2021)</c>, when no ongoing item uses it any more; the name alone while it is in use, when
    /// no dated item shows it, or when its rating measures usage.
    /// </summary>
    /// <param name="skill">The skill.</param>
    /// <remarks>
    /// Decision: a document dates only the skills no longer in use; the page shows every skill's last use.
    /// Why: a year after every skill makes the list twice as long to read and hides the few that say something; a
    /// skill without a year then reads as current, which it is.
    /// </remarks>
    public string SkillName(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        return LastUse(skill) is { } lastUse ? $"{skill.Name} ({lastUse})" : skill.Name;
    }

    /// <summary>
    /// A skill's level in words and, when no longer in use, its last use, as the plain lists write them after the name:
    /// <c>Expert, last used 2021</c>; <see langword="null"/> when there is neither.
    /// </summary>
    /// <param name="skill">The skill.</param>
    public string? SkillDetails(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        var parts = new[] { Level(skill), LastUse(skill) }.OfType<string>().ToList();
        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    // A usage level already says how recently a skill was used ("used in earlier projects"); a year next to it would
    // repeat it, and on the ATS line its brackets would follow the skill's own.
    private string? LastUse(Skill skill) =>
        skill.Kind != RatingKind.Usage && _evidence.GetValueOrDefault(skill.Name) is { Ongoing: false, LastYear: { } year } ? Labels.LastUsed(year) : null;

    /// <summary>
    /// The skills of one level as the ATS layouts write them: <c>Expert: C#, LINQ</c>, or for usage ratings
    /// <c>C#, LINQ (Usage: core and recurring)</c>; the names alone without a level.
    /// </summary>
    /// <param name="level">The level in words, or <see langword="null"/> or empty for none.</param>
    /// <param name="skills">The skills at that level.</param>
    /// <param name="name">How to write each skill, such as <see cref="SkillName"/>; the name alone by default.</param>
    /// <remarks>
    /// Decision: a usage rating follows the skills, a proficiency rating leads them.
    /// Why: "Expert: C#" reads as a heading for what follows; a usage level is a remark on the skills, and its own words
    /// often end in a colon already ("Usage: core and recurring"), which a leading level would double.
    /// </remarks>
    public static string SkillLine(string? level, IReadOnlyList<Skill> skills, Func<Skill, string>? name = null)
    {
        ArgumentNullException.ThrowIfNull(skills);
        var names = string.Join(", ", skills.Select(name ?? (skill => skill.Name)));
        if (string.IsNullOrEmpty(level))
        {
            return names;
        }

        return skills.Any(skill => skill.Kind == RatingKind.Usage) ? $"{names} ({level})" : $"{level}: {names}";
    }

    /// <summary>
    /// A study's grade and courses on one line, such as <c>Grade: 8.11/10 · Courses: Databases, .NET</c>, or
    /// <see langword="null"/> when the CV names neither.
    /// </summary>
    /// <param name="study">The study.</param>
    public string? StudyDetails(Study study)
    {
        ArgumentNullException.ThrowIfNull(study);
        var parts = new List<string>(2);
        if (study.Score is { } score)
        {
            parts.Add($"{Labels.Grade}: {score}");
        }

        if (study.Courses.Count > 0)
        {
            parts.Add($"{Labels.Courses}: {string.Join(", ", study.Courses)}");
        }

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    /// <summary>Whether a CV writes a level longer than <see cref="ShortLevelLength"/>, so the layouts widen the level column.</summary>
    /// <param name="document">The CV.</param>
    /// <remarks>
    /// Decision: the engine's own level words never count; only a CV's own words can be long.
    /// Why: the engine's words (Expert, Proficient, Ismerkedő) all fit the usual column, so a CV that writes none keeps
    /// its layout exactly.
    /// </remarks>
    public static bool HasLongLevelsIn(ResumeDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.AllSkills.Any(skill => skill.Level is { Length: > ShortLevelLength });
    }

    private List<string> Keep(string id, IReadOnlyList<string> highlights) =>
        Focus is null || IsEmphasized(id) ? [.. highlights] : [.. highlights.Take(UnfocusedHighlights)];

    private bool HasContent(DocumentSection section) => section switch
    {
        DocumentSection.Profile => Summary is not null || (Variant == DocumentVariant.Ats && Strengths.Count > 0),
        DocumentSection.Strengths => Strengths.Count > 0,
        DocumentSection.Experience => Document.Positions.Count > 0,
        DocumentSection.Projects => Document.Projects.Count > 0,
        DocumentSection.Skills => Document.SkillGroups.Count > 0,
        DocumentSection.Education => Document.Education.Count > 0 || (Variant == DocumentVariant.Designed && Document.Certificates.Count > 0),
        DocumentSection.Certificates => Document.Certificates.Count > 0,
        DocumentSection.Awards => Document.Awards.Count > 0,
        DocumentSection.Publications => Document.Publications.Count > 0,
        DocumentSection.Volunteering => Document.Volunteering.Count > 0,
        DocumentSection.Languages => Document.Languages.Count > 0,
        _ => throw new UnreachableException($"No content rule for the section {section}."),
    };
}
