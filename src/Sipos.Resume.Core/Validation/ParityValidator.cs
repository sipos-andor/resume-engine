using System.Globalization;
using Sipos.Resume.Core.Content;
using Sipos.Resume.Core.Mapping;

namespace Sipos.Resume.Core.Validation;

/// <summary>Checks that the language versions of a CV state the same facts.</summary>
/// <remarks>
/// Decision: every language file is a complete JSON Resume, and the build fails when a language-neutral fact differs
/// from the default language's: identifiers, dates, organization and client names, addresses, technologies, levels,
/// flags and the number of items.
/// Why: each file stays a standard JSON Resume any tool reads, and a date or a technology changed in one language only
/// cannot reach a reader as two different CVs.
/// Considered: one file of facts with per-language texts merged in, which no other JSON Resume tool can read.
/// </remarks>
public static class ParityValidator
{
    /// <summary>Compares every other language with the default one.</summary>
    /// <param name="reference">The default language's file name and document.</param>
    /// <param name="others">The other languages' file names and documents.</param>
    public static IReadOnlyList<ValidationIssue> Compare((string Source, JsonResume Resume) reference, IEnumerable<(string Source, JsonResume Resume)> others)
    {
        var expected = Facts(reference.Resume);
        var issues = new List<ValidationIssue>();
        foreach (var (source, resume) in others)
        {
            var actual = Facts(resume);
            var missingProjectIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Math.Min(reference.Resume.Projects.Count, resume.Projects.Count); i++)
            {
                var project = resume.Projects[i];
                var original = reference.Resume.Projects[i];
                if (!string.Equals(original.Name, project.Name, StringComparison.Ordinal)
                    && (string.IsNullOrEmpty(original.Id) || string.IsNullOrEmpty(project.Id)))
                {
                    var path = $"/projects/{i}/x-id";
                    missingProjectIds.Add(path);
                    var missingSource = string.IsNullOrEmpty(project.Id) ? source : reference.Source;
                    issues.Add(new ValidationIssue(missingSource, path, "Projects with different names across languages need the same explicit x-id in every language."));
                }
            }

            foreach (var (path, value) in expected)
            {
                if (missingProjectIds.Contains(path))
                {
                    continue;
                }

                if (!actual.TryGetValue(path, out var other))
                {
                    issues.Add(new ValidationIssue(source, path, $"Is missing; {reference.Source} has '{value}'."));
                }
                else if (!string.Equals(value, other, StringComparison.Ordinal))
                {
                    issues.Add(new ValidationIssue(source, path, $"Is '{other}', but {reference.Source} has '{value}'; language-neutral facts must match."));
                }
            }

            foreach (var path in actual.Keys.Where(path => !expected.ContainsKey(path)))
            {
                issues.Add(new ValidationIssue(source, path, $"Is not in {reference.Source}; every language must list the same items."));
            }
        }

        return issues;
    }

    /// <summary>The language-neutral facts of a document, by JSON pointer.</summary>
    /// <param name="resume">The document.</param>
    internal static SortedDictionary<string, string> Facts(JsonResume resume)
    {
        var facts = new SortedDictionary<string, string>(StringComparer.Ordinal);
        void Add(string path, object? value)
        {
            if (value is not null)
            {
                facts[path] = value switch
                {
                    IEnumerable<string> list => string.Join(" | ", list),
                    bool flag => flag ? "true" : "false",
                    IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
                    _ => value.ToString()!,
                };
            }
        }

        var basics = resume.Basics;
        Add("/basics/phone", basics?.Phone);
        Add("/basics/image", basics?.Image);
        Add("/basics/x-givenName", basics?.GivenName);
        Add("/basics/x-familyName", basics?.FamilyName);
        Add("/basics/x-availability/status", basics?.Availability?.Status);
        Add("/basics/x-availability/from", basics?.Availability?.From);
        for (var i = 0; i < (basics?.Profiles.Count ?? 0); i++)
        {
            Add($"/basics/profiles/{i}/network", basics!.Profiles[i].Network);
            Add($"/basics/profiles/{i}/url", basics.Profiles[i].Url);
        }

        Add("/work", resume.Work.Count);
        for (var i = 0; i < resume.Work.Count; i++)
        {
            var work = resume.Work[i];
            Add($"/work/{i}/x-id", Identifiers.Of(work));
            Add($"/work/{i}/name", work.Name);
            Add($"/work/{i}/url", work.Url);
            Add($"/work/{i}/startDate", work.StartDate);
            Add($"/work/{i}/endDate", work.EndDate);
            Add($"/work/{i}/highlights", work.Highlights.Count);
            Add($"/work/{i}/x-keywords", work.Keywords);
            Add($"/work/{i}/x-short", work.Short);
            Add($"/work/{i}/x-shortHighlights", work.ShortHighlights);
            Add($"/work/{i}/x-focus", work.Focus);
        }

        Add("/projects", resume.Projects.Count);
        for (var i = 0; i < resume.Projects.Count; i++)
        {
            var project = resume.Projects[i];
            Add($"/projects/{i}/x-id", Identifiers.Of(project));
            Add($"/projects/{i}/x-work", project.Work);
            Add($"/projects/{i}/entity", project.Entity);
            Add($"/projects/{i}/url", project.Url);
            Add($"/projects/{i}/startDate", project.StartDate);
            Add($"/projects/{i}/endDate", project.EndDate);
            Add($"/projects/{i}/keywords", project.Keywords);
            Add($"/projects/{i}/highlights", project.Highlights.Count);
            Add($"/projects/{i}/roles", project.Roles.Count);
            Add($"/projects/{i}/x-short", project.Short);
            Add($"/projects/{i}/x-shortHighlights", project.ShortHighlights);
            Add($"/projects/{i}/x-focus", project.Focus);
        }

        Add("/education", resume.Education.Count);
        for (var i = 0; i < resume.Education.Count; i++)
        {
            Add($"/education/{i}/url", resume.Education[i].Url);
            Add($"/education/{i}/startDate", resume.Education[i].StartDate);
            Add($"/education/{i}/endDate", resume.Education[i].EndDate);
        }

        Add("/certificates", resume.Certificates.Count);
        for (var i = 0; i < resume.Certificates.Count; i++)
        {
            Add($"/certificates/{i}/date", resume.Certificates[i].Date);
            Add($"/certificates/{i}/url", resume.Certificates[i].Url);
        }

        Add("/awards", resume.Awards.Count);
        for (var i = 0; i < resume.Awards.Count; i++)
        {
            Add($"/awards/{i}/date", resume.Awards[i].Date);
        }

        Add("/skills", resume.Skills.Count);
        for (var i = 0; i < resume.Skills.Count; i++)
        {
            Add($"/skills/{i}/keywords", resume.Skills[i].Keywords);
            Add($"/skills/{i}/x-rating", resume.Skills[i].Rating);
        }

        Add("/languages", resume.Languages.Count);
        Add("/x-strengths", resume.Strengths.Count);
        for (var i = 0; i < resume.Strengths.Count; i++)
        {
            Add($"/x-strengths/{i}/id", resume.Strengths[i].Id);
            Add($"/x-strengths/{i}/short", resume.Strengths[i].Short);
            Add($"/x-strengths/{i}/focus", resume.Strengths[i].Focus);
        }

        Add("/x-focusProfiles", resume.FocusProfiles.Count);
        for (var i = 0; i < resume.FocusProfiles.Count; i++)
        {
            Add($"/x-focusProfiles/{i}/id", resume.FocusProfiles[i].Id);
            Add($"/x-focusProfiles/{i}/skills", resume.FocusProfiles[i].Skills);
        }

        foreach (var (term, aliases) in resume.Aliases)
        {
            Add($"/x-aliases/{term}", aliases);
        }

        return facts;
    }
}
