// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using SolisCloud.Models;
using SolisCloud.Tests.TestSupport;

namespace SolisCloud.Tests;

/// <summary>
/// The typed operations against recorded responses from a real single-phase hybrid system.
/// </summary>
public sealed class QueryTests
{
    private const string StationId = "6426147095506735008";
    private const string InverterSn = "1666762972173069";
    private static readonly TimeSpan SouthAfrica = TimeSpan.FromHours(2);
    private static readonly DateTime CaptureDay = new(2026, 10, 14);

    [Fact]
    public async Task Stations_from_the_list()
    {
        using FixtureClient fixture = new();

        List<Station> stations = await fixture.Client.GetStationsAsync(TestContext.Current.CancellationToken).CollectAsync();

        Station station = Assert.Single(stations);
        Assert.Equal(StationId, station.Id);
        Assert.Equal(DeviceState.Online, station.State);
        Assert.Equal(SouthAfrica, station.UtcOffset);
        Assert.Equal("ZAR", station.Currency);
        Assert.Equal(1, station.InverterCount);
        Assert.Equal(5.6, station.Capacity!.Value.Kilowatts, precision: 9);
        Assert.Equal(997, station.Power!.Value.Watts, precision: 6);
        Assert.Equal(22.7, station.EnergyToday!.Value.KilowattHours, precision: 9);
        Assert.Equal(4782, station.EnergyTotal!.Value.KilowattHours, precision: 6);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1791988136023), station.LastUpdated);

        // Live battery and grid figures are only in the details.
        Assert.Null(station.BatteryPower);
        Assert.Equal("{\"pageNo\":1,\"pageSize\":100}", fixture.Handler.Requests[0].Body);
    }

    [Fact]
    public async Task Station_details_add_live_battery_grid_and_load()
    {
        using FixtureClient fixture = new();

        Station station = await fixture.Client.GetStationAsync(StationId, TestContext.Current.CancellationToken);

        Assert.Equal(-1978, station.BatteryPower!.Value.Watts, precision: 6);
        Assert.Equal(-32, station.GridPower!.Value.Watts, precision: 6);
        Assert.Equal(2590, station.LoadPower!.Value.Watts, precision: 6);
        Assert.Equal(73, station.BatteryStateOfCharge);
        Assert.Equal(0.39, station.GridImportedToday!.Value.KilowattHours, precision: 9);
        Assert.Equal(10, station.BatteryChargedToday!.Value.KilowattHours, precision: 9);
        Assert.Equal("{\"id\":\"" + StationId + "\"}", fixture.Handler.Requests[0].Body);
    }

    [Fact]
    public async Task Inverters_with_details()
    {
        using FixtureClient fixture = new();

        List<Inverter> inverters = await fixture.Client.GetInvertersAsync(TestContext.Current.CancellationToken).CollectAsync();

        Inverter inverter = Assert.Single(inverters);
        Assert.Equal(InverterSn, inverter.SerialNumber);
        Assert.Equal(StationId, inverter.StationId);
        Assert.Equal("S6-EH1P8K-L-PLUS", inverter.Model);
        Assert.Equal("270014", inverter.SoftwareVersion);
        Assert.Equal("7O53507254I41110", inverter.CollectorSerialNumber);
        Assert.Equal(8, inverter.RatedPower!.Value.Kilowatts, precision: 9);
        Assert.Equal(4783, inverter.EnergyTotal!.Value.KilowattHours, precision: 6);
        Assert.Equal(50.08, inverter.GridFrequency);
        Assert.Equal(45.1, inverter.Temperature);
        Assert.Equal(72, inverter.BatteryStateOfCharge);
        Assert.Equal(99, inverter.BatteryStateOfHealth);
        Assert.Equal(-37.4, inverter.BatteryCurrent);
        Assert.Equal(-1978, inverter.BatteryPower!.Value.Watts, precision: 6);

        // Two MPPT inputs (dcInputtype 1), one AC phase (acOutputType 0).
        Assert.Equal(2, inverter.PvStrings.Count);
        Assert.Equal(190.2, inverter.PvStrings[0].Voltage);
        Assert.Equal(3.1, inverter.PvStrings[0].Current);
        Assert.Equal(590, inverter.PvStrings[0].Power!.Value.Watts, precision: 6);
        AcPhase phase = Assert.Single(inverter.AcPhases);
        Assert.Equal(237.0, phase.Voltage);
        Assert.Equal(SolisEndpoints.InverterDetailList.Path, fixture.Handler.Requests[0].Path);
    }

    [Fact]
    public async Task One_inverter_by_serial_number()
    {
        using FixtureClient fixture = new();

        Inverter inverter = await fixture.Client.GetInverterAsync(InverterSn, TestContext.Current.CancellationToken);

        Assert.Equal(InverterSn, inverter.SerialNumber);
        Assert.Equal(22.7, inverter.EnergyToday!.Value.KilowattHours, precision: 9);
        Assert.Equal("{\"sn\":\"" + InverterSn + "\"}", fixture.Handler.Requests[0].Body);
    }

    [Fact]
    public async Task Inverter_day_in_local_time_with_scaled_units()
    {
        using FixtureClient fixture = new();

        IReadOnlyList<InverterSample> samples = await fixture.Client.GetInverterDayAsync(
            InverterSn, CaptureDay, SouthAfrica, TestContext.Current.CancellationToken);

        Assert.Equal(198, samples.Count);
        Assert.Equal(new DateTimeOffset(2026, 10, 14, 0, 1, 40, SouthAfrica), TruncateMilliseconds(samples[0].Time));

        InverterSample last = samples[samples.Count - 1];
        Assert.Equal(new DateTimeOffset(2026, 10, 14, 16, 26, 52, SouthAfrica), TruncateMilliseconds(last.Time));
        Assert.Equal(SouthAfrica, last.Time.Offset);

        // "pac": 589, "pacPec": "0.001", "pacStr": "kW" is 0.589 kW.
        Assert.Equal(589, last.AcPower!.Value.Watts, precision: 6);
        Assert.Equal(-1978, last.BatteryPower!.Value.Watts, precision: 6);
        Assert.Equal(-32, last.GridPower!.Value.Watts, precision: 6);
        Assert.Equal(2590, last.LoadPower!.Value.Watts, precision: 6);
        Assert.Equal(72, last.BatteryStateOfCharge);
        Assert.Equal(22.7, last.EnergyToday!.Value.KilowattHours, precision: 9);
        Assert.Equal(2, last.PvStrings.Count);

        Assert.Equal(
            "{\"sn\":\"" + InverterSn + "\",\"money\":\"ZAR\",\"time\":\"2026-10-14\",\"timeZone\":2}",
            fixture.Handler.Requests[0].Body);
    }

    [Fact]
    public async Task Station_day_balances()
    {
        using FixtureClient fixture = new();

        IReadOnlyList<StationSample> points = await fixture.Client.GetStationDayAsync(
            StationId, CaptureDay, SouthAfrica, TestContext.Current.CancellationToken);

        StationSample last = points[points.Count - 1];
        Assert.Equal(new DateTimeOffset(2026, 10, 14, 16, 25, 0, SouthAfrica), last.Time);
        Assert.Equal(589, last.Power!.Value.Watts, precision: 6);
        Assert.Equal(2599, last.LoadPower!.Value.Watts, precision: 6);

        // Load = generation - export - charging, at every point.
        Assert.All(points, point => Assert.Equal(
            point.LoadPower!.Value.Watts,
            (point.Power!.Value - point.GridPower!.Value - point.BatteryPower!.Value).Watts,
            precision: 6));
    }

    [Fact]
    public async Task Station_month_year_and_lifetime_totals()
    {
        using FixtureClient fixture = new();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        IReadOnlyList<EnergyRecord> days = await fixture.Client.GetStationMonthAsync(StationId, 2026, 10, SouthAfrica, cancellationToken);
        IReadOnlyList<EnergyRecord> months = await fixture.Client.GetStationYearAsync(StationId, 2026, SouthAfrica, cancellationToken);
        IReadOnlyList<EnergyRecord> years = await fixture.Client.GetStationLifetimeAsync(StationId, SouthAfrica, cancellationToken);

        Assert.Equal(14, days.Count);
        Assert.Equal(new DateTime(2026, 10, 1), days[0].PeriodStart);
        EnergyRecord today = days[days.Count - 1];
        Assert.Equal(new DateTime(2026, 10, 14), today.PeriodStart);
        Assert.Equal(22.7, today.Produced!.Value.KilowattHours, precision: 9);
        Assert.Equal(19.09, today.Consumed!.Value.KilowattHours, precision: 9);
        Assert.Equal(0.39, today.GridImported!.Value.KilowattHours, precision: 9);
        Assert.Equal(10, today.BatteryCharged!.Value.KilowattHours, precision: 9);
        Assert.Equal(79.45m, today.Income);

        Assert.Equal(10, months.Count);
        Assert.Equal(new DateTime(2026, 10, 1), months[months.Count - 1].PeriodStart);

        // "money": 953.4, "moneyPec": "0.001", "moneyStr": "K ZAR" is R953.40.
        Assert.Equal(953.4m, months[months.Count - 1].Income);

        EnergyRecord lifetime = Assert.Single(years);
        Assert.Equal(new DateTime(2026, 1, 1), lifetime.PeriodStart);
        Assert.Equal(4770.9, lifetime.Produced!.Value.KilowattHours, precision: 6);
        Assert.Equal(16698.15m, lifetime.Income);
    }

    [Fact]
    public async Task Month_must_be_valid()
    {
        using FixtureClient fixture = new();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Client.GetStationMonthAsync(StationId, 2026, 13, SouthAfrica, TestContext.Current.CancellationToken));
        Assert.Empty(fixture.Handler.Requests);
    }

    [Fact]
    public async Task Alarms()
    {
        using FixtureClient fixture = new();

        List<Alarm> alarms = await fixture.Client.GetAlarmsAsync(TestContext.Current.CancellationToken).CollectAsync();

        Assert.Equal(28, alarms.Count);
        Alarm first = alarms[0];
        Assert.Equal("1015", first.Code);
        Assert.Equal("NO-Grid", first.Message);
        Assert.Equal("No Action Required", first.Advice);
        Assert.Equal(AlarmLevel.Tip, first.Level);
        Assert.Equal(AlarmState.Resolved, first.State);
        Assert.Equal(InverterSn, first.DeviceSerialNumber);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1788601864000), first.Started);
        Assert.True(first.Ended > first.Started);
    }

    [Fact]
    public async Task Paging_continues_until_a_short_page()
    {
        using FixtureClient fixture = new();
        string fullPage = "[" + string.Join(",", Enumerable.Repeat("{\"id\":\"1\"}", 100)) + "]";
        fixture.Handler.Respond(
            SolisEndpoints.AlarmList,
            StubHttpMessageHandler.SuccessBody("{\"records\":" + fullPage + ",\"pages\":2}"));

        // Every request gets the same full page, so this stops on "pages": 2, after two requests.
        List<Alarm> alarms = await fixture.Client.GetAlarmsAsync(TestContext.Current.CancellationToken).CollectAsync();

        Assert.Equal(200, alarms.Count);
        Assert.Equal(2, fixture.Handler.Requests.Count);
        Assert.Contains("\"pageNo\":2", fixture.Handler.Requests[1].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Raw_keeps_every_field()
    {
        using FixtureClient fixture = new();

        Inverter inverter = await fixture.Client.GetInverterAsync(InverterSn, TestContext.Current.CancellationToken);

        Assert.Equal("NRS097", inverter.Raw.GetProperty("nationalStandardstr").GetString());
    }

    private static DateTimeOffset TruncateMilliseconds(DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Offset);
}
