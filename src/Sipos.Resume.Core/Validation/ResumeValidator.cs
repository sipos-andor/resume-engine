using System.Buffers;
using System.Text.RegularExpressions;
using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Dates;
using Sipos.Resume.Core.Evidence;
using Sipos.Resume.Core.Mapping;

namespace Sipos.Resume.Core.Validation;

/// <summary>Checks one content file against what the engine needs, beyond the JSON Resume schema.</summary>
public static partial class ResumeValidator
{
    private static readonly string[] AvailabilityStatuses = ["available", "from", "on-request"];

    // The characters XML 1.0 cannot hold: the C0 controls but tab and the line breaks, and the two noncharacters.
    private static readonly SearchValues<char> NotInDocuments = SearchValues.Create(
        [.. Enumerable.Range(0, 32).Where(code => code is not ('\t' or '\n' or '\r')).Select(code => (char)code), '\uFFFE', '\uFFFF']);

    /// <summary>Returns every problem of one document; none means it can be rendered.</summary>
    /// <param name="source">The file's name, for the issues.</param>
    /// <param name="resume">The document as read.</param>
    public static IReadOnlyList<ValidationIssue> Validate(string source, JsonResume resume)
    {
        var issues = new List<ValidationIssue>();
        if (resume.Original is { ValueKind: System.Text.Json.JsonValueKind.Object } original)
        {
            issues.AddRange(PreservedResumeValidator.Check(source, original));
        }
        void Fail(string path, string message) => issues.Add(new ValidationIssue(source, path, message));

        CheckBasics(resume.Basics, Fail);
        var focusIds = CheckFocusProfiles(resume.FocusProfiles, Fail);

        // Positions, projects and strengths are anchors of one page, so they share one set of identifiers.
        var anchors = new HashSet<string>(StringComparer.Ordinal);
        var workIds = CheckWork(resume.Work, focusIds, anchors, Fail);
        CheckProjects(resume.Projects, workIds, focusIds, anchors, Fail);
        CheckStrengths(resume.Strengths, focusIds, anchors, Fail);
        CheckAliases(resume.Aliases, Fail);
        CheckMeta(resume.Meta, Fail);
        CheckCharacters(resume, Fail);

        for (var i = 0; i < resume.Education.Count; i++)
        {
            var study = resume.Education[i];
            CheckPeriod(
                string.IsNullOrEmpty(study.StartDate) ? null : study.StartDate,
                string.IsNullOrEmpty(study.EndDate) ? null : study.EndDate,
                $"/education/{i}",
                required: false,
                Fail);
            CheckUrl(resume.Education[i].Url, $"/education/{i}/url", Fail);
            CheckRequired(resume.Education[i].Institution, $"/education/{i}/institution", Fail);
        }

        for (var i = 0; i < resume.Certificates.Count; i++)
        {
            CheckRequired(resume.Certificates[i].Name, $"/certificates/{i}/name", Fail);
            CheckOptionalDate(resume.Certificates[i].Date, $"/certificates/{i}/date", Fail);
            CheckUrl(resume.Certificates[i].Url, $"/certificates/{i}/url", Fail);
        }

        for (var i = 0; i < resume.Awards.Count; i++)
        {
            CheckRequired(resume.Awards[i].Title, $"/awards/{i}/title", Fail);
            CheckOptionalDate(resume.Awards[i].Date, $"/awards/{i}/date", Fail);
        }

        for (var i = 0; i < resume.Skills.Count; i++)
        {
            var skill = resume.Skills[i];
            CheckRequired(skill.Name, $"/skills/{i}/name", Fail);
            if (skill.Rating is < 1 or > 5)
            {
                Fail($"/skills/{i}/x-rating", "Must be a whole number from 1 to 5.");
            }
        }

        for (var i = 0; i < resume.Languages.Count; i++)
        {
            CheckRequired(resume.Languages[i].Language, $"/languages/{i}/language", Fail);
        }

        // The same test the mapper applies: the first ten characters as YYYY-MM-DD, so 2026-10-07T12:00:00Z passes too.
        if (resume.Meta?.LastModified is { } modified
            && !(modified.Length >= 10 && DateOnly.TryParseExact(modified[..10], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _)))
        {
            Fail("/meta/lastModified", "Must start with a date in the form YYYY-MM-DD.");
        }

        return issues;
    }

    private static void CheckBasics(JsonResumeBasics? basics, Action<string, string> fail)
    {
        if (basics is null)
        {
            fail("/basics", "The person is missing; the engine needs at least basics.name.");
            return;
        }

        CheckRequired(basics.Name, "/basics/name", fail);
        if (basics.Email is not null)
        {
            fail("/basics/email", "E-mail addresses are not published; remove it and give a contact page in basics.x-contact.");
        }

        if (!string.IsNullOrWhiteSpace(basics.Phone) && !basics.Phone.Any(char.IsAsciiDigit))
        {
            fail("/basics/phone", "Must contain at least one ASCII digit.");
        }

        CheckUrl(basics.Url, "/basics/url", fail);
        CheckUrl(basics.Image, "/basics/image", fail);
        for (var i = 0; i < basics.Profiles.Count; i++)
        {
            CheckRequired(basics.Profiles[i].Network, $"/basics/profiles/{i}/network", fail);
            CheckRequired(basics.Profiles[i].Url, $"/basics/profiles/{i}/url", fail);
            CheckUrl(basics.Profiles[i].Url, $"/basics/profiles/{i}/url", fail);
        }

        if (basics.Contact is { } contact)
        {
            CheckRequired(contact.Url, "/basics/x-contact/url", fail);
            CheckUrl(contact.Url, "/basics/x-contact/url", fail);
            CheckRequired(contact.Label, "/basics/x-contact/label", fail);
        }

        if (basics.Availability is { } availability)
        {
            if (!AvailabilityStatuses.Contains(availability.Status))
            {
                fail("/basics/x-availability/status", $"Must be one of {string.Join(", ", AvailabilityStatuses)}.");
            }

            if (availability.Status == "from" && !PartialDate.TryParse(availability.From, out _))
            {
                fail("/basics/x-availability/from", "Status 'from' needs a date in the form YYYY, YYYY-MM or YYYY-MM-DD.");
            }

            CheckUrl(availability.Url, "/basics/x-availability/url", fail);
            CheckRequired(availability.Label, "/basics/x-availability/label", fail);
        }
    }

    // Decision: a profile's identifier must also give a file name no other download has, case aside.
    // Why: tailored downloads are named after it (tech-lead gives _TechLead.pdf); "ats" would give _Ats.pdf next to the
    // ATS file _ATS.pdf, and "a-b" and "ab" would give one name, which disks that ignore case cannot hold twice.
    private static HashSet<string> CheckFocusProfiles(IReadOnlyList<JsonResumeFocusProfile> profiles, Action<string, string> fail)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var suffixes = new HashSet<string>(["ATS"], StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < profiles.Count; i++)
        {
            var id = profiles[i].Id;
            if (id is null || !Identifiers.IsValid(id))
            {
                fail($"/x-focusProfiles/{i}/id", "Needs an identifier of lowercase letters, digits and hyphens.");
            }
            else if (!ids.Add(id))
            {
                fail($"/x-focusProfiles/{i}/id", $"The identifier '{id}' is used twice.");
            }
            else if (!suffixes.Add(DownloadCatalog.SuffixOf(id)))
            {
                fail($"/x-focusProfiles/{i}/id", $"The identifier '{id}' names its downloads like another file (…_{DownloadCatalog.SuffixOf(id)}); choose another.");
            }

            CheckRequired(profiles[i].Label, $"/x-focusProfiles/{i}/label", fail);
        }

        return ids;
    }

    private static HashSet<string> CheckWork(IReadOnlyList<JsonResumeWork> work, HashSet<string> focusIds, HashSet<string> anchors, Action<string, string> fail)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < work.Count; i++)
        {
            var item = work[i];
            var path = $"/work/{i}";
            CheckRequired(item.Name, $"{path}/name", fail);
            CheckPeriod(item.StartDate, item.EndDate, path, required: true, fail);
            CheckUrl(item.Url, $"{path}/url", fail);
            var id = Identifiers.Of(item);
            if (CheckIdentifier(id, item.Id is not null, anchors, $"{path}/x-id", fail))
            {
                ids.Add(id);
            }

            CheckFocus(item.Focus, focusIds, $"{path}/x-focus", fail);
            CheckShortHighlights(item.ShortHighlights, item.Highlights.Count, $"{path}/x-shortHighlights", fail);
        }

        return ids;
    }

    private static void CheckProjects(IReadOnlyList<JsonResumeProject> projects, HashSet<string> workIds, HashSet<string> focusIds, HashSet<string> anchors, Action<string, string> fail)
    {
        for (var i = 0; i < projects.Count; i++)
        {
            var item = projects[i];
            var path = $"/projects/{i}";
            CheckRequired(item.Name, $"{path}/name", fail);
            CheckPeriod(item.StartDate, item.EndDate, path, required: false, fail);
            if (item.StartDate is null && item.EndDate is { Length: > 0 })
            {
                fail($"{path}/startDate", "Needs a start date when an end date is supplied.");
            }

            CheckUrl(item.Url, $"{path}/url", fail);
            CheckIdentifier(Identifiers.Of(item), item.Id is not null, anchors, $"{path}/x-id", fail);
            if (item.Work is { } work && !workIds.Contains(work))
            {
                fail($"{path}/x-work", $"No position has the identifier '{work}'.");
            }

            CheckFocus(item.Focus, focusIds, $"{path}/x-focus", fail);
            CheckShortHighlights(item.ShortHighlights, item.Highlights.Count, $"{path}/x-shortHighlights", fail);
        }
    }

    private static void CheckStrengths(IReadOnlyList<JsonResumeStrength> strengths, HashSet<string> focusIds, HashSet<string> anchors, Action<string, string> fail)
    {
        for (var i = 0; i < strengths.Count; i++)
        {
            var path = $"/x-strengths/{i}";
            CheckRequired(strengths[i].Title, $"{path}/title", fail);
            if (strengths[i].Id is not { } id)
            {
                fail($"{path}/id", "Needs an identifier of lowercase letters, digits and hyphens.");
            }
            else
            {
                CheckIdentifier(id, given: true, anchors, $"{path}/id", fail);
            }

            CheckFocus(strengths[i].Focus, focusIds, $"{path}/focus", fail);
        }
    }

    // Returns whether the identifier is valid and new, so a position's projects can be checked against it.
    private static bool CheckIdentifier(string id, bool given, HashSet<string> ids, string path, Action<string, string> fail)
    {
        if (!Identifiers.IsValid(id))
        {
            fail(path, given ? "Must be lowercase letters, digits and hyphens, at most 64 characters." : "Cannot make an identifier from the name; give x-id.");
            return false;
        }

        if (Anchors.IsReserved(id))
        {
            fail(path, $"The identifier '{id}' is one the page uses for itself (a section, a generated anchor, cv-… or …-title); give the item another x-id.");
            return false;
        }

        if (!ids.Add(id))
        {
            fail(path, $"The identifier '{id}' is used twice; give the item its own x-id.");
            return false;
        }

        return true;
    }

    // Two terms whose spellings fold to one key would be one technology twice, with two alias lists; an alias that is
    // another term, or another term's alias too, would count as one technology in the filter (the first term takes
    // it) but match both in the job ad matcher.
    private static void CheckAliases(IReadOnlyDictionary<string, IReadOnlyList<string>> aliases, Action<string, string> fail)
    {
        var terms = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var term in aliases.Keys)
        {
            var key = Keys.Of(term);
            if (key.Length == 0)
            {
                fail($"/x-aliases/{Pointer(term)}", "The term has no letters or digits to match.");
            }
            else if (!terms.TryAdd(key, term))
            {
                fail($"/x-aliases/{Pointer(term)}", $"Is the same term as '{terms[key]}'; merge their aliases into one entry.");
            }
        }

        var owners = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (term, spellings) in aliases)
        {
            for (var index = 0; index < spellings.Count; index++)
            {
                var key = Keys.Of(spellings[index]);
                var path = $"/x-aliases/{Pointer(term)}/{index}";
                if (key.Length == 0)
                {
                    fail(path, "The alias has no letters or digits to match.");
                }
                else if (terms.TryGetValue(key, out var other) && other != term)
                {
                    fail(path, $"Is the term '{other}' itself; an alias may stand for one term only.");
                }
                else if (!owners.TryAdd(key, term) && owners[key] != term)
                {
                    fail(path, $"Is an alias of '{owners[key]}' too; an alias may stand for one term only.");
                }
            }
        }
    }

    // JSON may hold any control character as an escape, but XML 1.0, and so a Word document, holds none but tab and the
    // line breaks; one in the text would stop the build while it writes the DOCX.
    private static void CheckCharacters(JsonResume resume, Action<string, string> fail)
    {
        foreach (var (pointer, text) in ContentStrings.Of(resume))
        {
            var index = text.AsSpan().IndexOfAny(NotInDocuments);
            if (index >= 0)
            {
                fail(pointer, $"Holds the control character U+{(int)text[index]:X4}, which documents cannot carry; remove it.");
            }
        }
    }

    private static void CheckMeta(JsonResumeMeta? meta, Action<string, string> fail)
    {
        if (meta?.Path is { } path && !PathSegment().IsMatch(path))
        {
            fail("/meta/x-path", "Must be one URL path segment of lowercase letters, digits and hyphens, such as sr.");
        }
    }

    private static string Pointer(string name) => name.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex PathSegment();

    private static void CheckFocus(IReadOnlyList<string> focus, HashSet<string> focusIds, string path, Action<string, string> fail)
    {
        for (var i = 0; i < focus.Count; i++)
        {
            if (!focusIds.Contains(focus[i]))
            {
                fail($"{path}/{i}", $"No focus profile has the identifier '{focus[i]}'.");
            }
        }
    }

    private static void CheckShortHighlights(int? count, int highlights, string path, Action<string, string> fail)
    {
        if (count is { } n && (n < 0 || n > highlights))
        {
            fail(path, $"Must be from 0 to the number of highlights ({highlights}).");
        }
    }

    private static void CheckPeriod(string? start, string? end, string path, bool required, Action<string, string> fail)
    {
        var hasStart = PartialDate.TryParse(start, out var first);
        if (start is null)
        {
            if (required)
            {
                fail($"{path}/startDate", "Needs a start date in the form YYYY, YYYY-MM or YYYY-MM-DD.");
            }
        }
        else if (!hasStart)
        {
            fail($"{path}/startDate", "Must be a date in the form YYYY, YYYY-MM or YYYY-MM-DD.");
        }

        if (end is null or "")
        {
            return;
        }

        if (!PartialDate.TryParse(end, out var last))
        {
            fail($"{path}/endDate", "Must be a date in the form YYYY, YYYY-MM or YYYY-MM-DD, or empty for an ongoing item.");
        }
        else if (hasStart && last.LastDay < first.FirstDay)
        {
            fail($"{path}/endDate", $"Ends ({end}) before it starts ({start}).");
        }
    }

    private static void CheckOptionalDate(string? date, string path, Action<string, string> fail)
    {
        if (date is { Length: > 0 } && !PartialDate.TryParse(date, out _))
        {
            fail(path, "Must be a date in the form YYYY, YYYY-MM or YYYY-MM-DD.");
        }
    }

    private static void CheckRequired(string? value, string path, Action<string, string> fail)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            fail(path, "Is required.");
        }
    }

    private static void CheckUrl(string? url, string path, Action<string, string> fail)
    {
        if (url is { Length: > 0 } && !(Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)))
        {
            fail(path, "Must be an absolute http or https address.");
        }
    }
}
