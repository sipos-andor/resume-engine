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

    [Fact]
    public void LinksItemsThatUseTerm() => Vocabulary.Terms.Single(term => term.Term == "Azure").ItemIds.ShouldBe(["portal"]);
}
