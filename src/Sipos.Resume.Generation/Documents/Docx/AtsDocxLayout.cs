using DocumentFormat.OpenXml;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Model;
using static Sipos.Resume.Generation.Documents.Docx.DocxComposer;

namespace Sipos.Resume.Generation.Documents.Docx;

/// <summary>
/// The layout for applicant tracking systems: one column of styled paragraphs, standard headings, plain bullets,
/// numeric dates, labelled contact details and levels in words. No table, text box, picture, header or footer.
/// </summary>
/// <remarks>
/// Decision: contact details carry their label and a link's address is its visible text, such as
/// <c>Online CV: andor.sipos.io</c>.
/// Why: a parser reads the text, not a hyperlink's target, and finds a field by its label.
/// </remarks>
internal sealed class AtsDocxLayout(DocxComposer docx, DocumentContext context, DocumentOutline outline)
    : DocxLayout(docx, context, outline)
{
    private const string Separator = " | ";

    protected override void WriteContact()
    {
        var reach = new List<OpenXmlElement[]>();
        if (Person.Phone is { } phone)
        {
            reach.Add([Text($"{Labels.Phone}: {phone}")]);
        }

        reach.Add([Text($"{Labels.Web}: "), PageLink()]);
        if (Person.Location is { } location)
        {
            reach.Add([Text($"{Labels.Location}: {location}")]);
        }

        Docx.Add(StyleIds.Contact, Joined(reach, () => Text(Separator)));

        var links = new List<OpenXmlElement[]>();
        if (Person.Contact is { } contact)
        {
            links.Add([Text($"{contact.Label}: "), Docx.Link(contact.Url, Display(contact.Url))]);
        }

        links.AddRange(Person.Profiles.Select(profile => new[] { Text($"{profile.Network}: "), Docx.Link(profile.Url, Display(profile.Url)) }));
        if (links.Count > 0)
        {
            Docx.Add(StyleIds.Contact, Joined(links, () => Text(Separator)));
        }
    }

    protected override void WriteSection(DocumentSection section)
    {
        var document = Context.Document;
        switch (section)
        {
            case DocumentSection.Profile:
                Docx.AddText(StyleIds.Normal, Outline.Summary);
                foreach (var strength in Outline.Strengths)
                {
                    Docx.Add(StyleIds.ListBullet, Text(strength.Title, StyleIds.Strong), strength.Summary is { } summary ? Text($": {summary}") : null);
                }

                break;
            case DocumentSection.Skills:
                WriteSkills();
                break;
            case DocumentSection.Experience:
                foreach (var position in document.Positions)
                {
                    WritePosition(position);
                }

                break;
            case DocumentSection.Projects:
                foreach (var project in document.Projects)
                {
                    WriteProject(project, StyleIds.Heading2);
                }

                break;
            case DocumentSection.Education:
                foreach (var study in document.Education)
                {
                    var title = StudyTitle(study);
                    var rest = JoinText([title == study.Institution ? null : study.Institution], Dash, prefix: Dash) + JoinText([Period(study.Period)], Separator, prefix: Separator);
                    Docx.Add(StyleIds.Normal, Text(title, StyleIds.Strong), rest.Length > 0 ? Text(rest) : null);
                }

                break;
            case DocumentSection.Certificates:
                foreach (var certificate in document.Certificates)
                {
                    Docx.Add(StyleIds.ListBullet, Text(JoinText([certificate.Name, certificate.Issuer], Dash) + JoinText([Date(certificate.Date)], Separator, prefix: Separator)));
                }

                break;
            case DocumentSection.Awards:
                foreach (var award in document.Awards)
                {
                    Docx.Add(StyleIds.ListBullet, Text(JoinText([award.Title, award.Awarder], Dash) + JoinText([Date(award.Date)], Separator, prefix: Separator)));
                    Docx.AddText(StyleIds.ListContinue, award.Summary);
                }

                break;
            case DocumentSection.Languages:
                foreach (var language in document.Languages)
                {
                    Docx.Add(StyleIds.ListBullet, Text(JoinText([language.Name, language.Fluency], Dash)));
                }

                break;
        }
    }

    // Skills in words, one line per level of a group: "Expert: C#, LINQ".
    private void WriteSkills()
    {
        foreach (var group in Outline.SkillGroups)
        {
            Docx.Add(StyleIds.SkillGroup, Text(group.Name));
            foreach (var level in group.Skills.GroupBy(skill => Outline.Level(skill) ?? "", StringComparer.Ordinal))
            {
                var names = string.Join(", ", level.Select(skill => skill.Name));
                Docx.Add(StyleIds.Skill, Text(level.Key.Length == 0 ? names : $"{level.Key}: {names}"));
            }
        }
    }

    private void WritePosition(Position position)
    {
        var organization = position.Note is { } note ? $"{position.Organization} ({note})" : position.Organization;
        Docx.Add(StyleIds.Heading2, Text(JoinText([position.Role, organization], Dash)));
        Docx.Add(StyleIds.ItemMeta, Text(JoinText([Outline.Period(position.Period), position.Location], Separator)));
        Docx.AddText(StyleIds.Normal, position.Summary);
        WriteDetails(Outline.Highlights(position), position.Keywords);
        if (position.Engagements.Count > 0)
        {
            Docx.Add(StyleIds.GroupLabel, Text(Labels.ClientProjects));
            foreach (var engagement in position.Engagements)
            {
                WriteProject(engagement, StyleIds.Heading3);
            }
        }
    }

    private void WriteProject(Engagement engagement, string heading)
    {
        Docx.Add(heading, Text(ProjectTitle(engagement)));
        var meta = JoinText([string.Join(", ", engagement.Roles), Period(engagement.Period)], Separator);
        if (meta.Length > 0)
        {
            Docx.Add(StyleIds.ItemMeta, Text(meta));
        }

        Docx.AddText(StyleIds.Normal, engagement.Description);
        WriteDetails(Outline.Highlights(engagement), engagement.Keywords);
    }

    private void WriteDetails(IReadOnlyList<string> highlights, IReadOnlyList<string> keywords)
    {
        foreach (var highlight in highlights)
        {
            Docx.Add(StyleIds.ListBullet, Text(highlight));
        }

        if (keywords.Count > 0)
        {
            Docx.Add(StyleIds.Technologies, Text($"{Labels.Technologies}: {string.Join(", ", keywords)}"));
        }
    }

    private string? Date(PartialDate? date) => date is { } value ? Outline.Date(value) : null;

    private static string JoinText(IEnumerable<string?> parts, string separator, string prefix = "")
    {
        var text = string.Join(separator, parts.Where(part => !string.IsNullOrEmpty(part)));
        return text.Length == 0 ? "" : prefix + text;
    }
}
