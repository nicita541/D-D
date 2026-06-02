namespace Tests.Architecture;

public sealed class MojibakeGuardTests
{
    private static readonly string[] Markers =
    [
        "Р Сњ",
        "Р Сџ",
        "Р Т‘",
        "Р Вµ",
        "Р Р…",
        "РЎРѓ",
        "РІР‚",
        "РїС—Р…",
        "Рќ",
        "Рџ",
        "Рґ",
        "Рµ",
        "РЅ",
        "СЃ",
        "вЂ",
        "пїЅ"
    ];

    [Fact]
    public void BackendSourceFiles_DoNotContainKnownMojibakeMarkers()
    {
        var projectRoot = FindProjectRoot();
        var selfPath = Path.Combine(projectRoot, "Tests", "Architecture", "MojibakeGuardTests.cs");
        var scannedRoots = new[]
        {
            Path.Combine(projectRoot, "Modules"),
            Path.Combine(projectRoot, "Infrastructure"),
            Path.Combine(projectRoot, "Shared"),
            Path.Combine(projectRoot, "Tests")
        };

        var offenders = scannedRoots
            .Where(Directory.Exists)
            .SelectMany(path => Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            .Where(file => !string.Equals(
                Path.GetFullPath(file),
                Path.GetFullPath(selfPath),
                StringComparison.OrdinalIgnoreCase))
            .SelectMany(file =>
            {
                var text = File.ReadAllText(file);
                return Markers
                    .Where(text.Contains)
                    .Select(marker => $"{Path.GetRelativePath(projectRoot, file)} contains {marker}");
            })
            .ToArray();

        Assert.True(offenders.Length == 0, "Mojibake markers found:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

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
