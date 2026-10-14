// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Models;

/// <summary>
/// One DC input (PV string or MPPT) of an inverter: <c>uPvN</c>, <c>iPvN</c> and <c>powN</c> in the API.
/// </summary>
public sealed class PvString
{
    internal PvString(int number, double? voltage, double? current, Power? power)
    {
        Number = number;
        Voltage = voltage;
        Current = current;
        Power = power;
    }

    /// <summary>Gets the input's number, starting at 1.</summary>
    public int Number { get; }

    /// <summary>Gets the voltage in volts.</summary>
    public double? Voltage { get; }

    /// <summary>Gets the current in amperes.</summary>
    public double? Current { get; }

    /// <summary>Gets the power.</summary>
    public Power? Power { get; }
}
