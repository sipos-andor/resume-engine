using Sipos.Resume.Core.Languages;
using Sipos.Resume.Core.Mapping;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Testing;

/// <summary>The sample CVs as domain documents.</summary>
internal static class SampleDocuments
{
    public static readonly DateOnly Today = new(2026, 10, 7);

    public static ResumeDocument English() => ResumeMapper.Map(Samples.Read(Samples.English), LanguageCatalog.Describe("en", null, isDefault: true));

    public static ResumeDocument Hungarian()
    {
        var resume = Samples.Read(Samples.Hungarian);
        return ResumeMapper.Map(resume, LanguageCatalog.Describe("hu", resume.Meta, isDefault: false));
    }
}
