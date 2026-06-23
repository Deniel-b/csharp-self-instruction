using Newtonsoft.Json;
using kursach.Services;
using CourseContentModel = kursachFile.CourseContent;
using LearningTaskModel = kursachFile.LearningTask;
using PageModel = kursachFile.Page;
using PageResourceModel = kursachFile.PageResource;
using SectionModel = kursachFile.Section;
using ChapterModel = kursachFile.Chapter;
using TaskOptionModel = kursachFile.TaskOption;
using TaskTestCaseModel = kursachFile.TaskTestCase;

namespace kursach.ViewModels;

public sealed record AdminHistoryEntry(DateTime Timestamp, string Description, string Snapshot);

public sealed class AdminViewModel
{
    private readonly ContentEditingService _contentEditingService;
    private readonly List<AdminHistoryEntry> _history = new();

    public AdminViewModel(ContentEditingService contentEditingService)
    {
        _contentEditingService = contentEditingService;
    }

    public bool IsAdminMode { get; set; }
    public bool SuppressAdminEvents { get; set; }
    public bool SuppressHistory { get; set; }

    public ChapterModel? SelectedChapter { get; set; }
    public SectionModel? SelectedSection { get; set; }
    public PageModel? SelectedPage { get; set; }
    public LearningTaskModel? SelectedTask { get; set; }
    public TaskOptionModel? SelectedOption { get; set; }
    public TaskTestCaseModel? SelectedTest { get; set; }
    public PageResourceModel? SelectedResource { get; set; }

    public int HistoryIndex { get; private set; } = -1;
    public IList<AdminHistoryEntry> History => _history;
    public bool CanUndo => HistoryIndex > 0;
    public bool CanRedo => HistoryIndex >= 0 && HistoryIndex < _history.Count - 1;

    public void InitializeHistory(CourseContentModel? course, string description, JsonSerializerSettings jsonSettings)
    {
        _history.Clear();
        HistoryIndex = -1;
        RecordHistory(course, description, jsonSettings, force: true);
    }

    public void RecordHistory(CourseContentModel? course, string description, JsonSerializerSettings jsonSettings, bool force = false)
    {
        if (course is null)
        {
            return;
        }

        if (SuppressHistory && !force)
        {
            return;
        }

        var snapshot = _contentEditingService.CreateSnapshot(course, jsonSettings);
        if (HistoryIndex < _history.Count - 1)
        {
            _history.RemoveRange(HistoryIndex + 1, _history.Count - HistoryIndex - 1);
        }

        _history.Add(new AdminHistoryEntry(DateTime.Now, description, snapshot));
        HistoryIndex = _history.Count - 1;
    }

    public bool TryUndo(out AdminHistoryEntry? entry)
    {
        entry = null;
        if (!CanUndo)
        {
            return false;
        }

        HistoryIndex--;
        entry = _history[HistoryIndex];
        return true;
    }

    public bool TryRedo(out AdminHistoryEntry? entry)
    {
        entry = null;
        if (!CanRedo)
        {
            return false;
        }

        HistoryIndex++;
        entry = _history[HistoryIndex];
        return true;
    }
}

