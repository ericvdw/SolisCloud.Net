// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Models;

/// <summary>
/// One AC phase of an inverter's output: <c>uAcN</c> and <c>iAcN</c> in the API.
/// </summary>
public sealed class AcPhase
{
    internal AcPhase(int number, double? voltage, double? current)
    {
        Number = number;
        Voltage = voltage;
        Current = current;
    }

    /// <summary>Gets the phase number: 1, 2 or 3 (R, S and T in the API document).</summary>
    public int Number { get; }

    /// <summary>Gets the voltage in volts.</summary>
    public double? Voltage { get; }

    /// <summary>Gets the current in amperes.</summary>
    public double? Current { get; }
}
