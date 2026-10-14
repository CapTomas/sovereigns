using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Tests;

public sealed class TimeTests
{
    [Theory]
    [InlineData(0, "0.00:00:00.000")]
    [InlineData(3_723_004, "0.01:02:03.004")]
    [InlineData(90_061_001, "1.01:01:01.001")]
    [InlineData(-1_500, "-0.00:00:01.500")]
    public void ElapsedStringShowsDaysHoursMinutesSecondsMilliseconds(long milliseconds, string expected) =>
        Assert.Equal(expected, new SimTime(milliseconds).ToElapsedString());

    [Fact]
    public void ElapsedStringCoversTheWholeInt64Range()
    {
        // ±292 million years must format without overflow (ADR-0004 §4).
        Assert.StartsWith("-106751991167.", new SimTime(long.MinValue).ToElapsedString(), StringComparison.Ordinal);
        Assert.StartsWith("106751991167.", new SimTime(long.MaxValue).ToElapsedString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ArithmeticOverflowIsAnErrorNotAWrap() =>
        Assert.Throws<OverflowException>(() => new SimTime(long.MaxValue).Plus(1));
}
