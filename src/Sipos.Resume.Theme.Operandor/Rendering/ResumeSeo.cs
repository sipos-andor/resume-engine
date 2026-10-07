using System.Text.RegularExpressions;
using Operandor.SharedKernel.UI.Seo;
using Operandor.SharedKernel.UI.Site;
using Sipos.Resume.Core.Localization;
using Sipos.Resume.Core.Site;

namespace Sipos.Resume.Theme.Operandor.Rendering;

/// <summary>Describes a CV site to the shared kernel's <c>SeoHead</c>: alternates, locales, share images and the person.</summary>
internal static partial class ResumeSeo
{
    /// <summary>The share image's size, which every share service expects for a large card.</summary>
    public const int ImageWidth = 1200;

    /// <summary>The share image's height.</summary>
    public const int ImageHeight = 630;

    /// <summary>
    /// The identifier of the organization an operandor position names, the one operandor.io's own structured data
    /// gives itself, so search engines see one company.
    /// </summary>
    public const string OperandorOrganizationId = "https://operandor.io/#organization";

    /// <summary>The site options of a CV site: every language page has the others as alternates, the root as x-default.</summary>
    /// <param name="pages">Every language's page, the default first.</param>
    public static SiteOptions Options(IReadOnlyList<SitePage> pages)
    {
        var settings = pages[0].Settings;
        var byPath = pages.ToDictionary(page => page.Path, StringComparer.Ordinal);
        IReadOnlyList<SeoAlternate> alternates =
        [
            .. pages.Select(page => new SeoAlternate(page.Document.Language.Tag, page.Path)),
            new SeoAlternate("x-default", pages.First(page => page.Document.Language.IsDefault).Path),
        ];
        return new SiteOptions(settings.Origin)
        {
            AlternatesOf = path => byPath.ContainsKey(path) ? alternates : [],
            LocaleOf = path => byPath.TryGetValue(path, out var page) ? page.Document.Language.OgLocale : null,
            ImageOf = path => byPath.TryGetValue(path, out var page) && page.ShareImagePath is { } image
                ? new SeoImage(image, ImageWidth, ImageHeight, Title(page))
                : null,
        };
    }

    /// <summary>The page's title: the name, the word for CV and the person's title.</summary>
    /// <param name="page">The page.</param>
    public static string Title(SitePage page)
    {
        var person = page.Document.Person;
        var cv = new ResumeLabels(page.Document.Language.Culture).CurriculumVitae;
        return person.Title is null ? $"{person.Name} – {cv}" : $"{person.Name} – {cv}: {person.Title}";
    }

    /// <summary>The page's description: the profile's first sentence, cut at a word to at most 160 characters.</summary>
    /// <param name="page">The page.</param>
    public static string? Description(SitePage page)
    {
        if (page.Document.Person.Summary is not { } summary)
        {
            return page.Document.Person.Title;
        }

        var sentence = FirstSentence(summary);
        if (sentence.Length <= 160)
        {
            return sentence;
        }

        var cut = sentence.LastIndexOf(' ', 157);
        return sentence[..(cut < 0 ? 157 : cut)].TrimEnd(',', ';', ' ') + "…";
    }

    // Decision: a sentence ends at ". " only before a capital letter and after a word that is not a number.
    // Why: Croatian and Serbian write ordinals and years with a dot ("od 2013. godine"), Hungarian too ("2015. március",
    // "Kft. vezető"); cutting there would leave a description of "Od 2013.".
    private static string FirstSentence(string text)
    {
        foreach (Match end in SentenceEnd().Matches(text))
        {
            if (!end.Groups["word"].Value.All(char.IsDigit))
            {
                return text[..(end.Index + end.Groups["word"].Length + 1)];
            }
        }

        return text;
    }

    [GeneratedRegex(@"(?<word>\w+)\.\s+(?=\p{Lu})", RegexOptions.CultureInvariant)]
    private static partial Regex SentenceEnd();

    // Decision: an education entry is a degree in the structured data only when its type names one, such as BSc.
    // Why: JSON Resume's education list also holds summer schools and courses; calling them degrees would tell search
    // engines of degrees the person does not hold. The others stay credentials without a category.
    [GeneratedRegex(@"\b(B\.?\s?Sc|M\.?\s?Sc|B\.?\s?A|M\.?\s?A|B\.?\s?Eng|M\.?\s?Eng|Ph\.?\s?D|MBA|LL\.?\s?[BM]|Bachelor|Master|Doctor)\b", RegexOptions.CultureInvariant)]
    private static partial Regex Degree();

    /// <summary>
    /// The person as structured data, only with facts the page shows: names, title, profile, phone, profiles, the
    /// current employer, top skills, languages, schools, degrees, certificates and awards. Never an e-mail address.
    /// </summary>
    /// <param name="page">The page.</param>
    public static SeoPerson Person(SitePage page)
    {
        var document = page.Document;
        var person = document.Person;
        var settings = page.Settings;
        var employer = document.Positions.FirstOrDefault(position => position.Period.IsOngoing && position.Url is not null);
        return new SeoPerson(settings.Url("/").AbsoluteUri + "#person", person.Name)
        {
            GivenName = person.GivenName,
            FamilyName = person.FamilyName,
            JobTitle = person.Title,
            Description = person.Summary,
            Url = settings.Url("/").AbsoluteUri,
            ImagePath = page.ShareImagePath,
            Telephone = person.PhoneDial,
            SameAs = [.. person.Profiles.Select(profile => profile.Url)],
            WorksFor = employer is null ? null : new SeoReference(OrganizationId(employer.Url!), employer.Organization, employer.Url),
            KnowsAbout = [.. document.SkillGroups.SelectMany(group => group.Skills).Where(skill => skill.Rating >= 4).Select(skill => skill.Name).Distinct(StringComparer.Ordinal)],
            KnowsLanguage = [.. document.Languages.Select(language => language.Name)],
            AlumniOf = [.. document.Education.Select(study => study.Institution).Distinct(StringComparer.Ordinal)],
            Credentials =
            [
                .. document.Education.Where(study => study.StudyType is not null || study.Area is not null)
                    .Select(study => new SeoCredential(
                        string.Join(", ", new[] { study.StudyType, study.Area }.OfType<string>()),
                        study.StudyType is { } type && Degree().IsMatch(type) ? "degree" : null)),
                .. document.Certificates.Select(certificate => new SeoCredential(certificate.Name, "certificate")),
            ],
            Awards = [.. document.Awards.Select(award => award.Title)],
        };
    }

    // Decision: an employer is named by the identifier its own site's structured data gives it: operandor's is known,
    // any other organization's is its address with "#organization".
    // Why: a search engine joins two nodes into one entity only by identifier.
    private static string OrganizationId(string url) =>
        new Uri(url).Host.Equals("operandor.io", StringComparison.OrdinalIgnoreCase)
            ? OperandorOrganizationId
            : new UriBuilder(url) { Fragment = "organization" }.Uri.AbsoluteUri;
}
