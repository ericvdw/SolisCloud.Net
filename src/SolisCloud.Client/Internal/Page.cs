// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;

namespace SolisCloud.Internal;

/// <summary>
/// One page of a list response. Most lists nest it as <c>data.page</c>; the inverter detail list and the alarm list
/// put it directly in <c>data</c>.
/// </summary>
internal sealed class Page
{
    private Page(IReadOnlyList<JsonElement> records, int pages)
    {
        Records = records;
        Pages = pages;
    }

    public IReadOnlyList<JsonElement> Records { get; }

    /// <summary>Gets the total number of pages, or 1 if the response doesn't say.</summary>
    public int Pages { get; }

    public static Page Read(JsonElement data)
    {
        FieldReader f = new(data);
        JsonElement page = f.Value("page") is { ValueKind: JsonValueKind.Object } nested ? nested : data;
        FieldReader p = new(page);

        IReadOnlyList<JsonElement> records = p.Value("records") is { ValueKind: JsonValueKind.Array } array
            ? [.. array.EnumerateArray()]
            : [];
        return new Page(records, p.Int32("pages") is int pages && pages > 0 ? pages : 1);
    }
}
