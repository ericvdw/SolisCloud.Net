// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;
using SolisCloud.Internal;

namespace SolisCloud.Models;

/// <summary>
/// An alarm raised by a device (<see cref="SolisEndpoints.AlarmList"/>).
/// </summary>
public sealed class Alarm
{
    internal Alarm(JsonElement json)
    {
        FieldReader f = new(json);
        Raw = json.Clone();

        Id = f.String("id") ?? string.Empty;
        StationId = f.String("stationId");
        DeviceSerialNumber = f.String("alarmDeviceSn");
        Code = f.String("alarmCode");
        Message = f.String("alarmMsg");
        Advice = f.String("advice");
        Level = f.Enum<AlarmLevel>("alarmLevel") ?? AlarmLevel.Unknown;
        State = f.Enum<AlarmState>("state");
        Started = f.UnixMilliseconds("alarmBeginTime");
        Ended = f.UnixMilliseconds("alarmEndTime");
    }

    /// <summary>Gets the alarm ID.</summary>
    public string Id { get; }

    /// <summary>Gets the ID of the station the device belongs to.</summary>
    public string? StationId { get; }

    /// <summary>Gets the serial number of the device that raised the alarm.</summary>
    public string? DeviceSerialNumber { get; }

    /// <summary>Gets the fault code, e.g. <c>1015</c>.</summary>
    public string? Code { get; }

    /// <summary>Gets the description, e.g. <c>NO-Grid</c>.</summary>
    public string? Message { get; }

    /// <summary>Gets SolisCloud's suggested action, e.g. <c>No Action Required</c>.</summary>
    public string? Advice { get; }

    /// <summary>Gets how serious the alarm is.</summary>
    public AlarmLevel Level { get; }

    /// <summary>Gets where the alarm is in its life cycle, or <see langword="null"/> if not reported.</summary>
    public AlarmState? State { get; }

    /// <summary>Gets when the alarm started.</summary>
    public DateTimeOffset? Started { get; }

    /// <summary>Gets when the alarm ended, or <see langword="null"/> while it is active.</summary>
    public DateTimeOffset? Ended { get; }

    /// <summary>Gets the complete JSON record, including fields not modelled here.</summary>
    public JsonElement Raw { get; }
}
