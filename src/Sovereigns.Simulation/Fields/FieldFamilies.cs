namespace Sovereigns.Simulation.Fields;

public enum FieldAvailability
{
    /// <summary>The owning subsystem is not implemented; no value exists.</summary>
    Unavailable,

    /// <summary>Some quantities of the family exist; the rest are explicitly unavailable.</summary>
    Partial,

    Available,
}

/// <summary>
/// One spec §3.2 world data family, its authoritative writer from the system map, and the phase
/// that first implements it. SOV-P03-T02 replaces this availability list with full field contracts.
/// </summary>
public sealed record FieldFamily(string Key, string Name, string Owner, string FirstImplementedBy);

/// <summary>What this build can report about the spec §3.2 field families.</summary>
public static class FieldFamilies
{
    public static readonly FieldFamily GeographicReference =
        new("geographic_reference", "Geographic reference", "World foundation", "Phase 02");

    public static IReadOnlyList<FieldFamily> All { get; } =
    [
        GeographicReference,
        new("bedrock_landform", "Bedrock and landform", "World foundation", "Phase 04"),
        new("terrain_derivatives", "Terrain derivatives", "World foundation", "Phase 04"),
        new("surface_material", "Surface material", "World foundation; Ecology/soil", "Phase 04"),
        new("atmospheric_dynamics", "Atmospheric dynamics", "Atmosphere", "Phase 07"),
        new("atmospheric_water", "Atmospheric water", "Atmosphere", "Phase 07"),
        new("radiative_thermal", "Radiative/thermal state", "Atmosphere", "Phase 06"),
        new("surface_water", "Surface water and catchment", "Hydrology", "Phase 05"),
        new("subsurface_water", "Subsurface/soil water", "Hydrology", "Phase 08"),
        new("snow_frozen", "Snow and frozen state", "Hydrology", "Phase 09"),
        new("climate_reference", "Climate reference", "Atmosphere", "Phase 07"),
        new("ecosystems", "Ecosystems", "Ecology/soil", "Phase 10"),
        new("soil_productivity", "Soil productivity", "Ecology/soil", "Phase 10"),
        new("geological_resources", "Geological resources", "World foundation", "Phase 18"),
        new("human_landscape", "Human landscape", "Economy/population; Politics/government; Infrastructure", "Phase 15"),
        new("transient_local", "Transient local conditions", "Hydrology; Ecology/soil; Tactical warfare", "Phase 09"),
    ];

    public static FieldAvailability AvailabilityOf(FieldFamily family) =>
        family == GeographicReference ? FieldAvailability.Partial : FieldAvailability.Unavailable;

    public static string UnavailableReason(FieldFamily family)
    {
        ArgumentNullException.ThrowIfNull(family);
        return $"unavailable: {family.Owner} implements this family in {family.FirstImplementedBy}";
    }
}
