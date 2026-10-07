using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Focus;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Tests.Artifacts;

public class DocumentOutlineOf
{
    [Fact]
    public void ListsDesignedSectionsWithCertificatesUnderEducationGivenDesignedLayout() =>
        DocumentOutline.Of(SampleDocuments.English(), DocumentVariant.Designed, focus: null).Sections.ShouldBe(
        [
            DocumentSection.Profile, DocumentSection.Strengths, DocumentSection.Experience, DocumentSection.Projects,
            DocumentSection.Skills, DocumentSection.Education, DocumentSection.Awards, DocumentSection.Languages,
        ]);

    // Applicant tracking systems know Summary, Skills, Work Experience, Education and Certifications, and score early keywords.
    [Fact]
    public void PutsSkillsAfterSummaryAndLeavesOutStrengthsGivenAtsLayout()
    {
        var outline = DocumentOutline.Of(SampleDocuments.English(), DocumentVariant.Ats, focus: null);

        outline.Sections.ShouldBe(
        [
            DocumentSection.Profile, DocumentSection.Skills, DocumentSection.Experience, DocumentSection.Projects,
            DocumentSection.Education, DocumentSection.Certificates, DocumentSection.Awards, DocumentSection.Languages,
        ]);
        outline.Heading(DocumentSection.Experience).ShouldBe("Work Experience");
        outline.Heading(DocumentSection.Certificates).ShouldBe("Certifications");
    }

    [Fact]
    public void LeavesOutEmptySectionsGivenCvWithoutThem()
    {
        var document = SampleDocuments.English() with { Awards = [], Projects = [], Strengths = [] };

        DocumentOutline.Of(document, DocumentVariant.Designed, focus: null).Sections
            .ShouldNotContain(section => section == DocumentSection.Awards || section == DocumentSection.Projects || section == DocumentSection.Strengths);
    }

    [Fact]
    public void WritesHeadingsAndDatesInCvLanguageGivenLayout()
    {
        var document = SampleDocuments.English();
        var period = document.Positions[1].Period;

        DocumentOutline.Of(document, DocumentVariant.Designed, focus: null).Heading(DocumentSection.Experience).ShouldBe("Professional experience");
        DocumentOutline.Of(document, DocumentVariant.Designed, focus: null).Period(period).ShouldBe("March 2015 – October 2022");
        DocumentOutline.Of(document, DocumentVariant.Ats, focus: null).Period(period).ShouldBe("03/2015 – 10/2022");
    }

    [Fact]
    public void KeepsEveryHighlightGivenNoFocus()
    {
        var document = SampleDocuments.English() with { Positions = [SampleDocuments.English().Positions[0] with { Highlights = ["a", "b", "c"] }] };

        DocumentOutline.Of(document, DocumentVariant.Designed, focus: null).Highlights(document.Positions[0]).ShouldBe(["a", "b", "c"]);
    }

    // A tailored CV keeps the whole history in date order and only shortens what its focus passes by.
    [Fact]
    public void ShortensItemsFocusPassesByGivenFocus()
    {
        var english = SampleDocuments.English();
        var document = english with
        {
            Positions = [english.Positions[0], english.Positions[1] with { Highlights = ["a", "b", "c"] }],
        };
        var focus = FocusView.Build(document).Single();

        var outline = DocumentOutline.Of(document, DocumentVariant.Designed, focus);

        outline.IsEmphasized("acme").ShouldBeTrue();
        outline.IsEmphasized("initech").ShouldBeFalse();
        outline.Highlights(document.Positions[1]).ShouldBe(["a", "b"]);
        outline.Highlights(document.Positions[0]).ShouldBe(document.Positions[0].Highlights);
    }

    [Fact]
    public void UsesFocusSummaryAndOrderGivenFocusWithSummary()
    {
        var english = SampleDocuments.English();
        var document = english with
        {
            FocusProfiles = [english.FocusProfiles[0] with { Summary = "Designs systems." }],
            Strengths = [new Strength("a", "A", null, false, []), new Strength("b", "B", null, false, ["architect"])],
        };

        var outline = DocumentOutline.Of(document, DocumentVariant.Designed, FocusView.Build(document).Single());

        outline.Summary.ShouldBe("Designs systems.");
        outline.Strengths.Select(strength => strength.Id).ShouldBe(["b", "a"]);
        outline.SkillGroups.Select(group => group.Name).ShouldBe(["Cloud", ".NET platform"]);
    }

    [Fact]
    public void FallsBackToRatingWordGivenSkillWithoutLevel()
    {
        var outline = DocumentOutline.Of(SampleDocuments.English(), DocumentVariant.Ats, focus: null);

        outline.Level(new Skill("C#", 4, null)).ShouldBe("Advanced");
        outline.Level(new Skill("C#", 4, "Strong")).ShouldBe("Strong");
        outline.Level(new Skill("C#", null, null)).ShouldBeNull();
    }
}
