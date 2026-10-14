using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sovereigns.Simulation.Identity;

/// <summary>
/// Durable 128-bit identity of one world (SOV-P02-T01), written as 32 lowercase hex digits. The host creates it
/// once from operating-system randomness when a world is created; the simulation never generates or depends on
/// it, so two worlds from the same seed and commands are equal in state but distinct in identity.
/// </summary>
[JsonConverter(typeof(WorldIdJsonConverter))]
public readonly record struct WorldId(UInt128 Value)
{
    public static WorldId FromBytes(ReadOnlySpan<byte> bytes) =>
        bytes.Length == 16 ? new WorldId(new UInt128(BitConverter.ToUInt64(bytes[..8]), BitConverter.ToUInt64(bytes[8..]))) : throw new ArgumentException("a world ID is 16 bytes", nameof(bytes));

    public static bool TryParse(string? text, out WorldId id)
    {
        id = default;
        if (text is not { Length: 32 } || !text.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f'))
        {
            return false;
        }

        id = new WorldId(UInt128.Parse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
        return true;
    }

    public override string ToString() => Value.ToString("x32", CultureInfo.InvariantCulture);
}

internal sealed class WorldIdJsonConverter : JsonConverter<WorldId>
{
    public override WorldId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        WorldId.TryParse(reader.GetString(), out var id) ? id : throw new JsonException("a world ID is 32 lowercase hex digits");

    public override void Write(Utf8JsonWriter writer, WorldId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
