using Sipos.Resume.Core.Evidence;
using Sipos.Resume.Core.Focus;
using Sipos.Resume.Core.Matching;
using Sipos.Resume.Core.Model;
using Sipos.Resume.Core.Search;
using Sipos.Resume.Core.Timeline;

namespace Sipos.Resume.Core.Insights;

/// <summary>Everything the engine derives from a CV for its interactive features and tailored documents.</summary>
/// <param name="Search">The search index.</param>
/// <param name="Technologies">The technologies, for the filter.</param>
/// <param name="Evidence">Where and when each skill was used, by skill name.</param>
/// <param name="Focus">The view of each position profile.</param>
/// <param name="Vocabulary">The terms a job ad is matched against.</param>
/// <param name="Timeline">The positions and projects on a time axis.</param>
public sealed record ResumeInsights(
    SearchIndex Search,
    TechnologyIndex Technologies,
    IReadOnlyDictionary<string, SkillEvidence> Evidence,
    IReadOnlyList<FocusView> Focus,
    JobVocabulary Vocabulary,
    TimelineModel Timeline)
{
    /// <summary>Derives everything from a CV.</summary>
    /// <param name="document">The CV.</param>
    /// <param name="today">The date that stands for the present.</param>
    public static ResumeInsights Analyze(ResumeDocument document, DateOnly today)
    {
        var technologies = TechnologyIndex.Build(document);
        return new ResumeInsights(
            SearchIndex.Build(document),
            technologies,
            SkillEvidence.Build(document),
            FocusView.Build(document),
            JobVocabulary.Build(document, technologies),
            TimelineModel.Build(document, today));
    }
}
