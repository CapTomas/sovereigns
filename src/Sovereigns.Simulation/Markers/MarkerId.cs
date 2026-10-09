using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sovereigns.Simulation.Markers;

/// <summary>Developer marker ID from the world's per-kind counter; 0 means none (ADR-0004 §5).</summary>
[JsonConverter(typeof(MarkerIdJsonConverter))]
public readonly record struct MarkerId(ulong Value)
{
    public override string ToString() => "marker:" + Value.ToString(CultureInfo.InvariantCulture);
}

internal sealed class MarkerIdJsonConverter : JsonConverter<MarkerId>
{
    public override MarkerId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetUInt64());

    public override void Write(Utf8JsonWriter writer, MarkerId value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}
