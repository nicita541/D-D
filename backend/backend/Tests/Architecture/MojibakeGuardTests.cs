namespace Tests.Architecture;

public sealed class MojibakeGuardTests
{
    private static readonly string[] Markers =
    [
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
    public void ActiveModuleAndInfrastructureApiSourceFiles_DoNotContainKnownMojibakeMarkers()
    {
        var projectRoot = FindProjectRoot();
        var scannedRoots = new[]
        {
            Path.Combine(projectRoot, "Modules", "Play"),
            Path.Combine(projectRoot, "Modules", "Travel"),
            Path.Combine(projectRoot, "Modules", "Combat"),
            Path.Combine(projectRoot, "Modules", "Changes"),
            Path.Combine(projectRoot, "Infrastructure", "Api")
        };

        var offenders = scannedRoots
            .Where(Directory.Exists)
            .SelectMany(path => Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
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
