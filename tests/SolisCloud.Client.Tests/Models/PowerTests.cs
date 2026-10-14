// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;
using SolisCloud.Models;

namespace SolisCloud.Tests.Models;

public sealed class PowerTests
{
    [Fact]
    public void Converts_between_watts_and_kilowatts()
    {
        Assert.Equal(1154, Power.FromKilowatts(1.154).Watts, precision: 9);
        Assert.Equal(1.154, Power.FromWatts(1154).Kilowatts, precision: 12);
    }

    [Fact]
    public void Arithmetic_and_comparison()
    {
        var pv = Power.FromWatts(589);
        var battery = Power.FromWatts(-1978);

        Assert.Equal(Power.FromWatts(-1389), pv + battery);
        Assert.Equal(Power.FromWatts(2567), pv - battery);
        Assert.Equal(Power.FromWatts(1978), -battery);
        Assert.True(battery < pv);
        Assert.True(pv >= Power.FromWatts(589));
        Assert.Equal(Power.Zero, default);
        Assert.Equal(Power.Add(pv, battery), pv + battery);
    }

    [Theory]
    [InlineData(590, null, "590 W")]
    [InlineData(1154, null, "1.154 kW")]
    [InlineData(-1978, null, "-1.978 kW")]
    [InlineData(1154, "W", "1154 W")]
    [InlineData(590, "kW", "0.59 kW")]
    public void Formats_by_magnitude_or_as_asked(double watts, string? format, string expected)
    {
        Assert.Equal(expected, Power.FromWatts(watts).ToString(format, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Rejects_unknown_formats_and_types()
    {
        _ = Assert.Throws<FormatException>(() => Power.Zero.ToString("MW", CultureInfo.InvariantCulture));
        _ = Assert.Throws<ArgumentException>(() => Power.Zero.CompareTo("0 W"));
        Assert.Equal(1, Power.Zero.CompareTo(null));
    }
}
