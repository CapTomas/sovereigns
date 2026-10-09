using Sovereigns.Simulation.Fields;

namespace Sovereigns.Client.View;

/// <summary>
/// One entry in the layer list. A layer is available when this build can draw real data for it; an unavailable
/// layer carries the owner-and-phase reason from the simulation and is drawn only as an explicit no-data state.
/// </summary>
public sealed record MapLayer(string Key, string Name, FieldFamily? Family, string? UnavailableReason)
{
    public const string WorldFrameKey = "world_frame";
    public const string DeveloperMarkersKey = "developer_markers";

    public bool IsAvailable => UnavailableReason is null;

    public string ListText => IsAvailable ? Name : $"{Name} (unavailable)";

    public string Legend => UnavailableReason is { } reason
        ? $"No data — {reason}"
        : Key == DeveloperMarkersKey
            ? "pins from PlaceMarker commands, each labelled with its label and ID."
            : "world extent outline, adaptive grid, scale bar and north arrow.";
}

/// <summary>The layer list: the two layers this build can draw, then every spec §3.2 field family except geographic reference.</summary>
public static class MapLayers
{
    public static IReadOnlyList<MapLayer> All { get; } =
    [
        new(MapLayer.WorldFrameKey, "World frame and grid", null, null),
        new(MapLayer.DeveloperMarkersKey, "Developer markers", null, null),
        .. FieldFamilies.All
            .Where(family => family != FieldFamilies.GeographicReference)
            .Select(family => new MapLayer(family.Key, family.Name, family, FieldFamilies.UnavailableReason(family))),
    ];

    /// <summary>Index of the layer with this key or case-insensitive name, or -1.</summary>
    public static int IndexOf(string text)
    {
        for (var i = 0; i < All.Count; i++)
        {
            if (All[i].Key.Equals(text, StringComparison.OrdinalIgnoreCase) || All[i].Name.Equals(text, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
