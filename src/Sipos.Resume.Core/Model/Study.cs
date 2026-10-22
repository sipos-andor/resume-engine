using Sipos.Resume.Core.Dates;

namespace Sipos.Resume.Core.Model;

/// <summary>A study.</summary>
/// <param name="Institution">The school.</param>
/// <param name="Area">The field of study, or <see langword="null"/>.</param>
/// <param name="StudyType">The degree, such as <c>BSc</c>, or <see langword="null"/>.</param>
/// <param name="Period">When, or <see langword="null"/>; a study with only an end date starts and ends on it.</param>
/// <param name="Url">The school's site, or <see langword="null"/>.</param>
/// <param name="Score">The grade as the CV writes it, such as <c>8.11/10</c>, or <see langword="null"/>.</param>
/// <param name="Courses">The courses worth naming, in the CV's order.</param>
public sealed record Study(string Institution, string? Area, string? StudyType, DateRange? Period, string? Url, string? Score, IReadOnlyList<string> Courses);
