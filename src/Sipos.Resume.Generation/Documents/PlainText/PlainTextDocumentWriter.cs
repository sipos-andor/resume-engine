using System.Text;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Generation.Documents.PlainText;

/// <summary>
/// Writes the ATS layout of a CV as plain UTF-8 text: what an applicant tracking system reads best and what a portal's
/// "paste your CV" box accepts.
/// </summary>
/// <remarks>
/// <para>
/// Decision: headings in capitals on a line of their own, a blank line between entries, "- " bullets, numeric dates
/// and every URL written out in full.
/// Why: a text parser has no styles to find a heading by; capitals and blank lines are what such parsers and a
/// recruiter scanning a pasted text both recognise, and a link with hidden text loses its address.
/// </para>
/// <para>
/// Decision: UTF-8 with a byte order mark.
/// Why: without it older Windows tools and some portals read the file as ANSI and garble accented names such as
/// "Sípos"; every current reader skips the mark.
/// </para>
/// </remarks>
public sealed class PlainTextDocumentWriter : IDocumentWriter
{
    /// <inheritdoc />
    public DownloadFormat Format => DownloadFormat.PlainText;

    /// <inheritdoc />
    public bool Supports(DocumentVariant variant) => variant == DocumentVariant.Ats;

    /// <inheritdoc />
    public void Write(DocumentContext context, Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        output.Write(bytes.GetPreamble());
        output.Write(bytes.GetBytes(Render(context)));
    }

    /// <summary>Returns the text of a CV.</summary>
    /// <param name="context">The CV, the download and its focus.</param>
    public static string Render(DocumentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var outline = DocumentOutline.Of(context);
        var labels = outline.Labels;
        var person = context.Document.Person;
        var text = new StringBuilder();

        text.AppendLine(person.Name.ToUpper(labels.Culture));
        Line(text, person.Title);
        Line(text, person.Tagline);
        if (context.Focus is { } focus)
        {
            text.Append(labels.Focus).Append(": ").AppendLine(focus.Profile.Label);
        }

        if (person.Phone is { } phone)
        {
            text.Append(labels.Phone).Append(": ").AppendLine(phone);
        }

        text.Append(labels.Web).Append(": ").AppendLine(context.PageUrl.AbsoluteUri);
        if (person.Location is { } location)
        {
            text.Append(labels.Location).Append(": ").AppendLine(location);
        }

        if (person.Contact is { } contact)
        {
            text.Append(labels.Contact).Append(": ").Append(contact.Label).Append(" – ").AppendLine(contact.Url);
        }

        foreach (var profile in person.Profiles)
        {
            text.Append(profile.Network).Append(": ").AppendLine(profile.Url);
        }

        foreach (var section in outline.Sections)
        {
            text.AppendLine().AppendLine(outline.Heading(section).ToUpper(labels.Culture));
            switch (section)
            {
                case DocumentSection.Profile:
                    Line(text, outline.Summary);
                    foreach (var strength in outline.Strengths)
                    {
                        text.Append("- ").Append(strength.Title).AppendLine(strength.Summary is null ? "" : ": " + strength.Summary);
                    }

                    break;
                case DocumentSection.Skills:
                    foreach (var group in outline.SkillGroups)
                    {
                        text.Append(group.Name).Append(": ");
                        text.AppendLine(string.Join(", ", group.Skills.Select(skill => outline.Level(skill) is { } level ? $"{skill.Name} ({level})" : skill.Name)));
                    }

                    break;
                case DocumentSection.Experience:
                    var first = true;
                    foreach (var position in context.Document.Positions)
                    {
                        if (!first)
                        {
                            text.AppendLine();
                        }

                        first = false;
                        WritePosition(text, outline, position);
                    }

                    break;
                case DocumentSection.Projects:
                    WriteEngagements(text, outline, context.Document.Projects, first: true);
                    break;
                case DocumentSection.Education:
                    foreach (var study in context.Document.Education)
                    {
                        var parts = new[] { study.StudyType, study.Area, study.Institution, study.Period is { } period ? outline.Period(period) : null };
                        text.AppendLine(string.Join(" | ", parts.OfType<string>()));
                    }

                    break;
                case DocumentSection.Certificates:
                    foreach (var certificate in context.Document.Certificates)
                    {
                        var parts = new[] { certificate.Name, certificate.Issuer, certificate.Date is { } date ? outline.Date(date) : null, certificate.Url };
                        text.AppendLine(string.Join(" | ", parts.OfType<string>()));
                    }

                    break;
                case DocumentSection.Awards:
                    foreach (var award in context.Document.Awards)
                    {
                        var parts = new[] { award.Title, award.Awarder, award.Date is { } date ? outline.Date(date) : null };
                        text.Append(string.Join(" | ", parts.OfType<string>())).AppendLine(award.Summary is null ? "" : ": " + award.Summary);
                    }

                    break;
                case DocumentSection.Languages:
                    foreach (var language in context.Document.Languages)
                    {
                        text.Append(language.Name).AppendLine(language.Fluency is null ? "" : ": " + language.Fluency);
                    }

                    break;
                default:
                    break;
            }
        }

        return text.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static void WritePosition(StringBuilder text, DocumentOutline outline, Position position)
    {
        // Without a role the organization, note included, is the heading, so the note is never lost.
        var organization = position.Note is null ? position.Organization : $"{position.Organization} ({position.Note})";
        Line(text, position.Role ?? organization);
        var facts = new[] { position.Role is null ? null : organization, outline.Period(position.Period), position.Location, position.Url };
        text.AppendLine(string.Join(" | ", facts.OfType<string>()));
        Line(text, position.Summary);
        Bullets(text, outline.Highlights(position));
        Technologies(text, outline, position.Keywords);
        WriteEngagements(text, outline, position.Engagements, first: false);
    }

    private static void WriteEngagements(StringBuilder text, DocumentOutline outline, IReadOnlyList<Engagement> engagements, bool first)
    {
        foreach (var engagement in engagements)
        {
            if (!first)
            {
                text.AppendLine();
            }

            first = false;
            var title = engagement.Client is null ? engagement.Name : $"{engagement.Name} – {outline.Labels.Client}: {engagement.Client}";
            text.Append(outline.Heading(DocumentSection.Projects)).Append(": ").AppendLine(title);
            var facts = new[] { engagement.Roles.Count > 0 ? string.Join(", ", engagement.Roles) : null, engagement.Period is { } period ? outline.Period(period) : null, engagement.Url };
            if (facts.Any(fact => fact is not null))
            {
                text.AppendLine(string.Join(" | ", facts.OfType<string>()));
            }

            Line(text, engagement.Description);
            Bullets(text, outline.Highlights(engagement));
            Technologies(text, outline, engagement.Keywords);
        }
    }

    private static void Bullets(StringBuilder text, IReadOnlyList<string> items)
    {
        foreach (var item in items)
        {
            text.Append("- ").AppendLine(item);
        }
    }

    private static void Technologies(StringBuilder text, DocumentOutline outline, IReadOnlyList<string> keywords)
    {
        if (keywords.Count > 0)
        {
            text.Append(outline.Labels.Technologies).Append(": ").AppendLine(string.Join(", ", keywords));
        }
    }

    private static void Line(StringBuilder text, string? value)
    {
        if (value is not null)
        {
            text.AppendLine(value);
        }
    }
}
