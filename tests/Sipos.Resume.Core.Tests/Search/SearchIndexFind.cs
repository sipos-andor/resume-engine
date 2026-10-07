using Sipos.Resume.Core.Search;

namespace Sipos.Resume.Core.Tests.Search;

public class SearchIndexFind
{
    private static readonly SearchIndex Index = SearchIndex.Build(SampleDocuments.English());

    [Fact]
    public void ListsItemsInPageOrder() =>
        Index.Entries.Select(entry => entry.Id).ShouldBe(["delivery", "acme", "portal", "initech", "hobby", "skills-1", "skills-2", "education-1", "certificate-1", "award-1"]);

    [Fact]
    public void FindsByPrefixGivenPartialWord() => Index.Find("glob").Select(e => e.Id).ShouldBe(["portal"]);

    [Fact]
    public void FindsEveryItemWithTechnologyGivenName() => Index.Find("Azure").Select(e => e.Id).ShouldBe(["portal", "skills-2"]);

    // Every word must match, in any item text.
    [Fact]
    public void NeedsEveryWordGivenSeveralWords() => Index.Find("portal azure").Select(e => e.Id).ShouldBe(["portal"]);

    [Fact]
    public void IgnoresCaseAndDiacriticsGivenQuery() => Index.Find("PÓLYTECHNIC").Select(e => e.Id).ShouldBe(["education-1"]);

    [Theory]
    [InlineData("")]
    [InlineData("  ... ")]
    [InlineData("kubernetes")]
    public void FindsNothingGivenEmptyOrUnknownQuery(string query) => Index.Find(query).ShouldBeEmpty();
}
