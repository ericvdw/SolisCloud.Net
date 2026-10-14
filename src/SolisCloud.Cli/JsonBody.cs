// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;

namespace SolisCloud.Cli;

/// <summary>
/// Builds a flat JSON request body, e.g. <c>{"id":"1298…","money":"ZAR","time":"2026-10-14","timeZone":2}</c>.
/// </summary>
/// <remarks>
/// IDs are copied from earlier responses as raw JSON, so a 19-digit ID is sent exactly as it was received (string
/// or number) and never passes through a <see langword="double"/>.
/// </remarks>
internal sealed class JsonBody
{
    private readonly List<Action<Utf8JsonWriter>> _fields = [];

    public JsonBody Add(string name, string value)
    {
        _fields.Add(writer => writer.WriteString(name, value));
        return this;
    }

    public JsonBody Add(string name, int value)
    {
        _fields.Add(writer => writer.WriteNumber(name, value));
        return this;
    }

    public JsonBody Add(string name, decimal value)
    {
        _fields.Add(writer => writer.WriteNumber(name, value));
        return this;
    }

    public JsonBody Add(string name, JsonElement value)
    {
        _fields.Add(writer =>
        {
            writer.WritePropertyName(name);
            value.WriteTo(writer);
        });
        return this;
    }

    /// <summary>
    /// Adds a paged-list request: <c>"pageNo":1,"pageSize":100</c>, the documented maximum page size.
    /// </summary>
    public static JsonBody FirstPage() => new JsonBody().Add("pageNo", 1).Add("pageSize", 100);

    /// <summary>
    /// Returns the body as compact UTF-8 JSON, ready to send.
    /// </summary>
    public byte[] ToUtf8()
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream))
        {
            writer.WriteStartObject();
            foreach (Action<Utf8JsonWriter> field in _fields)
            {
                field(writer);
            }

            writer.WriteEndObject();
        }

        return stream.ToArray();
    }
}
