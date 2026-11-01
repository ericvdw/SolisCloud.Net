// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;
using SolisCloud.Internal;

namespace SolisCloud.Models;

/// <summary>
/// A data logger ("collector" in the API): the stick that sends an inverter's readings to SolisCloud
/// (<see cref="SolisEndpoints.CollectorList"/>).
/// </summary>
/// <remarks>
/// When a data logger loses its connection, SolisCloud raises a "Loss of internet connection" alarm and the inverter's
/// readings stop or repeat, so its signal is the first thing to check when data goes missing.
/// </remarks>
public sealed class DataLogger
{
    internal DataLogger(JsonElement json)
    {
        FieldReader f = new(json);
        Raw = json.Clone();

        Id = f.String("id") ?? string.Empty;
        SerialNumber = f.String("sn") ?? string.Empty;
        StationId = f.String("stationId");
        Model = f.String("model");
        State = f.Enum<DeviceState>("state") ?? DeviceState.Unknown;
        LastUpdated = f.UnixMilliseconds("dataTimestamp");
        SignalStrength = f.Number("rssi");
        SignalLevel = f.Int32("rssiLevel");
        UploadInterval = f.Number("dataUploadCycle") is double seconds and > 0 ? TimeSpan.FromSeconds(seconds) : null;
    }

    /// <summary>Gets the data logger's ID.</summary>
    public string Id { get; }

    /// <summary>Gets the serial number, e.g. as printed on the stick.</summary>
    public string SerialNumber { get; }

    /// <summary>Gets the ID of the station it reports for.</summary>
    public string? StationId { get; }

    /// <summary>Gets the model code, e.g. <c>WL</c> for a Wi-Fi stick.</summary>
    public string? Model { get; }

    /// <summary>Gets whether it is reporting.</summary>
    public DeviceState State { get; }

    /// <summary>Gets when it last sent data.</summary>
    public DateTimeOffset? LastUpdated { get; }

    /// <summary>
    /// Gets the Wi-Fi or mobile signal strength in dBm (<c>rssi</c>), e.g. <c>-52</c>. Around -80 and below, the
    /// connection tends to drop.
    /// </summary>
    public double? SignalStrength { get; }

    /// <summary>Gets SolisCloud's signal rating, from 1 (weak) to 4 (strong).</summary>
    public int? SignalLevel { get; }

    /// <summary>Gets how often it sends readings, typically 5 minutes.</summary>
    public TimeSpan? UploadInterval { get; }

    /// <summary>Gets the complete JSON record, including fields not modelled here.</summary>
    public JsonElement Raw { get; }
}
