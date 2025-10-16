using System.Linq;
using Newtonsoft.Json;

namespace kursachFile;

public sealed class CourseContent
{
    [JsonProperty("version")]
    public string Version { get; set; } = string.Empty;

    [JsonProperty("language")]
    public string Language { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("authors")]
    public List<Author> Authors { get; set; } = new();

    [JsonProperty("chapters")]
    public List<Chapter> Chapters { get; set; } = new();

    [JsonProperty("glossary")]
    public List<GlossaryEntry> Glossary { get; set; } = new();

    [JsonProperty("resources")]
    public List<ResourceLink> Resources { get; set; } = new();

    [JsonIgnore]
    private IReadOnlyList<Chapter>? _orderedChapters;

    [JsonIgnore]
    private Dictionary<string, Chapter>? _chaptersById;

    [JsonIgnore]
    public IReadOnlyList<Chapter> OrderedChapters =>
        _orderedChapters ??= Chapters
            .OrderBy(chapter => chapter.Order)
            .ThenBy(chapter => chapter.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public bool TryGetChapter(string? chapterId, out Chapter? chapter)
    {
        chapter = null;

        if (string.IsNullOrWhiteSpace(chapterId) || Chapters.Count == 0)
        {
            return false;
        }

        _chaptersById ??= Chapters.ToDictionary(
            keySelector: chapter => chapter.Id,
            elementSelector: chapter => chapter,
            comparer: StringComparer.OrdinalIgnoreCase);

        return _chaptersById.TryGetValue(chapterId, out chapter);
    }
}

public sealed class Author
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;
}

public sealed class Chapter
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("order")]
    public int Order { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("summary")]
    public string? Summary { get; set; }

    [JsonProperty("pages")]
    public List<Page> Pages { get; set; } = new();

    [JsonIgnore]
    private IReadOnlyList<Page>? _orderedPages;

    [JsonIgnore]
    private Dictionary<string, Page>? _pagesById;

    [JsonIgnore]
    public IReadOnlyList<Page> OrderedPages =>
        _orderedPages ??= Pages
            .OrderBy(page => page.Order)
            .ThenBy(page => page.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public bool TryGetPageByIndex(int index, out Page? page)
    {
        page = null;
        var pages = OrderedPages;

        if (index < 0 || index >= pages.Count)
        {
            return false;
        }

        page = pages[index];
        return true;
    }

    public bool TryGetPage(string? pageId, out Page? page)
    {
        page = null;

        if (string.IsNullOrWhiteSpace(pageId) || Pages.Count == 0)
        {
            return false;
        }

        _pagesById ??= Pages.ToDictionary(
            keySelector: page => page.Id,
            elementSelector: page => page,
            comparer: StringComparer.OrdinalIgnoreCase);

        return _pagesById.TryGetValue(pageId, out page);
    }
}

public sealed class Page
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("order")]
    public int Order { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("kind")]
    public string Kind { get; set; } = "reading";

    [JsonProperty("estimatedTimeMinutes")]
    public int? EstimatedTimeMinutes { get; set; }

    [JsonProperty("content")]
    public PageContent Content { get; set; } = new();

    [JsonProperty("tasks")]
    public List<LearningTask> Tasks { get; set; } = new();

    [JsonProperty("resources")]
    public List<PageResource> Resources { get; set; } = new();

    [JsonIgnore]
    public string KindNormalized => string.IsNullOrWhiteSpace(Kind)
        ? "reading"
        : Kind.Trim().ToLowerInvariant();

    [JsonIgnore]
    public bool IsTaskPage =>
        KindNormalized is "quiz" or "code" or "assessment" or "tasks";
}

public sealed class PageContent
{
    [JsonProperty("format")]
    public string Format { get; set; } = "richText";

    [JsonProperty("source")]
    public string Source { get; set; } = string.Empty;
}

public sealed class LearningTask
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("selection")]
    public string? Selection { get; set; }

    [JsonProperty("question")]
    public string? Question { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("prompt")]
    public string? Prompt { get; set; }

    [JsonProperty("starterCode")]
    public string? StarterCode { get; set; }

    [JsonProperty("entryPoint")]
    public CodeEntryPoint? EntryPoint { get; set; }

    [JsonProperty("constraints")]
    public CodeConstraints? Constraints { get; set; }

    [JsonProperty("scoring")]
    public TaskScoring Scoring { get; set; } = new();

    [JsonProperty("options")]
    public List<TaskOption> Options { get; set; } = new();

    [JsonProperty("solution")]
    public TaskSolution? Solution { get; set; }

    [JsonProperty("tests")]
    public List<TaskTestCase> Tests { get; set; } = new();

    [JsonProperty("hints")]
    public List<string> Hints { get; set; } = new();
}

public sealed class TaskOption
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    [JsonProperty("isCorrect")]
    public bool IsCorrect { get; set; }

    [JsonProperty("feedback")]
    public string? Feedback { get; set; }
}

public sealed class TaskScoring
{
    [JsonProperty("points")]
    public int Points { get; set; }

    [JsonProperty("partial")]
    public bool Partial { get; set; }
}

public sealed class TaskSolution
{
    [JsonProperty("correctOptionIds")]
    public List<string> CorrectOptionIds { get; set; } = new();

    [JsonProperty("explanation")]
    public string? Explanation { get; set; }
}

public sealed class CodeEntryPoint
{
    [JsonProperty("class")]
    public string ClassName { get; set; } = string.Empty;

    [JsonProperty("method")]
    public string MethodName { get; set; } = string.Empty;

    [JsonProperty("signature")]
    public string Signature { get; set; } = string.Empty;
}

public sealed class CodeConstraints
{
    [JsonProperty("timeLimitMs")]
    public int? TimeLimitMs { get; set; }

    [JsonProperty("memoryLimitMb")]
    public int? MemoryLimitMb { get; set; }

    [JsonProperty("disallow")]
    public List<string> Disallow { get; set; } = new();
}

public sealed class TaskTestCase
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("visibility")]
    public string Visibility { get; set; } = "hidden";

    [JsonProperty("input")]
    public string Input { get; set; } = string.Empty;

    [JsonProperty("expectedOutput")]
    public string ExpectedOutput { get; set; } = string.Empty;

    [JsonProperty("explanation")]
    public string? Explanation { get; set; }
}

public sealed class PageResource
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;
}

public sealed class GlossaryEntry
{
    [JsonProperty("term")]
    public string Term { get; set; } = string.Empty;

    [JsonProperty("definition")]
    public string Definition { get; set; } = string.Empty;
}

public sealed class ResourceLink
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;
}
