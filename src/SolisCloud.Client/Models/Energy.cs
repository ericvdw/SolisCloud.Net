// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;

namespace SolisCloud.Models;

/// <summary>
/// An amount of energy, stored in kilowatt-hours whatever unit SolisCloud reported it in.
/// </summary>
/// <remarks>
/// SolisCloud reports energy as a number plus a unit field (<c>"kWh"</c>, <c>"MWh"</c>) that changes with
/// magnitude, sometimes with a scale factor as well. This type removes that variation: read
/// <see cref="KilowattHours"/> and never think about the unit field again.
/// </remarks>
public readonly struct Energy : IEquatable<Energy>, IComparable<Energy>, IComparable, IFormattable
{
    private Energy(double kilowattHours)
    {
        KilowattHours = kilowattHours;
    }

    /// <summary>Gets zero energy.</summary>
    public static Energy Zero => default;

    /// <summary>Gets the energy in kilowatt-hours.</summary>
    public double KilowattHours { get; }

    /// <summary>Gets the energy in watt-hours.</summary>
    public double WattHours => KilowattHours * 1_000;

    /// <summary>Gets the energy in megawatt-hours.</summary>
    public double MegawattHours => KilowattHours / 1_000;

    /// <summary>Creates a value from watt-hours.</summary>
    /// <param name="wattHours">The energy in watt-hours.</param>
    /// <returns>The energy.</returns>
    public static Energy FromWattHours(double wattHours) => new(wattHours / 1_000);

    /// <summary>Creates a value from kilowatt-hours.</summary>
    /// <param name="kilowattHours">The energy in kilowatt-hours.</param>
    /// <returns>The energy.</returns>
    public static Energy FromKilowattHours(double kilowattHours) => new(kilowattHours);

    /// <summary>Creates a value from megawatt-hours.</summary>
    /// <param name="megawattHours">The energy in megawatt-hours.</param>
    /// <returns>The energy.</returns>
    public static Energy FromMegawattHours(double megawattHours) => new(megawattHours * 1_000);

    /// <summary>Adds two amounts of energy.</summary>
    public static Energy operator +(Energy left, Energy right) => new(left.KilowattHours + right.KilowattHours);

    /// <summary>Subtracts one energy from another.</summary>
    public static Energy operator -(Energy left, Energy right) => new(left.KilowattHours - right.KilowattHours);

    /// <summary>Reverses the sign of an amount of energy.</summary>
    public static Energy operator -(Energy value) => new(-value.KilowattHours);

    /// <summary>Compares two amounts of energy for equality.</summary>
    public static bool operator ==(Energy left, Energy right) => left.Equals(right);

    /// <summary>Compares two amounts of energy for inequality.</summary>
    public static bool operator !=(Energy left, Energy right) => !left.Equals(right);

    /// <summary>Returns <see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>.</summary>
    public static bool operator <(Energy left, Energy right) => left.CompareTo(right) < 0;

    /// <summary>Returns <see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>.</summary>
    public static bool operator >(Energy left, Energy right) => left.CompareTo(right) > 0;

    /// <summary>Returns <see langword="true"/> if <paramref name="left"/> is at most <paramref name="right"/>.</summary>
    public static bool operator <=(Energy left, Energy right) => left.CompareTo(right) <= 0;

    /// <summary>Returns <see langword="true"/> if <paramref name="left"/> is at least <paramref name="right"/>.</summary>
    public static bool operator >=(Energy left, Energy right) => left.CompareTo(right) >= 0;

    /// <summary>Adds two amounts of energy.</summary>
    /// <param name="left">The first energy.</param>
    /// <param name="right">The second energy.</param>
    /// <returns>The sum.</returns>
    public static Energy Add(Energy left, Energy right) => left + right;

    /// <summary>Subtracts one energy from another.</summary>
    /// <param name="left">The energy to subtract from.</param>
    /// <param name="right">The energy to subtract.</param>
    /// <returns>The difference.</returns>
    public static Energy Subtract(Energy left, Energy right) => left - right;

    /// <summary>Reverses the sign of an amount of energy.</summary>
    /// <param name="value">The energy.</param>
    /// <returns>The energy with its sign reversed.</returns>
    public static Energy Negate(Energy value) => -value;

    /// <inheritdoc />
    public bool Equals(Energy other) => KilowattHours.Equals(other.KilowattHours);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Energy other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => KilowattHours.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(Energy other) => KilowattHours.CompareTo(other.KilowattHours);

    /// <inheritdoc />
    public int CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            Energy other => CompareTo(other),
            _ => throw new ArgumentException("The object is not a " + nameof(Energy) + ".", nameof(obj)),
        };

    /// <summary>
    /// Formats the energy in kilowatt-hours below 1 MWh and in megawatt-hours from 1 MWh, e.g. <c>22.7 kWh</c> or
    /// <c>4.783 MWh</c>, using the current culture.
    /// </summary>
    /// <returns>The formatted energy.</returns>
    public override string ToString() => ToString(format: null, CultureInfo.CurrentCulture);

    /// <summary>
    /// Formats the energy.
    /// </summary>
    /// <param name="format">
    /// <c>"kWh"</c>, <c>"MWh"</c>, or <see langword="null"/>/<c>"G"</c> to choose by magnitude.
    /// </param>
    /// <param name="formatProvider">The culture to format numbers with.</param>
    /// <returns>The formatted energy, e.g. <c>22.7 kWh</c>.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is not supported.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        format switch
        {
            null or "" or "G" => Math.Abs(KilowattHours) < 1_000
                ? KilowattHours.ToString("0.##", formatProvider) + " kWh"
                : MegawattHours.ToString("0.###", formatProvider) + " MWh",
            "kWh" => KilowattHours.ToString("0.##", formatProvider) + " kWh",
            "MWh" => MegawattHours.ToString("0.###", formatProvider) + " MWh",
            _ => throw new FormatException("Unsupported format '" + format + "'. Use \"kWh\", \"MWh\" or \"G\"."),
        };
}
