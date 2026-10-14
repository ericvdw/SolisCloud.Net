// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Models;

/// <summary>
/// How serious an alarm is (its <c>alarmLevel</c> field).
/// </summary>
public enum AlarmLevel
{
    /// <summary>The level is missing or a value this library doesn't know.</summary>
    Unknown = 0,

    /// <summary>Informational.</summary>
    Tip = 1,

    /// <summary>A fault that needs attention.</summary>
    General = 2,

    /// <summary>A serious fault.</summary>
    Emergency = 3,
}
