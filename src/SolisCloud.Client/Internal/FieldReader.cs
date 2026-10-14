// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;
using System.Text.Json;
using SolisCloud.Models;

namespace SolisCloud.Internal;

/// <summary>
/// Reads fields from a SolisCloud JSON object, tolerating the API's inconsistencies: numbers sent as strings, IDs
/// sent as numbers, empty strings for missing values, and quantities split over three fields.
/// </summary>
/// <remarks>
/// <para>A quantity such as power arrives as up to three fields, e.g. for <c>pac</c>:</para>
/// <list type="bullet">
/// <item><c>pac</c>: the number;</item>
/// <item><c>pacStr</c>: its unit (<c>"W"</c>, <c>"kW"</c>, …);</item>
/// <item><c>pacPec</c>: a scale factor, so the value in that unit is <c>pac × pacPec</c>. History data uses it:
/// <c>pac: 1154, pacPec: "0.001", pacStr: "kW"</c> is 1.154 kW.</item>
/// </list>
/// <para>
/// A missing unit or scale falls back to a default the caller chooses per operation, from what real responses show.
/// </para>
/// </remarks>
internal readonly struct FieldReader
{
    public FieldReader(JsonElement element)
    {
        Element = element;
    }

    public JsonElement Element { get; }

    /// <summary>
    /// Returns the value if the property exists and isn't <c>null</c> or an empty string.
    /// </summary>
    public JsonElement? Value(string name)
    {
        if (Element.ValueKind != JsonValueKind.Object
            || !Element.TryGetProperty(name, out JsonElement value)
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            || (value.ValueKind == JsonValueKind.String && value.GetString()!.Length == 0))
        {
            return null;
        }

        return value;
    }

    /// <summary>
    /// Returns a string, or a number's original text (so a 19-digit ID survives exactly).
    /// </summary>
    public string? String(string name) =>
        Value(name) switch
        {
            { ValueKind: JsonValueKind.String } value => value.GetString(),
            { ValueKind: JsonValueKind.Number } value => value.GetRawText(),
            _ => null,
        };

    /// <summary>
    /// Returns the first of several alternative names that is present, e.g. <c>etoday</c> in lists and
    /// <c>eToday</c> in details.
    /// </summary>
    public string? String(string name, string alternative) => String(name) ?? String(alternative);

    /// <summary>
    /// Returns a number, accepting <c>2.0</c> and <c>"2.0"</c> alike.
    /// </summary>
    public double? Number(string name) =>
        Value(name) switch
        {
            { ValueKind: JsonValueKind.Number } value => value.GetDouble(),
            { ValueKind: JsonValueKind.String } value when double.TryParse(
                value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) => parsed,
            _ => null,
        };

    public int? Int32(string name) =>
        Number(name) is { } number && number >= int.MinValue && number <= int.MaxValue && Math.Floor(number) == number
            ? (int)number
            : null;

    /// <summary>
    /// Reads an enum from its numeric value (sent as a number or a string). Undefined values give
    /// <see langword="null"/>.
    /// </summary>
    public TEnum? Enum<TEnum>(string name)
        where TEnum : struct, Enum
    {
        if (Int32(name) is not int value)
        {
            return null;
        }

        var result = (TEnum)(object)value;
#if NET
        return System.Enum.IsDefined(result) ? result : null;
#else
        return System.Enum.IsDefined(typeof(TEnum), result) ? result : null;
#endif
    }

    public decimal? Decimal(string name) =>
        String(name) is { } text && decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value)
            ? value
            : null;

    /// <summary>
    /// Reads an epoch-milliseconds timestamp (sent as a number or a string).
    /// </summary>
    public DateTimeOffset? UnixMilliseconds(string name) =>
        String(name) is { } text
            && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long milliseconds)
            && milliseconds > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds)
            : null;

    /// <summary>
    /// Reads an offset in hours, e.g. <c>"timeZone": 2.0</c> or <c>5.5</c>.
    /// </summary>
    public TimeSpan? Hours(string name) =>
        Number(name) is { } hours && Math.Abs(hours) <= 14 ? TimeSpan.FromMinutes(Math.Round(hours * 60)) : null;

    /// <summary>
    /// Reads a power from <c>name</c>, <c>nameStr</c> and <c>namePec</c>.
    /// </summary>
    /// <param name="name">The value's property name.</param>
    /// <param name="defaultUnit">The unit to assume when <c>nameStr</c> is missing.</param>
    /// <returns>The power, or <see langword="null"/> if the value is missing or its unit isn't a power unit.</returns>
    public Power? Power(string name, string defaultUnit) =>
        Scaled(name, defaultUnit) is (double value, string unit) && Units.WattsPer(unit) is { } watts
            ? Models.Power.FromWatts(value * watts)
            : null;

    /// <summary>
    /// Reads an energy from <c>name</c>, <c>nameStr</c> and <c>namePec</c>.
    /// </summary>
    /// <param name="name">The value's property name.</param>
    /// <param name="defaultUnit">The unit to assume when <c>nameStr</c> is missing.</param>
    /// <returns>The energy, or <see langword="null"/> if the value is missing or its unit isn't an energy unit.</returns>
    public Energy? Energy(string name, string defaultUnit) =>
        Scaled(name, defaultUnit) is (double value, string unit) && Units.KilowattHoursPer(unit) is { } kilowattHours
            ? Models.Energy.FromKilowattHours(value * kilowattHours)
            : null;

    /// <summary>
    /// Like <see cref="Energy(string, string)"/>, but tries <paramref name="alternative"/> when
    /// <paramref name="name"/> is missing.
    /// </summary>
    public Energy? Energy(string name, string alternative, string defaultUnit) =>
        Energy(name, defaultUnit) ?? Energy(alternative, defaultUnit);

    /// <summary>
    /// Reads a plain number with its scale factor applied (<c>name × namePec</c>), for values such as money whose
    /// unit is a currency.
    /// </summary>
    public double? ScaledNumber(string name) => Number(name) is { } value ? value * Scale(name) : null;

    private (double Value, string Unit)? Scaled(string name, string defaultUnit) =>
        Number(name) is { } value ? (value * Scale(name), String(name + "Str") ?? defaultUnit) : null;

    private double Scale(string name) => Number(name + "Pec") is { } scale && scale > 0 ? scale : 1;
}
