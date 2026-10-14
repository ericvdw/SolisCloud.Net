// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;
using SolisCloud.Models;

namespace SolisCloud.Tests.Models;

public sealed class EnergyTests
{
    [Fact]
    public void Converts_between_units()
    {
        Assert.Equal(4783, Energy.FromMegawattHours(4.783).KilowattHours, precision: 9);
        Assert.Equal(0.39, Energy.FromWattHours(390).KilowattHours, precision: 12);
        Assert.Equal(22_700, Energy.FromKilowattHours(22.7).WattHours, precision: 9);
        Assert.Equal(4.783, Energy.FromKilowattHours(4783).MegawattHours, precision: 12);
    }

    [Fact]
    public void Arithmetic_and_comparison()
    {
        var produced = Energy.FromKilowattHours(22.7);
        var consumed = Energy.FromKilowattHours(19.09);

        Assert.Equal(3.61, (produced - consumed).KilowattHours, precision: 9);
        Assert.True(produced > consumed);
        Assert.Equal(Energy.Zero, produced + (-produced));
    }

    [Theory]
    [InlineData(22.7, null, "22.7 kWh")]
    [InlineData(4783, null, "4.783 MWh")]
    [InlineData(4783, "kWh", "4783 kWh")]
    [InlineData(272.4, "MWh", "0.272 MWh")]
    public void Formats_by_magnitude_or_as_asked(double kilowattHours, string? format, string expected)
    {
        Assert.Equal(expected, Energy.FromKilowattHours(kilowattHours).ToString(format, CultureInfo.InvariantCulture));
    }
}
