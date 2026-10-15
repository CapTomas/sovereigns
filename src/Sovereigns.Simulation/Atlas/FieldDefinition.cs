using Sovereigns.Simulation.Fields;

namespace Sovereigns.Simulation.Atlas;

public enum FieldKind
{
    /// <summary>One number per point.</summary>
    Scalar,

    /// <summary>An (east, north) pair, such as wind or a gradient (ADR-0004 §1).</summary>
    Vector,

    /// <summary>A class index, such as lithology or land use; never interpolated.</summary>
    Category,
}

/// <summary>Spec §3.3 classification of a field's values.</summary>
public enum FieldClass
{
    /// <summary>Generated once from seed and generator version; regenerable, so only changes to it are saved.</summary>
    Base,

    /// <summary>Authoritative and mutable, written by its owner; saved as overrides over a base value.</summary>
    Stored,

    /// <summary>Evolved over time by a scheduled system; saved at the save time.</summary>
    Dynamic,

    /// <summary>Computed from other fields or from position and time; never saved.</summary>
    Derived,
}

public enum Interpolation
{
    /// <summary>Bilinear between the four nearest cell centres; continuous across cells and chunks.</summary>
    Bilinear,

    /// <summary>The containing cell's value; for categories and step-like quantities.</summary>
    Nearest,
}

/// <summary>How a value is obtained at a level finer than the field's native one (SOV-P03-T09).</summary>
public enum Refinement
{
    /// <summary>Interpolate: continuous; the mean over a native cell can differ slightly from its value.</summary>
    Interpolate,

    /// <summary>
    /// Conserve: interpolate, then shift each native cell's refined values so their mean equals the native value
    /// exactly. Used for stored quantities such as water and biomass; values may step at native cell edges.
    /// </summary>
    Conserve,
}

/// <summary>
/// The contract of one field (SOV-P03-T02, T03; ADR-0009): what it means, its SI unit (ADR-0004), valid range,
/// no-data meaning, interpolation and refinement, native resolution, storage precision, owner and dependencies.
/// Fields of later phases are listed with their intended contract and stay unavailable until implemented; the
/// owning phase may refine the contract with an ADR amendment.
/// </summary>
/// <param name="Key">Stable lowercase name, persisted in saves.</param>
/// <param name="Unit">SI unit symbol; "1" for dimensionless fractions, "class" for categories.</param>
/// <param name="NoData">Why a value can be missing, for example "no generated terrain"; a missing value is never zero.</param>
/// <param name="Zero">What the value zero means, for example "calm air" (spec §3.5); class 0 for categories.</param>
/// <param name="Precision">Storage and error budget, for example "float64; exact" or "float32; ≤ 0.01 m".</param>
/// <param name="ImplementedBy">Phase that implements it, such as "Phase 02"; a later phase means unavailable now.</param>
/// <param name="Schema">Version of the field's saved representation; changing it needs a save migration (ADR-0008, ADR-0009).</param>
public sealed record FieldDefinition(
    string Key,
    FieldFamily Family,
    string Name,
    string Unit,
    FieldKind Kind,
    FieldClass Class,
    GridLevel NativeLevel,
    Interpolation Interpolation,
    Refinement Refinement,
    double Min,
    double Max,
    string NoData,
    string Zero,
    string Precision,
    string Owner,
    IReadOnlyList<string> DependsOn,
    string ImplementedBy,
    int Schema = 1)
{
    public bool InRange(double value) => value >= Min && value <= Max;
}
