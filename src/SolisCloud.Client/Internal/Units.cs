// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Internal;

/// <summary>
/// Conversion factors for the unit strings SolisCloud sends.
/// </summary>
internal static class Units
{
    /// <summary>
    /// Returns how many watts one <paramref name="unit"/> is, or <see langword="null"/> if it isn't a power unit.
    /// Peak-power units (<c>kWp</c>, used for installed capacity) count as their plain equivalents.
    /// </summary>
    public static double? WattsPer(string unit) =>
        unit.Trim() switch
        {
            "W" or "Wp" => 1,
            "kW" or "kWp" or "KW" => 1_000,
            "MW" or "MWp" => 1_000_000,
            "GW" or "GWp" => 1_000_000_000,
            _ => null,
        };

    /// <summary>
    /// Returns how many kilowatt-hours one <paramref name="unit"/> is, or <see langword="null"/> if it isn't an
    /// energy unit.
    /// </summary>
    public static double? KilowattHoursPer(string unit) =>
        unit.Trim() switch
        {
            "Wh" => 0.001,
            "kWh" or "KWh" => 1,
            "MWh" => 1_000,
            "GWh" => 1_000_000,
            _ => null,
        };
}
