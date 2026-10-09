using System.Globalization;

namespace Sovereigns.Client.Tests;

/// <summary>Raised by <see cref="Check"/> when an expectation does not hold.</summary>
public sealed class CheckFailedException(string message) : Exception(message);

/// <summary>Minimal assertions for the Godot-hosted client tests.</summary>
public static class Check
{
    public static void True(bool condition, string what)
    {
        if (!condition)
        {
            throw new CheckFailedException(what);
        }
    }

    public static void Equal<T>(T expected, T actual, string what)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new CheckFailedException($"{what}: expected {expected}, got {actual}");
        }
    }

    public static void Near(double expected, double actual, double tolerance, string what)
    {
        if (!(Math.Abs(expected - actual) <= tolerance))
        {
            throw new CheckFailedException(string.Create(CultureInfo.InvariantCulture, $"{what}: expected {expected} +/- {tolerance}, got {actual}"));
        }
    }

    public static void Contains(string text, string expected, string what)
    {
        if (!text.Contains(expected, StringComparison.Ordinal))
        {
            throw new CheckFailedException($"{what}: '{expected}' not found in:\n{text}");
        }
    }
}
