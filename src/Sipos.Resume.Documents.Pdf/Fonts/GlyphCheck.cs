using System.Text;
using System.Text.RegularExpressions;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Validation;
using Sipos.Resume.Documents.Pdf.Layouts;

namespace Sipos.Resume.Documents.Pdf.Fonts;

/// <summary>Finds the characters of a CV that the bundled Plex fonts cannot draw, before any PDF is written.</summary>
/// <remarks>
/// QuestPDF has no fallback font and fails on such a character while it draws, such as an emoji, a star or a CJK
/// name, which the pages show through the browser's fallback. The ATS layout always uses Plex Sans; the designed
/// layout's Plex Mono text is checked independently of its Sans family.
/// </remarks>
internal static partial class GlyphCheck
{
    /// <summary>Returns an issue for every text value with characters the fonts lack.</summary>
    /// <param name="edition">One language's content.</param>
    /// <param name="theme">The documents' look, or <see langword="null"/> for the bundled fonts.</param>
    /// <param name="contactEmail">The contact e-mail address shown in the PDF, or <see langword="null"/>.</param>
    public static IEnumerable<ValidationIssue> Check(ResumeEdition edition, DocumentTheme? theme, string? contactEmail)
    {
        ArgumentNullException.ThrowIfNull(edition);
        var resolved = PlexFonts.Resolve(theme ?? DocumentTheme.Neutral);
        if (contactEmail is not null && Unsupported("RESUME_CONTACT_EMAIL", "", contactEmail,
            mono: string.Equals(resolved.SansFamily, PlexFonts.Mono, StringComparison.OrdinalIgnoreCase)) is { } emailIssue)
        {
            yield return emailIssue;
        }

        foreach (var (pointer, text) in ContentStrings.Of(edition.Source))
        {
            if (!RenderedText().IsMatch(pointer))
            {
                continue;
            }

            // A custom location label replaces the location components in both layouts.
            if (pointer.StartsWith("/basics/location/", StringComparison.Ordinal)
                && pointer != "/basics/location/x-label" && !string.IsNullOrWhiteSpace(edition.Source.Basics?.Location?.Label))
            {
                continue;
            }

            // ATS always draws text in Plex Sans. Focus text and availability are designed-only; its technologies
            // use the Mono family, while its other text uses the Sans family. Either family can be host-supplied.
            var family = MonoText().IsMatch(pointer) ? resolved.MonoFamily : resolved.SansFamily;
            var designedOnly = pointer.StartsWith("/x-focusProfiles/", StringComparison.Ordinal) || pointer == "/basics/x-availability/label";
            var sans = !designedOnly || string.Equals(family, PlexFonts.Sans, StringComparison.OrdinalIgnoreCase);
            var mono = string.Equals(family, PlexFonts.Mono, StringComparison.OrdinalIgnoreCase);
            var drawn = pointer.StartsWith("/basics/profiles/", StringComparison.Ordinal) && pointer.EndsWith("/url", StringComparison.Ordinal)
                ? ContactItem.WithoutScheme(text) : text;
            if (Unsupported(edition.FileName, pointer, drawn, mono, sans) is { } issue)
            {
                yield return issue;
            }
        }
    }

    /// <summary>Checks only the text the share image draws, including the site's host name.</summary>
    public static IEnumerable<ValidationIssue> CheckShareImage(ResumeEdition edition, DocumentTheme? theme, Uri site)
    {
        ArgumentNullException.ThrowIfNull(edition);
        ArgumentNullException.ThrowIfNull(site);
        if (PlexFonts.Resolve(theme ?? DocumentTheme.Neutral).SansFamily != PlexFonts.Sans)
        {
            yield break;
        }

        var person = edition.Document.Person;
        (string Source, string Path, string? Text)[] values =
        [
            (edition.FileName, "/basics/name", person.Name),
            (edition.FileName, "/basics/label", person.Title),
            (edition.FileName, "/basics/x-tagline", person.Tagline),
            ("site.json", "/origin", site.Host),
        ];
        foreach (var (source, path, text) in values)
        {
            if (text is not null && Unsupported(source, path, text, mono: false) is { } issue)
            {
                yield return issue;
            }
        }
    }

    private static ValidationIssue? Unsupported(string source, string pointer, string text, bool mono, bool sans = true)
    {
        var missing = text.EnumerateRunes().Where(rune => !Rune.IsControl(rune)
            && ((sans && !PlexFonts.CoversFamily(rune.Value, mono: false)) || (mono && !PlexFonts.CoversFamily(rune.Value, mono: true)))).Distinct().ToList();
        return missing.Count == 0
            ? null
            : new ValidationIssue(
                source,
                pointer,
                $"Has characters the PDF's fonts cannot draw: {string.Join(", ", missing.Select(rune => $"U+{rune.Value:X4} ({rune})"))}; leave them out or write them another way.");
    }

    [GeneratedRegex("^/(work/[0-9]+/x-keywords|projects/[0-9]+/keywords)/[0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex MonoText();

    // Links are checked only when their URL is also visible text (the profile addresses). Identifiers, image and
    // link targets, profile usernames and focus skill selectors never enter a PDF's text layer.
    [GeneratedRegex("^/(basics/(name|label|phone|summary|x-tagline|location/(city|region|countryCode|x-label)|profiles/[0-9]+/(network|url)|x-contact/label|x-availability/label)|work/[0-9]+/(name|location|description|position|summary|highlights/[0-9]+|x-keywords/[0-9]+)|projects/[0-9]+/(name|entity|description|roles/[0-9]+|highlights/[0-9]+|keywords/[0-9]+)|education/[0-9]+/(institution|area|studyType)|certificates/[0-9]+/(name|issuer)|awards/[0-9]+/(title|awarder|summary)|skills/[0-9]+/(name|level|keywords/[0-9]+)|languages/[0-9]+/(language|fluency)|x-strengths/[0-9]+/(title|summary)|x-focusProfiles/[0-9]+/(label|summary))$", RegexOptions.CultureInvariant)]
    private static partial Regex RenderedText();
}
