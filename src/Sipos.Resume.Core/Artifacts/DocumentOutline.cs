using Sipos.Resume.Core.Dates;
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

    private static readonly DocumentSection[] DesignedSections =
    [
        DocumentSection.Profile, DocumentSection.Strengths, DocumentSection.Experience, DocumentSection.Projects,
        DocumentSection.Skills, DocumentSection.Education, DocumentSection.Awards, DocumentSection.Languages,
    ];

    private static readonly DocumentSection[] AtsSections =
    [
        DocumentSection.Profile, DocumentSection.Skills, DocumentSection.Experience, DocumentSection.Projects,
        DocumentSection.Education, DocumentSection.Certificates, DocumentSection.Awards, DocumentSection.Languages,
    ];

    private DocumentOutline(ResumeDocument document, DocumentVariant variant, FocusView? focus)
    {
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
        (DocumentVariant.Ats, DocumentSection.Languages) => Labels.AtsLanguages,
        (_, DocumentSection.Profile) => Labels.Profile,
        (_, DocumentSection.Strengths) => Labels.Strengths,
        (_, DocumentSection.Experience) => Labels.Experience,
        (_, DocumentSection.Projects) => Labels.Projects,
        (_, DocumentSection.Skills) => Labels.Skills,
        (_, DocumentSection.Education) => Labels.Education,
        (_, DocumentSection.Certificates) => Labels.Certificates,
        (_, DocumentSection.Awards) => Labels.Awards,
        _ => Labels.Languages,
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
        _ => Document.Languages.Count > 0,
    };
}
