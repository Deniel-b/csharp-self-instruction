using System.IO;
using kursachFile;

namespace kursach.Services;

public enum ContentIssueSeverity
{
    Warning,
    Error
}

public sealed record ContentIssue(ContentIssueSeverity Severity, string Message);

public sealed class ContentValidator
{
    public IReadOnlyList<ContentIssue> Validate(CourseContent course, string assetsRoot)
    {
        var issues = new List<ContentIssue>();

        if (course.Chapters.Count == 0)
        {
            issues.Add(new ContentIssue(ContentIssueSeverity.Error, "Курс не содержит глав."));
            return issues;
        }

        var chapterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var chapter in course.Chapters)
        {
            if (string.IsNullOrWhiteSpace(chapter.Id))
            {
                issues.Add(new ContentIssue(ContentIssueSeverity.Error,
                    $"Глава \"{chapter.Title}\" не имеет идентификатора."));
            }
            else if (!chapterIds.Add(chapter.Id))
            {
                issues.Add(new ContentIssue(ContentIssueSeverity.Error,
                    $"Дублирующийся id главы: {chapter.Id}"));
            }

            if (chapter.Sections.Count == 0)
            {
                issues.Add(new ContentIssue(ContentIssueSeverity.Warning,
                    $"В главе \"{chapter.Title}\" отсутствуют разделы."));
            }

            var sectionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var section in chapter.Sections)
            {
                if (string.IsNullOrWhiteSpace(section.Id))
                {
                    issues.Add(new ContentIssue(ContentIssueSeverity.Error,
                        $"Раздел \"{section.Title}\" в главе \"{chapter.Title}\" не имеет идентификатора."));
                }
                else if (!sectionIds.Add(section.Id))
                {
                    issues.Add(new ContentIssue(ContentIssueSeverity.Error,
                        $"Дублирующийся id раздела в главе \"{chapter.Title}\": {section.Id}"));
                }

                if (section.Pages.Count == 0)
                {
                    issues.Add(new ContentIssue(ContentIssueSeverity.Warning,
                        $"В разделе \"{section.Title}\" главы \"{chapter.Title}\" нет страниц."));
                }

                var pageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var page in section.Pages)
                {
                    if (string.IsNullOrWhiteSpace(page.Id))
                    {
                        issues.Add(new ContentIssue(ContentIssueSeverity.Error,
                            $"Страница \"{page.Title}\" в разделе \"{section.Title}\" не имеет идентификатора."));
                    }
                    else if (!pageIds.Add(page.Id))
                    {
                        issues.Add(new ContentIssue(ContentIssueSeverity.Error,
                            $"Дублирующийся id страницы в разделе \"{section.Title}\": {page.Id}"));
                    }

                    if (page.Content is not null)
                    {
                        if (string.Equals(page.Content.Format, "richText", StringComparison.OrdinalIgnoreCase)
                            && string.IsNullOrWhiteSpace(page.Content.Source))
                        {
                            issues.Add(new ContentIssue(ContentIssueSeverity.Warning,
                                $"Страница \"{page.Title}\" ожидает richText, но путь к контенту пуст."));
                        }

                        if (!string.IsNullOrWhiteSpace(page.Content.Source))
                        {
                            var relativePath = page.Content.Source.Replace('/', Path.DirectorySeparatorChar);
                            var filePath = Path.Combine(assetsRoot, relativePath);
                            if (!File.Exists(filePath))
                            {
                                issues.Add(new ContentIssue(ContentIssueSeverity.Warning,
                                    $"Страница \"{page.Title}\" ссылается на отсутствующий файл: {filePath}"));
                            }
                        }
                    }
                }
            }
        }

        return issues;
    }
}
