using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CSharp;
using kursachFile;

namespace kursach.Services;

public sealed class CodeTaskRunner
{
    private const int DefaultTimeoutMs = 2000;
    private const int MinTimeoutMs = 200;
    private const int LogFieldLimit = 1200;

    private static readonly string[] DefaultDisallowTokens =
    {
        "System.IO",
        "System.Net",
        "System.Diagnostics",
        "System.Reflection",
        "System.Runtime.InteropServices",
        "System.Threading",
        "System.Security",
        "System.Management",
        "Microsoft.Win32",
        "Environment.Exit",
        "Environment.FailFast",
        "Process.Start",
        "Process.Kill",
        "FileStream",
        "File.",
        "Directory.",
        "DllImport",
        "Marshal.",
        "Thread.",
        "AppDomain",
        "Assembly.Load",
        "Assembly.LoadFrom",
        "Assembly.LoadFile",
        "Activator.CreateInstance"
    };

    public Task<CodeRunResult> RunAsync(string userCode, CodeEntryPoint? entryPoint, CodeConstraints? constraints, IList<TaskTestCase> tests)
    {
        return Task.Factory.StartNew(() => RunInternal(userCode, entryPoint, constraints, tests));
    }

    private static CodeRunResult RunInternal(string userCode, CodeEntryPoint? entryPoint, CodeConstraints? constraints, IList<TaskTestCase> tests)
    {
        if (string.IsNullOrWhiteSpace(userCode))
        {
            AppLogger.Warn("\u041a\u043e\u0434 \u043f\u0443\u0441\u0442\u043e\u0439.");
            return CodeRunResult.Fail("\u041a\u043e\u0434 \u043f\u0443\u0441\u0442\u043e\u0439.");
        }

        var violations = FindUnsafeUsages(userCode, constraints);
        if (violations.Count > 0)
        {
            var message = "\u0417\u0430\u043f\u0440\u0435\u0449\u0435\u043d\u043d\u044b\u0435 API:\n" + string.Join(Environment.NewLine, violations.Select(v => "- " + v));
            AppLogger.Warn("\u0417\u0430\u043f\u0440\u0435\u0449\u0435\u043d\u043d\u044b\u0435 API: " + string.Join(", ", violations));
            return CodeRunResult.Fail(message);
        }

        string executablePath;
        string tempDir;
        string compileError;
        if (!TryCompileExecutable(userCode, out executablePath, out tempDir, out compileError))
        {
            AppLogger.Error(compileError);
            return CodeRunResult.Fail(compileError);
        }

        var targetClass = entryPoint != null && !string.IsNullOrWhiteSpace(entryPoint.ClassName) ? entryPoint.ClassName : "Solution";
        var targetMethod = entryPoint != null && !string.IsNullOrWhiteSpace(entryPoint.MethodName) ? entryPoint.MethodName : "Solve";
        var timeoutMs = Math.Max(MinTimeoutMs, constraints != null && constraints.TimeLimitMs.HasValue ? constraints.TimeLimitMs.Value : DefaultTimeoutMs);
        var memoryLimitMb = constraints != null ? constraints.MemoryLimitMb : null;

        try
        {
            if (tests.Count == 0)
            {
                var singleRun = ExecuteTestProcess(executablePath, targetClass, targetMethod, string.Empty, string.Empty, timeoutMs, memoryLimitMb);
                LogSingleRun(singleRun, targetClass, targetMethod, timeoutMs, memoryLimitMb);
                return singleRun.Passed
                    ? CodeRunResult.SuccessResult("\u041a\u043e\u0434 \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d \u0431\u0435\u0437 \u0442\u0435\u0441\u0442\u043e\u0432.")
                    : CodeRunResult.Fail(singleRun.Message ?? "\u041e\u0448\u0438\u0431\u043a\u0430 \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u0438\u044f.");
            }

            var results = new List<TestRunResult>();
            foreach (var test in tests)
            {
                var result = ExecuteTestProcess(
                    executablePath,
                    targetClass,
                    targetMethod,
                    test.Input ?? string.Empty,
                    test.ExpectedOutput ?? string.Empty,
                    timeoutMs,
                    memoryLimitMb);
                results.Add(result.WithTest(test.Id, test.Visibility));
            }

            var summary = CodeRunResult.FromTests(results);
            LogTestResults(results, targetClass, targetMethod, timeoutMs, memoryLimitMb);
            return summary;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
            }
        }
    }

    private static IList<string> FindUnsafeUsages(string code, CodeConstraints? constraints)
    {
        var violations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (Regex.IsMatch(code, "\\bunsafe\\b", RegexOptions.IgnoreCase))
        {
            violations.Add("unsafe");
        }

        foreach (var token in DefaultDisallowTokens)
        {
            if (!string.IsNullOrWhiteSpace(token) && Regex.IsMatch(code, Regex.Escape(token), RegexOptions.IgnoreCase))
            {
                violations.Add(token);
            }
        }

        if (constraints != null && constraints.Disallow != null)
        {
            foreach (var item in constraints.Disallow)
            {
                if (!string.IsNullOrWhiteSpace(item) && Regex.IsMatch(code, Regex.Escape(item), RegexOptions.IgnoreCase))
                {
                    violations.Add(item.Trim());
                }
            }
        }

        return violations.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool TryCompileExecutable(string userCode, out string executablePath, out string tempDir, out string errorMessage)
    {
        tempDir = Path.Combine(Path.GetTempPath(), "kursach-runner", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        executablePath = Path.Combine(tempDir, "UserCode.exe");

        var source = userCode + Environment.NewLine + BuildRunnerSource();
        var parameters = new CompilerParameters
        {
            GenerateExecutable = true,
            GenerateInMemory = false,
            IncludeDebugInformation = false,
            OutputAssembly = executablePath,
            CompilerOptions = "/optimize /target:exe /main:Runner"
        };
        parameters.ReferencedAssemblies.Add("System.dll");
        parameters.ReferencedAssemblies.Add("System.Core.dll");

        try
        {
            using (var provider = new CSharpCodeProvider())
            {
                var result = provider.CompileAssemblyFromSource(parameters, source);
                if (!result.Errors.HasErrors)
                {
                    errorMessage = string.Empty;
                    return true;
                }

                var errors = result.Errors
                    .Cast<CompilerError>()
                    .Where(error => !error.IsWarning)
                    .Select(error => error.ToString())
                    .ToList();

                errorMessage = "\u041e\u0448\u0438\u0431\u043a\u0438 \u043a\u043e\u043c\u043f\u0438\u043b\u044f\u0446\u0438\u0438:\n" + string.Join(Environment.NewLine, errors);
            }
        }
        catch (PlatformNotSupportedException)
        {
            if (TryCompileWithFrameworkCsc(source, executablePath, tempDir, out errorMessage))
            {
                return true;
            }
        }

        try
        {
            Directory.Delete(tempDir, true);
        }
        catch
        {
        }

        return false;
    }

    private static bool TryCompileWithFrameworkCsc(string source, string executablePath, string tempDir, out string errorMessage)
    {
        var cscPath = ResolveFrameworkCscPath();
        if (string.IsNullOrWhiteSpace(cscPath) || !File.Exists(cscPath))
        {
            errorMessage = "\u041a\u043e\u043c\u043f\u0438\u043b\u044f\u0442\u043e\u0440 C# \u0434\u043b\u044f .NET Framework 4.0 \u043d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d.";
            return false;
        }

        var sourcePath = Path.Combine(tempDir, "UserCode.cs");
        File.WriteAllText(sourcePath, source, Encoding.UTF8);

        var startInfo = new ProcessStartInfo
        {
            FileName = cscPath,
            Arguments = "/nologo /optimize /target:exe /main:Runner /out:\"" + executablePath + "\" /reference:System.dll /reference:System.Core.dll \"" + sourcePath + "\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = tempDir
        };

        using (var process = Process.Start(startInfo))
        {
            if (process == null)
            {
                errorMessage = "\u041d\u0435 \u0443\u0434\u0430\u043b\u043e\u0441\u044c \u0437\u0430\u043f\u0443\u0441\u0442\u0438\u0442\u044c csc.exe.";
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0 && File.Exists(executablePath))
            {
                errorMessage = string.Empty;
                return true;
            }

            errorMessage = "\u041e\u0448\u0438\u0431\u043a\u0438 \u043a\u043e\u043c\u043f\u0438\u043b\u044f\u0446\u0438\u0438:\n" + (string.IsNullOrWhiteSpace(error) ? output : error);
            return false;
        }
    }

    private static string ResolveFrameworkCscPath()
    {
        var windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var candidates = new[]
        {
            Path.Combine(programFiles, "Microsoft Visual Studio", "18", "Community", "MSBuild", "Current", "Bin", "Roslyn", "csc.exe"),
            Path.Combine(programFiles, "Microsoft Visual Studio", "17", "Community", "MSBuild", "Current", "Bin", "Roslyn", "csc.exe"),
            Path.Combine(programFiles, "Microsoft Visual Studio", "2022", "Community", "MSBuild", "Current", "Bin", "Roslyn", "csc.exe"),
            Path.Combine(windir, "Microsoft.NET", "Framework", "v4.0.30319", "csc.exe"),
            Path.Combine(windir, "Microsoft.NET", "Framework64", "v4.0.30319", "csc.exe")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static string BuildRunnerSource()
    {
        return @"
public static class Runner
{
    public static void Main(string[] args)
    {
        try
        {
            var className = args.Length > 0 ? args[0] : ""Solution"";
            var methodName = args.Length > 1 ? args[1] : ""Solve"";
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            var type = System.Linq.Enumerable.FirstOrDefault(asm.GetTypes(), t => string.Equals(t.Name, className, System.StringComparison.Ordinal)
                                                         || string.Equals(t.FullName, className, System.StringComparison.Ordinal));
            if (type == null)
            {
                throw new System.InvalidOperationException(""����� '"" + className + ""' �� ������."");
            }

            var method = type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (method == null)
            {
                throw new System.InvalidOperationException(""����� '"" + methodName + ""' �� ������."");
            }

            if (method.GetParameters().Length != 0)
            {
                throw new System.InvalidOperationException(""����� '"" + methodName + ""' ������ ���� ��� ����������."");
            }

            method.Invoke(null, null);
        }
        catch (System.Reflection.TargetInvocationException ex)
        {
            var inner = ex.InnerException ?? ex;
            System.Console.Error.WriteLine(inner.ToString());
            System.Environment.ExitCode = 1;
        }
        catch (System.Exception ex)
        {
            System.Console.Error.WriteLine(ex.ToString());
            System.Environment.ExitCode = 1;
        }
    }
}
";
    }

    private static TestRunResult ExecuteTestProcess(string executablePath, string className, string methodName, string input, string expected, int timeoutMs, int? memoryLimitMb)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = "\"" + className + "\" \"" + methodName + "\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty
        };

        try
        {
            using (var process = new Process { StartInfo = startInfo })
            {
                if (!process.Start())
                {
                    return new TestRunResult(string.Empty, false, "\u041d\u0435 \u0443\u0434\u0430\u043b\u043e\u0441\u044c \u0437\u0430\u043f\u0443\u0441\u0442\u0438\u0442\u044c \u043f\u0440\u043e\u0446\u0435\u0441\u0441.", input, string.Empty, expected, "hidden");
                }

                JobObject job = null;
                if (memoryLimitMb.HasValue && memoryLimitMb.Value > 0)
                {
                    job = JobObject.TryCreateAndAssign(process, memoryLimitMb.Value);
                }

                if (!string.IsNullOrEmpty(input))
                {
                    process.StandardInput.Write(input);
                }
                process.StandardInput.Close();

                if (!process.WaitForExit(timeoutMs))
                {
                    TryKillProcess(process);
                    if (job != null)
                    {
                        job.Dispose();
                    }

                    return new TestRunResult(string.Empty, false, "\u041f\u0440\u0435\u0432\u044b\u0448\u0435\u043d \u043b\u0438\u043c\u0438\u0442 \u0432\u0440\u0435\u043c\u0435\u043d\u0438 " + timeoutMs + " \u043c\u0441.", input, string.Empty, expected, "hidden");
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (job != null)
                {
                    job.Dispose();
                }

                if (process.ExitCode != 0)
                {
                    var message = string.IsNullOrWhiteSpace(error)
                        ? "\u041e\u0448\u0438\u0431\u043a\u0430 \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u0438\u044f. ExitCode=" + process.ExitCode + "."
                        : "\u041e\u0448\u0438\u0431\u043a\u0430 \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u0438\u044f. ExitCode=" + process.ExitCode + ". " + error;
                    return new TestRunResult(string.Empty, false, message, input, output, expected, "hidden");
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    return new TestRunResult(string.Empty, false, "\u041e\u0448\u0438\u0431\u043a\u0430 \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u0438\u044f: " + error, input, output, expected, "hidden");
                }

                var passed = string.Equals(NormalizeOutput(output), NormalizeOutput(expected), StringComparison.Ordinal);
                return new TestRunResult(string.Empty, passed, null, input, output, expected, "hidden");
            }
        }
        catch (Exception ex)
        {
            return new TestRunResult(string.Empty, false, "\u041e\u0448\u0438\u0431\u043a\u0430 \u0437\u0430\u043f\u0443\u0441\u043a\u0430: " + ex.Message, input, string.Empty, expected, "hidden");
        }
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
        catch
        {
        }
    }

    private static void LogSingleRun(TestRunResult result, string className, string methodName, int timeoutMs, int? memoryLimitMb)
    {
        var sb = new StringBuilder();
        sb.AppendLine("\u0417\u0430\u043f\u0443\u0441\u043a \u0431\u0435\u0437 \u0442\u0435\u0441\u0442\u043e\u0432: " + className + "." + methodName);
        sb.AppendLine("\u0422\u0430\u0439\u043c\u0430\u0443\u0442: " + timeoutMs + " \u043c\u0441. \u041f\u0430\u043c\u044f\u0442\u044c: " + FormatMemoryLimit(memoryLimitMb) + ".");
        sb.AppendLine("\u0421\u0442\u0430\u0442\u0443\u0441: " + (result.Passed ? "\u041e\u041a" : "\u041f\u0440\u043e\u0432\u0430\u043b"));

        if (!result.Passed && !string.IsNullOrWhiteSpace(result.Message))
        {
            sb.AppendLine("\u0421\u043e\u043e\u0431\u0449\u0435\u043d\u0438\u0435: " + TruncateForLog(result.Message));
        }

        AppLogger.Info(sb.ToString().Trim());
    }

    private static void LogTestResults(IList<TestRunResult> results, string className, string methodName, int timeoutMs, int? memoryLimitMb)
    {
        if (results.Count == 0)
        {
            return;
        }

        var passedCount = results.Count(result => result.Passed);
        var sb = new StringBuilder();
        sb.AppendLine("\u0420\u0435\u0437\u0443\u043b\u044c\u0442\u0430\u0442\u044b \u0442\u0435\u0441\u0442\u043e\u0432: " + className + "." + methodName);
        sb.AppendLine("\u0422\u0430\u0439\u043c\u0430\u0443\u0442: " + timeoutMs + " \u043c\u0441. \u041f\u0430\u043c\u044f\u0442\u044c: " + FormatMemoryLimit(memoryLimitMb) + ".");
        sb.AppendLine("\u0412\u0441\u0435\u0433\u043e: " + results.Count + ", \u0443\u0441\u043f\u0435\u0445: " + passedCount + ", \u043f\u0440\u043e\u0432\u0430\u043b: " + (results.Count - passedCount) + ".");

        foreach (var test in results)
        {
            sb.AppendLine("\u0422\u0435\u0441\u0442 " + test.TestId + ": " + (test.Passed ? "\u041e\u041a" : "\u041f\u0440\u043e\u0432\u0430\u043b") + " (" + test.Visibility + ")");
            if (!test.Passed && !string.IsNullOrWhiteSpace(test.Message))
            {
                sb.AppendLine("\u0421\u043e\u043e\u0431\u0449\u0435\u043d\u0438\u0435: " + TruncateForLog(test.Message));
            }
        }

        AppLogger.Info(sb.ToString().Trim());
    }

    private static string FormatMemoryLimit(int? memoryLimitMb)
    {
        return !memoryLimitMb.HasValue || memoryLimitMb.Value <= 0
            ? "\u0431\u0435\u0437 \u043b\u0438\u043c\u0438\u0442\u0430"
            : memoryLimitMb.Value + " \u041c\u0411";
    }

    private static string TruncateForLog(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\r\n", "\n").TrimEnd();
        return normalized.Length <= LogFieldLimit
            ? normalized
            : normalized.Substring(0, LogFieldLimit) + "... (\u043e\u0431\u0440\u0435\u0437\u0430\u043d\u043e)";
    }

    private static string NormalizeOutput(string? value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("\r\n", "\n").TrimEnd();
    }

    private sealed class JobObject : IDisposable
    {
        public static JobObject TryCreateAndAssign(Process process, int memoryLimitMb)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}

public sealed record CodeRunResult(bool Success, string Summary, IList<TestRunResult> Tests)
{
    public static CodeRunResult Fail(string message) => new(false, message, new List<TestRunResult>());

    public static CodeRunResult SuccessResult(string message) => new(true, message, new List<TestRunResult>());

    public static CodeRunResult FromTests(IList<TestRunResult> results)
    {
        var sb = new StringBuilder();
        var allPassed = true;
        var publicCounter = 0;
        var hiddenCounter = 0;

        foreach (var test in results)
        {
            var passed = test.Passed;
            var isPublic = string.Equals(test.Visibility, "public", StringComparison.OrdinalIgnoreCase);
            allPassed &= passed;

            var status = passed ? "пройден" : "не пройден";
            string testLabel;
            if (isPublic)
            {
                publicCounter++;
                testLabel = "Публичный тест " + publicCounter;
            }
            else
            {
                hiddenCounter++;
                testLabel = "Скрытый тест " + hiddenCounter;
            }

            sb.AppendLine(testLabel + ": " + status + ".");

            if (!passed)
            {
                if (!string.IsNullOrWhiteSpace(test.Message))
                {
                    sb.AppendLine(test.Message);
                }

                if (isPublic)
                {
                    sb.AppendLine("\u0412\u0445\u043e\u0434:");
                    sb.AppendLine(string.IsNullOrWhiteSpace(test.Input) ? "<пусто>" : test.Input);
                    sb.AppendLine("\u041e\u0436\u0438\u0434\u0430\u0435\u0442\u0441\u044f:");
                    sb.AppendLine(string.IsNullOrWhiteSpace(test.Expected) ? "<пусто>" : test.Expected);
                    sb.AppendLine("\u0412\u044b\u0432\u043e\u0434:");
                    sb.AppendLine(string.IsNullOrWhiteSpace(test.Output) ? "<пусто>" : test.Output);
                    sb.AppendLine("\u0420\u0430\u0437\u043d\u0438\u0446\u0430:");
                    sb.AppendLine(BuildDiff(test.Expected, test.Output));
                    sb.AppendLine("Ожидалось (экранир.): " + EscapeOutput(test.Expected));
                    sb.AppendLine("Получено (экранир.): " + EscapeOutput(test.Output));
                }
            }
        }

        return new CodeRunResult(allPassed, sb.ToString().Trim(), results);
    }

    private static string BuildDiff(string? expected, string? actual)
    {
        var expectedLines = SplitLines(expected);
        var actualLines = SplitLines(actual);

        var lcs = new int[expectedLines.Length + 1, actualLines.Length + 1];
        for (var i = expectedLines.Length - 1; i >= 0; i--)
        {
            for (var j = actualLines.Length - 1; j >= 0; j--)
            {
                lcs[i, j] = string.Equals(expectedLines[i], actualLines[j], StringComparison.Ordinal)
                    ? lcs[i + 1, j + 1] + 1
                    : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }

        var diff = new List<string>();
        var x = 0;
        var y = 0;
        while (x < expectedLines.Length && y < actualLines.Length)
        {
            if (string.Equals(expectedLines[x], actualLines[y], StringComparison.Ordinal))
            {
                diff.Add("  " + expectedLines[x]);
                x++;
                y++;
            }
            else if (lcs[x + 1, y] >= lcs[x, y + 1])
            {
                diff.Add("- " + expectedLines[x++]);
            }
            else
            {
                diff.Add("+ " + actualLines[y++]);
            }
        }

        while (x < expectedLines.Length)
        {
            diff.Add("- " + expectedLines[x++]);
        }

        while (y < actualLines.Length)
        {
            diff.Add("+ " + actualLines[y++]);
        }

        if (diff.Count == 0)
        {
            diff.Add("<нет различий>");
        }

        return string.Join(Environment.NewLine, diff);
    }

    private static string[] SplitLines(string? value)
    {
        var normalized = NormalizeOutput(value);
        return string.IsNullOrEmpty(normalized) ? new string[0] : normalized.Split('\n');
    }

    private static string NormalizeOutput(string? value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("\r\n", "\n").TrimEnd();
    }

    private static string EscapeOutput(string? value)
    {
        return string.IsNullOrEmpty(value)
            ? "<пусто>"
            : value.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t").Replace(" ", "\u00b7");
    }
}

public sealed record TestRunResult(string TestId, bool Passed, string? Message, string? Input, string? Output, string? Expected, string Visibility)
{
    public TestRunResult WithTest(string testId, string visibility)
    {
        return new TestRunResult(testId, Passed, Message, Input, Output, Expected, visibility);
    }
}

