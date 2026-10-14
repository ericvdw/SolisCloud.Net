// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;

namespace SolisCloud.Cli.Tests;

public sealed class JsonDataTests
{
    [Theory]
    [InlineData("{\"page\":{\"records\":[{},{}]}}", 2)]
    [InlineData("{\"records\":[{}]}", 1)]
    [InlineData("[{},{},{}]", 3)]
    [InlineData("{\"page\":{\"total\":0}}", 0)]
    [InlineData("null", 0)]
    public void Records_finds_the_list_wherever_it_is(string json, int expected)
    {
        using var document = JsonDocument.Parse(json);

        Assert.Equal(expected, JsonData.Records(document.RootElement).Count);
    }

    [Fact]
    public void TryGetText_returns_strings_and_numbers_as_sent()
    {
        using var document = JsonDocument.Parse("{\"a\":\"1298491919448631809\",\"b\":1298491919448631809,\"c\":10.00,\"d\":true}");
        JsonElement root = document.RootElement;

        Assert.Equal("1298491919448631809", JsonData.TryGetText(root, "a"));
        Assert.Equal("1298491919448631809", JsonData.TryGetText(root, "b"));
        Assert.Equal("10.00", JsonData.TryGetText(root, "c"));
        Assert.Null(JsonData.TryGetText(root, "d"));
        Assert.Null(JsonData.TryGetText(root, "missing"));
    }

    [Fact]
    public void TryGet_treats_null_and_empty_strings_as_missing()
    {
        using var document = JsonDocument.Parse("{\"a\":null,\"b\":\"\",\"c\":0}");

        Assert.Null(JsonData.TryGet(document.RootElement, "a"));
        Assert.Null(JsonData.TryGet(document.RootElement, "b"));
        Assert.NotNull(JsonData.TryGet(document.RootElement, "c"));
    }

    [Theory]
    [InlineData("{\"tz\":2.00}", 2)]
    [InlineData("{\"tz\":\"5.5\"}", 5.5)]
    [InlineData("{\"tz\":-9}", -9)]
    public void TryGetDecimal_accepts_numbers_and_numeric_strings(string json, double expected)
    {
        using var document = JsonDocument.Parse(json);

        Assert.Equal((decimal)expected, JsonData.TryGetDecimal(document.RootElement, "tz"));
    }
}
