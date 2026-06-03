namespace Tests.Architecture;

public sealed class ModularMonolithStructureTests
{
    private static readonly string[] LegacyFolders =
    [
        "Controllers",
        "Services",
        "Repositories",
        "Contracts",
        "Models"
    ];

    [Fact]
    public void LegacyLayerFolders_DoNotContainSourceFiles()
    {
        var projectRoot = FindProjectRoot();

        var offenders = LegacyFolders
            .Select(folder => Path.Combine(projectRoot, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories))
            .Select(file => Path.GetRelativePath(projectRoot, file))
            .OrderBy(file => file)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "Legacy layer folders still contain source files:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
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
