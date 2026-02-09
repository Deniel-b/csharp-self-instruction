using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using kursachFile;

namespace kursach.Services;

public sealed class CodeTaskRunner
{
    private const int DefaultTimeoutMs = 2000;

    public Task<CodeRunResult> RunAsync(string userCode, CodeEntryPoint? entryPoint, CodeConstraints? constraints, IReadOnlyList<TaskTestCase> tests)
    {
        return Task.Run(() => RunInternal(userCode, entryPoint, constraints, tests));
    }

    private static CodeRunResult RunInternal(string userCode, CodeEntryPoint? entryPoint, CodeConstraints? constraints, IReadOnlyList<TaskTestCase> tests)
    {
        if (string.IsNullOrWhiteSpace(userCode))
        {
            return CodeRunResult.Fail("Код пустой.");
        }

        var disallow = constraints?.Disallow ?? new List<string>();
        foreach (var item in disallow)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            if (userCode.Contains(item, StringComparison.OrdinalIgnoreCase))
            {
                return CodeRunResult.Fail($"Код содержит запрещённое использование: {item}");
            }
        }

        var compilation = CreateCompilation(userCode);
        using var assemblyStream = new MemoryStream();
        var emitResult = compilation.Emit(assemblyStream);
        if (!emitResult.Success)
        {
            var errors = emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .ToList();

            return CodeRunResult.Fail("Ошибки компиляции:\n" + string.Join(Environment.NewLine, errors));
        }

        assemblyStream.Seek(0, SeekOrigin.Begin);
        var assemblyBytes = assemblyStream.ToArray();

        var targetClass = entryPoint?.ClassName ?? "Solution";
        var targetMethod = entryPoint?.MethodName ?? "Solve";
        var timeoutMs = constraints?.TimeLimitMs ?? DefaultTimeoutMs;

        if (tests.Count == 0)
        {
            var singleRun = ExecuteTest(assemblyBytes, targetClass, targetMethod, string.Empty, string.Empty, timeoutMs);
            return singleRun.Passed
                ? CodeRunResult.SuccessResult("Код выполнен без тестов.")
                : CodeRunResult.Fail(singleRun.Message ?? "Ошибка выполнения.");
        }

        var results = new List<TestRunResult>();
        foreach (var test in tests)
        {
            var result = ExecuteTest(
                assemblyBytes,
                targetClass,
                targetMethod,
                test.Input ?? string.Empty,
                test.ExpectedOutput ?? string.Empty,
                timeoutMs);
            result = result with { TestId = test.Id, Visibility = test.Visibility };
            results.Add(result);
        }

        return CodeRunResult.FromTests(results);
    }

    private static CSharpCompilation CreateCompilation(string userCode)
    {
        var runnerCode = @"
using System;
using System.Linq;
using System.Reflection;

public static class Runner
{
    public static void Main(string[] args)
    {
        var className = args.Length > 0 ? args[0] : ""Solution"";
        var methodName = args.Length > 1 ? args[1] : ""Solve"";
        var asm = Assembly.GetExecutingAssembly();
        var type = asm.GetTypes().FirstOrDefault(t => string.Equals(t.Name, className, StringComparison.Ordinal)
                                                     || string.Equals(t.FullName, className, StringComparison.Ordinal));
        if (type == null)
        {
            throw new InvalidOperationException($""Class '{className}' not found."");
        }

        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null)
        {
            throw new InvalidOperationException($""Method '{methodName}' not found."");
        }

        if (method.GetParameters().Length != 0)
        {
            throw new InvalidOperationException($""Method '{methodName}' must be parameterless."");
        }

        method.Invoke(null, null);
    }
}
";

        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(userCode),
            CSharpSyntaxTree.ParseText(runnerCode)
        };

        var referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddReference(Assembly? assembly)
        {
            if (assembly is null || assembly.IsDynamic)
            {
                return;
            }

            var location = assembly.Location;
            if (!string.IsNullOrWhiteSpace(location))
            {
                referencePaths.Add(location);
            }
        }

        AddReference(typeof(object).Assembly); // System.Private.CoreLib
        AddReference(typeof(Console).Assembly); // System.Console
        AddReference(typeof(Enumerable).Assembly); // System.Linq
        AddReference(typeof(List<>).Assembly); // System.Collections
        AddReference(typeof(System.Runtime.GCSettings).Assembly); // System.Runtime

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            AddReference(assembly);
        }

        var references = referencePaths
            .Select(path => MetadataReference.CreateFromFile(path))
            .Cast<MetadataReference>()
            .ToList();

        return CSharpCompilation.Create(
            assemblyName: "UserCodeAssembly",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release,
                mainTypeName: "Runner"));
    }

    private static TestRunResult ExecuteTest(byte[] assemblyBytes, string className, string methodName, string input, string expected, int timeoutMs)
    {
        var alc = new AssemblyLoadContext($"TaskRunner_{Guid.NewGuid():N}", isCollectible: true);
        using var asmStream = new MemoryStream(assemblyBytes);
        var assembly = alc.LoadFromStream(asmStream);

        var originalIn = Console.In;
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        using var inputReader = new StringReader(input ?? string.Empty);
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        Console.SetIn(inputReader);
        Console.SetOut(outputWriter);
        Console.SetError(errorWriter);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        Exception? executionError = null;
        var task = Task.Run(() =>
        {
            try
            {
                var runnerType = assembly.GetType("Runner", throwOnError: true);
                var main = runnerType?.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
                main?.Invoke(null, new object[] { new[] { className, methodName } });
            }
            catch (TargetInvocationException ex)
            {
                executionError = ex.InnerException ?? ex;
            }
            catch (Exception ex)
            {
                executionError = ex;
            }
        });

        var completed = task.Wait(timeoutMs);
        sw.Stop();

        Console.SetIn(originalIn);
        Console.SetOut(originalOut);
        Console.SetError(originalErr);

        alc.Unload();

        if (!completed)
        {
            return new TestRunResult(string.Empty, false, $"Превышен лимит времени {timeoutMs} ms.", input, string.Empty, expected, "hidden");
        }

        if (executionError is not null)
        {
            return new TestRunResult(string.Empty, false, $"Ошибка выполнения: {executionError.Message}", input, string.Empty, expected, "hidden");
        }

        var output = outputWriter.ToString();
        var passed = string.Equals(NormalizeOutput(output), NormalizeOutput(expected), StringComparison.Ordinal);
        return new TestRunResult(string.Empty, passed, null, input, output, expected, "hidden");
    }

    private static string NormalizeOutput(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("\r\n", "\n").TrimEnd();
    }
}

public sealed record CodeRunResult(bool Success, string Summary, IReadOnlyList<TestRunResult> Tests)
{
    public static CodeRunResult Fail(string message) => new(false, message, Array.Empty<TestRunResult>());

    public static CodeRunResult SuccessResult(string message) => new(true, message, Array.Empty<TestRunResult>());

    public static CodeRunResult FromTests(IReadOnlyList<TestRunResult> results)
    {
        var publicTests = results.Where(r => string.Equals(r.Visibility, "public", StringComparison.OrdinalIgnoreCase)).ToList();
        var hiddenTests = results.Where(r => !string.Equals(r.Visibility, "public", StringComparison.OrdinalIgnoreCase)).ToList();

        var sb = new StringBuilder();
        var allPassed = true;
        foreach (var test in publicTests)
        {
            var passed = test.Passed;
            allPassed &= passed;
            sb.AppendLine($"{test.TestId}: {(passed ? "OK" : "FAIL")}");

            if (!passed)
            {
                if (!string.IsNullOrWhiteSpace(test.Message))
                {
                    sb.AppendLine(test.Message);
                }
                else
                {
                    sb.AppendLine("Вход:");
                    sb.AppendLine(string.IsNullOrWhiteSpace(test.Input) ? "<empty>" : test.Input);
                    sb.AppendLine("Ожидалось:");
                    sb.AppendLine(string.IsNullOrWhiteSpace(test.Expected) ? "<empty>" : test.Expected);
                    sb.AppendLine("Получено:");
                    sb.AppendLine(string.IsNullOrWhiteSpace(test.Output) ? "<empty>" : test.Output);
                    sb.AppendLine("Ожидалось (escaped): " + EscapeOutput(test.Expected));
                    sb.AppendLine("Получено   (escaped): " + EscapeOutput(test.Output));
                }
            }
        }

        if (hiddenTests.Count > 0)
        {
            var hiddenPassed = hiddenTests.Count(t => t.Passed);
            allPassed &= hiddenPassed == hiddenTests.Count;
            sb.AppendLine($"Скрытые тесты: {hiddenPassed}/{hiddenTests.Count}");
        }

        return new CodeRunResult(allPassed, sb.ToString().Trim(), results);
    }

    private static string NormalizeOutput(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("\r\n", "\n").TrimEnd();
    }

    private static string EscapeOutput(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "<empty>";
        }

        return value
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace(" ", "·");
    }
}

public sealed record TestRunResult(string TestId, bool Passed, string? Message, string? Input, string? Output, string? Expected, string Visibility);
