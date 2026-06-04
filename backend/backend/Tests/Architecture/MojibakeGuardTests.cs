using System.Diagnostics;
using System.Text;

namespace Tests.Architecture;

public sealed class MojibakeGuardTests
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".csproj", ".slnx", ".json", ".md", ".sql", ".ts", ".tsx", ".js", ".mjs",
        ".cjs", ".css", ".html", ".yml", ".yaml", ".http", ".env", ".example", ".ps1", ".sh",
        ".conf", ".dockerignore", ".gitignore"
    };

    private static readonly string[] Markers =
    [
        "\uFFFD",
        "\u0420\u00A0\u0421\u045C",
        "\u0420\u00A0\u0421\u045F",
        "\u0420\u00A0\u0422\u2018",
        "\u0420\u00A0\u0412\u00B5",
        "\u0420\u00A0\u0420\u2026",
        "\u0420\u040E\u0420\u0453",
        "\u0420\u0406\u0420\u201A",
        "\u0420\u0457\u0421\u2014\u0420\u2026",
        "\u0420\u045C",
        "\u0420\u045F",
        "\u0420\u0491",
        "\u0420\u00B5",
        "\u0420\u0405",
        "\u0421\u0403",
        "\u0432\u0402",
        "\u043F\u0457\u0405"
    ];

    [Fact]
    public void TrackedTextFiles_AreValidUtf8AndDoNotContainKnownMojibakeMarkers()
    {
        var projectRoot = FindProjectRoot();
        var repositoryRoot = Directory.GetParent(projectRoot)?.Parent?.FullName
            ?? throw new InvalidOperationException("Could not find repository root.");
        var selfPath = Path.Combine(projectRoot, "Tests", "Architecture", "MojibakeGuardTests.cs");
        var utf8 = new UTF8Encoding(false, true);
        var offenders = GetTrackedFiles(repositoryRoot)
            .Where(File.Exists)
            .Where(IsTextFile)
            .Where(file => !string.Equals(
                Path.GetFullPath(file),
                Path.GetFullPath(selfPath),
                StringComparison.OrdinalIgnoreCase))
            .SelectMany(file =>
            {
                try
                {
                    var text = File.ReadAllText(file, utf8);
                    return Markers
                        .Where(text.Contains)
                        .Select(marker => $"{Path.GetRelativePath(repositoryRoot, file)} contains {marker}");
                }
                catch (DecoderFallbackException)
                {
                    return [$"{Path.GetRelativePath(repositoryRoot, file)} is not valid UTF-8"];
                }
            })
            .ToArray();

        Assert.True(offenders.Length == 0, "Mojibake markers found:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    private static IEnumerable<string> GetTrackedFiles(string repositoryRoot)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("ls-files");
        startInfo.ArgumentList.Add("-z");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start git.");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git ls-files failed: {process.StandardError.ReadToEnd()}");
        }

        return output
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(path => Path.Combine(repositoryRoot, path));
    }

    private static bool IsTextFile(string path)
        => TextExtensions.Contains(Path.GetExtension(path))
           || string.Equals(Path.GetFileName(path), "Dockerfile", StringComparison.OrdinalIgnoreCase);

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find backend project root.");
    }
}
