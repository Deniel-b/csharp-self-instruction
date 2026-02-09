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
            return ContentLoadResult.Fail(ContentLoadFailureKind.InvalidPath, "Content path is empty.");
        }

        if (!File.Exists(contentPath))
        {
            return ContentLoadResult.Fail(ContentLoadFailureKind.NotFound,
                $"Content description file not found: {contentPath}");
        }

        try
        {
            var json = File.ReadAllText(contentPath);
            var course = JsonConvert.DeserializeObject<CourseContent>(json);

            if (course is null)
            {
                return ContentLoadResult.Fail(ContentLoadFailureKind.InvalidJson,
                    "Unable to parse content manifest.");
            }

            return ContentLoadResult.SuccessResult(course);
        }
        catch (JsonException ex)
        {
            return ContentLoadResult.Fail(ContentLoadFailureKind.InvalidJson,
                $"Content file is damaged: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ContentLoadResult.Fail(ContentLoadFailureKind.Unexpected,
                $"Failed to load content: {ex.Message}");
        }
    }
}
