using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sovereigns.Simulation.Content;

/// <summary>
/// Stable content identifier <c>namespace:kind/name</c> (ADR-0004 §5). Each segment uses lowercase
/// a–z, 0–9 and underscore, starting with a letter.
/// </summary>
[JsonConverter(typeof(ContentIdJsonConverter))]
public readonly record struct ContentId(string Namespace, string Kind, string Name)
{
    public static ContentId Parse(string text) =>
        TryParse(text, out var id, out var error) ? id : throw new FormatException(error);

    public static bool TryParse(string? text, out ContentId id, out string error)
    {
        id = default;
        if (string.IsNullOrEmpty(text))
        {
            error = "content ID is empty; expected namespace:kind/name, for example core:scenario/empty_world";
            return false;
        }

        var colon = text.IndexOf(':', StringComparison.Ordinal);
        var slash = colon < 0 ? -1 : text.IndexOf('/', colon + 1);
        if (colon < 0 || slash < 0 || text.IndexOf(':', colon + 1) >= 0 || text.IndexOf('/', slash + 1) >= 0)
        {
            error = $"'{text}' is not namespace:kind/name, for example core:scenario/empty_world";
            return false;
        }

        var parts = new[] { text[..colon], text[(colon + 1)..slash], text[(slash + 1)..] };
        string[] names = ["namespace", "kind", "name"];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!IsValidSegment(parts[i]))
            {
                error = $"'{text}' has an invalid {names[i]} segment '{parts[i]}'; use lowercase a-z, 0-9 and _, starting with a letter";
                return false;
            }
        }

        id = new ContentId(parts[0], parts[1], parts[2]);
        error = "";
        return true;
    }

    public static bool IsValidSegment(string segment) =>
        segment.Length > 0 && segment[0] is >= 'a' and <= 'z' &&
        segment.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');

    public override string ToString() => $"{Namespace}:{Kind}/{Name}";
}

internal sealed class ContentIdJsonConverter : JsonConverter<ContentId>
{
    public override ContentId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"expected a content ID string, found {reader.TokenType}");
        }

        return ContentId.TryParse(reader.GetString(), out var id, out var error) ? id : throw new JsonException(error);
    }

    public override void Write(Utf8JsonWriter writer, ContentId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
