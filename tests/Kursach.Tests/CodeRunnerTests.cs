using kursach.Services;
using kursachFile;

namespace Kursach.Tests;

public sealed class CodeRunnerTests
{
    [Fact]
    public async Task RunAsync_ReturnsSuccess_ForValidCode()
    {
        var runner = new CodeTaskRunner();
        var tests = new List<TaskTestCase>
        {
            new()
            {
                Id = "sample-1",
                Visibility = "public",
                Input = "Alice\n",
                ExpectedOutput = "Hello, Alice!"
            }
        };

        var code = """
using System;

public static class Solution
{
    public static void Solve()
    {
        var name = Console.ReadLine();
        Console.WriteLine($"Hello, {name}!");
    }
}
""";

        var result = await runner.RunAsync(code, null, null, tests);

        Assert.True(result.Success);
        Assert.Single(result.Tests);
        Assert.True(result.Tests[0].Passed);
    }

    [Fact]
    public async Task RunAsync_ReturnsFailure_ForDisallowedApi()
    {
        var runner = new CodeTaskRunner();
        var code = """
using System.IO;

public static class Solution
{
    public static void Solve()
    {
        File.ReadAllText("secret.txt");
    }
}
""";

        var result = await runner.RunAsync(code, null, null, Array.Empty<TaskTestCase>());

        Assert.False(result.Success);
        Assert.Contains("Запрещенные API", result.Summary);
    }

    [Fact]
    public async Task RunAsync_IncludesDiff_ForFailedTest()
    {
        var runner = new CodeTaskRunner();
        var tests = new List<TaskTestCase>
        {
            new()
            {
                Id = "sample-1",
                Visibility = "public",
                Input = string.Empty,
                ExpectedOutput = "A"
            }
        };

        var code = """
using System;

public static class Solution
{
    public static void Solve()
    {
        Console.WriteLine("B");
    }
}
""";

        var result = await runner.RunAsync(code, null, null, tests);

        Assert.False(result.Success);
        Assert.Contains("Разница:", result.Summary);
        Assert.Contains("- A", result.Summary);
        Assert.Contains("+ B", result.Summary);
    }
}
