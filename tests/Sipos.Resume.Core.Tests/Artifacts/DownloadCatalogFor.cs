using Sipos.Resume.Core.Artifacts;
using Sipos.Resume.Core.Languages;

namespace Sipos.Resume.Core.Tests.Artifacts;

public class DownloadCatalogFor
{
    [Fact]
    public void NamesEveryFormatAndProfileGivenLanguage()
    {
        var language = LanguageCatalog.Describe("sr-Latn", null, isDefault: false);

        DownloadCatalog.For(language, "Andor_Sipos_CV", ["architect", "tech-lead"]).Select(d => d.FileName).ShouldBe(
        [
            "Andor_Sipos_CV_SR.pdf", "Andor_Sipos_CV_SR.docx",
            "Andor_Sipos_CV_SR_ATS.pdf", "Andor_Sipos_CV_SR_ATS.docx", "Andor_Sipos_CV_SR_ATS.txt",
            "Andor_Sipos_CV_SR.md", "Andor_Sipos_CV_SR.json",
            "Andor_Sipos_CV_SR_Architect.pdf", "Andor_Sipos_CV_SR_Architect.docx",
            "Andor_Sipos_CV_SR_TechLead.pdf", "Andor_Sipos_CV_SR_TechLead.docx",
        ]);
    }

    // Portals and mail clients mangle anything else.
    [Fact]
    public void UsesAsciiNamesUnderDownloads()
    {
        var downloads = DownloadCatalog.For(LanguageCatalog.Describe("hu", null, false), "Andor_Sipos_CV", ["ai"]);

        downloads.ShouldAllBe(d => d.FileName.All(c => char.IsAscii(c) && c != ' ') && d.Path == "/downloads/" + d.FileName);
        downloads.Single(d => d.Format == DocumentFormat.Docx && d.FocusId is null && d.Variant == DocumentVariant.Ats).MediaType
            .ShouldBe("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    }
}
