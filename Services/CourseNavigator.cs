using ChapterModel = kursachFile.Chapter;
using CourseContentModel = kursachFile.CourseContent;
using PageModel = kursachFile.Page;
using SectionModel = kursachFile.Section;

namespace kursach.Services;

public enum NavigationFailureKind
{
    None,
    NotFoundChapter,
    NotFoundSection,
    EmptyChapter,
    EmptySection,
    NoChapterSelected,
    NoSectionSelected,
    OutOfRange
}

public sealed record NavigationResult(bool Success, NavigationFailureKind FailureKind, string? Message)
{
    public static NavigationResult Ok() => new(true, NavigationFailureKind.None, null);

    public static NavigationResult Fail(NavigationFailureKind kind, string message) =>
        new(false, kind, message);
}

public sealed class CourseNavigator
{
    public CourseNavigator(CourseContentModel course)
    {
        Course = course ?? throw new ArgumentNullException(nameof(course));
    }

    public CourseContentModel Course { get; }
    public ChapterModel? CurrentChapter { get; private set; }
    public SectionModel? CurrentSection { get; private set; }
    public PageModel? CurrentPage { get; private set; }
    public int CurrentSectionIndex { get; private set; } = -1;
    public int CurrentPageIndex { get; private set; } = -1;

    public IReadOnlyList<SectionModel> CurrentChapterSections =>
        CurrentChapter?.OrderedSections ?? Array.Empty<SectionModel>();

    public IReadOnlyList<PageModel> CurrentSectionPages =>
        CurrentSection?.OrderedPages ?? Array.Empty<PageModel>();

    public bool CanMoveNext => HasNextPage();
    public bool CanMovePrevious => HasPreviousPage();

    public NavigationResult NavigateTo(string chapterId, string sectionId, int pageIndex)
    {
        if (!Course.TryGetChapter(chapterId, out var chapter) || chapter is null)
        {
            return NavigationResult.Fail(NavigationFailureKind.NotFoundChapter,
                $"Chapter not found: {chapterId}");
        }

        CurrentChapter = chapter;
        var sections = chapter.OrderedSections;

        if (sections.Count == 0)
        {
            CurrentSectionIndex = -1;
            CurrentSection = null;
            CurrentPageIndex = -1;
            CurrentPage = null;
            return NavigationResult.Fail(NavigationFailureKind.EmptyChapter,
                $"Chapter \"{chapter.Title}\" has no sections to display.");
        }

        if (!chapter.TryGetSection(sectionId, out var section) || section is null)
        {
            CurrentSectionIndex = -1;
            CurrentSection = null;
            CurrentPageIndex = -1;
            CurrentPage = null;
            return NavigationResult.Fail(NavigationFailureKind.NotFoundSection,
                $"Section not found: {sectionId}");
        }

        CurrentSection = section;
        CurrentSectionIndex = FindSectionIndex(sections, section.Id);
        var pages = section.OrderedPages;

        if (pages.Count == 0)
        {
            CurrentPageIndex = -1;
            CurrentPage = null;
            return NavigationResult.Fail(NavigationFailureKind.EmptySection,
                $"Section \"{section.Title}\" has no pages to display.");
        }

        CurrentPageIndex = Math.Clamp(pageIndex, 0, pages.Count - 1);
        CurrentPage = pages[CurrentPageIndex];
        return NavigationResult.Ok();
    }

    public NavigationResult MoveNext()
    {
        if (CurrentChapter is null)
        {
            return NavigationResult.Fail(NavigationFailureKind.NoChapterSelected,
                "No chapter selected.");
        }

        if (CurrentSection is null)
        {
            return NavigationResult.Fail(NavigationFailureKind.NoSectionSelected,
                "No section selected.");
        }

        var pages = CurrentSectionPages;
        if (pages.Count == 0)
        {
            return NavigationResult.Fail(NavigationFailureKind.EmptySection,
                $"Section \"{CurrentSection.Title}\" has no pages to display.");
        }

        if (CurrentPageIndex < pages.Count - 1)
        {
            CurrentPageIndex++;
            CurrentPage = pages[CurrentPageIndex];
            return NavigationResult.Ok();
        }

        var sections = CurrentChapterSections;
        for (var index = CurrentSectionIndex + 1; index < sections.Count; index++)
        {
            var nextSection = sections[index];
            if (nextSection.OrderedPages.Count == 0)
            {
                continue;
            }

            CurrentSection = nextSection;
            CurrentSectionIndex = index;
            CurrentPageIndex = 0;
            CurrentPage = nextSection.OrderedPages[0];
            return NavigationResult.Ok();
        }

        return NavigationResult.Fail(NavigationFailureKind.OutOfRange,
            "Already at the last page.");
    }

    public NavigationResult MovePrevious()
    {
        if (CurrentChapter is null)
        {
            return NavigationResult.Fail(NavigationFailureKind.NoChapterSelected,
                "No chapter selected.");
        }

        if (CurrentSection is null)
        {
            return NavigationResult.Fail(NavigationFailureKind.NoSectionSelected,
                "No section selected.");
        }

        var pages = CurrentSectionPages;
        if (pages.Count == 0)
        {
            return NavigationResult.Fail(NavigationFailureKind.EmptySection,
                $"Section \"{CurrentSection.Title}\" has no pages to display.");
        }

        if (CurrentPageIndex > 0)
        {
            CurrentPageIndex--;
            CurrentPage = pages[CurrentPageIndex];
            return NavigationResult.Ok();
        }

        var sections = CurrentChapterSections;
        for (var index = CurrentSectionIndex - 1; index >= 0; index--)
        {
            var previousSection = sections[index];
            var previousPages = previousSection.OrderedPages;
            if (previousPages.Count == 0)
            {
                continue;
            }

            CurrentSection = previousSection;
            CurrentSectionIndex = index;
            CurrentPageIndex = previousPages.Count - 1;
            CurrentPage = previousPages[CurrentPageIndex];
            return NavigationResult.Ok();
        }

        return NavigationResult.Fail(NavigationFailureKind.OutOfRange,
            "Already at the first page.");
    }

    private static int FindSectionIndex(IReadOnlyList<SectionModel> sections, string sectionId)
    {
        for (var index = 0; index < sections.Count; index++)
        {
            if (string.Equals(sections[index].Id, sectionId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private bool HasNextPage()
    {
        if (CurrentChapter is null || CurrentSection is null)
        {
            return false;
        }

        var pages = CurrentSectionPages;
        if (pages.Count == 0)
        {
            return false;
        }

        if (CurrentPageIndex < pages.Count - 1)
        {
            return true;
        }

        var sections = CurrentChapterSections;
        for (var index = CurrentSectionIndex + 1; index < sections.Count; index++)
        {
            if (sections[index].OrderedPages.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasPreviousPage()
    {
        if (CurrentChapter is null || CurrentSection is null)
        {
            return false;
        }

        var pages = CurrentSectionPages;
        if (pages.Count == 0)
        {
            return false;
        }

        if (CurrentPageIndex > 0)
        {
            return true;
        }

        var sections = CurrentChapterSections;
        for (var index = CurrentSectionIndex - 1; index >= 0; index--)
        {
            if (sections[index].OrderedPages.Count > 0)
            {
                return true;
            }
        }

        return false;
    }
}
