// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using SolisCloud.Cli.Sanitizing;
using SolisCloud.Cli.Tests.TestSupport;

namespace SolisCloud.Cli.Tests;

public sealed class JsonSanitizerTests
{
    private readonly JsonSanitizer _sanitizer = new(new Pseudonymizer(Encoding.UTF8.GetBytes("test-key")));

    [Fact]
    public void Personal_data_is_removed()
    {
        string result = _sanitizer.Sanitize(SampleData.StationList);

        Assert.DoesNotContain(SampleData.StationId, result, StringComparison.Ordinal);
        Assert.DoesNotContain(SampleData.StationName, result, StringComparison.Ordinal);
        Assert.DoesNotContain(SampleData.Address, result, StringComparison.Ordinal);
        Assert.DoesNotContain("555 0123", result, StringComparison.Ordinal);
        Assert.DoesNotContain("33.9321", result, StringComparison.Ordinal);
        Assert.DoesNotContain("18.8602", result, StringComparison.Ordinal);
        Assert.DoesNotContain(CliHarness.ApiId, result, StringComparison.Ordinal);
    }

    [Fact]
    public void Measurements_and_settings_are_kept_exactly()
    {
        JsonElement station = Records(_sanitizer.Sanitize(SampleData.StationList))[0];

        Assert.Equal("8.2", station.GetProperty("capacity").GetRawText());
        Assert.Equal("2.00", station.GetProperty("timeZone").GetRawText());
        Assert.Equal("ZAR", station.GetProperty("money").GetString());
        Assert.Equal("115", station.GetProperty("timeZoneId").GetString());
        Assert.Equal(1, station.GetProperty("state").GetInt32());
    }

    [Fact]
    public void Replacements_keep_their_json_type()
    {
        JsonElement station = Records(_sanitizer.Sanitize(SampleData.StationList))[0];

        Assert.Equal(JsonValueKind.String, station.GetProperty("id").ValueKind);
        Assert.Equal(SampleData.StationId.Length, station.GetProperty("id").GetString()!.Length);
        Assert.Equal(JsonValueKind.Number, station.GetProperty("userId").ValueKind);
        Assert.Equal(CliHarness.ApiId.Length, station.GetProperty("userId").GetRawText().Length);
        Assert.Equal(JsonSanitizer.Redacted, station.GetProperty("stationName").GetString());
        Assert.Equal("0", station.GetProperty("latitude").GetString());
        Assert.Equal(0, station.GetProperty("longitude").GetInt32());
    }

    [Fact]
    public void Relationships_survive_across_documents()
    {
        JsonElement station = Records(_sanitizer.Sanitize(SampleData.StationList))[0];
        JsonElement inverter = Records(_sanitizer.Sanitize(SampleData.InverterList))[0];

        Assert.Equal(station.GetProperty("id").GetString(), inverter.GetProperty("stationId").GetString());
    }

    [Fact]
    public void Sensitive_values_are_found_under_unknown_names()
    {
        _ = _sanitizer.Sanitize(SampleData.StationList);

        string result = _sanitizer.Sanitize(
            "{\"title\":\"Van Wyk Family Home\",\"message\":\"Inverter at Van Wyk Family Home went offline\",\"plantKey\":\"1298491919448631809\"}");

        Assert.DoesNotContain(SampleData.StationName, result, StringComparison.Ordinal);
        Assert.DoesNotContain(SampleData.StationId, result, StringComparison.Ordinal);
        Assert.Contains("Inverter at REDACTED went offline", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Short_values_are_not_replaced_by_coincidence()
    {
        // "ZAR" and "1" are ordinary values; an ID of "1" must not turn every 1 into a fake.
        string result = _sanitizer.Sanitize("{\"id\":\"1\",\"state\":1,\"code\":\"1\",\"money\":\"ZAR\"}");
        using var document = JsonDocument.Parse(result);

        Assert.Equal(1, document.RootElement.GetProperty("state").GetInt32());
        Assert.Equal("1", document.RootElement.GetProperty("code").GetString());
        Assert.Equal("ZAR", document.RootElement.GetProperty("money").GetString());
    }

    [Fact]
    public void Every_serial_number_in_a_list_is_replaced()
    {
        string result = _sanitizer.Sanitize("{\"snList\":[\"120B40198150131\",\"120B40198150132\"]}");
        using var document = JsonDocument.Parse(result);

        string[] serials = [.. document.RootElement.GetProperty("snList").EnumerateArray().Select(e => e.GetString()!)];
        Assert.Equal(2, serials.Length);
        Assert.DoesNotContain("120B40198150131", serials, StringComparer.Ordinal);
        Assert.DoesNotContain("120B40198150132", serials, StringComparer.Ordinal);
        Assert.All(serials, serial => Assert.Equal(15, serial.Length));
    }

    [Fact]
    public void Null_empty_and_boolean_values_pass_through()
    {
        string result = _sanitizer.Sanitize("{\"stationName\":\"\",\"sn\":null,\"isS5\":false}");
        using var document = JsonDocument.Parse(result);

        Assert.Equal(string.Empty, document.RootElement.GetProperty("stationName").GetString());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("sn").ValueKind);
        Assert.False(document.RootElement.GetProperty("isS5").GetBoolean());
    }

    [Fact]
    public void Non_ascii_text_is_written_unescaped()
    {
        string result = _sanitizer.Sanitize("{\"tmpUnit\":\"℉\",\"dipStr\":\"30.0°\"}");

        Assert.Contains("℉", result, StringComparison.Ordinal);
        Assert.Contains("30.0°", result, StringComparison.Ordinal);
    }

    private static JsonElement[] Records(string json)
    {
        using var document = JsonDocument.Parse(json);
        return [.. document.RootElement.GetProperty("page").GetProperty("records").EnumerateArray().Select(e => e.Clone())];
    }
}
