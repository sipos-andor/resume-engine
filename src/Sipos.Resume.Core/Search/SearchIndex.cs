using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Search;

/// <summary>One searchable item of a CV, found by its anchor.</summary>
/// <param name="Id">The item's anchor on the page, such as <c>lightbloom</c>.</param>
/// <param name="Title">What a result shows, such as <c>Lightbloom – Lead Software Engineer</c>.</param>
public sealed record SearchEntry(string Id, string Title);

/// <summary>
/// The CV's search index: its items, and for every folded token the items that contain it. The page embeds it, and the
/// browser answers a query with a prefix match on the tokens.
/// </summary>
/// <param name="Entries">The searchable items, in the page's order.</param>
/// <param name="Tokens">For each folded token, the indexes of the entries that contain it, ascending.</param>
public sealed record SearchIndex(IReadOnlyList<SearchEntry> Entries, IReadOnlyDictionary<string, IReadOnlyList<int>> Tokens)
{
    /// <summary>Builds the index of a CV: strengths, positions, projects, skill groups, studies, certificates and awards.</summary>
    /// <param name="document">The CV.</param>
    public static SearchIndex Build(ResumeDocument document)
    {
        var items = new List<(SearchEntry Entry, string Text)>();
        foreach (var strength in document.Strengths)
        {
            items.Add((new SearchEntry(strength.Id, strength.Title), $"{strength.Title} {strength.Summary}"));
        }

        foreach (var position in document.Positions)
        {
            var title = position.Role is null ? position.Organization : $"{position.Organization} – {position.Role}";
            items.Add((new SearchEntry(position.Id, title), string.Join(' ', [position.Organization, position.Role, position.Note, position.Location, position.Summary, .. position.Highlights, .. position.Keywords])));
            items.AddRange(position.Engagements.Select(engagement => Engagement(engagement)));
        }

        items.AddRange(document.Projects.Select(Engagement));
        for (var i = 0; i < document.SkillGroups.Count; i++)
        {
            var group = document.SkillGroups[i];
            items.Add((new SearchEntry($"skills-{i + 1}", group.Name), string.Join(' ', [group.Name, .. group.Skills.Select(skill => skill.Name)])));
        }

        for (var i = 0; i < document.Education.Count; i++)
        {
            var study = document.Education[i];
            items.Add((new SearchEntry($"education-{i + 1}", study.Institution), $"{study.Institution} {study.Area} {study.StudyType}"));
        }

        for (var i = 0; i < document.Certificates.Count; i++)
        {
            items.Add((new SearchEntry($"certificate-{i + 1}", document.Certificates[i].Name), $"{document.Certificates[i].Name} {document.Certificates[i].Issuer}"));
        }

        for (var i = 0; i < document.Awards.Count; i++)
        {
            items.Add((new SearchEntry($"award-{i + 1}", document.Awards[i].Title), $"{document.Awards[i].Title} {document.Awards[i].Awarder} {document.Awards[i].Summary}"));
        }

        var tokens = new SortedDictionary<string, SortedSet<int>>(StringComparer.Ordinal);
        for (var i = 0; i < items.Count; i++)
        {
            foreach (var token in TextNormalizer.Tokens(items[i].Text))
            {
                if (!tokens.TryGetValue(token, out var set))
                {
                    tokens[token] = set = [];
                }

                set.Add(i);
            }
        }

        return new SearchIndex(
            [.. items.Select(item => item.Entry)],
            tokens.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<int>)[.. pair.Value], StringComparer.Ordinal));
    }

    /// <summary>
    /// Answers a query as the browser does: every query token must prefix a token of the entry; entries keep the
    /// page's order. An empty query finds nothing.
    /// </summary>
    /// <param name="query">What the reader typed.</param>
    public IReadOnlyList<SearchEntry> Find(string query)
    {
        var words = TextNormalizer.Tokens(query);
        if (words.Count == 0)
        {
            return [];
        }

        IEnumerable<int>? hits = null;
        foreach (var word in words)
        {
            var matches = Tokens.Where(pair => pair.Key.StartsWith(word, StringComparison.Ordinal)).SelectMany(pair => pair.Value).ToHashSet();
            hits = hits is null ? matches : hits.Intersect(matches);
        }

        return [.. hits!.Order().Select(index => Entries[index])];
    }

    private static (SearchEntry, string) Engagement(Engagement engagement)
    {
        var title = engagement.Client is null ? engagement.Name : $"{engagement.Name} – {engagement.Client}";
        return (new SearchEntry(engagement.Id, title), string.Join(' ', [engagement.Name, engagement.Client, engagement.Description, .. engagement.Roles, .. engagement.Highlights, .. engagement.Keywords]));
    }
}
