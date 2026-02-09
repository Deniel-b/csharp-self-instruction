using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    public Task<CodeRunResult> RunAsync(string userCode, CodeEntryPoint? entryPoint, CodeConstraints? constraints, IReadOnlyList<TaskTestCase> tests)
    {
        return Task.Run(() => RunInternal(userCode, entryPoint, constraints, tests));
    }

    private static CodeRunResult RunInternal(string userCode, CodeEntryPoint? entryPoint, CodeConstraints? constraints, IReadOnlyList<TaskTestCase> tests)
    {
        if (string.IsNullOrWhiteSpace(userCode))
        {
            AppLogger.Warn("\u041a\u043e\u0434 \u043f\u0443\u0441\u0442\u043e\u0439.");
            return CodeRunResult.Fail("\u041a\u043e\u0434 \u043f\u0443\u0441\u0442\u043e\u0439.");
        }

        var violations = FindUnsafeUsages(userCode, constraints);
        if (violations.Count > 0)
        {
            var message = "\u0417\u0430\u043f\u0440\u0435\u0449\u0435\u043d\u043d\u044b\u0435 API:\n" + string.Join(Environment.NewLine, violations.Select(v => $"- {v}"));
            AppLogger.Warn("\u0417\u0430\u043f\u0440\u0435\u0449\u0435\u043d\u043d\u044b\u0435 API: " + string.Join(", ", violations));
            return CodeRunResult.Fail(message);
        }

        if (!TryCompileAssembly(userCode, out var assemblyPath, out var tempDir, out var compileError))
        {
            AppLogger.Error(compileError ?? "\u041e\u0448\u0438\u0431\u043a\u0430 \u043a\u043e\u043c\u043f\u0438\u043b\u044f\u0446\u0438\u0438.");
            return CodeRunResult.Fail(compileError ?? "\u041e\u0448\u0438\u0431\u043a\u0430 \u043a\u043e\u043c\u043f\u0438\u043b\u044f\u0446\u0438\u0438.");
        }

        var targetClass = entryPoint?.ClassName ?? "Solution";
        var targetMethod = entryPoint?.MethodName ?? "Solve";
        var timeoutMs = Math.Max(MinTimeoutMs, constraints?.TimeLimitMs ?? DefaultTimeoutMs);
        var memoryLimitMb = constraints?.MemoryLimitMb;

        try
        {
            if (tests.Count == 0)
            {
                var singleRun = ExecuteTestProcess(assemblyPath, targetClass, targetMethod, string.Empty, string.Empty, timeoutMs, memoryLimitMb);
                LogSingleRun(singleRun, targetClass, targetMethod, timeoutMs, memoryLimitMb);
                return singleRun.Passed
                    ? CodeRunResult.SuccessResult("\u041a\u043e\u0434 \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d \u0431\u0435\u0437 \u0442\u0435\u0441\u0442\u043e\u0432.")
                    : CodeRunResult.Fail(singleRun.Message ?? "\u041e\u0448\u0438\u0431\u043a\u0430 \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u0438\u044f.");
            }

            var results = new List<TestRunResult>();
            foreach (var test in tests)
            {
                var result = ExecuteTestProcess(
                    assemblyPath,
                    targetClass,
                    targetMethod,
                    test.Input ?? string.Empty,
                    test.ExpectedOutput ?? string.Empty,
                    timeoutMs,
                    memoryLimitMb);
                result = result with { TestId = test.Id, Visibility = test.Visibility };
                results.Add(result);
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
                // ignore cleanup errors
            }
        }
    }

    private static IReadOnlyList<string> FindUnsafeUsages(string code, CodeConstraints? constraints)
    {
        var violations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (Regex.IsMatch(code, "\\bunsafe\\b", RegexOptions.IgnoreCase))
        {
            violations.Add("unsafe");
        }

        foreach (var token in DefaultDisallowTokens)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (Regex.IsMatch(code, Regex.Escape(token), RegexOptions.IgnoreCase))
            {
                violations.Add(token);
            }
        }

        if (constraints?.Disallow is not null)
        {
            foreach (var item in constraints.Disallow)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }

                if (Regex.IsMatch(code, Regex.Escape(item), RegexOptions.IgnoreCase))
                {
                    violations.Add(item.Trim());
                }
            }
        }

        return violations.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool TryCompileAssembly(string userCode, out string assemblyPath, out string tempDir, out string? errorMessage)
    {
        var compilation = CreateCompilation(userCode);
        tempDir = Path.Combine(Path.GetTempPath(), "kursach-runner", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        assemblyPath = Path.Combine(tempDir, "UserCode.dll");

        using var assemblyStream = new FileStream(assemblyPath, FileMode.Create, FileAccess.Write, FileShare.None);
        var emitResult = compilation.Emit(assemblyStream);
        if (!emitResult.Success)
        {
            var errors = emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .ToList();

            errorMessage = "\u041e\u0448\u0438\u0431\u043a\u0438 \u043a\u043e\u043c\u043f\u0438\u043b\u044f\u0446\u0438\u0438:\n" + string.Join(Environment.NewLine, errors);
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // ignore cleanup errors
            }
            return false;
        }

        if (!TryWriteRuntimeConfig(tempDir, out var configError))
        {
            errorMessage = configError ?? "\u041d\u0435 \u0443\u0434\u0430\u043b\u043e\u0441\u044c \u0441\u043e\u0437\u0434\u0430\u0442\u044c runtimeconfig.";
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // ignore cleanup errors
            }
            return false;
        }

        errorMessage = null;
        return true;
    }

    private static CSharpCompilation CreateCompilation(string userCode)
    {
        var runnerCode = """
using System;
using System.Linq;
using System.Reflection;

public static class Runner
{
    public static void Main(string[] args)
    {
        try
        {
            var className = args.Length > 0 ? args[0] : "Solution";
            var methodName = args.Length > 1 ? args[1] : "Solve";
            var asm = Assembly.GetExecutingAssembly();
            var type = asm.GetTypes().FirstOrDefault(t => string.Equals(t.Name, className, StringComparison.Ordinal)
                                                         || string.Equals(t.FullName, className, StringComparison.Ordinal));
            if (type == null)
            {
                throw new InvalidOperationException($"\u041a\u043b\u0430\u0441\u0441 '{className}' \u043d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d.");
            }

            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
            {
                throw new InvalidOperationException($"\u041c\u0435\u0442\u043e\u0434 '{methodName}' \u043d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d.");
            }

            if (method.GetParameters().Length != 0)
            {
                throw new InvalidOperationException($"\u041c\u0435\u0442\u043e\u0434 '{methodName}' \u0434\u043e\u043b\u0436\u0435\u043d \u0431\u044b\u0442\u044c \u0431\u0435\u0437 \u043f\u0430\u0440\u0430\u043c\u0435\u0442\u0440\u043e\u0432.");
            }

            method.Invoke(null, null);
        }
        catch (TargetInvocationException ex)
        {
            var inner = ex.InnerException ?? ex;
            Console.Error.WriteLine(inner.ToString());
            Environment.ExitCode = 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            Environment.ExitCode = 1;
        }
    }
}
""";

        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(userCode),
            CSharpSyntaxTree.ParseText(runnerCode)
        };

        var references = ResolvePlatformReferences();

        return CSharpCompilation.Create(
            assemblyName: "UserCodeAssembly",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release,
                allowUnsafe: false,
                mainTypeName: "Runner"));
    }

    private static MetadataReference[] ResolvePlatformReferences()
    {
        var references = new List<MetadataReference>();
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (!string.IsNullOrWhiteSpace(tpa))
        {
            foreach (var path in tpa.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        if (references.Count == 0)
        {
            references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location));
        }

        return references.ToArray();
    }

    private static TestRunResult ExecuteTestProcess(string assemblyPath, string className, string methodName, string input, string expected, int timeoutMs, int? memoryLimitMb)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{assemblyPath}\" \"{className}\" \"{methodName}\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(assemblyPath) ?? string.Empty
        };

        try
        {
            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                return new TestRunResult(string.Empty, false, "\u041d\u0435 \u0443\u0434\u0430\u043b\u043e\u0441\u044c \u0437\u0430\u043f\u0443\u0441\u0442\u0438\u0442\u044c \u043f\u0440\u043e\u0446\u0435\u0441\u0441.", input, string.Empty, expected, "hidden");
            }

            JobObject? job = null;
            if (memoryLimitMb.HasValue && memoryLimitMb.Value > 0)
            {
                job = JobObject.TryCreateAndAssign(process, memoryLimitMb.Value);
            }

            if (!string.IsNullOrEmpty(input))
            {
                process.StandardInput.Write(input);
            }
            process.StandardInput.Close();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(timeoutMs))
            {
                TryKillProcess(process);
                job?.Dispose();
                return new TestRunResult(string.Empty, false, $"\u041f\u0440\u0435\u0432\u044b\u0448\u0435\u043d \u043b\u0438\u043c\u0438\u0442 \u0432\u0440\u0435\u043c\u0435\u043d\u0438 {timeoutMs} \u043c\u0441.", input, string.Empty, expected, "hidden");
            }

            try
            {
                Task.WhenAll(outputTask, errorTask).GetAwaiter().GetResult();
            }
            catch
            {
                // ignore read errors
            }

            var output = outputTask.Result ?? string.Empty;
            var error = errorTask.Result ?? string.Empty;

            job?.Dispose();

            if (process.ExitCode != 0)
            {
                var message = string.IsNullOrWhiteSpace(error)
                    ? $"\u041e\u0448\u0438\u0431\u043a\u0430 \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u0438\u044f. ExitCode={process.ExitCode}."
                    : $"\u041e\u0448\u0438\u0431\u043a\u0430 \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u0438\u044f. ExitCode={process.ExitCode}. {error}";
                return new TestRunResult(string.Empty, false, message, input, output, expected, "hidden");
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                return new TestRunResult(string.Empty, false, $"\u041e\u0448\u0438\u0431\u043a\u0430 \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u0438\u044f: {error}", input, output, expected, "hidden");
            }

            var passed = string.Equals(NormalizeOutput(output), NormalizeOutput(expected), StringComparison.Ordinal);
            return new TestRunResult(string.Empty, passed, null, input, output, expected, "hidden");
        }
        catch (Exception ex)
        {
            return new TestRunResult(string.Empty, false, $"\u041e\u0448\u0438\u0431\u043a\u0430 \u0437\u0430\u043f\u0443\u0441\u043a\u0430: {ex.Message}", input, string.Empty, expected, "hidden");
        }
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // ignore kill errors
        }
    }

    private static void LogSingleRun(TestRunResult result, string className, string methodName, int timeoutMs, int? memoryLimitMb)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\u0417\u0430\u043f\u0443\u0441\u043a \u0431\u0435\u0437 \u0442\u0435\u0441\u0442\u043e\u0432: {className}.{methodName}");
        sb.AppendLine($"\u0422\u0430\u0439\u043c\u0430\u0443\u0442: {timeoutMs} \u043c\u0441. \u041f\u0430\u043c\u044f\u0442\u044c: {FormatMemoryLimit(memoryLimitMb)}.");
        sb.AppendLine($"\u0421\u0442\u0430\u0442\u0443\u0441: {(result.Passed ? "\u041e\u041a" : "\u041f\u0440\u043e\u0432\u0430\u043b")}");

        if (!result.Passed)
        {
            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                sb.AppendLine("\u0421\u043e\u043e\u0431\u0449\u0435\u043d\u0438\u0435: " + TruncateForLog(result.Message));
            }

            if (!string.IsNullOrWhiteSpace(result.Output))
            {
                sb.AppendLine("\u0412\u044b\u0432\u043e\u0434: " + TruncateForLog(result.Output));
            }
        }

        AppLogger.Info(sb.ToString().Trim());
    }

    private static void LogTestResults(IReadOnlyList<TestRunResult> results, string className, string methodName, int timeoutMs, int? memoryLimitMb)
    {
        if (results.Count == 0)
        {
            return;
        }

        var passedCount = results.Count(result => result.Passed);
        var failedCount = results.Count - passedCount;

        var sb = new StringBuilder();
        sb.AppendLine($"\u0420\u0435\u0437\u0443\u043b\u044c\u0442\u0430\u0442\u044b \u0442\u0435\u0441\u0442\u043e\u0432: {className}.{methodName}");
        sb.AppendLine($"\u0422\u0430\u0439\u043c\u0430\u0443\u0442: {timeoutMs} \u043c\u0441. \u041f\u0430\u043c\u044f\u0442\u044c: {FormatMemoryLimit(memoryLimitMb)}.");
        sb.AppendLine($"\u0412\u0441\u0435\u0433\u043e: {results.Count}, \u0443\u0441\u043f\u0435\u0445: {passedCount}, \u043f\u0440\u043e\u0432\u0430\u043b: {failedCount}.");

        foreach (var test in results)
        {
            sb.AppendLine($"\u0422\u0435\u0441\u0442 {test.TestId}: {(test.Passed ? "\u041e\u041a" : "\u041f\u0440\u043e\u0432\u0430\u043b")} ({test.Visibility})");

            if (test.Passed)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(test.Message))
            {
                sb.AppendLine("\u0421\u043e\u043e\u0431\u0449\u0435\u043d\u0438\u0435: " + TruncateForLog(test.Message));
            }

            if (!string.IsNullOrWhiteSpace(test.Input))
            {
                sb.AppendLine("\u0412\u0445\u043e\u0434: " + TruncateForLog(test.Input));
            }

            if (!string.IsNullOrWhiteSpace(test.Expected))
            {
                sb.AppendLine("\u041e\u0436\u0438\u0434\u0430\u0435\u0442\u0441\u044f: " + TruncateForLog(test.Expected));
            }

            if (!string.IsNullOrWhiteSpace(test.Output))
            {
                sb.AppendLine("\u0412\u044b\u0432\u043e\u0434: " + TruncateForLog(test.Output));
            }
        }

        AppLogger.Info(sb.ToString().Trim());
    }

    private static string FormatMemoryLimit(int? memoryLimitMb)
    {
        if (!memoryLimitMb.HasValue || memoryLimitMb.Value <= 0)
        {
            return "\u0431\u0435\u0437 \u043b\u0438\u043c\u0438\u0442\u0430";
        }

        return memoryLimitMb.Value + " \u041c\u0411";
    }

    private static string TruncateForLog(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\r\n", "\n").TrimEnd();
        if (normalized.Length <= LogFieldLimit)
        {
            return normalized;
        }

        return normalized.Substring(0, LogFieldLimit) + "... (\u043e\u0431\u0440\u0435\u0437\u0430\u043d\u043e)";
    }

    private static bool TryWriteRuntimeConfig(string tempDir, out string? errorMessage)
    {
        try
        {
            var version = Environment.Version;
            var majorMinor = $"{version.Major}.{version.Minor}";
            var runtimeConfigPath = Path.Combine(tempDir, "UserCode.runtimeconfig.json");
            var configJson =
                "{\n" +
                "  \"runtimeOptions\": {\n" +
                $"    \"tfm\": \"net{majorMinor}\",\n" +
                "    \"rollForward\": \"LatestMinor\",\n" +
                "    \"framework\": {\n" +
                "      \"name\": \"Microsoft.NETCore.App\",\n" +
                $"      \"version\": \"{version.Major}.{version.Minor}.{version.Build}\"\n" +
                "    }\n" +
                "  }\n" +
                "}\n";
            File.WriteAllText(runtimeConfigPath, configJson, Encoding.UTF8);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static string NormalizeOutput(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("\r\n", "\n").TrimEnd();
    }

    private sealed class JobObject : IDisposable
    {
        private readonly IntPtr _handle;

        private JobObject(IntPtr handle)
        {
            _handle = handle;
        }

        public static JobObject? TryCreateAndAssign(Process process, int memoryLimitMb)
        {
            if (memoryLimitMb <= 0)
            {
                return null;
            }

            var handle = CreateJobObject(IntPtr.Zero, null);
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            var job = new JobObject(handle);
            if (!job.SetMemoryLimit(memoryLimitMb))
            {
                job.Dispose();
                return null;
            }

            if (!AssignProcessToJobObject(handle, process.Handle))
            {
                job.Dispose();
                return null;
            }

            return job;
        }

        private bool SetMemoryLimit(int memoryLimitMb)
        {
            var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = JobObjectLimitFlags.JOB_OBJECT_LIMIT_PROCESS_MEMORY | JobObjectLimitFlags.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                },
                ProcessMemoryLimit = (UIntPtr)(memoryLimitMb * 1024L * 1024L)
            };

            var length = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
            var infoPtr = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(info, infoPtr, false);
                return SetInformationJobObject(_handle, JobObjectInfoClass.ExtendedLimitInformation, infoPtr, (uint)length);
            }
            finally
            {
                Marshal.FreeHGlobal(infoPtr);
            }
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                CloseHandle(_handle);
            }
        }

        private enum JobObjectInfoClass
        {
            ExtendedLimitInformation = 9
        }

        [Flags]
        private enum JobObjectLimitFlags : uint
        {
            JOB_OBJECT_LIMIT_PROCESS_MEMORY = 0x00000100,
            JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public JobObjectLimitFlags LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public long Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll")]
        private static extern bool SetInformationJobObject(IntPtr hJob, JobObjectInfoClass jobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}

public sealed record CodeRunResult(bool Success, string Summary, IReadOnlyList<TestRunResult> Tests)
{
    public static CodeRunResult Fail(string message) => new(false, message, Array.Empty<TestRunResult>());

    public static CodeRunResult SuccessResult(string message) => new(true, message, Array.Empty<TestRunResult>());

    public static CodeRunResult FromTests(IReadOnlyList<TestRunResult> results)
    {
        var sb = new StringBuilder();
        var allPassed = true;

        foreach (var test in results)
        {
            var passed = test.Passed;
            var isPublic = string.Equals(test.Visibility, "public", StringComparison.OrdinalIgnoreCase);
            allPassed &= passed;

            var visibilityLabel = isPublic ? string.Empty : " (\u0441\u043a\u0440\u044b\u0442\u044b\u0439)";
            sb.AppendLine($"{test.TestId}: {(passed ? "\u041e\u041a" : "\u041f\u0440\u043e\u0432\u0430\u043b")}{visibilityLabel}");

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
                if (string.Equals(expectedLines[i], actualLines[j], StringComparison.Ordinal))
                {
                    lcs[i, j] = lcs[i + 1, j + 1] + 1;
                }
                else
                {
                    lcs[i, j] = Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
                }
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
                diff.Add("- " + expectedLines[x]);
                x++;
            }
            else
            {
                diff.Add("+ " + actualLines[y]);
                y++;
            }
        }

        while (x < expectedLines.Length)
        {
            diff.Add("- " + expectedLines[x]);
            x++;
        }

        while (y < actualLines.Length)
        {
            diff.Add("+ " + actualLines[y]);
            y++;
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
        if (string.IsNullOrEmpty(normalized))
        {
            return Array.Empty<string>();
        }

        return normalized.Split('\n');
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
            return "<пусто>";
        }

        return value
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t")
            .Replace(" ", "\u00b7");
    }
}

public sealed record TestRunResult(string TestId, bool Passed, string? Message, string? Input, string? Output, string? Expected, string Visibility);
