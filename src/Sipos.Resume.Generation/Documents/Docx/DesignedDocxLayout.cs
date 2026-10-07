using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Model;
using static Sipos.Resume.Generation.Documents.Docx.DocxComposer;
using Position = Sipos.Resume.Core.Model.Position;

namespace Sipos.Resume.Generation.Documents.Docx;

/// <summary>The designed layout: the theme's colours and fonts, ∧ bullets, dates set right and level meters.</summary>
internal sealed class DesignedDocxLayout(DocxComposer docx, DocumentContext context, DocumentOutline outline, DocxLook look)
    : DocxLayout(docx, context, outline)
{
    private const string Separator = " · ";

    protected override void WriteContact()
    {
        var reach = new List<OpenXmlElement[]>();
        if (Person.Phone is { } phone)
        {
            reach.Add([Text(phone)]);
        }

        reach.Add([PageLink()]);
        if (Person.Location is { } location)
        {
            reach.Add([Text(location)]);
        }

        Docx.Add(StyleIds.Contact, Joined(reach, () => Text(Separator)));

        var links = new List<OpenXmlElement[]>();
        if (Person.Contact is { } contact)
        {
            links.Add([Docx.Link(contact.Url, contact.Label)]);
        }

        links.AddRange(Person.Profiles.Select(profile => new[] { Docx.Link(profile.Url, Display(profile.Url)) }));

        // The availability, as the designed PDF and the Markdown show it, so both designed documents say the same.
        if (Person.Availability is { } availability)
        {
            var label = availability.Status == AvailabilityStatus.From && availability.From is { } from
                ? $"{availability.Label} ({Outline.Date(from)})"
                : availability.Label;
            links.Add([availability.Url is { } url ? Docx.Link(url, label) : Text(label, StyleIds.Strong)]);
        }

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
                break;
            case DocumentSection.Strengths:
                foreach (var strength in Outline.Strengths)
                {
                    Docx.Add(StyleIds.ListBullet, Text(strength.Title, StyleIds.Strong), strength.Summary is { } summary ? Text($"{Dash}{summary}") : null);
                }

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
            case DocumentSection.Skills:
                WriteSkills();
                break;
            case DocumentSection.Education:
                WriteStudies();
                WriteCertificates();
                break;
            case DocumentSection.Certificates:
                WriteCertificates();
                break;
            case DocumentSection.Awards:
                WriteAwards();
                break;
            case DocumentSection.Languages:
                foreach (var language in document.Languages)
                {
                    Docx.Add(StyleIds.ListBullet, Text(language.Name), language.Fluency is { } fluency ? Text($"{Dash}{fluency}", StyleIds.Muted) : null);
                }

                break;
        }
    }

    private void WritePosition(Position position)
    {
        Docx.Add(
            StyleIds.Heading2,
            Text(position.Organization),
            position.Note is { } note ? Text($"{Separator}{note}", StyleIds.Note) : null,
            Tab(),
            Text(Outline.Period(position.Period), StyleIds.Date));

        var meta = new List<OpenXmlElement[]>();
        if (position.Role is { } role)
        {
            meta.Add([Text(role, StyleIds.Strong)]);
        }

        if (position.Location is { } location)
        {
            meta.Add([Text(location, StyleIds.Muted)]);
        }

        if (meta.Count > 0)
        {
            Docx.Add(StyleIds.ItemMeta, Joined(meta, () => Text(Separator, StyleIds.Muted)));
        }

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
        Docx.Add(
            heading,
            Text(engagement.Name),
            engagement.Client is { } client ? Text($"{Dash}{client}", StyleIds.Note) : null,
            engagement.Period is null ? null : Tab(),
            engagement.Period is null ? null : Text(Period(engagement.Period), StyleIds.Date));
        if (engagement.Roles.Count > 0)
        {
            Docx.Add(StyleIds.ItemMeta, Text(string.Join(", ", engagement.Roles), StyleIds.Muted));
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
            Docx.Add(StyleIds.Technologies, Text(string.Join(Separator, keywords)));
        }
    }

    /// <remarks>
    /// Decision: each skill group is a borderless two-pair grid (skill, meter and level; skill, meter and level).
    /// Why: one skill per line takes most of a page for forty skills; two per line with tab stops misalign as soon as
    /// a name such as "Azure OpenAI (LLM integration)" outgrows its stop, while a cell wraps it and keeps the meters
    /// in line.
    /// </remarks>
    private void WriteSkills()
    {
        var half = look.TextWidth / 2;
        var nameWidth = half * 3 / 5;
        int[] widths = [nameWidth, half - nameWidth, nameWidth, look.TextWidth - half - nameWidth];
        foreach (var group in Outline.SkillGroups)
        {
            Docx.Add(StyleIds.SkillGroup, Text(group.Name));
            if (group.Skills.Count == 0)
            {
                continue;
            }

            var table = new Table(
                new TableProperties
                {
                    TableWidth = new TableWidth { Width = Twips(look.TextWidth), Type = TableWidthUnitValues.Dxa },
                    TableLayout = new TableLayout { Type = TableLayoutValues.Fixed },
                    TableCellMarginDefault = new TableCellMarginDefault(
                        new TableCellLeftMargin { Width = 0, Type = TableWidthValues.Dxa },
                        new TableCellRightMargin { Width = 113, Type = TableWidthValues.Dxa }),
                    TableLook = new TableLook { Val = "0000", NoHorizontalBand = true, NoVerticalBand = true },
                    TableCaption = new TableCaption { Val = group.Name },
                },
                new TableGrid(widths.Select(width => new GridColumn { Width = Twips(width) })));
            var rows = group.Skills.Chunk(2).ToList();
            foreach (var (pair, index) in rows.Select((pair, index) => (pair, index)))
            {
                var row = new TableRow(new TableRowProperties(new CantSplit()));
                var keep = index < rows.Count - 1;
                for (var column = 0; column < 2; column++)
                {
                    var skill = column < pair.Length ? pair[column] : null;
                    row.Append(
                        Cell(widths[2 * column], keep, skill is null ? [] : [Text(skill.Name)]),
                        Cell(widths[(2 * column) + 1], keep, skill is null ? [] : Meter(skill)));
                }

                table.Append(row);
            }

            Docx.Add(table);
        }
    }

    private OpenXmlElement[] Meter(Skill skill) => [.. MeterRuns(skill)];

    private IEnumerable<OpenXmlElement> MeterRuns(Skill skill)
    {
        if (skill.Rating is { } rating)
        {
            var filled = Math.Clamp(rating, 0, 5);
            if (filled > 0)
            {
                yield return Text(new string('●', filled), StyleIds.MeterFilled);
            }

            if (filled < 5)
            {
                yield return Text(new string('○', 5 - filled), StyleIds.MeterEmpty);
            }
        }

        if (Outline.Level(skill) is { } level)
        {
            yield return Text(skill.Rating is null ? level : $"  {level}", StyleIds.Muted);
        }
    }

    // A group's grid stays on one page: every row but the last keeps with the next, as Word keeps a table together.
    private static TableCell Cell(int width, bool keepWithNext, OpenXmlElement[] content)
    {
        var paragraph = Paragraph(StyleIds.Skill, content);
        if (keepWithNext)
        {
            paragraph.ParagraphProperties!.KeepNext = new KeepNext();
        }

        return new TableCell(new TableCellProperties(new TableCellWidth { Width = Twips(width), Type = TableWidthUnitValues.Dxa }), paragraph);
    }

    private void WriteStudies()
    {
        foreach (var study in Context.Document.Education)
        {
            var title = StudyTitle(study);
            Docx.Add(
                StyleIds.ListBullet,
                Text(title, StyleIds.Strong),
                title == study.Institution ? null : Text($"{Dash}{study.Institution}"),
                study.Period is null ? null : Tab(),
                study.Period is null ? null : Text(Period(study.Period), StyleIds.Date));
        }
    }

    private void WriteCertificates()
    {
        foreach (var certificate in Context.Document.Certificates)
        {
            Docx.Add(
                StyleIds.ListBullet,
                Text(certificate.Name),
                certificate.Issuer is { } issuer ? Text($"{Dash}{issuer}", StyleIds.Muted) : null,
                certificate.Date is null ? null : Tab(),
                certificate.Date is { } date ? Text(Outline.Date(date), StyleIds.Date) : null);
        }
    }

    private void WriteAwards()
    {
        foreach (var award in Context.Document.Awards)
        {
            Docx.Add(
                StyleIds.ListBullet,
                Text(award.Title),
                award.Awarder is { } awarder ? Text($"{Dash}{awarder}", StyleIds.Muted) : null,
                award.Date is null ? null : Tab(),
                award.Date is { } date ? Text(Outline.Date(date), StyleIds.Date) : null,
                award.Summary is null ? null : Break(),
                award.Summary is { } summary ? Text(summary, StyleIds.Muted) : null);
        }
    }

    private static string Twips(int value) => value.ToString(CultureInfo.InvariantCulture);
}
