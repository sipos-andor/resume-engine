using System.Globalization;
using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Languages;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Mapping;

/// <summary>Turns a validated JSON Resume into the domain model.</summary>
public static class ResumeMapper
{
    /// <summary>Maps a document that <see cref="Validation.ResumeValidator"/> found valid.</summary>
    /// <param name="resume">The document as read.</param>
    /// <param name="language">The language the document is written in.</param>
    /// <exception cref="InvalidOperationException">The document was not validated first and lacks a required fact.</exception>
    public static ResumeDocument Map(JsonResume resume, ResumeLanguage language)
    {
        var basics = resume.Basics ?? throw new InvalidOperationException("A validated document has basics.");
        var engagements = resume.Projects.Select(project => (project.Work, Engagement: MapEngagement(project))).ToList();
        var positions = resume.Work.Select(work =>
        {
            var id = Identifiers.Of(work);
            return new Position(
                id,
                work.Name!,
                Blank(work.Description),
                Blank(work.Position),
                Blank(work.Url),
                Blank(work.Location),
                Period(work.StartDate, work.EndDate) ?? throw new InvalidOperationException($"A validated position has a start date: {id}."),
                Blank(work.Summary),
                work.Highlights,
                work.Keywords,
                [.. engagements.Where(item => item.Work == id).Select(item => item.Engagement)],
                work.Short,
                work.ShortHighlights,
                work.Focus);
        }).ToList();

        return new ResumeDocument(
            language,
            MapPerson(basics),
            [.. resume.Strengths.Select(s => new Strength(s.Id!, s.Title!, Blank(s.Summary), s.Short, s.Focus))],
            positions,
            [.. engagements.Where(item => item.Work is null).Select(item => item.Engagement)],
            [.. resume.Education.Select(e => new Study(e.Institution!, Blank(e.Area), Blank(e.StudyType), Period(e.StartDate, e.EndDate), Blank(e.Url)))],
            [.. resume.Certificates.Select(c => new Certificate(c.Name!, Date(c.Date), Blank(c.Issuer), Blank(c.Url)))],
            [.. resume.Awards.Select(a => new Award(a.Title!, Date(a.Date), Blank(a.Awarder), Blank(a.Summary)))],
            [.. resume.Languages.Select(l => new SpokenLanguage(l.Language!, Blank(l.Fluency)))],
            Groups(resume.Skills),
            [.. resume.FocusProfiles.Select(f => new FocusProfile(f.Id!, f.Label!, Blank(f.Summary), f.Skills))],
            resume.Aliases,
            resume.Meta?.LastModified is { Length: >= 10 } modified
                ? DateOnly.ParseExact(modified[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture)
                : null);
    }

    private static Person MapPerson(JsonResumeBasics basics) => new(
        basics.Name!,
        Blank(basics.GivenName),
        Blank(basics.FamilyName),
        Blank(basics.Label),
        Blank(basics.Tagline),
        Blank(basics.Summary),
        Blank(basics.Phone),
        Blank(basics.Url),
        Blank(basics.Location?.Label) ?? Location(basics.Location),
        basics.Contact is { Url: { } url, Label: { } label } ? new Link(url, label) : null,
        basics.Availability is { } availability
            ? new Availability(
                availability.Status switch { "from" => AvailabilityStatus.From, "on-request" => AvailabilityStatus.OnRequest, _ => AvailabilityStatus.Available },
                Date(availability.From),
                Blank(availability.Url),
                availability.Label!)
            : null,
        [.. basics.Profiles.Select(p => new Profile(p.Network!, p.Url!, Blank(p.Username)))],
        Blank(basics.Image));

    private static Engagement MapEngagement(JsonResumeProject project) => new(
        Identifiers.Of(project),
        project.Name!,
        Blank(project.Entity),
        project.Roles,
        Blank(project.Description),
        Period(project.StartDate, project.EndDate),
        project.Highlights,
        project.Keywords,
        Blank(project.Url),
        project.Short,
        project.ShortHighlights,
        project.Focus);

    // Entries of one group name are merged in the file's order, each keyword keeping its entry's level.
    private static List<SkillGroup> Groups(IReadOnlyList<JsonResumeSkill> skills) =>
        [.. skills
            .GroupBy(skill => skill.Name!, StringComparer.Ordinal)
            .Select(group => new SkillGroup(group.Key, [.. group.SelectMany(entry => entry.Keywords.Select(keyword => new Skill(keyword, entry.Rating, Blank(entry.Level))))]))];

    private static string? Location(JsonResumeLocation? location) =>
        location is null ? null : Blank(string.Join(", ", new[] { location.City, location.Region, location.CountryCode }.Where(part => !string.IsNullOrWhiteSpace(part))));

    private static DateRange? Period(string? start, string? end) =>
        PartialDate.TryParse(start, out var first) ? new DateRange(first, PartialDate.TryParse(end, out var last) ? last : null) : null;

    private static PartialDate? Date(string? text) => PartialDate.TryParse(text, out var date) ? date : null;

    private static string? Blank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;
}
