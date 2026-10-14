using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sovereigns.Simulation.Ids;

/// <summary>A kind of entity with its own ID space; <see cref="Name"/> prefixes IDs in logs, such as <c>marker:3</c>.</summary>
public interface IEntityKind
{
    static abstract string Name { get; }
}

/// <summary>
/// Stable entity ID (ADR-0004 §5): an unsigned 64-bit value from the world's per-kind counter, never reused, with
/// 0 meaning none. The kind is part of the type, so IDs of different kinds cannot be mixed up.
/// </summary>
[JsonConverter(typeof(EntityIdJsonConverterFactory))]
public readonly record struct EntityId<TKind>(ulong Value)
    where TKind : IEntityKind
{
    public bool IsNone => Value == 0;

    public override string ToString() => TKind.Name + ":" + Value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>The entity kinds of ADR-0004 §5 plus kernel and developer kinds. Names are persisted; never rename one.</summary>
public static class Kind
{
    /// <summary>Every kind name; saves may only hold counters for these.</summary>
    public static IReadOnlyList<string> Names { get; } =
    [
        NaturalFeature.Name, Settlement.Name, Facility.Name, Army.Name, Regiment.Name, Polity.Name,
        TacticalObject.Name, Event.Name, Marker.Name, Breakpoint.Name,
    ];

    public sealed class NaturalFeature : IEntityKind
    {
        public static string Name => "feature";
    }

    public sealed class Settlement : IEntityKind
    {
        public static string Name => "settlement";
    }

    public sealed class Facility : IEntityKind
    {
        public static string Name => "facility";
    }

    public sealed class Army : IEntityKind
    {
        public static string Name => "army";
    }

    public sealed class Regiment : IEntityKind
    {
        public static string Name => "regiment";
    }

    public sealed class Polity : IEntityKind
    {
        public static string Name => "polity";
    }

    public sealed class TacticalObject : IEntityKind
    {
        public static string Name => "tactical";
    }

    /// <summary>World-event ledger entries.</summary>
    public sealed class Event : IEntityKind
    {
        public static string Name => "event";
    }

    /// <summary>Developer markers pinned to positions.</summary>
    public sealed class Marker : IEntityKind
    {
        public static string Name => "marker";
    }

    /// <summary>Developer breakpoints: scheduled decision interrupts.</summary>
    public sealed class Breakpoint : IEntityKind
    {
        public static string Name => "breakpoint";
    }
}

internal sealed class EntityIdJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(EntityId<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(EntityIdJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;

    private sealed class EntityIdJsonConverter<TKind> : JsonConverter<EntityId<TKind>>
        where TKind : IEntityKind
    {
        public override EntityId<TKind> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetUInt64());

        public override void Write(Utf8JsonWriter writer, EntityId<TKind> value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value.Value);
    }
}
