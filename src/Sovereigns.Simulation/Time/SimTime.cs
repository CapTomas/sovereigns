using System.Globalization;

namespace Sovereigns.Simulation.Time;

/// <summary>
/// Authoritative simulation timestamp: signed milliseconds since the world epoch (ADR-0004 §4).
/// The calendar, epoch meaning and local solar time arrive with SOV-P02-T02.
/// </summary>
public readonly record struct SimTime(long Milliseconds) : IComparable<SimTime>
{
    public static readonly SimTime Zero = new(0);

    public static SimTime FromSeconds(long seconds) => new(checked(seconds * 1000));

    public SimTime Plus(long milliseconds) => new(checked(Milliseconds + milliseconds));

    public int CompareTo(SimTime other) => Milliseconds.CompareTo(other.Milliseconds);

    public static bool operator <(SimTime left, SimTime right) => left.Milliseconds < right.Milliseconds;
    public static bool operator >(SimTime left, SimTime right) => left.Milliseconds > right.Milliseconds;
    public static bool operator <=(SimTime left, SimTime right) => left.Milliseconds <= right.Milliseconds;
    public static bool operator >=(SimTime left, SimTime right) => left.Milliseconds >= right.Milliseconds;

    /// <summary>Elapsed time as <c>[-]d.hh:mm:ss.fff</c> since the epoch; not a calendar date.</summary>
    public string ToElapsedString()
    {
        // UInt128 keeps long.MinValue representable after negation.
        var total = (UInt128)(Milliseconds < 0 ? -(Int128)Milliseconds : Milliseconds);
        var days = total / 86_400_000;
        var rest = (ulong)(total % 86_400_000);
        return string.Create(CultureInfo.InvariantCulture,
            $"{(Milliseconds < 0 ? "-" : "")}{days}.{rest / 3_600_000:00}:{rest / 60_000 % 60:00}:{rest / 1000 % 60:00}.{rest % 1000:000}");
    }

    public override string ToString() => Milliseconds.ToString(CultureInfo.InvariantCulture) + " ms";
}
