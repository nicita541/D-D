namespace Tests.Architecture;

public sealed class MojibakeGuardTests
{
    private static readonly string[] Markers =
    [
        "Рќ",
        "Рџ",
        "Рґ",
        "СЃ",
        "Рµ",
        "вЂ"
    ];

    [Fact]
    public void ActiveModuleSourceFiles_DoNotContainKnownMojibakeMarkers()
    {
        var projectRoot = FindProjectRoot();
        var modulesRoot = Path.Combine(projectRoot, "Modules");
        var activeModules = new[] { "Play", "Travel", "Combat", "Changes" };

        var offenders = activeModules
            .Select(module => Path.Combine(modulesRoot, module))
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
