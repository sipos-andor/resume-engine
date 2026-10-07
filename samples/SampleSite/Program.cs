// Builds a CV site: dotnet publish, then run the published program with --content and --output.
using QuestPDF.Infrastructure;
using Sipos.Resume.Documents.Pdf;
using Sipos.Resume.Generation;
using Sipos.Resume.Theme.Operandor;

// The sample is an open-source example, which QuestPDF's Community licence covers.
var pdf = new PdfWriterOptions(LicenseType.Community);
return await ResumeGenerator.Create(args)
    .UseTheme(new OperandorTheme())
    .UseWriter(new PdfDocumentWriter(pdf))
    .UseShareImages(new PdfShareImageWriter(pdf))
    .RunAsync();
