using System.Text;

namespace Sovereigns.Simulation.Random;

/// <summary>
/// Deterministic, counter-based random stream (SOV-P02-T06, ADR-0007). Draw <c>i</c> is a pure function of the
/// stream key and <c>i</c>: SplitMix64 applied to <c>key + (i + 1) · γ</c>. Streams are keyed by world seed, a
/// purpose name and optional place/entity/event values, so a stream never depends on how many draws an unrelated
/// subsystem consumed. A system that keeps a stream across steps saves <see cref="Position"/>.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711", Justification = "'Random stream' is the term of spec §29.3 and ADR-0004; this is not a System.IO.Stream.")]
public sealed class RandomStream
{
    private const ulong Gamma = 0x9E3779B97F4A7C15;

    private RandomStream(ulong key, ulong position)
    {
        Key = key;
        Position = position;
    }

    /// <summary>The 64-bit stream key derived from seed, purpose and keys.</summary>
    public ulong Key { get; }

    /// <summary>Number of draws taken; the next draw is number <see cref="Position"/>.</summary>
    public ulong Position { get; private set; }

    /// <summary>
    /// A stream for <paramref name="purpose"/> (a stable lowercase name such as <c>genesis.rivers</c>) in the world
    /// with <paramref name="seed"/>, further keyed by place, entity or event values.
    /// </summary>
    public static RandomStream For(ulong seed, string purpose, params ReadOnlySpan<ulong> keys)
    {
        ArgumentException.ThrowIfNullOrEmpty(purpose);
        var key = Mix(seed ^ Fnv1a(purpose));
        foreach (var part in keys)
        {
            key = Mix(key ^ Mix(unchecked(part + Gamma)));
        }

        return new RandomStream(key, 0);
    }

    /// <summary>Resumes a stream at a saved position.</summary>
    public static RandomStream Resume(ulong key, ulong position) => new(key, position);

    // Wrapping arithmetic is the algorithm; Debug builds otherwise check overflow.
    public ulong NextUInt64() => Mix(unchecked(Key + (++Position * Gamma)));

    /// <summary>Uniform in [0, 1) with 53 random bits.</summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    /// <summary>Uniform integer in [0, <paramref name="exclusiveMax"/>) without modulo bias (Lemire's method).</summary>
    public ulong NextBelow(ulong exclusiveMax)
    {
        ArgumentOutOfRangeException.ThrowIfZero(exclusiveMax);
        var threshold = unchecked(0 - exclusiveMax) % exclusiveMax;
        while (true)
        {
            var product = (UInt128)NextUInt64() * exclusiveMax;
            if ((ulong)product >= threshold)
            {
                return (ulong)(product >> 64);
            }
        }
    }

    /// <summary>True with probability <paramref name="probability"/> (0–1).</summary>
    public bool NextChance(double probability) =>
        probability is >= 0 and <= 1 ? NextDouble() < probability : throw new ArgumentOutOfRangeException(nameof(probability));

    /// <summary>SplitMix64 finalizer (Steele, Lea and Flood 2014).</summary>
    private static ulong Mix(ulong z)
    {
        unchecked
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
            return z ^ (z >> 31);
        }
    }

    /// <summary>64-bit FNV-1a over UTF-8: a stable, platform-independent name hash.</summary>
    private static ulong Fnv1a(string text)
    {
        var hash = 0xCBF29CE484222325UL;
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            hash = unchecked((hash ^ b) * 0x100000001B3UL);
        }

        return hash;
    }
}
