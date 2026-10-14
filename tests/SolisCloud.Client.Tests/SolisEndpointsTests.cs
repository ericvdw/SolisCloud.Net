// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Tests;

public sealed class SolisEndpointsTests
{
    [Fact]
    public void All_lists_every_operation_in_the_api_document_once()
    {
        Assert.Equal(37, SolisEndpoints.All.Count);
        Assert.Equal(SolisEndpoints.All.Count, SolisEndpoints.All.Select(e => e.Path).Distinct(StringComparer.Ordinal).Count());
        Assert.All(SolisEndpoints.All, e => Assert.StartsWith("/v1/api/", e.Path, StringComparison.Ordinal));
    }

    [Fact]
    public void Only_station_administration_operations_modify_data()
    {
        string[] writes = [.. SolisEndpoints.All.Where(e => e.ModifiesData).Select(e => e.Name).OrderBy(n => n, StringComparer.Ordinal)];

        Assert.Equal(["addDevice", "addStation", "addStationBindCollector", "delCollector", "stationUpdate"], writes);
    }

    [Fact]
    public void Station_month_and_year_energy_lists_allow_ten_requests_per_second()
    {
        Assert.Equal(10, SolisEndpoints.StationMonthEnergyList.PermitsPerSecond);
        Assert.Equal(10, SolisEndpoints.StationYearEnergyList.PermitsPerSecond);
        Assert.Equal(2, SolisEndpoints.InverterDetail.PermitsPerSecond);
    }

    [Theory]
    [InlineData("inverterDetail")]
    [InlineData("INVERTERDETAIL")]
    [InlineData("/v1/api/inverterDetail")]
    public void Find_matches_name_or_path_ignoring_case(string nameOrPath)
    {
        Assert.Same(SolisEndpoints.InverterDetail, SolisEndpoints.Find(nameOrPath));
    }

    [Fact]
    public void Find_handles_names_containing_a_slash()
    {
        Assert.Same(SolisEndpoints.InverterWarranty, SolisEndpoints.Find("inverter/shelfTime"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("noSuchOperation")]
    public void Find_returns_null_when_nothing_matches(string? nameOrPath)
    {
        Assert.Null(SolisEndpoints.Find(nameOrPath));
    }

    [Fact]
    public void Constructor_derives_the_name_from_the_path()
    {
        Assert.Equal("collector/day", new SolisEndpoint("/v1/api/collector/day").Name);
        Assert.Equal("/v2/api/other", new SolisEndpoint("/v2/api/other").Name);
    }

    [Fact]
    public void Constructor_validates_arguments()
    {
        Assert.Throws<ArgumentException>(() => new SolisEndpoint("v1/api/x"));
        Assert.Throws<ArgumentException>(() => new SolisEndpoint(" "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SolisEndpoint("/v1/api/x", permitsPerSecond: 0));
    }
}
