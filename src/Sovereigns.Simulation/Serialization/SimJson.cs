using System.Text.Json;
using System.Text.Json.Serialization;
using Sovereigns.Simulation.Time;

namespace Sovereigns.Simulation.Serialization;

/// <summary>Strict JSON settings shared by content, command journals, logs and failure reports.</summary>
public static class SimJson
{
    /// <summary>Unknown members, missing required values and nulls in non-nullable members are errors.</summary>
    public static readonly JsonSerializerOptions Options = Create(indented: false);

    public static readonly JsonSerializerOptions Indented = Create(indented: true);

    private static JsonSerializerOptions Create(bool indented)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // A rejected command may carry NaN or infinity; journals and reports must still record it.
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            WriteIndented = indented,
        };
        options.Converters.Add(new SimTimeJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false));
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    private sealed class SimTimeJsonConverter : JsonConverter<SimTime>
    {
        public override SimTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetInt64());

        public override void Write(Utf8JsonWriter writer, SimTime value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.Milliseconds);
    }
}
