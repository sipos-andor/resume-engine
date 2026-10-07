using System.Text.Json;
using System.Text.RegularExpressions;
using Sipos.Resume.Core.Dates;

namespace Sipos.Resume.Core.Validation;

/// <summary>Checks standard JSON Resume v1.2.1 fields that the typed reader does not model.</summary>
internal static partial class PreservedResumeValidator
{
    /// <summary>Validates preserved sections and education fields without constraining unknown extensions.</summary>
    public static IEnumerable<ValidationIssue> Check(string source, JsonElement original)
    {
        var issues = new List<ValidationIssue>();
        Section("volunteer", ["organization", "position", "url", "startDate", "endDate", "summary"], ["highlights"]);
        Section("publications", ["name", "publisher", "releaseDate", "url", "summary"], []);
        Section("interests", ["name"], ["keywords"]);
        Section("references", ["name", "reference"], []);

        if (original.TryGetProperty("education", out var education) && education.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in education.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    Fields(item, $"/education/{index}", ["score"], ["courses"]);
                }

                index++;
            }
        }

        return issues;

        void Fail(string path, string message) => issues.Add(new ValidationIssue(source, path, message));

        void Section(string name, string[] strings, string[] lists)
        {
            if (!original.TryGetProperty(name, out var section))
            {
                return;
            }

            if (section.ValueKind != JsonValueKind.Array)
            {
                Fail($"/{name}", "Must be an array of objects (JSON Resume v1.2.1).");
                return;
            }

            var index = 0;
            foreach (var item in section.EnumerateArray())
            {
                var path = $"/{name}/{index++}";
                if (item.ValueKind != JsonValueKind.Object)
                {
                    Fail(path, "Must be an object (JSON Resume v1.2.1).");
                    continue;
                }

                Fields(item, path, strings, lists);
            }
        }

        void Fields(JsonElement item, string path, string[] strings, string[] lists)
        {
            foreach (var field in strings)
            {
                if (!item.TryGetProperty(field, out var value))
                {
                    continue;
                }

                var pointer = $"{path}/{field}";
                if (value.ValueKind != JsonValueKind.String)
                {
                    Fail(pointer, "Must be a string (JSON Resume v1.2.1).");
                }
                else if (field is "startDate" or "endDate" or "releaseDate"
                    && (!IsoDate().IsMatch(value.GetString()!) || !PartialDate.TryParse(value.GetString(), out _)))
                {
                    Fail(pointer, "Must be an ISO date: YYYY, YYYY-MM or YYYY-MM-DD (JSON Resume v1.2.1).");
                }
                else if (field == "url" && !Uri.TryCreate(value.GetString(), UriKind.Absolute, out _))
                {
                    Fail(pointer, "Must be an absolute URI (JSON Resume v1.2.1).");
                }
            }

            foreach (var field in lists)
            {
                if (!item.TryGetProperty(field, out var list))
                {
                    continue;
                }

                var pointer = $"{path}/{field}";
                if (list.ValueKind != JsonValueKind.Array)
                {
                    Fail(pointer, "Must be an array of strings (JSON Resume v1.2.1).");
                    continue;
                }

                var index = 0;
                foreach (var value in list.EnumerateArray())
                {
                    if (value.ValueKind != JsonValueKind.String)
                    {
                        Fail($"{pointer}/{index}", "Must be a string (JSON Resume v1.2.1).");
                    }

                    index++;
                }
            }
        }
    }

    [GeneratedRegex("^[12][0-9]{3}(-[01][0-9](-[0-3][0-9])?)?$", RegexOptions.CultureInvariant)]
    private static partial Regex IsoDate();
}
