using Newtonsoft.Json.Linq;

namespace Kursach.Tests;

public sealed class ContentFormatTests
{
    [Fact]
    public void ContentJson_IsFormatted()
    {
        var root = TestHelpers.FindRepoRoot();
        var path = Path.Combine(root, "src", "content.v2.json");

        var raw = File.ReadAllText(path);
        var token = JToken.Parse(raw);
        var formatted = token.ToString(Newtonsoft.Json.Formatting.Indented);

        Assert.Equal(TestHelpers.NormalizeText(formatted), TestHelpers.NormalizeText(raw));
    }
}
