// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;
using System.Text.Json;

namespace SolisCloud.Cli;

/// <summary>
/// Tolerant readers for response payloads whose shape the API document doesn't pin down.
/// </summary>
internal static class JsonData
{
    /// <summary>
    /// Returns the records of a list response: <c>data.page.records</c>, <c>data.records</c>, or <c>data</c> itself
    /// when it is an array. Returns nothing if none of those is present.
    /// </summary>
    public static IReadOnlyList<JsonElement> Records(JsonElement data)
    {
        if (data.ValueKind == JsonValueKind.Array)
        {
            return [.. data.EnumerateArray()];
        }

        if (data.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        JsonElement container = TryGet(data, "page") is { ValueKind: JsonValueKind.Object } page ? page : data;
        return TryGet(container, "records") is { ValueKind: JsonValueKind.Array } records
            ? [.. records.EnumerateArray()]
            : [];
    }

    /// <summary>
    /// Returns the property's value, or <see langword="null"/> if the element isn't an object, the property is
    /// missing, or its value is <c>null</c> or an empty string.
    /// </summary>
    public static JsonElement? TryGet(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out JsonElement value)
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            || (value.ValueKind == JsonValueKind.String && value.GetString()!.Length == 0))
        {
            return null;
        }

        return value;
    }

    /// <summary>
    /// Returns the property as text: a string as-is, a number in its original form. Otherwise <see langword="null"/>.
    /// </summary>
    public static string? TryGetText(JsonElement element, string propertyName) =>
        TryGet(element, propertyName) switch
        {
            { ValueKind: JsonValueKind.String } value => value.GetString(),
            { ValueKind: JsonValueKind.Number } value => value.GetRawText(),
            _ => null,
        };

    /// <summary>
    /// Returns the property as a number, accepting <c>10.00</c> and <c>"10.00"</c> alike.
    /// </summary>
    public static decimal? TryGetDecimal(JsonElement element, string propertyName) =>
        decimal.TryParse(TryGetText(element, propertyName), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value)
            ? value
            : null;
}
