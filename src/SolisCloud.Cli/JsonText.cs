// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SolisCloud.Cli;

/// <summary>
/// Formats JSON for people to read: indented, with non-ASCII text (°, ℉, 棵) left as-is rather than escaped.
/// </summary>
internal static class JsonText
{
    /// <summary>
    /// Writer settings for files and console output that are never embedded in HTML, so relaxed escaping is safe.
    /// </summary>
    public static JsonWriterOptions WriterOptions { get; } = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Re-formats a JSON value as indented text. Numbers keep their original representation (e.g. <c>10.00</c>).
    /// </summary>
    public static string Format(JsonElement element) => Write(writer => element.WriteTo(writer));

    /// <summary>
    /// Re-formats a JSON document as indented text.
    /// </summary>
    public static string Format(string json)
    {
        using var document = JsonDocument.Parse(json);
        return Format(document.RootElement);
    }

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="text"/> is a complete JSON value.
    /// </summary>
    public static bool IsValid(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Runs <paramref name="write"/> against an indented writer and returns the text produced.
    /// </summary>
    public static string Write(Action<Utf8JsonWriter> write)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream, WriterOptions))
        {
            write(writer);
        }

        return Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);
    }
}
