namespace Sipos.Resume.Core.Content;

/// <summary>A file of a CV's content: a <c>resume.{language}.json</c> or the site's <c>site.json</c>.</summary>
/// <param name="Name">The file name without folders, such as <c>resume.hu.json</c>.</param>
/// <param name="Content">The file's UTF-8 bytes.</param>
public sealed record ContentFile(string Name, ReadOnlyMemory<byte> Content);
