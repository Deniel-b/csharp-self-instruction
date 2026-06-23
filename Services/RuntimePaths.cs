using System.IO;

namespace kursach.Services;

public static class RuntimePaths
{
    public const string ContentDirectoryName = "content";
    public const string SourceContentDirectoryName = "src";
    public const string ContentFileName = "content.v2.json";
    public const string LogDirectoryName = "logs";

    public static string ResolveContentPath()
    {
        return Path.Combine(ResolveContentRoot(), ContentFileName);
    }

    public static string ResolveContentRoot()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var currentDirectory = Directory.GetCurrentDirectory();

        var candidates = new[]
        {
            Path.Combine(baseDirectory, ContentDirectoryName),
            Path.Combine(baseDirectory, "..", ContentDirectoryName),
            Path.Combine(currentDirectory, ContentDirectoryName),
            Path.Combine(currentDirectory, SourceContentDirectoryName)
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(Path.Combine(fullPath, ContentFileName)))
            {
                return fullPath;
            }
        }

        return Path.GetFullPath(candidates[0]);
    }

    public static string ResolveContentAssetPath(string relativePath)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(ResolveContentRoot(), normalizedPath);
    }

    public static string ResolveLogDirectory()
    {
        return Path.Combine(ResolvePackageRoot(), LogDirectoryName);
    }

    private static string ResolvePackageRoot()
    {
        var baseDirectory = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
        var parentDirectory = Directory.GetParent(baseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        if (parentDirectory is not null)
        {
            var siblingContent = Path.Combine(parentDirectory.FullName, ContentDirectoryName);
            if (Directory.Exists(siblingContent))
            {
                return parentDirectory.FullName;
            }
        }

        return baseDirectory;
    }
}
