using System.Text.Json;

namespace Sovereigns.Client.Controls;

/// <summary>Two or more actions that share one binding.</summary>
public sealed record BindingConflict(Binding Binding, IReadOnlyList<string> Actions);

/// <summary>
/// The rebindable action-to-binding model (SOV-P01-T12). It holds bindings as data and knows nothing of the
/// InputMap; <see cref="InputMapRegistrar"/> applies it. Player overrides live in a small JSON file mapping
/// an action name to its list of binding strings.
/// </summary>
public sealed class InputBindings
{
    private static readonly JsonSerializerOptions FileOptions = new() { WriteIndented = true };

    /// <summary>Format of <c>input_bindings.json</c>: <c>{"version": 1, "bindings": {"&lt;action&gt;": ["key:A", ...]}}</c>.</summary>
    public const int FileVersion = 1;

    private readonly Dictionary<string, IReadOnlyList<Binding>> _bindings = new(StringComparer.Ordinal);

    private InputBindings()
    {
    }

    public static InputBindings Defaults()
    {
        var bindings = new InputBindings();
        foreach (var action in InputActions.All)
        {
            bindings._bindings[action.Name] = [.. action.DefaultBindings.Select(ParseDefault)];
        }

        return bindings;
    }

    public IReadOnlyList<Binding> Get(string action) =>
        _bindings.TryGetValue(action, out var found) ? found : throw new ArgumentException($"unknown input action '{action}'", nameof(action));

    /// <summary>Replaces every binding of an action. Nothing changes when the action or any binding is invalid.</summary>
    public void Rebind(string action, IEnumerable<string> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        if (!_bindings.ContainsKey(action))
        {
            throw new ArgumentException($"unknown input action '{action}'", nameof(action));
        }

        _bindings[action] = ParseAll(bindings, out var errors) is { } parsed
            ? parsed
            : throw new ArgumentException(string.Join("; ", errors), nameof(bindings));
    }

    /// <summary>Bindings used by more than one action, in catalog order.</summary>
    public IReadOnlyList<BindingConflict> FindConflicts() =>
    [
        .. InputActions.All
            .SelectMany(action => Get(action.Name).Distinct().Select(binding => (binding, action: action.Name)))
            .GroupBy(pair => pair.binding.ToString(), StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => new BindingConflict(group.First().binding, [.. group.Select(pair => pair.action)])),
    ];

    /// <summary>
    /// Defaults plus the overrides in <paramref name="path"/>. A missing file means defaults. A problem in the
    /// file is reported through <paramref name="warn"/> and skipped: an unknown action, a malformed binding, or
    /// an override whose bindings are all malformed (the action then keeps its defaults).
    /// </summary>
    public static InputBindings Load(string path, Action<string> warn)
    {
        ArgumentNullException.ThrowIfNull(warn);
        var bindings = Defaults();
        if (!File.Exists(path))
        {
            return bindings;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.Number ||
                !root.TryGetProperty("bindings", out var overrides) || overrides.ValueKind != JsonValueKind.Object)
            {
                warn($"{path}: expected {{\"version\": {FileVersion}, \"bindings\": {{\"<action>\": [...]}}}}; using defaults");
                return bindings;
            }

            if (!version.TryGetInt32(out var number) || number != FileVersion)
            {
                warn($"{path}: input bindings format version {version.GetRawText()} is not supported (expected {FileVersion}); using defaults");
                return bindings;
            }

            foreach (var property in overrides.EnumerateObject())
            {
                bindings.ApplyOverride(path, property, warn);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            warn($"cannot read input bindings {path}: {ex.Message}; using defaults");
            return Defaults();
        }

        return bindings;
    }

    /// <summary>Writes only the actions whose bindings differ from the defaults.</summary>
    public void Save(string path)
    {
        var overrides = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var action in InputActions.All)
        {
            var current = Get(action.Name).Select(binding => binding.ToString()).ToArray();
            var defaults = action.DefaultBindings.Select(ParseDefault).Select(binding => binding.ToString());
            if (!current.SequenceEqual(defaults))
            {
                overrides[action.Name] = current;
            }
        }

        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { } directory)
        {
            Directory.CreateDirectory(directory);
        }

        var file = new SortedDictionary<string, object> { ["bindings"] = overrides, ["version"] = FileVersion };
        File.WriteAllText(path, JsonSerializer.Serialize(file, FileOptions) + "\n");
    }

    private void ApplyOverride(string path, JsonProperty property, Action<string> warn)
    {
        if (!_bindings.ContainsKey(property.Name))
        {
            warn($"{path}: unknown input action '{property.Name}' skipped");
            return;
        }

        if (property.Value.ValueKind != JsonValueKind.Array)
        {
            warn($"{path}: '{property.Name}' must be a list of binding strings; skipped");
            return;
        }

        var parsed = new List<Binding>();
        var requested = 0;
        foreach (var element in property.Value.EnumerateArray())
        {
            requested++;
            var error = "not a string";
            if (element.ValueKind == JsonValueKind.String && Binding.TryParse(element.GetString(), out var binding, out error))
            {
                parsed.Add(binding);
            }
            else
            {
                warn($"{path}: '{property.Name}' binding skipped: {error}");
            }
        }

        if (requested == 0 || parsed.Count > 0)
        {
            _bindings[property.Name] = [.. parsed.Distinct()];
        }
    }

    private static List<Binding>? ParseAll(IEnumerable<string> texts, out List<string> errors)
    {
        var parsed = new List<Binding>();
        errors = [];
        foreach (var text in texts)
        {
            if (Binding.TryParse(text, out var binding, out var error))
            {
                if (!parsed.Contains(binding))
                {
                    parsed.Add(binding);
                }
            }
            else
            {
                errors.Add(error);
            }
        }

        return errors.Count == 0 ? parsed : null;
    }

    private static Binding ParseDefault(string text) =>
        Binding.TryParse(text, out var binding, out var error)
            ? binding
            : throw new InvalidOperationException($"default binding {text} is invalid: {error}");
}
