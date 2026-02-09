using ChapterModel = kursachFile.Chapter;
using CourseContentModel = kursachFile.CourseContent;
using PageModel = kursachFile.Page;

namespace kursach.Services;

public enum NavigationFailureKind
{
    None,
    NotFound,
    EmptyChapter,
    NoChapterSelected,
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
    public PageModel? CurrentPage { get; private set; }
    public int CurrentPageIndex { get; private set; } = -1;

    public IReadOnlyList<PageModel> CurrentChapterPages =>
        CurrentChapter?.OrderedPages ?? Array.Empty<PageModel>();

    public NavigationResult NavigateTo(string chapterId, int pageIndex)
    {
        if (!Course.TryGetChapter(chapterId, out var chapter) || chapter is null)
        {
            return NavigationResult.Fail(NavigationFailureKind.NotFound,
                $"Chapter not found: {chapterId}");
        }

        CurrentChapter = chapter;
        var pages = chapter.OrderedPages;

        if (pages.Count == 0)
        {
            CurrentPageIndex = -1;
            CurrentPage = null;
            return NavigationResult.Fail(NavigationFailureKind.EmptyChapter,
                $"Chapter \"{chapter.Title}\" has no pages to display.");
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

        var pages = CurrentChapterPages;
        if (pages.Count == 0)
        {
            return NavigationResult.Fail(NavigationFailureKind.EmptyChapter,
                $"Chapter \"{CurrentChapter.Title}\" has no pages to display.");
        }

        if (CurrentPageIndex >= pages.Count - 1)
        {
            return NavigationResult.Fail(NavigationFailureKind.OutOfRange,
                "Already at the last page.");
        }

        CurrentPageIndex++;
        CurrentPage = pages[CurrentPageIndex];
        return NavigationResult.Ok();
    }

    public NavigationResult MovePrevious()
    {
        if (CurrentChapter is null)
        {
            return NavigationResult.Fail(NavigationFailureKind.NoChapterSelected,
                "No chapter selected.");
        }

        var pages = CurrentChapterPages;
        if (pages.Count == 0)
        {
            return NavigationResult.Fail(NavigationFailureKind.EmptyChapter,
                $"Chapter \"{CurrentChapter.Title}\" has no pages to display.");
        }

        if (CurrentPageIndex <= 0)
        {
            return NavigationResult.Fail(NavigationFailureKind.OutOfRange,
                "Already at the first page.");
        }

        CurrentPageIndex--;
        CurrentPage = pages[CurrentPageIndex];
        return NavigationResult.Ok();
    }
}
