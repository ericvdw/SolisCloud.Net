// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using SolisCloud.Internal;
using SolisCloud.Models;

namespace SolisCloud;

/// <content>
/// Typed read operations. Each returns normalised models and keeps the full JSON in their <c>Raw</c> property;
/// <see cref="SendAsync"/> remains available for anything not covered here.
/// </content>
public sealed partial class SolisCloudClient
{
    // The documented maximum page size.
    private const int PageSize = 100;

    /// <summary>
    /// Lists the stations the account can see, fetching further pages as the sequence is read.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the requests.</param>
    /// <returns>The stations, as summarised in the station list.</returns>
    /// <exception cref="SolisCloudException">A request failed.</exception>
    public IAsyncEnumerable<Station> GetStationsAsync(CancellationToken cancellationToken = default) =>
        GetPagesAsync(SolisEndpoints.StationList, static json => new Station(json), cancellationToken);

    /// <summary>
    /// Gets a station's details, including live battery, grid and load figures.
    /// </summary>
    /// <param name="stationId">The station ID, e.g. from <see cref="GetStationsAsync"/>.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The station.</returns>
    /// <exception cref="SolisCloudNotFoundException">There is no such station, or the account can't see it.</exception>
    /// <exception cref="SolisCloudException">The request failed.</exception>
    public async Task<Station> GetStationAsync(string stationId, CancellationToken cancellationToken = default)
    {
        Guard.NotNullOrWhiteSpace(stationId);
        SolisCloudResponse response = await SendAsync(
            SolisEndpoints.StationDetail,
            new RequestBody().Add("id", stationId).ToUtf8(),
            cancellationToken).ConfigureAwait(false);

        // An unknown ID is a success with "data": null.
        Station station = new(RequireObject(response.Data, SolisEndpoints.StationDetail, stationId));
        return station.Id.Length > 0 ? station : throw new SolisCloudNotFoundException(SolisEndpoints.StationDetail.Path, stationId);
    }

    /// <summary>
    /// Lists every inverter the account can see, with full details, fetching further pages as the sequence is read.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the requests.</param>
    /// <returns>The inverters.</returns>
    /// <exception cref="SolisCloudException">A request failed.</exception>
    /// <remarks>
    /// Uses <see cref="SolisEndpoints.InverterDetailList"/>, which returns up to 100 inverters' details per request,
    /// so polling a whole account stays well inside the rate limit.
    /// </remarks>
    public IAsyncEnumerable<Inverter> GetInvertersAsync(CancellationToken cancellationToken = default) =>
        GetPagesAsync(SolisEndpoints.InverterDetailList, static json => new Inverter(json), cancellationToken);

    /// <summary>
    /// Gets an inverter's details.
    /// </summary>
    /// <param name="serialNumber">The inverter's serial number.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The inverter.</returns>
    /// <exception cref="SolisCloudNotFoundException">There is no such inverter, or the account can't see it.</exception>
    /// <exception cref="SolisCloudException">The request failed.</exception>
    public async Task<Inverter> GetInverterAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        Guard.NotNullOrWhiteSpace(serialNumber);
        SolisCloudResponse response = await SendAsync(
            SolisEndpoints.InverterDetail,
            new RequestBody().Add("sn", serialNumber).ToUtf8(),
            cancellationToken).ConfigureAwait(false);

        // An unknown serial number is a success with a placeholder: another serial number (one digit short, all
        // zeros), no model, and a data timestamp of 0.
        Inverter inverter = new(RequireObject(response.Data, SolisEndpoints.InverterDetail, serialNumber));
        bool placeholder = !string.Equals(inverter.SerialNumber, serialNumber.Trim(), StringComparison.OrdinalIgnoreCase)
            || (inverter.Model is null && inverter.LastUpdated is not DateTimeOffset { Year: > 1970 });
        return placeholder ? throw new SolisCloudNotFoundException(SolisEndpoints.InverterDetail.Path, serialNumber) : inverter;
    }

    /// <summary>
    /// Gets an inverter's readings for one day, about every 5 minutes.
    /// </summary>
    /// <param name="serialNumber">The inverter's serial number.</param>
    /// <param name="day">The day, in the station's calendar. Only the date part is used.</param>
    /// <param name="utcOffset">The station's offset from UTC (<see cref="Station.UtcOffset"/>).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The readings in time order; empty for a day with no data.</returns>
    /// <exception cref="SolisCloudException">The request failed.</exception>
    public async Task<IReadOnlyList<InverterSample>> GetInverterDayAsync(
        string serialNumber,
        DateTime day,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNullOrWhiteSpace(serialNumber);
        RequestBody body = new RequestBody()
            .Add("sn", serialNumber)
            .Add("money", _currency)
            .Add("time", day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Add("timeZone", utcOffset);
        SolisCloudResponse response = await SendAsync(SolisEndpoints.InverterDay, body.ToUtf8(), cancellationToken).ConfigureAwait(false);
        return Map(response.Data, json => new InverterSample(json, utcOffset));
    }

    /// <summary>
    /// Gets a station's generation, consumption, battery and grid power for one day, every 5 minutes.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="day">The day, in the station's calendar. Only the date part is used.</param>
    /// <param name="utcOffset">The station's offset from UTC (<see cref="Station.UtcOffset"/>).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The points in time order; empty for a day with no data.</returns>
    /// <exception cref="SolisCloudException">The request failed.</exception>
    public async Task<IReadOnlyList<StationSample>> GetStationDayAsync(
        string stationId,
        DateTime day,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNullOrWhiteSpace(stationId);
        RequestBody body = History(stationId, utcOffset).Add("time", day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        SolisCloudResponse response = await SendAsync(SolisEndpoints.StationDay, body.ToUtf8(), cancellationToken).ConfigureAwait(false);
        DateTime date = day.Date;
        return Map(response.Data, json => new StationSample(json, date, utcOffset));
    }

    /// <summary>
    /// Gets a station's daily energy totals for one month.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month, 1 to 12.</param>
    /// <param name="utcOffset">The station's offset from UTC (<see cref="Station.UtcOffset"/>).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>One record per day with data.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="month"/> is not 1 to 12.</exception>
    /// <exception cref="SolisCloudException">The request failed.</exception>
    public async Task<IReadOnlyList<EnergyRecord>> GetStationMonthAsync(
        string stationId,
        int year,
        int month,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNullOrWhiteSpace(stationId);
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "The month must be 1 to 12.");
        }

        string period = new DateTime(year, month, 1).ToString("yyyy-MM", CultureInfo.InvariantCulture);
        RequestBody body = History(stationId, utcOffset).Add("month", period);
        SolisCloudResponse response = await SendAsync(SolisEndpoints.StationMonth, body.ToUtf8(), cancellationToken).ConfigureAwait(false);
        return Map(response.Data, static json => new EnergyRecord(json));
    }

    /// <summary>
    /// Gets a station's monthly energy totals for one year.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="year">The year.</param>
    /// <param name="utcOffset">The station's offset from UTC (<see cref="Station.UtcOffset"/>).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>One record per month with data.</returns>
    /// <exception cref="SolisCloudException">The request failed.</exception>
    public async Task<IReadOnlyList<EnergyRecord>> GetStationYearAsync(
        string stationId,
        int year,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNullOrWhiteSpace(stationId);
        RequestBody body = History(stationId, utcOffset).Add("year", year.ToString("0000", CultureInfo.InvariantCulture));
        SolisCloudResponse response = await SendAsync(SolisEndpoints.StationYear, body.ToUtf8(), cancellationToken).ConfigureAwait(false);
        return Map(response.Data, static json => new EnergyRecord(json));
    }

    /// <summary>
    /// Gets a station's yearly energy totals since it was installed.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="utcOffset">The station's offset from UTC (<see cref="Station.UtcOffset"/>).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>One record per year.</returns>
    /// <exception cref="SolisCloudException">The request failed.</exception>
    public async Task<IReadOnlyList<EnergyRecord>> GetStationLifetimeAsync(
        string stationId,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNullOrWhiteSpace(stationId);
        SolisCloudResponse response = await SendAsync(
            SolisEndpoints.StationAll,
            History(stationId, utcOffset).ToUtf8(),
            cancellationToken).ConfigureAwait(false);
        return Map(response.Data, static json => new EnergyRecord(json));
    }

    /// <summary>
    /// Lists the alarms on the account, newest first, fetching further pages as the sequence is read.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the requests.</param>
    /// <returns>The alarms.</returns>
    /// <exception cref="SolisCloudException">A request failed.</exception>
    public IAsyncEnumerable<Alarm> GetAlarmsAsync(CancellationToken cancellationToken = default) =>
        GetPagesAsync(SolisEndpoints.AlarmList, static json => new Alarm(json), cancellationToken);

    /// <summary>
    /// Lists the data loggers on the account, fetching further pages as the sequence is read.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the requests.</param>
    /// <returns>The data loggers, with their latest signal strength.</returns>
    /// <exception cref="SolisCloudException">A request failed.</exception>
    public IAsyncEnumerable<DataLogger> GetDataLoggersAsync(CancellationToken cancellationToken = default) =>
        GetPagesAsync(SolisEndpoints.CollectorList, static json => new DataLogger(json), cancellationToken);

    /// <summary>
    /// Gets a data logger's signal strength for one day, about every 5 minutes while it was connected.
    /// </summary>
    /// <param name="serialNumber">The data logger's serial number (<see cref="DataLogger.SerialNumber"/>, or
    /// <see cref="Inverter.CollectorSerialNumber"/>).</param>
    /// <param name="day">The day, in the station's calendar. Only the date part is used.</param>
    /// <param name="utcOffset">The station's offset from UTC (<see cref="Station.UtcOffset"/>).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The points in time order; empty for a day with no data. Gaps are times it was offline.</returns>
    /// <exception cref="SolisCloudException">The request failed.</exception>
    public async Task<IReadOnlyList<DataLoggerSample>> GetDataLoggerDayAsync(
        string serialNumber,
        DateTime day,
        TimeSpan utcOffset,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNullOrWhiteSpace(serialNumber);
        RequestBody body = new RequestBody()
            .Add("sn", serialNumber)
            .Add("time", day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Add("timeZone", utcOffset);
        SolisCloudResponse response = await SendAsync(SolisEndpoints.CollectorDay, body.ToUtf8(), cancellationToken).ConfigureAwait(false);
        return Map(response.Data, json => new DataLoggerSample(json, utcOffset));
    }

    private static JsonElement RequireObject(JsonElement data, SolisEndpoint endpoint, string id) =>
        data.ValueKind == JsonValueKind.Object ? data : throw new SolisCloudNotFoundException(endpoint.Path, id);

    private RequestBody History(string stationId, TimeSpan utcOffset) =>
        new RequestBody().Add("id", stationId).Add("money", _currency).Add("timeZone", utcOffset);

    private static List<T> Map<T>(JsonElement data, Func<JsonElement, T> create) =>
        data.ValueKind == JsonValueKind.Array ? [.. data.EnumerateArray().Select(create)] : [];

    private async IAsyncEnumerable<T> GetPagesAsync<T>(
        SolisEndpoint endpoint,
        Func<JsonElement, T> create,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int page = 1; ; page++)
        {
            byte[] body = new RequestBody().Add("pageNo", page).Add("pageSize", PageSize).ToUtf8();
            SolisCloudResponse response = await SendAsync(endpoint, body, cancellationToken).ConfigureAwait(false);
            var result = Page.Read(response.Data);

            foreach (JsonElement record in result.Records)
            {
                yield return create(record);
            }

            if (result.Records.Count < PageSize || page >= result.Pages)
            {
                yield break;
            }
        }
    }
}
