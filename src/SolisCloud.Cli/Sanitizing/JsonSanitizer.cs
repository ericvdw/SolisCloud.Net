// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;

namespace SolisCloud.Cli.Sanitizing;

/// <summary>
/// Removes personal data from SolisCloud JSON so responses can be shared, e.g. as test fixtures.
/// </summary>
/// <remarks>
/// <para>Each document is processed in two passes:</para>
/// <list type="number">
/// <item>Collect: every property that <see cref="FieldRules"/> marks as sensitive has its value recorded with a
/// replacement (a stable fake for identifiers, <see cref="Redacted"/> for personal text).</item>
/// <item>Write: those properties get their replacements, and so does any <em>other</em> string that contains a
/// recorded value. That second part catches sensitive data under names no rule knows about, such as a station
/// name repeated in an undocumented <c>title</c> field.</item>
/// </list>
/// <para>
/// Recorded values carry over between documents, so one instance should sanitise a whole capture, starting with
/// the station and device lists.
/// </para>
/// <para>
/// This is best-effort. Always read a capture before sharing it.
/// </para>
/// </remarks>
internal sealed class JsonSanitizer
{
    /// <summary>The value that replaces personal text.</summary>
    public const string Redacted = "REDACTED";

    // Values shorter than these are too likely to appear by coincidence ("1", "AU", 2024) to be replaced where no
    // rule marks them as sensitive: as a whole string, as a whole number, or inside a longer string.
    private const int MinimumExactMatchLength = 4;
    private const int MinimumNumberMatchLength = 6;
    private const int MinimumEmbeddedLength = 6;

    private readonly Pseudonymizer _pseudonymizer;
    private readonly Dictionary<string, string> _replacements = new(StringComparer.Ordinal);
    private List<KeyValuePair<string, string>>? _embeddedReplacements;

    public JsonSanitizer(Pseudonymizer pseudonymizer)
    {
        _pseudonymizer = pseudonymizer;
    }

    /// <summary>
    /// Returns a sanitised, indented copy of <paramref name="json"/>.
    /// </summary>
    /// <exception cref="JsonException"><paramref name="json"/> is not valid JSON.</exception>
    public string Sanitize(string json)
    {
        using var document = JsonDocument.Parse(json);
        return Sanitize(document.RootElement);
    }

    /// <summary>
    /// Returns a sanitised, indented copy of <paramref name="element"/>.
    /// </summary>
    public string Sanitize(JsonElement element)
    {
        Collect(element, FieldKind.Keep);
        _embeddedReplacements = null;
        return JsonText.Write(writer => Write(writer, element, FieldKind.Keep));
    }

    private void Collect(JsonElement element, FieldKind kind)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    Collect(property.Value, FieldRules.Classify(property.Name));
                }

                break;

            case JsonValueKind.Array:
                // The kind applies to every element, e.g. each serial number in "snList".
                foreach (JsonElement item in element.EnumerateArray())
                {
                    Collect(item, kind);
                }

                break;

            case JsonValueKind.String when kind is FieldKind.Identifier or FieldKind.PersonalText:
                Record(element.GetString()!, kind);
                break;

            case JsonValueKind.Number when kind is FieldKind.Identifier && IsInteger(element):
                Record(element.GetRawText(), kind);
                break;

            default:
                break;
        }
    }

    private void Record(string original, FieldKind kind)
    {
        if (original.Length == 0 || _replacements.ContainsKey(original))
        {
            return;
        }

        _replacements.Add(original, kind == FieldKind.Identifier ? _pseudonymizer.Pseudonymize(original) : Redacted);
    }

    private void Write(Utf8JsonWriter writer, JsonElement element, FieldKind kind)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    Write(writer, property.Value, FieldRules.Classify(property.Name));
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in element.EnumerateArray())
                {
                    Write(writer, item, kind);
                }

                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(SanitizeString(element.GetString()!, kind));
                break;

            case JsonValueKind.Number:
                WriteNumber(writer, element, kind);
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private string SanitizeString(string value, FieldKind kind)
    {
        if (value.Length == 0)
        {
            return value;
        }

        return kind switch
        {
            FieldKind.Location => "0",
            FieldKind.Identifier or FieldKind.PersonalText => _replacements[value],
            _ => value.Length >= MinimumExactMatchLength && _replacements.TryGetValue(value, out string? replacement)
                ? replacement
                : ReplaceEmbedded(value),
        };
    }

    private void WriteNumber(Utf8JsonWriter writer, JsonElement element, FieldKind kind)
    {
        string raw = element.GetRawText();
        bool replace = IsInteger(element)
            && (kind == FieldKind.Identifier || (kind == FieldKind.Keep && raw.Length >= MinimumNumberMatchLength));

        if (kind == FieldKind.Location)
        {
            writer.WriteNumberValue(0);
        }
        else if (replace && _replacements.TryGetValue(raw, out string? replacement))
        {
            // An ID sent as a number (the document's examples mix both); the fake keeps the digit count.
            writer.WriteRawValue(replacement, skipInputValidation: false);
        }
        else
        {
            writer.WriteRawValue(raw, skipInputValidation: true);
        }
    }

    private string ReplaceEmbedded(string value)
    {
        // Longest first, so a station name isn't partly replaced by a shorter value it contains.
        _embeddedReplacements ??= [.. _replacements
            .Where(pair => pair.Key.Length >= MinimumEmbeddedLength)
            .OrderByDescending(pair => pair.Key.Length)];

        foreach (KeyValuePair<string, string> pair in _embeddedReplacements)
        {
            if (value.Contains(pair.Key, StringComparison.Ordinal))
            {
                value = value.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
            }
        }

        return value;
    }

    private static bool IsInteger(JsonElement element)
    {
        string raw = element.GetRawText();
        return raw.Length > 0 && raw.All(char.IsAsciiDigit);
    }
}
