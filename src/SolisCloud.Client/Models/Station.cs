// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;
using SolisCloud.Internal;

namespace SolisCloud.Models;

/// <summary>
/// A power station (plant): one site, with one or more inverters.
/// </summary>
/// <remarks>
/// <para>
/// Built from the station list (<see cref="SolisEndpoints.StationList"/>) or the station details
/// (<see cref="SolisEndpoints.StationDetail"/>). The details fill in more: the live battery, grid and load
/// figures are only in the details. Anything not available is <see langword="null"/>.
/// </para>
/// <para>
/// Sign conventions follow SolisCloud: <see cref="BatteryPower"/> is positive while charging, and
/// <see cref="GridPower"/> is positive while exporting.
/// </para>
/// <para>
/// The response has hundreds of fields; the ones not modelled here are in <see cref="Raw"/>.
/// </para>
/// </remarks>
public sealed class Station
{
    internal Station(JsonElement json)
    {
        FieldReader f = new(json);
        Raw = json.Clone();

        Id = f.String("id") ?? string.Empty;
        Name = f.String("stationName");
        State = f.Enum<DeviceState>("state") ?? DeviceState.Unknown;
        LastUpdated = f.UnixMilliseconds("dataTimestamp");
        UtcOffset = f.Hours("timeZone");
        TimeZoneId = f.String("timeZoneStandardId");
        Currency = f.String("money");
        InverterCount = f.Int32("inverterCount");

        Capacity = f.Power("capacity", "kWp");
        Power = f.Power("power", "kW");
        BatteryPower = f.Power("batteryPower", "kW");
        BatteryStateOfCharge = f.Number("batteryPercent");
        GridPower = f.Power("psum", "kW");
        LoadPower = f.Power("totalLoadPower", "kW");

        EnergyToday = f.Energy("dayEnergy", "kWh");
        EnergyThisMonth = f.Energy("monthEnergy", "kWh");
        EnergyThisYear = f.Energy("yearEnergy", "kWh");
        EnergyTotal = f.Energy("allEnergy", "kWh");
        GridImportedToday = f.Energy("gridPurchasedDayEnergy", "kWh");
        GridExportedToday = f.Energy("gridSellDayEnergy", "kWh");
        LoadToday = f.Energy("homeLoadTodayEnergy", "kWh");
        BatteryChargedToday = f.Energy("batteryChargeEnergy", "batteryTodayChargeEnergy", "kWh");
        BatteryDischargedToday = f.Energy("batteryDischargeEnergy", "batteryTodayDischargeEnergy", "kWh");
    }

    /// <summary>Gets the station ID, a 19-digit number kept as a string so it survives exactly.</summary>
    public string Id { get; }

    /// <summary>Gets the station's name.</summary>
    public string? Name { get; }

    /// <summary>Gets the connection state.</summary>
    public DeviceState State { get; }

    /// <summary>Gets when the data was last updated. The server refreshes it about every 5 minutes.</summary>
    public DateTimeOffset? LastUpdated { get; }

    /// <summary>Gets the station's offset from UTC, needed for history requests.</summary>
    public TimeSpan? UtcOffset { get; }

    /// <summary>Gets the station's IANA time zone, e.g. <c>Africa/Johannesburg</c> (details only).</summary>
    public string? TimeZoneId { get; }

    /// <summary>Gets the currency revenue is calculated in, e.g. <c>ZAR</c>.</summary>
    public string? Currency { get; }

    /// <summary>Gets the number of inverters.</summary>
    public int? InverterCount { get; }

    /// <summary>Gets the installed (peak) PV capacity.</summary>
    public Power? Capacity { get; }

    /// <summary>Gets the current generation.</summary>
    public Power? Power { get; }

    /// <summary>Gets the battery power: positive while charging, negative while discharging (details only).</summary>
    public Power? BatteryPower { get; }

    /// <summary>Gets the battery's state of charge in percent (details only).</summary>
    public double? BatteryStateOfCharge { get; }

    /// <summary>Gets the grid power: positive while exporting, negative while importing (details only).</summary>
    public Power? GridPower { get; }

    /// <summary>Gets the household consumption (details only).</summary>
    public Power? LoadPower { get; }

    /// <summary>Gets today's generation.</summary>
    public Energy? EnergyToday { get; }

    /// <summary>Gets this month's generation.</summary>
    public Energy? EnergyThisMonth { get; }

    /// <summary>Gets this year's generation.</summary>
    public Energy? EnergyThisYear { get; }

    /// <summary>Gets the lifetime generation.</summary>
    public Energy? EnergyTotal { get; }

    /// <summary>Gets the energy bought from the grid today (details only).</summary>
    public Energy? GridImportedToday { get; }

    /// <summary>Gets the energy sold to the grid today (details only).</summary>
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
