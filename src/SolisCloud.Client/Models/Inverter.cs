// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;
using SolisCloud.Internal;

namespace SolisCloud.Models;

/// <summary>
/// An inverter and its latest readings.
/// </summary>
/// <remarks>
/// <para>
/// Built from the inverter list (<see cref="SolisEndpoints.InverterList"/>) or the inverter details
/// (<see cref="SolisEndpoints.InverterDetail"/>). The details fill in more, e.g. per-phase AC readings and
/// battery voltage. Anything not available is <see langword="null"/>.
/// </para>
/// <para>
/// Sign conventions follow SolisCloud: <see cref="BatteryPower"/> and <see cref="BatteryCurrent"/> are positive
/// while charging, and <see cref="GridPower"/> is positive while exporting. On a hybrid inverter,
/// <see cref="AcPower"/> tracks PV generation.
/// </para>
/// <para>
/// The details have about 800 fields; the ones not modelled here are in <see cref="Raw"/>.
/// </para>
/// </remarks>
public sealed class Inverter
{
    internal Inverter(JsonElement json)
    {
        FieldReader f = new(json);
        Raw = json.Clone();

        Id = f.String("id") ?? string.Empty;
        SerialNumber = f.String("sn") ?? string.Empty;
        StationId = f.String("stationId");
        CollectorSerialNumber = f.String("collectorSn", "collectorsn");
        Model = f.String("machine");
        SoftwareVersion = f.String("version", "inverterSoftwareVersion");
        State = f.Enum<DeviceState>("state") ?? DeviceState.Unknown;
        LastUpdated = f.UnixMilliseconds("dataTimestamp");

        RatedPower = f.Power("power", "kW");
        AcPower = f.Power("pac", "kW");
        GridFrequency = f.Number("fac");
        Temperature = f.Number("inverterTemperature");
        PvStrings = Readings.PvStrings(f);
        AcPhases = Readings.AcPhases(f);

        BatteryPower = f.Power("batteryPower", "kW");
        BatteryStateOfCharge = f.Number("batteryCapacitySoc");
        BatteryStateOfHealth = f.Number("batteryHealthSoh");
        BatteryVoltage = f.Number("batteryVoltage");
        BatteryCurrent = f.Number("storageBatteryCurrent");
        GridPower = f.Power("psum", "kW");
        LoadPower = f.Power("totalLoadPower", "kW");

        EnergyToday = f.Energy("eToday", "etoday", "kWh");
        EnergyThisMonth = f.Energy("eMonth", "kWh");
        EnergyThisYear = f.Energy("eYear", "kWh");
        EnergyTotal = f.Energy("eTotal", "etotal", "kWh");
        GridImportedToday = f.Energy("gridPurchasedTodayEnergy", "kWh");
        GridExportedToday = f.Energy("gridSellTodayEnergy", "kWh");
        LoadToday = f.Energy("homeLoadTodayEnergy", "kWh");
        BatteryChargedToday = f.Energy("batteryTodayChargeEnergy", "kWh");
        BatteryDischargedToday = f.Energy("batteryTodayDischargeEnergy", "kWh");
    }

    /// <summary>Gets the inverter ID, kept as a string so it survives exactly.</summary>
    public string Id { get; }

    /// <summary>Gets the serial number.</summary>
    public string SerialNumber { get; }

    /// <summary>Gets the ID of the station the inverter belongs to.</summary>
    public string? StationId { get; }

    /// <summary>Gets the serial number of the data logger it reports through.</summary>
    public string? CollectorSerialNumber { get; }

    /// <summary>Gets the model name, e.g. <c>S6-EH1P8K-L-PLUS</c>.</summary>
    public string? Model { get; }

    /// <summary>Gets the firmware version.</summary>
    public string? SoftwareVersion { get; }

    /// <summary>Gets the connection state.</summary>
    public DeviceState State { get; }

    /// <summary>Gets when the inverter last reported.</summary>
    public DateTimeOffset? LastUpdated { get; }

    /// <summary>Gets the rated output power.</summary>
    public Power? RatedPower { get; }

    /// <summary>Gets the current output (on a hybrid inverter, the PV generation).</summary>
    public Power? AcPower { get; }

    /// <summary>Gets the grid frequency in hertz (details only).</summary>
    public double? GridFrequency { get; }

    /// <summary>Gets the inverter temperature in °C (details only).</summary>
    public double? Temperature { get; }

    /// <summary>Gets the DC inputs, numbered from 1.</summary>
    public IReadOnlyList<PvString> PvStrings { get; }

    /// <summary>Gets the AC output phases (details only; empty otherwise).</summary>
    public IReadOnlyList<AcPhase> AcPhases { get; }

    /// <summary>Gets the battery power: positive while charging, negative while discharging.</summary>
    public Power? BatteryPower { get; }

    /// <summary>Gets the battery's state of charge in percent.</summary>
    public double? BatteryStateOfCharge { get; }

    /// <summary>Gets the battery's state of health in percent (details only).</summary>
    public double? BatteryStateOfHealth { get; }

    /// <summary>Gets the battery voltage in volts (details only).</summary>
    public double? BatteryVoltage { get; }

    /// <summary>Gets the battery current in amperes: positive while charging (details only).</summary>
    public double? BatteryCurrent { get; }

    /// <summary>Gets the grid power: positive while exporting, negative while importing.</summary>
    public Power? GridPower { get; }

    /// <summary>Gets the household consumption.</summary>
    public Power? LoadPower { get; }

    /// <summary>Gets today's generation.</summary>
    public Energy? EnergyToday { get; }

    /// <summary>Gets this month's generation (details only).</summary>
    public Energy? EnergyThisMonth { get; }

    /// <summary>Gets this year's generation (details only).</summary>
    public Energy? EnergyThisYear { get; }

    /// <summary>Gets the lifetime generation.</summary>
    public Energy? EnergyTotal { get; }

    /// <summary>Gets the energy bought from the grid today.</summary>
    public Energy? GridImportedToday { get; }

    /// <summary>Gets the energy sold to the grid today.</summary>
    public Energy? GridExportedToday { get; }

    /// <summary>Gets today's household consumption.</summary>
    public Energy? LoadToday { get; }

    /// <summary>Gets the energy put into the battery today.</summary>
    public Energy? BatteryChargedToday { get; }

    /// <summary>Gets the energy taken from the battery today.</summary>
    public Energy? BatteryDischargedToday { get; }

    /// <summary>Gets the complete JSON record, including fields not modelled here.</summary>
    public JsonElement Raw { get; }
}
