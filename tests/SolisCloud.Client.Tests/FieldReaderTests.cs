// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;
using SolisCloud.Internal;
using SolisCloud.Models;

namespace SolisCloud.Tests;

public sealed class FieldReaderTests
{
    [Theory]
    [InlineData("{\"pac\":0.589,\"pacStr\":\"kW\"}", 589)]
    [InlineData("{\"pac\":589.0}", 589)]
    [InlineData("{\"pac\":1154,\"pacPec\":\"0.001\",\"pacStr\":\"kW\"}", 1154)]
    [InlineData("{\"pac\":\"2.5\",\"pacStr\":\"MW\"}", 2_500_000)]
    [InlineData("{\"pac\":5.6,\"pacStr\":\"kWp\"}", 5600)]
    // The unit can change between records of one series: on its first day this inverter reported "W" and a scale of 1.
    [InlineData("{\"pac\":57,\"pacPec\":\"1\",\"pacStr\":\"W\"}", 57)]
    public void Power_applies_the_unit_and_scale(string json, double expectedWatts)
    {
        Power? power = Read(json).Power("pac", "W");

        Assert.NotNull(power);
        Assert.Equal(expectedWatts, power.Value.Watts, precision: 6);
    }

    [Theory]
    [InlineData("{\"e\":4.783,\"eStr\":\"MWh\"}", 4783)]
    [InlineData("{\"e\":4770.9,\"ePec\":\"0.001\",\"eStr\":\"MWh\"}", 4770.9)]
    [InlineData("{\"e\":22.7}", 22.7)]
    public void Energy_applies_the_unit_and_scale(string json, double expectedKilowattHours)
    {
        Energy? energy = Read(json).Energy("e", "kWh");

        Assert.NotNull(energy);
        Assert.Equal(expectedKilowattHours, energy.Value.KilowattHours, precision: 6);
    }

    [Theory]
    [InlineData("{\"pac\":1,\"pacStr\":\"V\"}")]
    [InlineData("{\"pac\":\"\"}")]
    [InlineData("{\"pac\":null}")]
    [InlineData("{}")]
    public void Missing_values_and_foreign_units_give_null(string json)
    {
        Assert.Null(Read(json).Power("pac", "W"));
    }

    [Fact]
    public void Strings_keep_large_ids_exactly()
    {
        FieldReader f = Read("{\"a\":\"6426147095506735008\",\"b\":6426147095506735008}");

        Assert.Equal("6426147095506735008", f.String("a"));
        Assert.Equal("6426147095506735008", f.String("b"));
    }

    [Fact]
    public void Numbers_timestamps_and_offsets()
    {
        FieldReader f = Read("{\"n\":\"72.0\",\"ts\":\"1791988012709\",\"tz\":2.0,\"half\":5.5,\"bad\":99}");

        Assert.Equal(72.0, f.Number("n"));
        Assert.Equal(new DateTimeOffset(2026, 10, 14, 14, 26, 52, 709, TimeSpan.Zero), f.UnixMilliseconds("ts"));
        Assert.Equal(TimeSpan.FromHours(2), f.Hours("tz"));
        Assert.Equal(TimeSpan.FromMinutes(330), f.Hours("half"));
        Assert.Null(f.Hours("bad"));
    }

    [Theory]
    [InlineData("{\"s\":1}", DeviceState.Online)]
    [InlineData("{\"s\":\"3\"}", DeviceState.Alarm)]
    public void Enums_accept_numbers_and_strings(string json, DeviceState expected)
    {
        Assert.Equal(expected, Read(json).Enum<DeviceState>("s"));
    }

    [Theory]
    [InlineData("{\"s\":9}")]
    [InlineData("{\"s\":\"x\"}")]
    [InlineData("{}")]
    public void Undefined_enum_values_give_null(string json)
    {
        Assert.Null(Read(json).Enum<DeviceState>("s"));
    }

    private static FieldReader Read(string json)
    {
        using var document = JsonDocument.Parse(json);
        return new FieldReader(document.RootElement.Clone());
    }
}
