using System.IO;
using System.Text;

namespace kursach.Services;

public static class AppLogger
{
    private static readonly object SyncRoot = new();
    private static readonly string LogDirectory = RuntimePaths.ResolveLogDirectory();
    private static readonly string LogFilePath = Path.Combine(LogDirectory, "app.log");

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message) => Write("ERROR", message);

    public static void Error(Exception ex, string? message = null)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(message))
        {
            sb.AppendLine(message);
        }

        sb.AppendLine(ex.ToString());
        Write("ERROR", sb.ToString().Trim());
    }

    private static void Write(string level, string message)
    {
        try
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(LogDirectory);
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level}: {message}";
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // logging must not crash the app
        }
    }
}
