using Sovereigns.Simulation.Random;

namespace Sovereigns.Simulation.Tests;

/// <summary>SOV-P02-T06: deterministic random streams keyed by seed, purpose, place and identity (ADR-0007).</summary>
public sealed class RandomStreamTests
{
    [Fact]
    public void StreamMatchesTheDocumentedAlgorithm()
    {
        // Golden values from tools/random_stream_reference.py, an independent implementation of ADR-0007; they hold on every platform.
        var stream = RandomStream.For(20261014, "genesis.rivers", 7, 42);

        Assert.Equal(0x348619051c7a2cd4UL, stream.Key);
        Assert.Equal(0x905d30fcafb27273UL, stream.NextUInt64());
        Assert.Equal(0x5c59462207abf377UL, stream.NextUInt64());
        Assert.Equal(0xe28c47e47e364887UL, stream.NextUInt64());
        Assert.Equal(0xa3643ca19fa7afeeUL, RandomStream.For(1, "calendar.test").NextUInt64());
    }

    [Fact]
    public void EveryKeyPartSelectsADifferentStream()
    {
        var reference = RandomStream.For(1, "purpose", 2, 3).NextUInt64();

        Assert.Equal(reference, RandomStream.For(1, "purpose", 2, 3).NextUInt64());
        Assert.NotEqual(reference, RandomStream.For(2, "purpose", 2, 3).NextUInt64());
        Assert.NotEqual(reference, RandomStream.For(1, "purposf", 2, 3).NextUInt64());
        Assert.NotEqual(reference, RandomStream.For(1, "purpose", 3, 2).NextUInt64());
        Assert.NotEqual(reference, RandomStream.For(1, "purpose", 2).NextUInt64());
    }

    [Fact]
    public void DrawsDoNotDependOnOtherStreamsConsumption()
    {
        var quiet = RandomStream.For(9, "rivers");
        var busy = RandomStream.For(9, "weather");
        for (var i = 0; i < 1_000; i++)
        {
            busy.NextUInt64();
        }

        Assert.Equal(RandomStream.For(9, "rivers").NextUInt64(), quiet.NextUInt64());
    }

    [Fact]
    public void ASavedPositionResumesTheSameSequence()
    {
        var stream = RandomStream.For(5, "crops", 11);
        stream.NextUInt64();
        stream.NextUInt64();
        var resumed = RandomStream.Resume(stream.Key, stream.Position);

        Assert.Equal(stream.NextUInt64(), resumed.NextUInt64());
    }

    [Fact]
    public void BoundedDrawsStayInRangeAndAreRoughlyUniform()
    {
        var stream = RandomStream.For(3, "uniformity");
        var buckets = new int[10];
        for (var i = 0; i < 100_000; i++)
        {
            var value = stream.NextBelow(10);
            Assert.InRange(value, 0UL, 9UL);
            buckets[value]++;
            var unit = stream.NextDouble();
            Assert.InRange(unit, 0.0, 0.9999999999999999);
        }

        // Each bucket expects 10 000; ±5 % is far outside random variation (σ ≈ 95) yet catches gross bias.
        Assert.All(buckets, count => Assert.InRange(count, 9_500, 10_500));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.NextBelow(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.NextChance(1.5));
    }
}
