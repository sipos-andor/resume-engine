using Sipos.Resume.Core.Evidence;
using Sipos.Resume.Core.Matching;

namespace Sipos.Resume.Core.Tests.Matching;

public class JobVocabularyMatch
{
    private static readonly JobVocabulary Vocabulary = JobVocabulary.Build(SampleDocuments.English(), TechnologyIndex.Build(SampleDocuments.English()));

    [Fact]
    public void ListsSkillsFirstThenOtherTechnologies() =>
        Vocabulary.Terms.Select(term => (term.Term, term.IsSkill)).ShouldBe([("C#", true), ("LINQ", true), ("Blazor", true), ("Azure", true), ("Rust", false)]);

    [Fact]
    public void FindsTermsGivenJobAd() =>
        Vocabulary.Match("We need a senior C# developer with Azure and some Rust.").Select(term => term.Term).ShouldBe(["C#", "Azure", "Rust"]);

    [Fact]
    public void FindsTermGivenAlias() => Vocabulary.Match("CSharp experience").Select(term => term.Term).ShouldBe(["C#"]);

    // Whole tokens only: "rusty" is not Rust, "blazorise" is not Blazor.
    [Fact]
    public void IgnoresPartOfWordGivenLongerWord() => Vocabulary.Match("rusty blazorise linqpad").ShouldBeEmpty();

    // The validator refuses such content, but a document built without it still gets a vocabulary: the first term wins.
    [Fact]
    public void KeepsFirstTermGivenTwoSpellingsOfOneTerm()
    {
        var english = SampleDocuments.English();
        var document = english with { Aliases = new Dictionary<string, IReadOnlyList<string>> { ["C#"] = ["csharp"], ["c#"] = ["c sharp"] } };

        JobVocabulary.Build(document, TechnologyIndex.Build(document)).Match("csharp").Select(term => term.Term).ShouldBe(["C#"]);
    }

    [Fact]
    public void LinksItemsThatUseTerm() => Vocabulary.Terms.Single(term => term.Term == "Azure").ItemIds.ShouldBe(["portal"]);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanonicalizesSkillAliasBeforeDeduplicationAndEvidenceLookup(bool includeCanonicalSkill)
    {
        var english = SampleDocuments.English();
        var group = english.SkillGroups[0];
        var skill = group.Skills[0];
        var document = english with
        {
            SkillGroups = [group with { Skills = includeCanonicalSkill ? [skill with { Name = "csharp" }, skill] : [skill with { Name = "csharp" }] }],
            Aliases = new Dictionary<string, IReadOnlyList<string>> { ["C#"] = ["csharp"] }
        };
        var technologies = TechnologyIndex.Build(document);
        var vocabulary = JobVocabulary.Build(document, technologies);

        var match = vocabulary.Match("csharp").ShouldHaveSingleItem();
        match.Term.ShouldBe("C#");
        match.IsSkill.ShouldBeTrue();
        match.ItemIds.ShouldBe(technologies.Find(technologies.KeyOf("C#"))!.ItemIds);
        match.ItemIds.ShouldNotBeEmpty();
        vocabulary.Match("C#").ShouldBe([match]);
    }
}
