using Sovereigns.Simulation.Fields;

namespace Sovereigns.Simulation.Atlas;

/// <summary>
/// Every field of the world atlas (SOV-P03-T02, T03; ADR-0009), grouped by spec §3.2 family. Fields of later phases
/// carry their intended contract and stay unavailable until their phase implements a provider; that phase may refine
/// the contract. Keys are persisted: never rename or reuse one.
/// </summary>
public static class FieldCatalog
{
    /// <summary>Developer tools: annotations for exercising and inspecting the atlas; not physical state.</summary>
    public static readonly FieldFamily Developer = new("developer", "Developer tools", "Developer tools", "Phase 03");

    public static readonly FieldDefinition Latitude = Def("latitude", FieldFamilies.GeographicReference, "Latitude", "rad", FieldKind.Scalar,
        FieldClass.Derived, GridLevel.Regional, -Math.PI / 2, Math.PI / 2, "outside the world frame", "the equator", "float64; exact",
        "World foundation", [], "Phase 02");

    public static readonly FieldDefinition LocalSolarTime = Def("local_solar_time", FieldFamilies.GeographicReference, "Local mean solar time", "s",
        FieldKind.Scalar, FieldClass.Derived, GridLevel.Regional, 0, 86_400, "outside the world frame", "local mean midnight", "float64; 1 ms",
        "Simulation scheduler", [], "Phase 02");

    public static readonly FieldDefinition Scratch = Def("scratch", Developer, "Developer scratch", "1", FieldKind.Scalar, FieldClass.Stored,
        GridLevel.Local, 0, 1, "outside the world frame", "unpainted", "float64; exact", "Developer tools", [], "Phase 03");

    public static readonly FieldDefinition ScratchGradient = Def("scratch_gradient", Developer, "Developer scratch gradient", "m-1", FieldKind.Vector,
        FieldClass.Derived, GridLevel.Local, -1, 1, "outside the world frame", "flat scratch", "float64; central differences at the native level",
        "Developer tools", ["scratch"], "Phase 03");

    public static IReadOnlyList<FieldDefinition> All { get; } =
    [
        Latitude,
        LocalSolarTime,
        Def("elevation", Family("bedrock_landform"), "Elevation above datum", "m", FieldKind.Scalar, FieldClass.Base, GridLevel.Regional,
            -11_000, 9_000, "no generated terrain", "the sea datum", "float32; ≤ 0.05 m", "World foundation", [], "Phase 04"),
        Def("lithology", Family("bedrock_landform"), "Lithology", "class", FieldKind.Category, FieldClass.Base, GridLevel.Regional,
            0, 255, "no generated geology", "class 0: unclassified rock", "uint8 class", "World foundation", [], "Phase 04"),
        Def("slope", Family("terrain_derivatives"), "Slope", "rad", FieldKind.Scalar, FieldClass.Derived, GridLevel.Regional,
            0, Math.PI / 2, "no elevation", "level ground", "float64", "World foundation", ["elevation"], "Phase 04"),
        Def("aspect", Family("terrain_derivatives"), "Downslope direction, counter-clockwise from east", "rad", FieldKind.Scalar,
            FieldClass.Derived, GridLevel.Regional, 0, 2 * Math.PI, "no elevation, or level ground with no downslope direction", "downslope towards the east", "float64", "World foundation", ["elevation"], "Phase 04"),
        Def("curvature", Family("terrain_derivatives"), "Surface curvature", "m-1", FieldKind.Scalar, FieldClass.Derived, GridLevel.Regional,
            -1, 1, "no elevation", "planar surface", "float64", "World foundation", ["elevation"], "Phase 04"),
        Def("surface_material", Family("surface_material"), "Surface material", "class", FieldKind.Category, FieldClass.Base, GridLevel.Regional,
            0, 255, "no generated material", "class 0: bare rock", "uint8 class", "World foundation", ["lithology"], "Phase 04"),
        Def("soil_depth", Family("surface_material"), "Soil depth", "m", FieldKind.Scalar, FieldClass.Base, GridLevel.Regional,
            0, 50, "no generated material", "bare rock, no soil", "float32; ≤ 0.01 m", "World foundation", ["lithology", "slope"], "Phase 04"),
        Def("air_temperature", Family("atmospheric_dynamics"), "Air temperature at 2 m", "K", FieldKind.Scalar, FieldClass.Dynamic,
            GridLevel.Macro, 180, 340, "no atmosphere state", "0 K is outside the valid range; water freezes at 273.15 K", "float32; ≤ 0.01 K", "Atmosphere", ["elevation"], "Phase 07"),
        Def("wind", Family("atmospheric_dynamics"), "Wind at 10 m (towards)", "m s-1", FieldKind.Vector, FieldClass.Dynamic, GridLevel.Macro,
            -100, 100, "no atmosphere state", "calm air: no horizontal motion at 10 m", "float32; ≤ 0.01 m s-1", "Atmosphere", ["elevation"], "Phase 07"),
        Def("pressure_anomaly", Family("atmospheric_dynamics"), "Sea-level pressure anomaly", "Pa", FieldKind.Scalar, FieldClass.Dynamic,
            GridLevel.Macro, -10_000, 10_000, "no atmosphere state", "standard sea-level pressure", "float32; ≤ 1 Pa", "Atmosphere", [], "Phase 07"),
        Def("specific_humidity", Family("atmospheric_water"), "Specific humidity at 2 m", "kg kg-1", FieldKind.Scalar, FieldClass.Dynamic,
            GridLevel.Macro, 0, 0.05, "no atmosphere state", "perfectly dry air: no water vapour", "float32", "Atmosphere", ["air_temperature"], "Phase 07"),
        Def("cloud_cover", Family("atmospheric_water"), "Cloud cover", "1", FieldKind.Scalar, FieldClass.Dynamic, GridLevel.Macro,
            0, 1, "no atmosphere state", "clear sky", "float32", "Atmosphere", ["specific_humidity"], "Phase 07"),
        Def("precipitation_rate", Family("atmospheric_water"), "Precipitation rate", "kg m-2 s-1", FieldKind.Scalar, FieldClass.Dynamic,
            GridLevel.Macro, 0, 0.1, "no atmosphere state", "no precipitation reaching the ground", "float32", "Atmosphere", ["cloud_cover"], "Phase 07"),
        Def("shortwave_down", Family("radiative_thermal"), "Downward shortwave radiation at the surface", "W m-2", FieldKind.Scalar,
            FieldClass.Dynamic, GridLevel.Macro, 0, 1_400, "no atmosphere state", "no sunlight: night or full shading", "float32", "Atmosphere", ["cloud_cover"], "Phase 06"),
        Def("surface_temperature", Family("radiative_thermal"), "Surface temperature", "K", FieldKind.Scalar, FieldClass.Dynamic,
            GridLevel.Regional, 180, 350, "no thermal state", "0 K is outside the valid range; water freezes at 273.15 K", "float32; ≤ 0.01 K", "Atmosphere", ["shortwave_down"], "Phase 06"),
        Def("surface_water_depth", Family("surface_water"), "Surface water depth", "m", FieldKind.Scalar, FieldClass.Dynamic,
            GridLevel.Regional, 0, 100, "no hydrology state", "no surface water", "float32; ≤ 0.001 m", "Hydrology", ["elevation"], "Phase 05", Refinement.Conserve),
        Def("flow_accumulation", Family("surface_water"), "Upstream contributing area", "m2", FieldKind.Scalar, FieldClass.Derived,
            GridLevel.Regional, 0, 1e13, "no drainage computed", "no upstream area: a divide", "float64", "Hydrology", ["elevation"], "Phase 05"),
        Def("soil_water", Family("subsurface_water"), "Soil water", "kg m-2", FieldKind.Scalar, FieldClass.Dynamic, GridLevel.Regional,
            0, 2_000, "no hydrology state, or no soil", "dry soil: no stored water", "float32; ≤ 0.01 kg m-2", "Hydrology", ["soil_depth"], "Phase 08", Refinement.Conserve),
        Def("groundwater_storage", Family("subsurface_water"), "Groundwater storage", "kg m-2", FieldKind.Scalar, FieldClass.Dynamic,
            GridLevel.Regional, 0, 1e6, "no hydrology state, or no aquifer", "empty aquifer", "float32", "Hydrology", ["lithology"], "Phase 08", Refinement.Conserve),
        Def("snow_water_equivalent", Family("snow_frozen"), "Snow water equivalent", "kg m-2", FieldKind.Scalar, FieldClass.Dynamic,
            GridLevel.Regional, 0, 20_000, "no hydrology state", "no snow", "float32; ≤ 0.01 kg m-2", "Hydrology", ["precipitation_rate"], "Phase 09", Refinement.Conserve),
        Def("snow_depth", Family("snow_frozen"), "Snow depth", "m", FieldKind.Scalar, FieldClass.Dynamic, GridLevel.Regional,
            0, 30, "no hydrology state", "no snow", "float32; ≤ 0.001 m", "Hydrology", ["snow_water_equivalent"], "Phase 09"),
        Def("frozen_soil_fraction", Family("snow_frozen"), "Frozen soil fraction", "1", FieldKind.Scalar, FieldClass.Dynamic,
            GridLevel.Regional, 0, 1, "no hydrology state", "unfrozen ground", "float32", "Hydrology", ["surface_temperature"], "Phase 09"),
        Def("mean_air_temperature", Family("climate_reference"), "Long-run mean air temperature", "K", FieldKind.Scalar, FieldClass.Base,
            GridLevel.Macro, 200, 330, "no climate computed", "0 K is outside the valid range; water freezes at 273.15 K", "float32", "Atmosphere", ["elevation"], "Phase 07"),
        Def("mean_precipitation_rate", Family("climate_reference"), "Long-run mean precipitation rate", "kg m-2 s-1", FieldKind.Scalar,
            FieldClass.Base, GridLevel.Macro, 0, 0.001, "no climate computed", "no long-run precipitation", "float32", "Atmosphere", ["elevation"], "Phase 07"),
        Def("biomass", Family("ecosystems"), "Living biomass", "kg m-2", FieldKind.Scalar, FieldClass.Dynamic, GridLevel.Regional,
            0, 100, "no ecology state", "bare ground: no living biomass", "float32", "Ecology/soil", ["soil_water"], "Phase 10", Refinement.Conserve),
        Def("tree_cover", Family("ecosystems"), "Tree cover", "1", FieldKind.Scalar, FieldClass.Dynamic, GridLevel.Regional,
            0, 1, "no ecology state", "treeless", "float32", "Ecology/soil", ["biomass"], "Phase 10"),
        Def("soil_fertility", Family("soil_productivity"), "Relative soil fertility", "1", FieldKind.Scalar, FieldClass.Dynamic,
            GridLevel.Regional, 0, 1, "no ecology state", "infertile", "float32", "Ecology/soil", ["soil_depth"], "Phase 10"),
        Def("ore_grade", Family("geological_resources"), "Ore mass fraction", "kg kg-1", FieldKind.Scalar, FieldClass.Base,
            GridLevel.Regional, 0, 1, "no generated geology", "no ore", "float32", "World foundation", ["lithology"], "Phase 18", interpolation: Interpolation.Nearest),
        Def("land_use", Family("human_landscape"), "Land use", "class", FieldKind.Category, FieldClass.Stored, GridLevel.Local,
            0, 255, "no land use recorded", "class 0: unused land", "uint8 class", "Economy/population", [], "Phase 15"),
        Def("mud_depth", Family("transient_local"), "Mud depth", "m", FieldKind.Scalar, FieldClass.Dynamic, GridLevel.Local,
            0, 2, "no hydrology state", "firm ground, no mud", "float32; ≤ 0.001 m", "Hydrology", ["soil_water"], "Phase 09"),
        Def("standing_water", Family("transient_local"), "Standing water", "kg m-2", FieldKind.Scalar, FieldClass.Dynamic, GridLevel.Local,
            0, 1_000, "no hydrology state", "no standing water", "float32", "Hydrology", ["surface_water_depth"], "Phase 09", Refinement.Conserve),
        Scratch,
        ScratchGradient,
    ];

    public static FieldDefinition Get(string key) =>
        All.FirstOrDefault(field => field.Key == key) ?? throw new KeyNotFoundException($"no field '{key}' in the atlas catalog");

    public static IEnumerable<FieldDefinition> InFamily(FieldFamily family) => All.Where(field => field.Family == family);

    private static FieldFamily Family(string key) => FieldFamilies.All.Single(family => family.Key == key);

    private static FieldDefinition Def(string key, FieldFamily family, string name, string unit, FieldKind kind, FieldClass fieldClass,
        GridLevel level, double min, double max, string noData, string zero, string precision, string owner, IReadOnlyList<string> dependsOn,
        string implementedBy, Refinement refinement = Refinement.Interpolate, Interpolation? interpolation = null) =>
        new(key, family, name, unit, kind, fieldClass, level,
            interpolation ?? (kind == FieldKind.Category ? Interpolation.Nearest : Interpolation.Bilinear),
            refinement, min, max, noData, zero, precision, owner, dependsOn, implementedBy);
}
