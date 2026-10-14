// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;
using System.Text.Json;
using SolisCloud.Internal;

namespace SolisCloud.Models;

/// <summary>
/// One point of a station's day series (<see cref="SolisEndpoints.StationDay"/>), every 5 minutes.
/// </summary>
/// <remarks>
/// <para>
/// Sign conventions follow SolisCloud: <see cref="BatteryPower"/> is positive while charging, and
/// <see cref="GridPower"/> is positive while exporting. The four powers balance:
/// <c>LoadPower = Power - GridPower - BatteryPower</c>.
/// </para>
/// </remarks>
public sealed class StationSample
{
    internal StationSample(JsonElement json, DateTime day, TimeSpan utcOffset)
    {
        FieldReader f = new(json);
        Raw = json.Clone();

        // The record's "time" epoch is anchored to China time (UTC+8). Its "timeStr" (e.g. "16:25:00") is the
        // station's local time of day, which is what this uses.
        Time = TimeSpan.TryParseExact(f.String("timeStr"), @"hh\:mm\:ss", CultureInfo.InvariantCulture, out TimeSpan timeOfDay)
            ? new DateTimeOffset(day.Date + timeOfDay, utcOffset)
            : default;

        Power = f.Power("power", "W");
        BatteryPower = f.Power("batteryPower", "W");
        BatteryStateOfCharge = f.Number("batteryCapacitySoc");
        GridPower = f.Power("psum", "W");

        // Despite the name, consumeEnergy here is the consumption at that moment, in watts.
        LoadPower = f.Power("consumeEnergy", "W");
    }

    /// <summary>Gets the time of the point, in the station's time zone.</summary>
    public DateTimeOffset Time { get; }

    /// <summary>Gets the generation.</summary>
    public Power? Power { get; }

    /// <summary>Gets the battery power: positive while charging, negative while discharging.</summary>
    public Power? BatteryPower { get; }

    /// <summary>Gets the battery's state of charge in percent.</summary>
    public double? BatteryStateOfCharge { get; }

    /// <summary>Gets the grid power: positive while exporting, negative while importing.</summary>
    public Power? GridPower { get; }

    /// <summary>Gets the household consumption.</summary>
    public Power? LoadPower { get; }

    /// <summary>Gets the complete JSON record, including fields not modelled here.</summary>
    public JsonElement Raw { get; }
}
