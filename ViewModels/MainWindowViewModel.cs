using System.IO;
using System.Linq;
using kursach.Services;
using ChapterModel = kursachFile.Chapter;
using CourseContentModel = kursachFile.CourseContent;
using PageModel = kursachFile.Page;
using SectionModel = kursachFile.Section;

namespace kursach.ViewModels;

public sealed record CourseLoadState(
    bool Success,
    string? ErrorMessage,
    IList<ContentIssue> Issues,
    int ErrorCount,
    int WarningCount);

public sealed class MainWindowViewModel
{
    private readonly ContentLoader _contentLoader;
    private readonly ContentValidator _contentValidator;

    public MainWindowViewModel(ContentLoader? contentLoader = null, ContentValidator? contentValidator = null)
    {
        _contentLoader = contentLoader ?? new ContentLoader();
        _contentValidator = contentValidator ?? new ContentValidator();
    }

    public CourseContentModel? Course { get; private set; }
    public IList<ChapterModel> Chapters { get; private set; } = new ChapterModel[0];
    public CourseNavigator? Navigator { get; private set; }
    public string? CourseTitle => Course?.Title;

    public IList<SectionModel> CurrentSections =>
        Navigator?.CurrentChapterSections ?? new SectionModel[0];

    public IList<PageModel> CurrentPages =>
        Navigator?.CurrentSectionPages ?? new PageModel[0];

    public int CurrentPageIndex => Navigator?.CurrentPageIndex ?? -1;
    public ChapterModel? CurrentChapter => Navigator?.CurrentChapter;
    public SectionModel? CurrentSection => Navigator?.CurrentSection;
    public PageModel? CurrentPage => Navigator?.CurrentPage;

    public CourseLoadState LoadCourse(string contentPath, string assetsRoot)
    {
        var loadResult = _contentLoader.Load(contentPath);
        if (!loadResult.Success)
        {
            return new CourseLoadState(false, loadResult.Message, new ContentIssue[0], 0, 0);
        }

        var course = loadResult.Course!;
        var issues = _contentValidator.Validate(course, Path.GetFullPath(assetsRoot));
        var errorCount = issues.Count(issue => issue.Severity == ContentIssueSeverity.Error);
        var warningCount = issues.Count - errorCount;

        if (errorCount > 0)
        {
            return new CourseLoadState(false, $"Контент содержит ошибки: {errorCount}. Подробности см. в logs/app.log.", issues, errorCount, warningCount);
        }

        SetCourse(course);
        AppLogger.Info($"Контент загружен из {Path.GetFullPath(contentPath)}");

        if (Chapters.Count == 0)
        {
            return new CourseLoadState(false, "В контенте не найдено ни одной главы.", issues, errorCount, warningCount);
        }

        return new CourseLoadState(true, null, issues, errorCount, warningCount);
    }

    public void SetCourse(CourseContentModel course)
    {
        Course = course;
        Navigator = new CourseNavigator(course);
        Chapters = course.OrderedChapters;
    }

    public NavigationResult NavigateToPage(string chapterId, string sectionId, int pageIndex)
    {
        if (Navigator is null)
        {
            return NavigationResult.Fail(NavigationFailureKind.NoChapterSelected, "Курс не загружен.");
        }

        return Navigator.NavigateTo(chapterId, sectionId, pageIndex);
    }

    public NavigationResult MoveNext()
    {
        if (Navigator is null)
        {
            return NavigationResult.Fail(NavigationFailureKind.NoChapterSelected, "Курс не загружен.");
        }

        return Navigator.MoveNext();
    }

    public NavigationResult MovePrevious()
    {
        if (Navigator is null)
        {
            return NavigationResult.Fail(NavigationFailureKind.NoChapterSelected, "Курс не загружен.");
        }

        return Navigator.MovePrevious();
    }

    public bool NavigateToFirstPage()
    {
        foreach (var chapter in Chapters)
        {
            foreach (var section in chapter.OrderedSections)
            {
                if (section.OrderedPages.Count == 0)
                {
                    continue;
                }

                var result = NavigateToPage(chapter.Id, section.Id, 0);
                return result.Success;
            }
        }

        return false;
    }

    public bool TryNavigateTo(string? chapterId, string? sectionId, int pageIndex)
    {
        if (string.IsNullOrWhiteSpace(chapterId) || string.IsNullOrWhiteSpace(sectionId))
        {
            return false;
        }

        var chapter = Chapters.FirstOrDefault(item =>
            string.Equals(item.Id, chapterId, StringComparison.OrdinalIgnoreCase));
        if (chapter is null)
        {
            return false;
        }

        var section = chapter.Sections.FirstOrDefault(item =>
            string.Equals(item.Id, sectionId, StringComparison.OrdinalIgnoreCase));
        if (section is null || section.OrderedPages.Count == 0)
        {
            return false;
        }

        var index = Compatibility.Clamp(pageIndex, 0, section.OrderedPages.Count - 1);
        return NavigateToPage(chapter.Id, section.Id, index).Success;
    }
}


