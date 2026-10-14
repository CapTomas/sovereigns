namespace Sovereigns.Simulation.State;

/// <summary>Raised when a second owner claims an authoritative component (SOV-P02-T14).</summary>
public sealed class DuplicateOwnerException(string message) : InvalidOperationException(message);

/// <summary>
/// One writer per authoritative component (SOV-P02-T09, system map). Owners register at world creation; a second
/// claim on the same component is a defect and fails immediately.
/// </summary>
public sealed class OwnershipRegistry
{
    private readonly SortedDictionary<string, string> _owners = new(StringComparer.Ordinal);

    /// <summary>Component name to owner name, in ordinal order of the component.</summary>
    public IReadOnlyDictionary<string, string> Owners => _owners;

    public void Register(string component, string owner)
    {
        ArgumentException.ThrowIfNullOrEmpty(component);
        ArgumentException.ThrowIfNullOrEmpty(owner);
        if (_owners.TryGetValue(component, out var existing))
        {
            throw new DuplicateOwnerException($"component '{component}' already has owner '{existing}'; '{owner}' cannot also write it");
        }

        _owners.Add(component, owner);
    }

    public string OwnerOf(string component) =>
        _owners.TryGetValue(component, out var owner) ? owner : throw new KeyNotFoundException($"no owner registered for component '{component}'");
}

/// <summary>
/// Write version of one authoritative component. Its owner bumps it on every change; derived caches record the
/// versions they were computed from and recompute when any differs.
/// </summary>
public sealed class StateVersion(string component)
{
    public string Component { get; } = component;

    public long Value { get; private set; }

    internal void Bump() => Value++;
}

/// <summary>
/// A cached derived value with declared sources (SOV-P02-T09). It is never saved and never written directly: reading
/// it after any source changed recomputes it, so it cannot become a disconnected truth.
/// </summary>
public sealed class DerivedCache<T>
{
    private readonly Func<T> _compute;
    private readonly StateVersion[] _sources;
    private readonly long[] _seen;
    private T _value = default!;
    private bool _valid;

    public DerivedCache(Func<T> compute, params StateVersion[] sources)
    {
        ArgumentNullException.ThrowIfNull(compute);
        ArgumentNullException.ThrowIfNull(sources);
        if (sources.Length == 0)
        {
            throw new ArgumentException("a derived cache needs at least one source", nameof(sources));
        }

        _compute = compute;
        _sources = sources;
        _seen = new long[sources.Length];
    }

    public IEnumerable<string> Sources => _sources.Select(source => source.Component);

    /// <summary>Number of recomputations so far; diagnostics only.</summary>
    public int Computations { get; private set; }

    public T Value
    {
        get
        {
            if (!_valid || _sources.Where((source, i) => source.Value != _seen[i]).Any())
            {
                _value = _compute();
                for (var i = 0; i < _sources.Length; i++)
                {
                    _seen[i] = _sources[i].Value;
                }

                _valid = true;
                Computations++;
            }

            return _value;
        }
    }
}
