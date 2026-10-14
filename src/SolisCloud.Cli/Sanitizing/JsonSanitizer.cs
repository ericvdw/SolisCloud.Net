// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;

namespace SolisCloud.Cli.Sanitizing;

/// <summary>
/// Removes personal data from SolisCloud JSON so responses can be shared, e.g. as test fixtures.
/// </summary>
/// <remarks>
/// <para><see cref="SanitizeAll"/> works in two passes over a whole set of documents:</para>
/// <list type="number">
/// <item>Collect: every property that <see cref="FieldRules"/> marks as sensitive has its value recorded with a
/// replacement (a stable fake for identifiers, <see cref="Redacted"/> for personal text).</item>
/// <item>Write: those properties get their replacements, and so does any <em>other</em> string that contains a
/// recorded value. That second part catches sensitive data under names no rule knows about, such as a station
/// name repeated in an undocumented <c>title</c> field.</item>
/// </list>
/// <para>
/// Collecting from every document before writing any matters: a station's details can mention an inverter's
/// serial number before the inverter list (where the rules recognise it) has been seen.
/// </para>
/// <para>
/// Some string values are themselves JSON (station details carry an <c>extraInfo</c> string with inverter serial
/// numbers inside). Those are parsed, sanitised by the same rules, and written back as strings.
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

    private static readonly JsonWriterOptions CompactWriterOptions = new()
    {
        Encoder = JsonText.WriterOptions.Encoder,
    };

    private readonly Pseudonymizer _pseudonymizer;
    private readonly Dictionary<string, string> _replacements = new(StringComparer.Ordinal);
    private List<KeyValuePair<string, string>>? _embeddedReplacements;

    public JsonSanitizer(Pseudonymizer pseudonymizer)
    {
        _pseudonymizer = pseudonymizer;
    }

    /// <summary>
    /// Returns sanitised, indented copies of <paramref name="documents"/>, in the same order.
    /// </summary>
    /// <exception cref="JsonException">A document is not valid JSON.</exception>
    public IReadOnlyList<string> SanitizeAll(IReadOnlyList<string> documents)
    {
        List<JsonDocument> parsed = new(documents.Count);
        try
        {
            foreach (string json in documents)
            {
                parsed.Add(JsonDocument.Parse(json));
            }

            foreach (JsonDocument document in parsed)
            {
                Collect(document.RootElement, FieldKind.Keep);
            }

            _embeddedReplacements = null;
            return [.. parsed.Select(document => JsonText.Write(writer => Write(writer, document.RootElement, FieldKind.Keep)))];
        }
        finally
        {
            foreach (JsonDocument document in parsed)
            {
                document.Dispose();
            }
        }
    }

    /// <summary>
    /// Returns a sanitised, indented copy of one document. Values recorded from earlier calls still apply.
    /// </summary>
    /// <exception cref="JsonException"><paramref name="json"/> is not valid JSON.</exception>
    public string Sanitize(string json) => SanitizeAll([json])[0];

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

            case JsonValueKind.String:
                CollectEmbedded(element.GetString()!);
                break;

            case JsonValueKind.Number when kind is FieldKind.Identifier && IsInteger(element):
                Record(element.GetRawText(), kind);
                break;

            default:
                break;
        }
    }

    private void CollectEmbedded(string value)
    {
        using JsonDocument? embedded = TryParseEmbedded(value);
        if (embedded is not null)
        {
            Collect(embedded.RootElement, FieldKind.Keep);
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

        switch (kind)
        {
            case FieldKind.Location:
                return "0";

            case FieldKind.Identifier or FieldKind.PersonalText:
                return _replacements[value];

            default:
                if (value.Length >= MinimumExactMatchLength && _replacements.TryGetValue(value, out string? replacement))
                {
                    return replacement;
                }

                using (JsonDocument? embedded = TryParseEmbedded(value))
                {
                    return embedded is null ? ReplaceEmbedded(value) : WriteCompact(embedded.RootElement);
                }
        }
    }

    private string WriteCompact(JsonElement element)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream, CompactWriterOptions))
        {
            Write(writer, element, FieldKind.Keep);
        }

        return System.Text.Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
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

    /// <summary>
    /// Parses a string that holds a JSON object or array, such as <c>"{\"inverterSn\":\"…\"}"</c>.
    /// Returns <see langword="null"/> for anything else, which is almost every string.
    /// </summary>
    private static JsonDocument? TryParseEmbedded(string value)
    {
        string trimmed = value.Trim();
        bool looksLikeJson = trimmed.Length >= 2
            && ((trimmed[0] == '{' && trimmed[^1] == '}') || (trimmed[0] == '[' && trimmed[^1] == ']'));
        if (!looksLikeJson)
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(trimmed);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsInteger(JsonElement element)
    {
        string raw = element.GetRawText();
        return raw.Length > 0 && raw.All(char.IsAsciiDigit);
    }
}
