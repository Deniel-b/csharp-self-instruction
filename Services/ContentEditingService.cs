using System.IO;
using System.Text;
using Newtonsoft.Json;
using ChapterModel = kursachFile.Chapter;
using CourseContentModel = kursachFile.CourseContent;
using LearningTaskModel = kursachFile.LearningTask;
using PageModel = kursachFile.Page;
using PageContentModel = kursachFile.PageContent;
using PageResourceModel = kursachFile.PageResource;
using SectionModel = kursachFile.Section;
using TaskScoringModel = kursachFile.TaskScoring;
using TaskOptionModel = kursachFile.TaskOption;
using TaskTestCaseModel = kursachFile.TaskTestCase;

namespace kursach.Services;

public sealed class ContentEditingService
{
    public string GenerateId(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}".Substring(0, Math.Min(24, prefix.Length + 1 + 12));
    }

    public void NormalizeOrdering(CourseContentModel course)
    {
        var chapterOrder = 1;
        foreach (var chapter in course.Chapters)
        {
            chapter.Order = chapterOrder++;
            var sectionOrder = 1;
            foreach (var section in chapter.Sections)
            {
                section.Order = sectionOrder++;
                var pageOrder = 1;
                foreach (var page in section.Pages)
                {
                    page.Order = pageOrder++;
                }
            }
        }
    }

    public bool MovePage(SectionModel section, PageModel page, int direction, out int newIndex)
    {
        newIndex = -1;
        var index = section.Pages.IndexOf(page);
        if (index < 0)
        {
            return false;
        }

        var targetIndex = index + direction;
        if (targetIndex < 0 || targetIndex >= section.Pages.Count)
        {
            return false;
        }

        (section.Pages[targetIndex], section.Pages[index]) = (section.Pages[index], section.Pages[targetIndex]);
        newIndex = targetIndex;
        return true;
    }

    public bool MovePageToIndex(SectionModel section, PageModel page, int requestedIndex, out int actualIndex)
    {
        actualIndex = -1;
        var oldIndex = section.Pages.IndexOf(page);
        if (oldIndex < 0)
        {
            return false;
        }

        var newIndex = Math.Clamp(requestedIndex, 0, section.Pages.Count - 1);
        if (oldIndex == newIndex)
        {
            return false;
        }

        section.Pages.RemoveAt(oldIndex);
        if (oldIndex < newIndex)
        {
            newIndex--;
        }

        section.Pages.Insert(newIndex, page);
        actualIndex = newIndex;
        return true;
    }

    public bool MoveTask(IList<LearningTaskModel> tasks, LearningTaskModel task, int direction, out int newIndex)
    {
        newIndex = -1;
        var index = tasks.IndexOf(task);
        if (index < 0)
        {
            return false;
        }

        var targetIndex = index + direction;
        if (targetIndex < 0 || targetIndex >= tasks.Count)
        {
            return false;
        }

        (tasks[targetIndex], tasks[index]) = (tasks[index], tasks[targetIndex]);
        newIndex = targetIndex;
        return true;
    }

    public bool MoveTaskToIndex(IList<LearningTaskModel> tasks, LearningTaskModel task, int requestedIndex, out int actualIndex)
    {
        actualIndex = -1;
        var oldIndex = tasks.IndexOf(task);
        if (oldIndex < 0)
        {
            return false;
        }

        var newIndex = Math.Clamp(requestedIndex, 0, tasks.Count - 1);
        if (oldIndex == newIndex)
        {
            return false;
        }

        tasks.RemoveAt(oldIndex);
        if (oldIndex < newIndex)
        {
            newIndex--;
        }

        tasks.Insert(newIndex, task);
        actualIndex = newIndex;
        return true;
    }

    public bool RemoveChapter(CourseContentModel course, ChapterModel chapter)
    {
        return course.Chapters.Remove(chapter);
    }

    public void UpdateChapter(ChapterModel chapter, string title)
    {
        chapter.Title = title;
    }

    public void AddChapter(CourseContentModel course, ChapterModel chapter)
    {
        course.Chapters.Add(chapter);
    }

    public bool RemoveSection(ChapterModel chapter, SectionModel section)
    {
        return chapter.Sections.Remove(section);
    }

    public void UpdateSection(SectionModel section, string title)
    {
        section.Title = title;
    }

    public void AddSection(ChapterModel chapter, SectionModel section)
    {
        chapter.Sections.Add(section);
    }

    public bool RemovePage(SectionModel section, PageModel page)
    {
        return section.Pages.Remove(page);
    }

    public void UpdatePage(PageModel page, string title, string? kind, int? estimatedTimeMinutes, string? contentFormat, string? contentSource)
    {
        page.Title = title;
        page.Kind = string.Equals(kind, "reading", StringComparison.OrdinalIgnoreCase)
            ? null
            : kind;
        page.EstimatedTimeMinutes = estimatedTimeMinutes;

        var normalizedSource = contentSource?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(contentFormat) && string.IsNullOrWhiteSpace(normalizedSource))
        {
            page.Content = null;
            return;
        }

        page.Content ??= new PageContentModel();
        page.Content.Format = string.IsNullOrWhiteSpace(contentFormat) ? "richText" : contentFormat;
        page.Content.Source = normalizedSource;
    }

    public void AddPage(SectionModel section, PageModel page)
    {
        section.Pages.Add(page);
    }

    public bool RemoveTask(IList<LearningTaskModel> tasks, LearningTaskModel task)
    {
        return tasks.Remove(task);
    }

    public void UpdateTaskCore(
        LearningTaskModel task,
        string title,
        int? points,
        bool partial,
        IReadOnlyList<string> hints,
        string type)
    {
        task.Title = title;
        task.Scoring ??= new TaskScoringModel();
        if (points.HasValue)
        {
            task.Scoring.Points = points.Value;
        }

        task.Scoring.Partial = partial;
        task.Hints = hints.ToList();
        task.Type = string.IsNullOrWhiteSpace(type) ? "quiz" : type;
    }

    public void AddTask(ICollection<LearningTaskModel> tasks, LearningTaskModel task)
    {
        tasks.Add(task);
    }

    public bool RemoveOption(IList<TaskOptionModel> options, TaskOptionModel option)
    {
        return options.Remove(option);
    }

    public void UpdateOption(TaskOptionModel option, string text, bool isCorrect, string feedback)
    {
        option.Text = text;
        option.IsCorrect = isCorrect;
        option.Feedback = feedback;
    }

    public void AddOption(ICollection<TaskOptionModel> options, TaskOptionModel option)
    {
        options.Add(option);
    }

    public bool RemoveTest(IList<TaskTestCaseModel> tests, TaskTestCaseModel test)
    {
        return tests.Remove(test);
    }

    public void UpdateTest(TaskTestCaseModel test, string id, string visibility, string input, string expectedOutput, string explanation)
    {
        test.Id = id;
        test.Visibility = visibility;
        test.Input = input;
        test.ExpectedOutput = expectedOutput;
        test.Explanation = explanation;
    }

    public void AddTest(ICollection<TaskTestCaseModel> tests, TaskTestCaseModel test)
    {
        tests.Add(test);
    }

    public bool RemoveResource(IList<PageResourceModel> resources, PageResourceModel resource)
    {
        return resources.Remove(resource);
    }

    public void UpdateResource(PageResourceModel resource, string type, string title, string url)
    {
        resource.Type = type;
        resource.Title = title;
        resource.Url = url;
    }

    public void AddResource(ICollection<PageResourceModel> resources, PageResourceModel resource)
    {
        resources.Add(resource);
    }

    public string CreateSnapshot(CourseContentModel course, JsonSerializerSettings jsonSettings)
    {
        return JsonConvert.SerializeObject(course, Formatting.None, jsonSettings);
    }

    public CourseContentModel? RestoreSnapshot(string snapshot)
    {
        return JsonConvert.DeserializeObject<CourseContentModel>(snapshot);
    }

    public bool TryWriteContentFile(string contentPath, string json, out string errorMessage)
    {
        var directory = Path.GetDirectoryName(contentPath);
        var tempPath = contentPath + ".tmp";
        var backupPath = contentPath + ".bak";

        try
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(tempPath, json, Encoding.UTF8);

            if (File.Exists(contentPath))
            {
                File.Replace(tempPath, contentPath, backupPath, true);
            }
            else
            {
                File.Move(tempPath, contentPath);
            }

            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Не удалось сохранить файл контента.");
            errorMessage = ex.Message;
            return false;
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // ignore cleanup errors
            }
        }
    }
}
