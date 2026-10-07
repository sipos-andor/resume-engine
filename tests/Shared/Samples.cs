using System.Text;
using Sipos.Resume.Core.Content;

namespace Sipos.Resume.Testing;

/// <summary>Small content files for tests: a valid English CV and its Hungarian version.</summary>
internal static class Samples
{
    public const string English = """
        {
          "$schema": "https://raw.githubusercontent.com/jsonresume/resume-schema/v1.0.0/schema.json",
          "basics": {
            "name": "Ann Example",
            "label": "Software Architect",
            "phone": "+36 30 123 4567",
            "url": "https://cv.example.com/",
            "summary": "Builds .NET systems.",
            "location": { "region": "Europe", "x-label": "Remote from Europe" },
            "profiles": [ { "network": "GitHub", "username": "ann", "url": "https://github.com/ann" } ],
            "x-givenName": "Ann",
            "x-familyName": "Example",
            "x-contact": { "url": "https://example.com/contact", "label": "Get in touch" },
            "x-availability": { "status": "available", "url": "https://example.com/capacity", "label": "Available now" }
          },
          "work": [
            { "x-id": "acme", "name": "acme", "position": "Founder", "startDate": "2022-11", "highlights": ["Leads", "Builds"], "x-short": true, "x-focus": ["architect"] },
            { "x-id": "initech", "name": "Initech", "position": "Engineer", "startDate": "2015-03", "endDate": "2022-10", "highlights": ["Shipped"] }
          ],
          "projects": [
            { "x-id": "portal", "x-work": "acme", "name": "Portal", "entity": "Globex", "roles": ["Lead"], "startDate": "2023-03", "endDate": "2023-05", "keywords": ["C#", "Azure"], "highlights": ["Built"] },
            { "x-id": "hobby", "name": "Hobby", "keywords": ["Rust"] }
          ],
          "education": [ { "institution": "Polytechnic", "area": "Informatics", "studyType": "BSc", "endDate": "2012" } ],
          "certificates": [ { "name": "Programming in C#", "date": "2014" } ],
          "awards": [ { "title": "Hackathon, 2nd place", "date": "2016" } ],
          "skills": [
            { "name": ".NET platform", "level": "Expert", "x-rating": 5, "keywords": ["C#", "LINQ"] },
            { "name": ".NET platform", "level": "Proficient", "x-rating": 3, "keywords": ["Blazor"] },
            { "name": "Cloud", "level": "Advanced", "x-rating": 4, "keywords": ["Azure"] }
          ],
          "languages": [ { "language": "Hungarian", "fluency": "Native" } ],
          "meta": { "lastModified": "2026-10-07", "x-ogLocale": "en_GB" },
          "x-strengths": [ { "id": "delivery", "title": "Full-stack delivery", "summary": "End to end.", "short": true } ],
          "x-focusProfiles": [ { "id": "architect", "label": "Software architect", "skills": ["Azure"] } ],
          "x-aliases": { "C#": ["csharp"] }
        }
        """;

    public static readonly string Hungarian = English
        .Replace("\"name\": \"Ann Example\"", "\"name\": \"Example Ann\"", StringComparison.Ordinal)
        .Replace("Software Architect", "Szoftverarchitekt", StringComparison.Ordinal)
        .Replace("Remote from Europe", "Távmunkában, Európából", StringComparison.Ordinal)
        .Replace("\"x-ogLocale\": \"en_GB\"", "\"x-endonym\": \"Magyar\"", StringComparison.Ordinal);

    /// <summary>The English CV with Hungarian and Croatian letters in the name and the location: í ő ű č ć š ž đ.</summary>
    public static readonly string Accented = English
        .Replace("\"name\": \"Ann Example\"", "\"name\": \"Ann Sípos\"", StringComparison.Ordinal)
        .Replace("Remote from Europe", "Győr, Hűvösvölgy; Čačak, Pašman, Ivanić-Grad, Đurđevac, Požega", StringComparison.Ordinal);

    public static JsonResume Read(string json) =>
        ResumeReader.Read("resume.en.json", Encoding.UTF8.GetBytes(json)).Resume ?? throw new InvalidOperationException("The sample is not valid.");
}
