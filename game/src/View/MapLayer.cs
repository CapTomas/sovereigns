using Sovereigns.Simulation.Atlas;
using Sovereigns.Simulation.Fields;
using Sovereigns.Simulation.Geometry;

namespace Sovereigns.Client.View;

/// <summary>
/// One entry in the layer list: the world frame, the developer markers, or one atlas field. A field layer is
/// available when the atlas has a provider for it and is then drawn as an overlay sampled from atlas queries; an
/// unavailable one carries the owner-and-phase reason from the simulation and is drawn only as an explicit no-data state.
/// </summary>
public sealed record MapLayer(string Key, string Name, FieldFamily? Family, string? UnavailableReason, FieldDefinition? Field = null)
{
    public const string WorldFrameKey = "world_frame";
    public const string DeveloperMarkersKey = "developer_markers";

    public bool IsAvailable => UnavailableReason is null;

    /// <summary>True for a layer that draws a field overlay from the atlas.</summary>
    public bool IsFieldOverlay => Field is not null && IsAvailable;

    public string ListText => IsAvailable || Field is null ? Name : $"{Name} (unavailable — {Field.Owner}, {Field.ImplementedBy})";

    /// <summary>Static explanation; the legend adds level, range and scale for an available field.</summary>
    public string Legend => UnavailableReason is { } reason
        ? $"No data — {reason}"
        : Key == DeveloperMarkersKey
            ? "pins from PlaceMarker commands, each labelled with its label and ID."
            : Field is not null
                ? $"{Field.Name}, {Field.Unit}; owner {Field.Owner}."
                : "world extent outline, adaptive grid, scale bar and north arrow.";
}

/// <summary>The layer list for one world: the two base layers, then every atlas field grouped by spec §3.2 family.</summary>
public static class MapLayers
{
    /// <summary>
    /// Layers in display order: families with a field this build can draw come first (so the usable layers are at the
    /// top of the list), then the rest in specification order; fields keep catalogue order inside a family.
    /// </summary>
    public static IReadOnlyList<MapLayer> For(FieldAtlas atlas)
    {
        ArgumentNullException.ThrowIfNull(atlas);
        var layers = new List<MapLayer>
        {
            new(MapLayer.WorldFrameKey, "World frame and grid", null, null),
            new(MapLayer.DeveloperMarkersKey, "Developer markers", null, null),
        };

        var families = FieldFamilies.All.Append(FieldCatalog.Developer)
            .Select(family => (Family: family, Fields: FieldCatalog.InFamily(family).ToList()))
            .Where(group => group.Fields.Count > 0)
            .OrderBy(group => group.Fields.Any(field => atlas.IsAvailable(field.Key)) ? 0 : 1);
        foreach (var (family, fields) in families)
        {
            layers.AddRange(fields.Select(field => new MapLayer(field.Key, field.Name, family,
                atlas.IsAvailable(field.Key) ? null : atlas.Sample(field.Key, new WorldPoint(0, 0)).UnavailableReason, field)));
        }

        return layers;
    }

    /// <summary>Index of the layer with this key or case-insensitive name, or -1.</summary>
    public static int IndexOf(IReadOnlyList<MapLayer> layers, string text)
    {
        ArgumentNullException.ThrowIfNull(layers);
        for (var i = 0; i < layers.Count; i++)
        {
            if (layers[i].Key.Equals(text, StringComparison.OrdinalIgnoreCase) || layers[i].Name.Equals(text, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
