using System.IO;
using Newtonsoft.Json;
using kursachFile;

namespace kursach.Services;

public enum ContentLoadFailureKind
{
    None,
    InvalidPath,
    NotFound,
    InvalidJson,
    Unexpected
}

public sealed record ContentLoadResult(CourseContent? Course, ContentLoadFailureKind FailureKind, string? Message)
{
    public bool Success => Course is not null && FailureKind == ContentLoadFailureKind.None;

    public static ContentLoadResult SuccessResult(CourseContent course) =>
        new(course, ContentLoadFailureKind.None, null);

    public static ContentLoadResult Fail(ContentLoadFailureKind kind, string message) =>
        new(null, kind, message);
}

public sealed class ContentLoader
{
    public ContentLoadResult Load(string contentPath)
    {
        if (string.IsNullOrWhiteSpace(contentPath))
        {
            AppLogger.Warn("Путь к контенту пустой.");
            return ContentLoadResult.Fail(ContentLoadFailureKind.InvalidPath, "Путь к контенту пустой.");
        }

        if (!File.Exists(contentPath))
        {
            AppLogger.Error($"Файл описания контента не найден: {contentPath}");
            return ContentLoadResult.Fail(ContentLoadFailureKind.NotFound,
                $"Файл описания контента не найден: {contentPath}");
        }

        try
        {
            var json = File.ReadAllText(contentPath);
            var course = JsonConvert.DeserializeObject<CourseContent>(json);

            if (course is null)
            {
                AppLogger.Error("Не удалось разобрать файл контента.");
                return ContentLoadResult.Fail(ContentLoadFailureKind.InvalidJson,
                    "Не удалось разобрать файл контента.");
            }

            return ContentLoadResult.SuccessResult(course);
        }
        catch (JsonException ex)
        {
            AppLogger.Error(ex, "Файл контента поврежден.");
            return ContentLoadResult.Fail(ContentLoadFailureKind.InvalidJson,
                $"Файл контента поврежден: {ex.Message}");
        }
        catch (Exception ex)
        {
            AppLogger.Error(ex, "Не удалось загрузить контент.");
            return ContentLoadResult.Fail(ContentLoadFailureKind.Unexpected,
                $"Не удалось загрузить контент: {ex.Message}");
        }
    }
}
