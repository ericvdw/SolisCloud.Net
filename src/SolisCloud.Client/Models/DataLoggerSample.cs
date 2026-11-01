// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;
using SolisCloud.Internal;

namespace SolisCloud.Models;

/// <summary>
/// One point of a data logger's day series (<see cref="SolisEndpoints.CollectorDay"/>): its signal, about every
/// 5 minutes while it is connected.
/// </summary>
/// <remarks>A gap in the series is a time the data logger wasn't connected.</remarks>
public sealed class DataLoggerSample
{
    internal DataLoggerSample(JsonElement json, TimeSpan utcOffset)
    {
        FieldReader f = new(json);
        Raw = json.Clone();

        Time = f.UnixMilliseconds("dataTimestamp")?.ToOffset(utcOffset) ?? default;
        SignalStrength = f.Number("rssi");
        SignalLevel = f.Int32("rssiLevel");
    }

    /// <summary>Gets when the point was recorded, in the station's time zone.</summary>
    public DateTimeOffset Time { get; }

    /// <summary>Gets the signal strength in dBm (<c>rssi</c>).</summary>
    public double? SignalStrength { get; }

    /// <summary>Gets SolisCloud's signal rating, from 1 (weak) to 4 (strong).</summary>
    public int? SignalLevel { get; }

    /// <summary>Gets the complete JSON record, including fields not modelled here.</summary>
    public JsonElement Raw { get; }
}
