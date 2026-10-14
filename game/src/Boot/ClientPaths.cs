namespace Sovereigns.Client.Boot;

/// <summary>Where the client finds content and fixtures and writes logs, reports and input bindings.</summary>
public sealed record ClientPaths(
    string? RepositoryRoot,
    string ContentDirectory,
    string? FixturesDirectory,
    string LogsDirectory,
    string ReportsDirectory,
    string BindingsFile)
{
    /// <summary>
    /// A project run from source finds content/ and tests/fixtures/ in the repository above game/. An exported
    /// build finds content/ next to its executable and has no fixtures; options can override the locations.
    /// </summary>
    public static ClientPaths Resolve(LaunchArguments args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var user = ProjectSettings.GlobalizePath("user://");
        string? repositoryRoot = null;
        string content;
        string? fixtures = null;
        if (OS.HasFeature("template"))
        {
            content = Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath()) ?? ".", "content");
        }
        else
        {
            repositoryRoot = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), ".."));
            content = Path.Combine(repositoryRoot, "content");
            fixtures = Path.Combine(repositoryRoot, "tests", "fixtures");
        }

        return new ClientPaths(
            repositoryRoot,
            args.ContentDirectory ?? content,
            fixtures,
            Path.Combine(user, "logs"),
            args.ReportsDirectory ?? Path.Combine(user, "reports"),
            args.BindingsFile ?? Path.Combine(user, "input_bindings.json"));
    }
}
