using System.Text;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Localization;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Generation.Documents.Markdown;

/// <summary>
/// Writes a CV as Markdown: the download, the page's <c>index.md</c> and the text language models read through
/// <c>llms.txt</c>. Plain CommonMark, so any reader shows it.
/// </summary>
/// <remarks>
/// Decision: the designed outline (with strengths and the client projects under their position), written as plain
/// CommonMark with headings, lists and links only.
/// Why: the Markdown is read by people in a repository viewer and by language models; both follow a heading
/// structure and lose nothing without tables or HTML.
/// </remarks>
public sealed class MarkdownDocumentWriter : IDocumentWriter
{
    /// <inheritdoc />
    public DownloadFormat Format => DownloadFormat.Markdown;

    /// <inheritdoc />
    public bool Supports(DocumentVariant variant) => variant == DocumentVariant.Designed;

    /// <inheritdoc />
    public void Write(DocumentContext context, Stream output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);
        var bytes = Encoding.UTF8.GetBytes(Render(context));
        output.Write(bytes);
    }

    /// <summary>Returns the Markdown of a CV.</summary>
    /// <param name="context">The CV, the download and its focus.</param>
    public static string Render(DocumentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var outline = DocumentOutline.Of(context);
        var person = context.Document.Person;
        var labels = outline.Labels;
        var text = new StringBuilder();

        text.Append("# ").AppendLine(MarkdownText.Escape(person.Name)).AppendLine();
        var title = string.Join(" · ", new[] { person.Title, person.Tagline }.OfType<string>().Select(MarkdownText.Escape));
        if (title.Length > 0)
        {
            text.AppendLine(title).AppendLine();
        }

        if (context.Focus is { } focus)
        {
            text.Append("**").Append(MarkdownText.Escape(labels.Focus)).Append(":** ").AppendLine(MarkdownText.Escape(focus.Profile.Label)).AppendLine();
        }

        WriteContacts(text, context, labels);
        foreach (var section in outline.Sections)
        {
            text.Append("## ").AppendLine(MarkdownText.Escape(outline.Heading(section))).AppendLine();
            switch (section)
            {
                case DocumentSection.Profile:
                    text.AppendLine(MarkdownText.EscapeBlock(outline.Summary!)).AppendLine();
                    break;
                case DocumentSection.Strengths:
                    foreach (var strength in outline.Strengths)
                    {
                        text.Append("- **").Append(MarkdownText.Escape(strength.Title)).Append("**");
                        text.AppendLine(strength.Summary is null ? "" : " – " + MarkdownText.Escape(strength.Summary));
                    }

                    text.AppendLine();
                    break;
                case DocumentSection.Experience:
                    foreach (var position in context.Document.Positions)
                    {
                        WritePosition(text, outline, position);
                    }

                    break;
                case DocumentSection.Projects:
                    foreach (var engagement in context.Document.Projects)
                    {
                        WriteEngagement(text, outline, engagement, "###");
                    }

                    break;
                case DocumentSection.Skills:
                    foreach (var group in outline.SkillGroups)
                    {
                        text.Append("### ").AppendLine(MarkdownText.Escape(group.Name)).AppendLine();
                        foreach (var skill in group.Skills)
                        {
                            text.Append("- ").Append(MarkdownText.EscapeBlock(skill.Name));
                            text.AppendLine(outline.Level(skill) is { } level ? " – " + MarkdownText.Escape(level) : "");
                        }

                        text.AppendLine();
                    }

                    break;
                case DocumentSection.Education:
                    WriteEducation(text, outline);
                    break;
                case DocumentSection.Certificates:
                    WriteCertificates(text, outline);
                    break;
                case DocumentSection.Awards:
                    foreach (var award in context.Document.Awards)
                    {
                        text.Append("- **").Append(MarkdownText.Escape(award.Title)).Append("**");
                        text.Append(string.Concat(new[] { award.Awarder, award.Date is { } date ? outline.Date(date) : null }.OfType<string>().Select(part => ", " + MarkdownText.Escape(part))));
                        text.AppendLine(award.Summary is null ? "" : " – " + MarkdownText.Escape(award.Summary));
                    }

                    text.AppendLine();
                    break;
                case DocumentSection.Languages:
                    foreach (var language in context.Document.Languages)
                    {
                        text.Append("- ").Append(MarkdownText.EscapeBlock(language.Name));
                        text.AppendLine(language.Fluency is null ? "" : " – " + MarkdownText.Escape(language.Fluency));
                    }

                    text.AppendLine();
                    break;
            }
        }

        // AppendLine writes the system's line ending; a line feed everywhere gives every build the same bytes.
        return text.ToString().ReplaceLineEndings("\n").TrimEnd('\n') + "\n";
    }

    private static void WriteContacts(StringBuilder text, DocumentContext context, ResumeLabels labels)
    {
        var person = context.Document.Person;
        var lines = new List<string>();
        if (person.Phone is { } phone)
        {
            lines.Add($"{MarkdownText.Escape(labels.Phone)}: [{MarkdownText.Escape(phone)}](tel:{person.PhoneDial})");
        }

        lines.Add($"{MarkdownText.Escape(labels.Web)}: {MarkdownText.Link(context.PageUrl.AbsoluteUri)}");
        if (person.Location is { } location)
        {
            lines.Add($"{MarkdownText.Escape(labels.Location)}: {MarkdownText.Escape(location)}");
        }

        if (person.Contact is { } contact)
        {
            lines.Add($"{MarkdownText.Escape(labels.Contact)}: [{MarkdownText.Escape(contact.Label)}]({contact.Url})");
        }

        lines.AddRange(person.Profiles.Select(profile => $"{MarkdownText.EscapeBlock(profile.Network)}: {MarkdownText.Link(profile.Url)}"));
        if (person.Availability is { } availability)
        {
            lines.Add(availability.Url is null ? $"**{MarkdownText.Escape(availability.Label)}**" : $"**[{MarkdownText.Escape(availability.Label)}]({availability.Url})**");
        }

        foreach (var line in lines)
        {
            text.Append("- ").AppendLine(line);
        }

        text.AppendLine();
    }

    private static void WritePosition(StringBuilder text, DocumentOutline outline, Position position)
    {
        var heading = position.Role is null ? position.Organization : $"{position.Role} – {position.Organization}";
        text.Append("### ").AppendLine(MarkdownText.Escape(heading)).AppendLine();
        var facts = new[] { position.Note, outline.Period(position.Period), position.Location }.OfType<string>().Select(MarkdownText.Escape);
        text.Append('*').Append(string.Join(" · ", facts)).Append('*');
        text.AppendLine(position.Url is { } url ? " · " + MarkdownText.Link(url) : "").AppendLine();
        if (position.Summary is { } summary)
        {
            text.AppendLine(MarkdownText.EscapeBlock(summary)).AppendLine();
        }

        WriteBullets(text, outline.Highlights(position));
        WriteTechnologies(text, outline, position.Keywords);
        if (position.Engagements.Count > 0)
        {
            text.Append("#### ").AppendLine(MarkdownText.Escape(outline.Labels.ClientProjects)).AppendLine();
            foreach (var engagement in position.Engagements)
            {
                WriteEngagement(text, outline, engagement, "#####");
            }
        }
    }

    private static void WriteEngagement(StringBuilder text, DocumentOutline outline, Engagement engagement, string level)
    {
        var heading = engagement.Client is null ? engagement.Name : $"{engagement.Name} – {engagement.Client}";
        text.Append(level).Append(' ').AppendLine(MarkdownText.Escape(heading)).AppendLine();
        var facts = new[] { engagement.Roles.Count > 0 ? string.Join(", ", engagement.Roles) : null, engagement.Period is { } period ? outline.Period(period) : null }
            .OfType<string>().Select(MarkdownText.Escape).ToList();
        if (facts.Count > 0)
        {
            text.Append('*').Append(string.Join(" · ", facts)).AppendLine("*").AppendLine();
        }

        if (engagement.Description is { } description)
        {
            text.AppendLine(MarkdownText.EscapeBlock(description)).AppendLine();
        }

        WriteBullets(text, outline.Highlights(engagement));
        WriteTechnologies(text, outline, engagement.Keywords);
    }

    private static void WriteEducation(StringBuilder text, DocumentOutline outline)
    {
        foreach (var study in outline.Document.Education)
        {
            var parts = new[] { study.StudyType, study.Area }.OfType<string>().ToList();
            text.Append("- **").Append(MarkdownText.Escape(study.Institution)).Append("**");
            text.Append(parts.Count > 0 ? ", " + MarkdownText.Escape(string.Join(", ", parts)) : "");
            text.AppendLine(study.Period is { } period ? " (" + MarkdownText.Escape(outline.Period(period)) + ")" : "");
        }

        text.AppendLine();
        if (outline.Variant == DocumentVariant.Designed && outline.Document.Certificates.Count > 0)
        {
            text.Append("### ").AppendLine(MarkdownText.Escape(outline.Labels.Certificates)).AppendLine();
            WriteCertificates(text, outline);
        }
    }

    private static void WriteCertificates(StringBuilder text, DocumentOutline outline)
    {
        foreach (var certificate in outline.Document.Certificates)
        {
            var name = certificate.Url is null ? MarkdownText.Escape(certificate.Name) : $"[{MarkdownText.Escape(certificate.Name)}]({certificate.Url})";
            text.Append("- ").Append(name);
            text.AppendLine(string.Concat(new[] { certificate.Issuer, certificate.Date is { } date ? outline.Date(date) : null }.OfType<string>().Select(part => ", " + MarkdownText.Escape(part))));
        }

        text.AppendLine();
    }

    private static void WriteBullets(StringBuilder text, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        foreach (var item in items)
        {
            text.Append("- ").AppendLine(MarkdownText.EscapeBlock(item));
        }

        text.AppendLine();
    }

    private static void WriteTechnologies(StringBuilder text, DocumentOutline outline, IReadOnlyList<string> keywords)
    {
        if (keywords.Count > 0)
        {
            text.Append("**").Append(MarkdownText.Escape(outline.Labels.Technologies)).Append(":** ").AppendLine(MarkdownText.Escape(string.Join(", ", keywords))).AppendLine();
        }
    }
}
