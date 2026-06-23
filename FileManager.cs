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
    public IList<Chapter> OrderedChapters =>
        Chapters
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

        chapter = Chapters.FirstOrDefault(ch =>
            string.Equals(ch.Id, chapterId, StringComparison.OrdinalIgnoreCase));
        return chapter is not null;
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

    [JsonProperty("sections")]
    public List<Section> Sections { get; set; } = new();

    [JsonIgnore]
    public IList<Section> OrderedSections =>
        Sections
            .OrderBy(section => section.Order)
            .ThenBy(section => section.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public bool TryGetSection(string? sectionId, out Section? section)
    {
        section = null;

        if (string.IsNullOrWhiteSpace(sectionId) || Sections.Count == 0)
        {
            return false;
        }

        section = Sections.FirstOrDefault(item =>
            string.Equals(item.Id, sectionId, StringComparison.OrdinalIgnoreCase));
        return section is not null;
    }
}

public sealed class Section
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
    public IList<Page> OrderedPages =>
        Pages
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

        page = Pages.FirstOrDefault(item =>
            string.Equals(item.Id, pageId, StringComparison.OrdinalIgnoreCase));
        return page is not null;
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
    public string? Kind { get; set; }

    [JsonProperty("estimatedTimeMinutes")]
    public int? EstimatedTimeMinutes { get; set; }

    [JsonProperty("content")]
    public PageContent? Content { get; set; }

    [JsonProperty("tasks")]
    public List<LearningTask>? Tasks { get; set; }

    [JsonProperty("resources")]
    public List<PageResource>? Resources { get; set; }

    [JsonIgnore]
    public IList<LearningTask> TasksOrEmpty =>
        Tasks ?? (IList<LearningTask>)new LearningTask[0];

    [JsonIgnore]
    public IList<PageResource> ResourcesOrEmpty =>
        Resources ?? (IList<PageResource>)new PageResource[0];

    [JsonIgnore]
    public bool HasContent => Content is not null && !string.IsNullOrWhiteSpace(Content.Source);

    [JsonIgnore]
    public bool HasTasks => Tasks is { Count: > 0 };

    [JsonIgnore]
    public string KindNormalized
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Kind))
            {
                return Kind!.Trim().ToLowerInvariant();
            }

            var inferred = InferKindFromTasks();
            return inferred ?? "reading";
        }
    }

    [JsonIgnore]
    public bool IsTaskPage =>
        HasTasks || KindNormalized is "quiz" or "code" or "assessment" or "tasks";

    private string? InferKindFromTasks()
    {
        if (Tasks is null || Tasks.Count == 0)
        {
            return null;
        }

        var distinctTypes = Tasks
            .Select(task => task.Type)
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Select(type => type.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctTypes.Count == 1)
        {
            return distinctTypes[0];
        }

        return "tasks";
    }
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


