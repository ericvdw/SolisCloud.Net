// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;

namespace SolisCloud.Internal;

/// <summary>
/// Builds a flat JSON request body, e.g. <c>{"id":"1298…","money":"ZAR","time":"2026-10-14","timeZone":2}</c>.
/// </summary>
internal sealed class RequestBody
{
    private readonly List<Action<Utf8JsonWriter>> _fields = [];

    public RequestBody Add(string name, string value)
    {
        _fields.Add(writer => writer.WriteString(name, value));
        return this;
    }

    public RequestBody Add(string name, int value)
    {
        _fields.Add(writer => writer.WriteNumber(name, value));
        return this;
    }

    /// <summary>
    /// Adds a UTC offset as hours, e.g. <c>2</c> or <c>5.5</c>, the form the <c>timeZone</c> parameter takes.
    /// </summary>
    public RequestBody Add(string name, TimeSpan offset)
    {
        _fields.Add(writer => writer.WriteNumber(name, offset.TotalHours));
        return this;
    }

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
