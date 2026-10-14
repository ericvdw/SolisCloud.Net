// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Models;

/// <summary>
/// Where an alarm is in its life cycle (its <c>state</c> field).
/// </summary>
public enum AlarmState
{
    /// <summary>Raised and not yet looked at.</summary>
    Unprocessed = 0,

    /// <summary>Acknowledged.</summary>
    Processed = 1,

    /// <summary>The condition has cleared.</summary>
    Resolved = 2,
}
