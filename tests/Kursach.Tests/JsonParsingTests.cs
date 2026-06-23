using kursach.Services;
using kursachFile;

namespace Kursach.Tests;

public sealed class JsonParsingTests
{
    [Fact]
    public void Load_ReturnsSuccess_ForValidJson()
    {
        var json = """
        {
          "title": "Test Course",
          "chapters": [
            {
              "id": "ch1",
              "title": "Chapter 1",
              "order": 1,
              "sections": [
                {
                  "id": "sec1",
                  "title": "Section 1",
                  "order": 1,
                  "pages": [
                    {
                      "id": "p1",
                      "title": "Page 1",
                      "order": 1,
                      "content": { "format": "richText", "source": "content/test.rtf" }
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;

        var tempFile = Path.Combine(Path.GetTempPath(), $"kursach_test_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, json);

        try
        {
            var loader = new ContentLoader();
            var result = loader.Load(tempFile);

            Assert.True(result.Success);
            Assert.NotNull(result.Course);
            Assert.Single(result.Course!.Chapters);
            Assert.Single(result.Course.Chapters[0].Sections);
            Assert.Single(result.Course.Chapters[0].Sections[0].Pages);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void Load_ReturnsInvalidJson_ForBrokenFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"kursach_test_{Guid.NewGuid():N}.json");
        File.WriteAllText(tempFile, "{");

        try
        {
            var loader = new ContentLoader();
            var result = loader.Load(tempFile);

            Assert.False(result.Success);
            Assert.Equal(ContentLoadFailureKind.InvalidJson, result.FailureKind);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void Load_ReturnsNotFound_ForMissingFile()
    {
        var loader = new ContentLoader();
        var result = loader.Load(Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.json"));

        Assert.False(result.Success);
        Assert.Equal(ContentLoadFailureKind.NotFound, result.FailureKind);
    }
}
