// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using SolisCloud.Models;
using SolisCloud.Tests.TestSupport;

namespace SolisCloud.Tests;

/// <summary>
/// The typed operations against recorded responses from a real single-phase hybrid system.
/// </summary>
public sealed class QueryTests
{
    private const string StationId = "5225445071986602902";
    private const string InverterSn = "1243838603845561";
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
        Assert.Equal(7, station.Power!.Value.Watts, precision: 6);
        Assert.Equal(23.2, station.EnergyToday!.Value.KilowattHours, precision: 9);
        Assert.Equal(4783, station.EnergyTotal!.Value.KilowattHours, precision: 6);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1791994651589), station.LastUpdated);

        // Live battery and grid figures are only in the details.
        Assert.Null(station.BatteryPower);
        Assert.Equal("{\"pageNo\":1,\"pageSize\":100}", fixture.Handler.Requests[0].Body);
    }

    [Fact]
    public async Task Station_details_add_live_battery_grid_and_load()
    {
        using FixtureClient fixture = new();

        Station station = await fixture.Client.GetStationAsync(StationId, TestContext.Current.CancellationToken);

        Assert.Equal(-460, station.BatteryPower!.Value.Watts, precision: 6);
        Assert.Equal(0, station.GridPower!.Value.Watts, precision: 6);
        Assert.Equal(440, station.LoadPower!.Value.Watts, precision: 6);
        Assert.Equal(61, station.BatteryStateOfCharge);
        Assert.Equal("Africa/Harare", station.TimeZoneId);
        Assert.Equal(0.42, station.GridImportedToday!.Value.KilowattHours, precision: 9);
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
        Assert.Equal("2Z27895689H95440", inverter.CollectorSerialNumber);
        Assert.Equal(8, inverter.RatedPower!.Value.Kilowatts, precision: 9);
        Assert.Equal(4783, inverter.EnergyTotal!.Value.KilowattHours, precision: 6);
        Assert.Equal(50.1, inverter.GridFrequency);
        Assert.Equal(44.8, inverter.Temperature);
        Assert.Equal(61, inverter.BatteryStateOfCharge);
        Assert.Equal(99, inverter.BatteryStateOfHealth);
        Assert.Equal(-8.8, inverter.BatteryCurrent);
        Assert.Equal(-460, inverter.BatteryPower!.Value.Watts, precision: 6);

        // Two MPPT inputs (dcInputtype 1), one AC phase (acOutputType 0).
        Assert.Equal(2, inverter.PvStrings.Count);
        Assert.Equal(74.9, inverter.PvStrings[0].Voltage);
        Assert.Equal(0.1, inverter.PvStrings[0].Current);
        Assert.Equal(7, inverter.PvStrings[0].Power!.Value.Watts, precision: 6);
        AcPhase phase = Assert.Single(inverter.AcPhases);
        Assert.Equal(236.1, phase.Voltage);
        Assert.Equal(SolisEndpoints.InverterDetailList.Path, fixture.Handler.Requests[0].Path);
    }

    [Fact]
    public async Task One_inverter_by_serial_number()
    {
        using FixtureClient fixture = new();

        Inverter inverter = await fixture.Client.GetInverterAsync(InverterSn, TestContext.Current.CancellationToken);

        Assert.Equal(InverterSn, inverter.SerialNumber);
        Assert.Equal(23.2, inverter.EnergyToday!.Value.KilowattHours, precision: 9);
        Assert.Equal("{\"sn\":\"" + InverterSn + "\"}", fixture.Handler.Requests[0].Body);
    }

    [Fact]
    public async Task Inverter_day_in_local_time_with_scaled_units()
    {
        using FixtureClient fixture = new();

        IReadOnlyList<InverterSample> samples = await fixture.Client.GetInverterDayAsync(
            InverterSn, CaptureDay, SouthAfrica, TestContext.Current.CancellationToken);

        Assert.Equal(220, samples.Count);
        Assert.Equal(new DateTimeOffset(2026, 10, 14, 0, 1, 40, SouthAfrica), TruncateMilliseconds(samples[0].Time));

        InverterSample last = samples[samples.Count - 1];
        Assert.Equal(new DateTimeOffset(2026, 10, 14, 18, 16, 53, SouthAfrica), TruncateMilliseconds(last.Time));
        Assert.Equal(SouthAfrica, last.Time.Offset);

        // "pac": 7, "pacPec": "0.001", "pacStr": "kW" is 0.007 kW.
        Assert.Equal(7, last.AcPower!.Value.Watts, precision: 6);
        Assert.Equal(-460, last.BatteryPower!.Value.Watts, precision: 6);
        Assert.Equal(0, last.GridPower!.Value.Watts, precision: 6);
        Assert.Equal(440, last.LoadPower!.Value.Watts, precision: 6);
        Assert.Equal(61, last.BatteryStateOfCharge);
        Assert.Equal(23.2, last.EnergyToday!.Value.KilowattHours, precision: 9);
        Assert.Equal(2, last.PvStrings.Count);

        // The day series has no "pow1": DC power is worked out from "uPv1": 192.1 and "iPv1": 4.6.
        InverterSample morning = samples[110];
        Assert.Equal(192.1 * 4.6, morning.PvStrings[0].Power!.Value.Watts, precision: 6);
        Assert.Equal(0, morning.PvStrings[1].Power!.Value.Watts, precision: 6);

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
        Assert.Equal(new DateTimeOffset(2026, 10, 14, 18, 15, 0, SouthAfrica), last.Time);
        Assert.Equal(7, last.Power!.Value.Watts, precision: 6);
        Assert.Equal(467, last.LoadPower!.Value.Watts, precision: 6);

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
        Assert.Equal(23.2, today.Produced!.Value.KilowattHours, precision: 9);
        Assert.Equal(21.62, today.Consumed!.Value.KilowattHours, precision: 9);
        Assert.Equal(0.42, today.GridImported!.Value.KilowattHours, precision: 9);
        Assert.Equal(10, today.BatteryCharged!.Value.KilowattHours, precision: 9);
        Assert.Equal(81.2m, today.Income);

        Assert.Equal(10, months.Count);
        Assert.Equal(new DateTime(2026, 10, 1), months[months.Count - 1].PeriodStart);

        // "money": 955.15, "moneyPec": "0.001", "moneyStr": "K ZAR" is R955.15.
        Assert.Equal(955.15m, months[months.Count - 1].Income);

        EnergyRecord lifetime = Assert.Single(years);
        Assert.Equal(new DateTime(2026, 1, 1), lifetime.PeriodStart);
        Assert.Equal(4771.4, lifetime.Produced!.Value.KilowattHours, precision: 6);
        Assert.Equal(16699.9m, lifetime.Income);
    }

    [Fact]
    public async Task Load_splits_into_backup_and_grid_side()
    {
        using FixtureClient fixture = new();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        // Details: everything was on the backup port at capture time ("totalLoadPower" = "bypassLoadPower" = 0.44 kW).
        // The inverter's own "familyLoadPower" says 0.44 too and the station's says 0, so neither is used.
        Station station = await fixture.Client.GetStationAsync(StationId, cancellationToken);
        Inverter inverter = await fixture.Client.GetInverterAsync(InverterSn, cancellationToken);
        foreach ((Power? backup, Power? nonBackup, Energy? backupToday) in new[]
        {
            (station.BackupLoadPower, station.NonBackupLoadPower, station.BackupLoadToday),
            (inverter.BackupLoadPower, inverter.NonBackupLoadPower, inverter.BackupLoadToday),
        })
        {
            Assert.Equal(440, backup!.Value.Watts, precision: 6);
            Assert.Equal(0, nonBackup!.Value.Watts, precision: 6);
            Assert.Equal(12.3, backupToday!.Value.KilowattHours, precision: 9);
        }

        // Day series: "familyLoadPower": 97 (grid side) and "bypassLoadPower": 1070 (backup port) at 09:31.
        IReadOnlyList<InverterSample> samples = await fixture.Client.GetInverterDayAsync(InverterSn, CaptureDay, SouthAfrica, cancellationToken);
        InverterSample sample = samples[114];
        Assert.Equal(97, sample.NonBackupLoadPower!.Value.Watts, precision: 6);
        Assert.Equal(1070, sample.BackupLoadPower!.Value.Watts, precision: 6);
        Assert.Equal(1167, sample.LoadPower!.Value.Watts, precision: 6);

        // SolisCloud's consumption is a balance, not a measurement: 23.2 + 0.42 - 0 - 10 + 8 = 21.62.
        IReadOnlyList<EnergyRecord> days = await fixture.Client.GetStationMonthAsync(StationId, 2026, 10, SouthAfrica, cancellationToken);
        EnergyRecord today = days[days.Count - 1];
        double balance = today.Produced!.Value.KilowattHours + today.GridImported!.Value.KilowattHours
            - today.GridExported!.Value.KilowattHours - today.BatteryCharged!.Value.KilowattHours
            + today.BatteryDischarged!.Value.KilowattHours;
        Assert.Equal(balance, today.Consumed!.Value.KilowattHours, precision: 9);
        Assert.Equal(12.3, today.BackupLoad!.Value.KilowattHours, precision: 9);
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
    public async Task Data_loggers_with_their_signal()
    {
        using FixtureClient fixture = new();

        List<DataLogger> loggers = await fixture.Client.GetDataLoggersAsync(TestContext.Current.CancellationToken).CollectAsync();

        DataLogger logger = Assert.Single(loggers);
        Assert.Equal("2Z27895689H95440", logger.SerialNumber);
        Assert.Equal(StationId, logger.StationId);
        Assert.Equal("WL", logger.Model);
        Assert.Equal(DeviceState.Online, logger.State);
        Assert.Equal(-82, logger.SignalStrength);
        Assert.Equal(1, logger.SignalLevel);
        Assert.Equal(TimeSpan.FromMinutes(5), logger.UploadInterval);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1791994613154), logger.LastUpdated);
    }

    [Fact]
    public async Task Data_logger_day_in_local_time()
    {
        using FixtureClient fixture = new();

        IReadOnlyList<DataLoggerSample> samples = await fixture.Client.GetDataLoggerDayAsync(
            "2Z27895689H95440", CaptureDay, SouthAfrica, TestContext.Current.CancellationToken);

        Assert.Equal(220, samples.Count);

        // "timeStr" is "06:01:40" here, in China time (UTC+8); "dataTimestamp" gives the true time.
        Assert.Equal(new DateTimeOffset(2026, 10, 14, 0, 1, 40, SouthAfrica), TruncateMilliseconds(samples[0].Time));
        Assert.Equal(-52, samples[0].SignalStrength);
        Assert.Equal(4, samples[0].SignalLevel);
        Assert.Equal(
            "{\"sn\":\"2Z27895689H95440\",\"time\":\"2026-10-14\",\"timeZone\":2}",
            fixture.Handler.Requests[0].Body);
    }

    [Fact]
    public async Task An_unknown_station_is_not_found()
    {
        using FixtureClient fixture = new();

        // What SolisCloud really returns for an ID it doesn't know.
        fixture.Handler.Respond(SolisEndpoints.StationDetail, StubHttpMessageHandler.SuccessBody("null"));

        SolisCloudNotFoundException exception = await Assert.ThrowsAsync<SolisCloudNotFoundException>(
            () => fixture.Client.GetStationAsync("1", TestContext.Current.CancellationToken));
        Assert.Equal("1", exception.Id);
        Assert.Equal(SolisEndpoints.StationDetail.Path, exception.EndpointPath);
    }

    [Fact]
    public async Task An_unknown_inverter_is_not_found()
    {
        using FixtureClient fixture = new();
        fixture.Handler.Respond(SolisEndpoints.InverterDetail, StubHttpMessageHandler.SuccessBody("{}"));

        _ = await Assert.ThrowsAsync<SolisCloudNotFoundException>(
            () => fixture.Client.GetInverterAsync("0000000000000000", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_day_with_no_data_is_empty()
    {
        using FixtureClient fixture = new();

        // A future date, a date before installation and an impossible date all come back as "data": null.
        fixture.Handler.Respond(SolisEndpoints.StationDay, StubHttpMessageHandler.SuccessBody("null"));

        IReadOnlyList<StationSample> points = await fixture.Client.GetStationDayAsync(
            StationId, new DateTime(2027, 1, 1), SouthAfrica, TestContext.Current.CancellationToken);
        Assert.Empty(points);
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
