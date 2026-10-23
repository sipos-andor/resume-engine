using System.Globalization;
using System.Text.Json;
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
                    IEnumerable<string> list => string.Concat(list.Select(item => item.Length.ToString(CultureInfo.InvariantCulture) + ":" + item)),
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
        Add("/basics/location/countryCode", basics?.Location?.CountryCode);
        Add("/basics/location/postalCode", basics?.Location?.PostalCode);
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

        Add("/volunteer", resume.Volunteer.Count);
        for (var i = 0; i < resume.Volunteer.Count; i++)
        {
            var work = resume.Volunteer[i];
            Add($"/volunteer/{i}/organization", work.Organization);
            Add($"/volunteer/{i}/url", work.Url);
            Add($"/volunteer/{i}/startDate", work.StartDate);
            Add($"/volunteer/{i}/endDate", work.EndDate);
            Add($"/volunteer/{i}/highlights", work.Highlights.Count);
        }

        // A study's score is written in the language's own way (8.11/10, 8,11/10), so only the number of courses is a fact.
        Add("/education", resume.Education.Count);
        for (var i = 0; i < resume.Education.Count; i++)
        {
            Add($"/education/{i}/url", resume.Education[i].Url);
            Add($"/education/{i}/startDate", resume.Education[i].StartDate);
            Add($"/education/{i}/endDate", resume.Education[i].EndDate);
            Add($"/education/{i}/courses", resume.Education[i].Courses.Count);
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

        Add("/publications", resume.Publications.Count);
        for (var i = 0; i < resume.Publications.Count; i++)
        {
            Add($"/publications/{i}/publisher", resume.Publications[i].Publisher);
            Add($"/publications/{i}/releaseDate", resume.Publications[i].ReleaseDate);
            Add($"/publications/{i}/url", resume.Publications[i].Url);
        }

        Add("/skills", resume.Skills.Count);
        for (var i = 0; i < resume.Skills.Count; i++)
        {
            Add($"/skills/{i}/keywords", resume.Skills[i].Keywords);
            Add($"/skills/{i}/x-rating", resume.Skills[i].Rating);
            Add($"/skills/{i}/x-ratingKind", resume.Skills[i].RatingKind);
        }

        // The scale's words and the note are translated; the ratings, the basis and the confirmed uses are facts.
        if (resume.SkillsAssessment is { } assessment)
        {
            Add("/x-skillsAssessment/basis", assessment.Basis);
            Add("/x-skillsAssessment/assessedOn", assessment.AssessedOn);
            Add("/x-skillsAssessment/scale", assessment.Scale.Count);
            for (var i = 0; i < assessment.Scale.Count; i++)
            {
                Add($"/x-skillsAssessment/scale/{i}/value", assessment.Scale[i].Value);
            }

            Add("/x-skillsAssessment/confirmedAssociations", assessment.ConfirmedAssociations.Count);
            for (var i = 0; i < assessment.ConfirmedAssociations.Count; i++)
            {
                var association = assessment.ConfirmedAssociations[i];
                Add($"/x-skillsAssessment/confirmedAssociations/{i}/skill", association.Skill);
                Add($"/x-skillsAssessment/confirmedAssociations/{i}/work", association.Work);
                Add($"/x-skillsAssessment/confirmedAssociations/{i}/project", association.Project);
                Add($"/x-skillsAssessment/confirmedAssociations/{i}/context", association.Context);
                Add($"/x-skillsAssessment/confirmedAssociations/{i}/startDate", association.StartDate);
                Add($"/x-skillsAssessment/confirmedAssociations/{i}/endDate", association.EndDate);
            }
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

        // These standard sections are republished from Original even though the domain does not render them.
        OriginalSection("interests", [], ["keywords"]);
        OriginalSection("references", ["name"], []);

        return facts;

        void OriginalSection(string name, string[] neutralFields, string[] translatedLists)
        {
            var original = resume.Original;
            var count = original is { ValueKind: JsonValueKind.Object } json
                && json.TryGetProperty(name, out var section) && section.ValueKind == JsonValueKind.Array
                ? section.GetArrayLength() : 0;
            Add($"/{name}", count);
            if (count == 0)
            {
                return;
            }

            var index = 0;
            foreach (var item in original!.Value.GetProperty(name).EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in neutralFields)
                    {
                        if (item.TryGetProperty(field, out var value) && value.ValueKind != JsonValueKind.Null)
                        {
                            Add($"/{name}/{index}/{field}", value.ToString());
                        }
                    }

                    foreach (var field in translatedLists)
                    {
                        Add($"/{name}/{index}/{field}", item.TryGetProperty(field, out var value)
                            && value.ValueKind == JsonValueKind.Array ? value.GetArrayLength() : 0);
                    }
                }

                index++;
            }
        }
    }
}
