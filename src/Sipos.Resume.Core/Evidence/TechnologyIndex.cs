using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Evidence;

/// <summary>A technology the CV names, with the items it appears in.</summary>
/// <param name="Name">The name as the CV first writes it, such as <c>Azure Service Bus</c>.</param>
/// <param name="Key">The folded key, the same for every spelling and alias.</param>
/// <param name="ItemIds">The positions and projects that name it, in the CV's order.</param>
public sealed record Technology(string Name, string Key, IReadOnlyList<string> ItemIds);

/// <summary>Every technology the positions and projects name, most used first: the technology filter's choices.</summary>
/// <param name="Technologies">The technologies, by number of items, then by name.</param>
public sealed record TechnologyIndex(IReadOnlyList<Technology> Technologies)
{
    /// <summary>Collects the technologies of a CV, merging aliases into the name they stand for.</summary>
    /// <param name="document">The CV.</param>
    public static TechnologyIndex Build(ResumeDocument document)
    {
        var canonical = Canonical(document);
        var byKey = new Dictionary<string, (string Name, List<string> Ids)>(StringComparer.Ordinal);
        void Add(string keyword, string id)
        {
            var key = Keys.Of(keyword);
            if (key.Length == 0)
            {
                return;
            }

            if (canonical.TryGetValue(key, out var target))
            {
                (key, keyword) = (Keys.Of(target), target);
            }

            if (!byKey.TryGetValue(key, out var entry))
            {
                byKey[key] = entry = (keyword, []);
            }

            if (!entry.Ids.Contains(id))
            {
                entry.Ids.Add(id);
            }
        }

        foreach (var position in document.Positions)
        {
            foreach (var keyword in position.Keywords)
            {
                Add(keyword, position.Id);
            }

            foreach (var engagement in position.Engagements)
            {
                foreach (var keyword in engagement.Keywords)
                {
                    Add(keyword, engagement.Id);
                }
            }
        }

        foreach (var engagement in document.Projects)
        {
            foreach (var keyword in engagement.Keywords)
            {
                Add(keyword, engagement.Id);
            }
        }

        return new TechnologyIndex(
            [.. byKey.Select(pair => new Technology(pair.Value.Name, pair.Key, pair.Value.Ids))
                .OrderByDescending(technology => technology.ItemIds.Count)
                .ThenBy(technology => technology.Key, StringComparer.Ordinal)]);
    }

    /// <summary>Returns the technology with a key, or <see langword="null"/>.</summary>
    /// <param name="key">The folded key.</param>
    public Technology? Find(string key) => Technologies.FirstOrDefault(technology => technology.Key == key);

    /// <summary>Maps the key of every alias to the name it stands for.</summary>
    /// <param name="document">The CV, whose <c>x-aliases</c> name the aliases.</param>
    internal static Dictionary<string, string> Canonical(ResumeDocument document)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (term, aliases) in document.Aliases)
        {
            foreach (var alias in aliases)
            {
                map.TryAdd(Keys.Of(alias), term);
            }
        }

        return map;
    }
}
