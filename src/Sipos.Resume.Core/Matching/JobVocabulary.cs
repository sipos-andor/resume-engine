using Sipos.Resume.Core.Evidence;
using Sipos.Resume.Core.Model;
using Sipos.Resume.Core.Search;

namespace Sipos.Resume.Core.Matching;

/// <summary>A technology or skill a job ad can name, with every spelling that counts and the items that show it.</summary>
/// <param name="Term">The name as the CV writes it.</param>
/// <param name="Phrases">The folded token sequences that name it: its own and its aliases'.</param>
/// <param name="ItemIds">The positions and projects that use it.</param>
/// <param name="IsSkill">Whether the skills section lists it.</param>
public sealed record VocabularyTerm(string Term, IReadOnlyList<IReadOnlyList<string>> Phrases, IReadOnlyList<string> ItemIds, bool IsSkill);

/// <summary>
/// The words of a CV a job ad is matched against: its skills and technologies with their aliases. The page embeds it,
/// and the browser finds the terms of a pasted ad without sending it anywhere.
/// </summary>
/// <param name="Terms">The terms, skills first in the skills section's order, then the other technologies.</param>
public sealed record JobVocabulary(IReadOnlyList<VocabularyTerm> Terms)
{
    /// <summary>Builds the vocabulary of a CV.</summary>
    /// <param name="document">The CV.</param>
    /// <param name="technologies">The CV's technologies, for the items each one appears in.</param>
    public static JobVocabulary Build(ResumeDocument document, TechnologyIndex technologies)
    {
        // The validator refuses terms that fold to one key; the first one wins here too, so a document built without
        // validation still has a vocabulary.
        var aliases = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var (term, spellings) in document.Aliases)
        {
            aliases.TryAdd(Keys.Of(term), spellings);
        }

        var terms = new List<VocabularyTerm>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        void Add(string name, bool isSkill)
        {
            var key = Keys.Of(name);
            if (key.Length == 0 || !seen.Add(key))
            {
                return;
            }

            var phrases = new List<IReadOnlyList<string>> { TextNormalizer.Tokens(name) };
            if (aliases.TryGetValue(key, out var spellings))
            {
                phrases.AddRange(spellings.Select(TextNormalizer.Tokens).Where(tokens => tokens.Count > 0));
            }

            terms.Add(new VocabularyTerm(name, phrases, technologies.Find(key)?.ItemIds ?? [], isSkill));
        }

        foreach (var skill in document.SkillGroups.SelectMany(group => group.Skills))
        {
            Add(skill.Name, isSkill: true);
        }

        foreach (var technology in technologies.Technologies)
        {
            Add(technology.Name, isSkill: false);
        }

        return new JobVocabulary(terms);
    }

    /// <summary>Returns the terms a job ad names, as the browser finds them: a phrase matches whole consecutive tokens.</summary>
    /// <param name="jobAd">The ad's text.</param>
    public IReadOnlyList<VocabularyTerm> Match(string jobAd)
    {
        var tokens = TextNormalizer.Tokens(jobAd);
        return [.. Terms.Where(term => term.Phrases.Any(phrase => Contains(tokens, phrase)))];
    }

    private static bool Contains(IReadOnlyList<string> tokens, IReadOnlyList<string> phrase)
    {
        for (var start = 0; start + phrase.Count <= tokens.Count; start++)
        {
            var match = true;
            for (var i = 0; i < phrase.Count && match; i++)
            {
                match = tokens[start + i] == phrase[i];
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }
}
