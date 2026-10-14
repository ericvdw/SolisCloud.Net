// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Models;

/// <summary>
/// The connection state SolisCloud reports for a station or inverter (its <c>state</c> field).
/// </summary>
public enum DeviceState
{
    /// <summary>The state is missing or a value this library doesn't know.</summary>
    Unknown = 0,

    /// <summary>Reporting normally.</summary>
    Online = 1,

    /// <summary>Not reporting, e.g. at night or when the data logger lost its connection.</summary>
    Offline = 2,

    /// <summary>Reporting, with an active alarm.</summary>
    Alarm = 3,
}
