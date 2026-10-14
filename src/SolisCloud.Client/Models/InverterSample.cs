// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;
using SolisCloud.Internal;

namespace SolisCloud.Models;

/// <summary>
/// One reading from an inverter's day series (<see cref="SolisEndpoints.InverterDay"/>), about every 5 minutes.
/// </summary>
/// <remarks>
/// Sign conventions follow SolisCloud: <see cref="BatteryPower"/> is positive while charging, and
/// <see cref="GridPower"/> is positive while exporting.
/// </remarks>
public sealed class InverterSample
{
    internal InverterSample(JsonElement json, TimeSpan utcOffset)
    {
        FieldReader f = new(json);
        Raw = json.Clone();

        // dataTimestamp is a true epoch. The record's own "timeZone" is 8 (China) whatever the station's zone, so
        // the station's offset, which the request was made with, is used to show local time.
        Time = f.UnixMilliseconds("dataTimestamp")?.ToOffset(utcOffset) ?? default;

        AcPower = f.Power("pac", "W");
        BatteryPower = f.Power("batteryPower", "W");
        BatteryStateOfCharge = f.Number("batteryCapacitySoc");
        GridPower = f.Power("pSum", "W") ?? f.Power("psum", "W");
        LoadPower = Sum(f.Power("familyLoadPower", "W"), f.Power("bypassLoadPower", "W"));
        Temperature = f.Number("inverterTemperature");
        PvStrings = Readings.PvStrings(f);

        EnergyToday = f.Energy("eToday", "kWh");
        GridImportedToday = f.Energy("gridPurchasedTodayEnergy", "kWh");
        GridExportedToday = f.Energy("gridSellTodayEnergy", "kWh");
        BatteryChargedToday = f.Energy("batteryTodayChargeEnergy", "kWh");
        BatteryDischargedToday = f.Energy("batteryTodayDischargeEnergy", "kWh");
    }

    /// <summary>Gets when the reading was taken, in the station's time zone.</summary>
    public DateTimeOffset Time { get; }

    /// <summary>Gets the output (on a hybrid inverter, the PV generation).</summary>
    public Power? AcPower { get; }

    /// <summary>Gets the battery power: positive while charging, negative while discharging.</summary>
    public Power? BatteryPower { get; }

    /// <summary>Gets the battery's state of charge in percent.</summary>
    public double? BatteryStateOfCharge { get; }

    /// <summary>Gets the grid power: positive while exporting, negative while importing.</summary>
    public Power? GridPower { get; }

    /// <summary>
    /// Gets the household consumption: the grid-side load (<c>familyLoadPower</c>) plus the backup-port load
    /// (<c>bypassLoadPower</c>). Either may be zero depending on how the house is wired.
    /// </summary>
    public Power? LoadPower { get; }

    /// <summary>Gets the inverter temperature in °C.</summary>
    public double? Temperature { get; }

    /// <summary>Gets the DC inputs, numbered from 1.</summary>
    public IReadOnlyList<PvString> PvStrings { get; }

    /// <summary>Gets the generation so far that day.</summary>
    public Energy? EnergyToday { get; }

    /// <summary>Gets the energy bought from the grid so far that day.</summary>
    public Energy? GridImportedToday { get; }

    /// <summary>Gets the energy sold to the grid so far that day.</summary>
    public Energy? GridExportedToday { get; }

    /// <summary>Gets the energy put into the battery so far that day.</summary>
    public Energy? BatteryChargedToday { get; }

    /// <summary>Gets the energy taken from the battery so far that day.</summary>
    public Energy? BatteryDischargedToday { get; }

    /// <summary>Gets the complete JSON record, including fields not modelled here.</summary>
    public JsonElement Raw { get; }

    private static Power? Sum(Power? first, Power? second) =>
        first is null && second is null ? null : (first ?? Power.Zero) + (second ?? Power.Zero);
}
