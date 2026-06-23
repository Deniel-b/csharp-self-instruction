using System.Runtime.ExceptionServices;

namespace Kursach.Tests;

internal static class TestHelpers
{
    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var projectPath = Path.Combine(dir.FullName, "selfinstruction.csproj");
            if (File.Exists(projectPath))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Не удалось найти корень репозитория.");
    }

    public static void RunSta(Action action)
    {
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        })
        {
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }
    }

    public static string NormalizeText(string value)
    {
        return value.Replace("\r\n", "\n").Trim();
    }
}
