using System.Globalization;
using Sovereigns.Simulation.Atlas;

namespace Sovereigns.Client.Ui;

/// <summary>
/// Display text for field values, shared by the inspector and the map legend. The simulation reports SI numbers
/// (ADR-0004 §1); degrees and hh:mm:ss are display conversions made here, at the client boundary.
/// </summary>
public static class FieldFormat
{
    /// <summary>A number in about four significant digits without trailing zeros: <c>50000</c>, <c>0.0313</c>, <c>1.2e-5</c>.</summary>
    public static string Compact(double value)
    {
        if (double.IsNaN(value))
        {
            return "no data";
        }

        var magnitude = Math.Abs(value);
        return value == 0 || (magnitude >= 0.001 && magnitude < 1e6)
            ? value.ToString("0.####", CultureInfo.InvariantCulture)
            : value.ToString("0.###e+0", CultureInfo.InvariantCulture);
    }

    /// <summary>Space and unit, or a short phrase for unit "1" (dimensionless) and nothing without a unit.</summary>
    public static string UnitSuffix(string? unit) => unit switch
    {
        null or "" => "",
        "1" => " (dimensionless)",
        _ => " " + unit,
    };

    /// <summary>A value of a field's quantity: degrees for radians, hh:mm:ss for local solar time, otherwise the SI number with its unit.</summary>
    public static string Value(double value, string? unit, bool isSolarTime = false, bool withUnit = true)
    {
        if (unit == "rad")
        {
            return string.Create(CultureInfo.InvariantCulture, $"{value * 180 / Math.PI:0.0000}°");
        }

        if (unit == "s" && isSolarTime)
        {
            var seconds = (long)Math.Floor(value);
            return string.Create(CultureInfo.InvariantCulture, $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}");
        }

        return withUnit ? Compact(value) + UnitSuffix(unit) : Compact(value);
    }

    /// <summary>A field's value for a legend, where the unit is stated once in the title: no unit suffix except converted ones (° and hh:mm:ss).</summary>
    public static string Value(FieldDefinition field, double value, bool withUnit = false)
    {
        ArgumentNullException.ThrowIfNull(field);
        return Value(value, field.Unit, field.Key == FieldCatalog.LocalSolarTime.Key, withUnit);
    }

    /// <summary>Feature kind as words: <c>SurveyLine</c> is "survey line".</summary>
    public static string Words(Enum value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var text = value.ToString();
        var result = new System.Text.StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            if (i > 0 && char.IsUpper(text[i]))
            {
                result.Append(' ');
            }

            result.Append(char.ToLowerInvariant(text[i]));
        }

        return result.ToString();
    }
}
