using Sipos.Resume.Core.Languages;
using Sipos.Resume.Core.Mapping;
using Sipos.Resume.Core.Model;

namespace Sipos.Resume.Core.Tests.Helpers;

/// <summary>The sample CVs as domain documents.</summary>
internal static class Documents
{
    public static readonly DateOnly Today = new(2026, 10, 7);

    public static ResumeDocument English() => ResumeMapper.Map(Samples.Read(Samples.English), LanguageCatalog.Describe("en", null, isDefault: true));
}
