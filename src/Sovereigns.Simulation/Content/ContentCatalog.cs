using System.Text.Json;
using System.Text.RegularExpressions;
using Sovereigns.Simulation.Serialization;

namespace Sovereigns.Simulation.Content;

/// <summary>Result of loading a content directory. Hosts must refuse to run while <see cref="Errors"/> is nonempty.</summary>
public sealed record ContentLoadResult(ContentCatalog Catalog, IReadOnlyList<ContentError> Errors)
{
    public bool Succeeded => Errors.Count == 0;
}

/// <summary>
/// Read-only content definitions keyed by ID (SOV-P01-T13). Loading validates layout, IDs, schema,
/// semantic rules and cross-references, and reports every problem rather than stopping at the first.
/// </summary>
public sealed partial class ContentCatalog
{
    private readonly SortedDictionary<ContentId, IContentDefinition> _definitions;

    private ContentCatalog(SortedDictionary<ContentId, IContentDefinition> definitions) => _definitions = definitions;

    public IReadOnlyCollection<ContentId> Ids => _definitions.Keys;

    public bool TryGet<T>(ContentId id, out T definition)
        where T : class, IContentDefinition
    {
        if (_definitions.TryGetValue(id, out var found) && found is T typed)
        {
            definition = typed;
            return true;
        }

        definition = null!;
        return false;
    }

    public T Get<T>(ContentId id)
        where T : class, IContentDefinition =>
        TryGet<T>(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"No {typeof(T).Name} content with ID {id}{Suggestion(id, _definitions.Keys)}");

    /// <summary>The ID that a definition file must declare, derived from <c>&lt;root&gt;/&lt;namespace&gt;/&lt;kind&gt;/&lt;name&gt;.json</c>.</summary>
    public static bool TryIdFromPath(string root, string file, out ContentId id)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(file)).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        id = default;
        return relative.Length == 3 && relative[2].EndsWith(".json", StringComparison.Ordinal) &&
               ContentId.TryParse($"{relative[0]}:{relative[1]}/{relative[2][..^5]}", out id, out _);
    }

    public static ContentLoadResult Load(string root, IReadOnlyDictionary<string, Type>? kinds = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        kinds ??= ContentKinds.Default;
        var errors = new List<ContentError>();
        var definitions = new SortedDictionary<ContentId, IContentDefinition>(Comparer<ContentId>.Create((a, b) => string.CompareOrdinal(a.ToString(), b.ToString())));
        var files = new Dictionary<ContentId, string>();
        if (!Directory.Exists(root))
        {
            errors.Add(new(root, "content directory does not exist", Hint: "pass the repository's content/ directory"));
            return new(new(definitions), errors);
        }

        foreach (var stray in Visible(Directory.EnumerateFiles(root)).Where(path => !path.EndsWith(".md", StringComparison.Ordinal)))
        {
            errors.Add(new(Display(root, stray), "only namespace directories and Markdown notes belong at the content root",
                Hint: "move definitions to <namespace>/<kind>/<name>.json"));
        }

        foreach (var namespaceDir in Visible(Directory.EnumerateDirectories(root)))
        {
            var ns = Path.GetFileName(namespaceDir);
            if (!ContentId.IsValidSegment(ns))
            {
                errors.Add(new(Display(root, namespaceDir), $"namespace directory '{ns}' is not a valid ID segment",
                    Hint: "use lowercase a-z, 0-9 and _, starting with a letter"));
                continue;
            }

            foreach (var stray in Visible(Directory.EnumerateFiles(namespaceDir)))
            {
                errors.Add(new(Display(root, stray), "files directly under a namespace are not loaded",
                    Hint: "move the file into a kind directory: <namespace>/<kind>/<name>.json"));
            }

            foreach (var kindDir in Visible(Directory.EnumerateDirectories(namespaceDir)))
            {
                var kind = Path.GetFileName(kindDir);
                if (!kinds.TryGetValue(kind, out var type))
                {
                    errors.Add(new(Display(root, kindDir), $"unknown content kind '{kind}'",
                        Hint: $"use one of: {string.Join(", ", kinds.Keys)}; or register the kind in ContentKinds"));
                    continue;
                }

                foreach (var file in Visible(Directory.EnumerateFileSystemEntries(kindDir)))
                {
                    var display = Display(root, file);
                    if (!file.EndsWith(".json", StringComparison.Ordinal) || Directory.Exists(file))
                    {
                        errors.Add(new(display, "only <name>.json definition files belong in a kind directory"));
                        continue;
                    }

                    if (!TryIdFromPath(root, file, out var expectedId))
                    {
                        errors.Add(new(display, $"file name '{Path.GetFileName(file)}' is not a valid ID segment",
                            Hint: "rename the file using lowercase a-z, 0-9 and _, starting with a letter"));
                        continue;
                    }

                    if (LoadDefinition(file, display, type, expectedId, errors) is { } definition)
                    {
                        definitions.Add(definition.Id, definition);
                        files.Add(definition.Id, display);
                    }
                }
            }
        }

        foreach (var (id, definition) in definitions)
        {
            foreach (var reference in definition.References())
            {
                if (!definitions.TryGetValue(reference.Target, out var target))
                {
                    var candidates = definitions.Keys.Where(candidate => candidate.Kind == reference.ExpectedKind);
                    errors.Add(new(files[id], $"unknown {reference.ExpectedKind} reference '{reference.Target}'", reference.JsonPath,
                        Hint: $"define it at <namespace>/{reference.ExpectedKind}/<name>.json or correct the ID{Suggestion(reference.Target, candidates)}"));
                }
                else if (target.Id.Kind != reference.ExpectedKind)
                {
                    errors.Add(new(files[id], $"'{reference.Target}' is a {target.Id.Kind}, but this field needs a {reference.ExpectedKind}", reference.JsonPath));
                }
            }
        }

        return new(new(definitions), errors);
    }

    /// <summary>Entries in ordinal order, without hidden files such as .DS_Store or editor swap files.</summary>
    private static IEnumerable<string> Visible(IEnumerable<string> paths) =>
        paths.Where(path => !Path.GetFileName(path).StartsWith('.')).Order(StringComparer.Ordinal);

    private static IContentDefinition? LoadDefinition(string file, string display, Type type, ContentId expectedId, List<ContentError> errors)
    {
        IContentDefinition? definition;
        try
        {
            using var stream = File.OpenRead(file);
            definition = JsonSerializer.Deserialize(stream, type, SimJson.Options) as IContentDefinition;
        }
        catch (JsonException ex)
        {
            errors.Add(new(display, CleanMessage(ex.Message), ex.Path, ex.LineNumber + 1, SchemaHint(ex, type)));
            return null;
        }

        if (definition is null)
        {
            errors.Add(new(display, "definition is null", "$", Hint: $"write one JSON object with the {expectedId.Kind} members"));
            return null;
        }

        var valid = true;
        if (definition.Id != expectedId)
        {
            errors.Add(new(display, $"id '{definition.Id}' does not match the file location", "$.id",
                Hint: $"set id to '{expectedId}' or move the file to {definition.Id.Namespace}/{definition.Id.Kind}/{definition.Id.Name}.json"));
            valid = false;
        }

        foreach (var issue in definition.Validate())
        {
            errors.Add(new(display, issue.Message, issue.JsonPath));
            valid = false;
        }

        return valid ? definition : null;
    }

    private static string? SchemaHint(JsonException ex, Type type)
    {
        if (!ex.Message.Contains("could not be mapped", StringComparison.Ordinal) &&
            !ex.Message.Contains("missing required properties", StringComparison.Ordinal))
        {
            return null;
        }

        var members = SimJson.Options.GetTypeInfo(type).Properties.Select(property => property.Name);
        return $"{type.Name} members are: {string.Join(", ", members)}";
    }

    /// <summary>System.Text.Json appends its own location; the error already carries path and line.</summary>
    private static string CleanMessage(string message) => LocationSuffix().Replace(message, "");

    private static string Display(string root, string path) =>
        Path.Combine(Path.GetFileName(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)), Path.GetRelativePath(root, path))
            .Replace('\\', '/');

    private static string Suggestion(ContentId wanted, IEnumerable<ContentId> candidates)
    {
        var text = wanted.ToString();
        var best = candidates
            .Select(candidate => (candidate, distance: EditDistance(text, candidate.ToString())))
            .Where(pair => pair.distance <= Math.Max(2, text.Length / 4))
            .OrderBy(pair => pair.distance)
            .ThenBy(pair => pair.candidate.ToString(), StringComparer.Ordinal)
            .Select(pair => pair.candidate)
            .FirstOrDefault();
        return best == default ? "" : $"; did you mean '{best}'?";
    }

    private static int EditDistance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }

    [GeneratedRegex(@"\s*Path: \$.*$", RegexOptions.Singleline)]
    private static partial Regex LocationSuffix();
}
