// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;

namespace SolisCloud.Models;

/// <summary>
/// An amount of power, stored in watts whatever unit SolisCloud reported it in.
/// </summary>
/// <remarks>
/// SolisCloud reports power as a number plus a unit field (<c>"W"</c>, <c>"kW"</c>, <c>"MW"</c>) that changes with
/// magnitude, sometimes with a scale factor as well. This type removes that variation: read <see cref="Watts"/>
/// or <see cref="Kilowatts"/> and never think about the unit field again.
/// </remarks>
public readonly struct Power : IEquatable<Power>, IComparable<Power>, IComparable, IFormattable
{
    private Power(double watts)
    {
        Watts = watts;
    }

    /// <summary>Gets zero power.</summary>
    public static Power Zero => default;

    /// <summary>Gets the power in watts.</summary>
    public double Watts { get; }

    /// <summary>Gets the power in kilowatts.</summary>
    public double Kilowatts => Watts / 1_000;

    /// <summary>Creates a value from watts.</summary>
    /// <param name="watts">The power in watts.</param>
    /// <returns>The power.</returns>
    public static Power FromWatts(double watts) => new(watts);

    /// <summary>Creates a value from kilowatts.</summary>
    /// <param name="kilowatts">The power in kilowatts.</param>
    /// <returns>The power.</returns>
    public static Power FromKilowatts(double kilowatts) => new(kilowatts * 1_000);

    /// <summary>Adds two powers.</summary>
    public static Power operator +(Power left, Power right) => new(left.Watts + right.Watts);

    /// <summary>Subtracts one power from another.</summary>
    public static Power operator -(Power left, Power right) => new(left.Watts - right.Watts);

    /// <summary>Reverses the direction of a power (e.g. charging becomes discharging).</summary>
    public static Power operator -(Power value) => new(-value.Watts);

    /// <summary>Compares two powers for equality.</summary>
    public static bool operator ==(Power left, Power right) => left.Equals(right);

    /// <summary>Compares two powers for inequality.</summary>
    public static bool operator !=(Power left, Power right) => !left.Equals(right);

    /// <summary>Returns <see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>.</summary>
    public static bool operator <(Power left, Power right) => left.CompareTo(right) < 0;

    /// <summary>Returns <see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>.</summary>
    public static bool operator >(Power left, Power right) => left.CompareTo(right) > 0;

    /// <summary>Returns <see langword="true"/> if <paramref name="left"/> is at most <paramref name="right"/>.</summary>
    public static bool operator <=(Power left, Power right) => left.CompareTo(right) <= 0;

    /// <summary>Returns <see langword="true"/> if <paramref name="left"/> is at least <paramref name="right"/>.</summary>
    public static bool operator >=(Power left, Power right) => left.CompareTo(right) >= 0;

    /// <summary>Adds two powers.</summary>
    /// <param name="left">The first power.</param>
    /// <param name="right">The second power.</param>
    /// <returns>The sum.</returns>
    public static Power Add(Power left, Power right) => left + right;

    /// <summary>Subtracts one power from another.</summary>
    /// <param name="left">The power to subtract from.</param>
    /// <param name="right">The power to subtract.</param>
    /// <returns>The difference.</returns>
    public static Power Subtract(Power left, Power right) => left - right;

    /// <summary>Reverses the direction of a power.</summary>
    /// <param name="value">The power.</param>
    /// <returns>The power with its sign reversed.</returns>
    public static Power Negate(Power value) => -value;

    /// <inheritdoc />
    public bool Equals(Power other) => Watts.Equals(other.Watts);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Power other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Watts.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(Power other) => Watts.CompareTo(other.Watts);

    /// <inheritdoc />
    public int CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            Power other => CompareTo(other),
            _ => throw new ArgumentException("The object is not a " + nameof(Power) + ".", nameof(obj)),
        };

    /// <summary>
    /// Formats the power in watts below 1 kW and in kilowatts from 1 kW, e.g. <c>590 W</c> or <c>1.154 kW</c>,
    /// using the current culture.
    /// </summary>
    /// <returns>The formatted power.</returns>
    public override string ToString() => ToString(format: null, CultureInfo.CurrentCulture);

    /// <summary>
    /// Formats the power.
    /// </summary>
    /// <param name="format">
    /// <c>"W"</c> for watts, <c>"kW"</c> for kilowatts, or <see langword="null"/>/<c>"G"</c> to choose by magnitude.
    /// </param>
    /// <param name="formatProvider">The culture to format numbers with.</param>
    /// <returns>The formatted power, e.g. <c>1.154 kW</c>.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is not supported.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        format switch
        {
            null or "" or "G" => Math.Abs(Watts) < 1_000
                ? Watts.ToString("0.#", formatProvider) + " W"
                : Kilowatts.ToString("0.###", formatProvider) + " kW",
            "W" => Watts.ToString("0.#", formatProvider) + " W",
            "kW" => Kilowatts.ToString("0.###", formatProvider) + " kW",
            _ => throw new FormatException("Unsupported format '" + format + "'. Use \"W\", \"kW\" or \"G\"."),
        };
}
