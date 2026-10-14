// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud;

/// <summary>
/// Every operation in the SolisCloud Platform API document, version 2.0.3.
/// </summary>
/// <remarks>Section numbers refer to that document.</remarks>
public static class SolisEndpoints
{
    // ---- Inverters (3.1 – 3.8) ----

    /// <summary>Gets the inverter list under the account (3.1).</summary>
    public static SolisEndpoint InverterList { get; } = new("/v1/api/inverterList");

    /// <summary>Gets details of a single inverter (3.2).</summary>
    public static SolisEndpoint InverterDetail { get; } = new("/v1/api/inverterDetail");

    /// <summary>Gets details of multiple inverters (3.3).</summary>
    public static SolisEndpoint InverterDetailList { get; } = new("/v1/api/inverterDetailList");

    /// <summary>Gets five-minute data for one inverter on a given day (3.4).</summary>
    public static SolisEndpoint InverterDay { get; } = new("/v1/api/inverterDay");

    /// <summary>Gets daily data for one inverter for a given month (3.5).</summary>
    public static SolisEndpoint InverterMonth { get; } = new("/v1/api/inverterMonth");

    /// <summary>Gets monthly data for one inverter for a given year (3.6).</summary>
    public static SolisEndpoint InverterYear { get; } = new("/v1/api/inverterYear");

    /// <summary>Gets yearly data for one inverter over its lifetime (3.7).</summary>
    public static SolisEndpoint InverterAll { get; } = new("/v1/api/inverterAll");

    /// <summary>Gets warranty dates for multiple inverters (3.8).</summary>
    public static SolisEndpoint InverterWarranty { get; } = new("/v1/api/inverter/shelfTime");

    // ---- Alarms (3.9) ----

    /// <summary>Gets the device alarm list under the account (3.9).</summary>
    public static SolisEndpoint AlarmList { get; } = new("/v1/api/alarmList");

    // ---- Collectors / data loggers (3.10 – 3.12) ----

    /// <summary>Gets the collector list under the account (3.10).</summary>
    public static SolisEndpoint CollectorList { get; } = new("/v1/api/collectorList");

    /// <summary>Gets details of a single collector (3.11).</summary>
    public static SolisEndpoint CollectorDetail { get; } = new("/v1/api/collectorDetail");

    /// <summary>Gets signal-strength data for one collector on a given day (3.12).</summary>
    public static SolisEndpoint CollectorDay { get; } = new("/v1/api/collector/day");

    // ---- Export power managers (3.13 – 3.18) ----

    /// <summary>Gets the EPM list under the account (3.13).</summary>
    public static SolisEndpoint EpmList { get; } = new("/v1/api/epmList");

    /// <summary>Gets details of a single EPM (3.14).</summary>
    public static SolisEndpoint EpmDetail { get; } = new("/v1/api/epmDetail");

    /// <summary>Gets five-minute data for one EPM on a given day (3.15).</summary>
    public static SolisEndpoint EpmDay { get; } = new("/v1/api/epm/day");

    /// <summary>Gets daily data for one EPM for a given month (3.16).</summary>
    public static SolisEndpoint EpmMonth { get; } = new("/v1/api/epm/month");

    /// <summary>Gets monthly data for one EPM for a given year (3.17).</summary>
    public static SolisEndpoint EpmYear { get; } = new("/v1/api/epm/year");

    /// <summary>Gets yearly data for one EPM over its lifetime (3.18).</summary>
    public static SolisEndpoint EpmAll { get; } = new("/v1/api/epm/all");

    // ---- Weather stations (3.19 – 3.20) ----

    /// <summary>Gets the weather station list under the account (3.19).</summary>
    public static SolisEndpoint WeatherList { get; } = new("/v1/api/weatherList");

    /// <summary>Gets details of a single weather station (3.20).</summary>
    public static SolisEndpoint WeatherDetail { get; } = new("/v1/api/weatherDetail");

    // ---- Meters (3.21 – 3.22) ----

    /// <summary>Gets the meter list under the account (3.21).</summary>
    public static SolisEndpoint MeterList { get; } = new("/v1/api/ammeterList");

    /// <summary>Gets details of a single meter (3.22).</summary>
    public static SolisEndpoint MeterDetail { get; } = new("/v1/api/ammeterDetail");

    // ---- Stations / plants (4.1 – 4.10) ----

    /// <summary>Gets the station list under the account (4.1).</summary>
    public static SolisEndpoint StationList { get; } = new("/v1/api/userStationList");

    /// <summary>Gets details of a single station (4.2).</summary>
    public static SolisEndpoint StationDetail { get; } = new("/v1/api/stationDetail");

    /// <summary>Gets details of multiple stations (4.3).</summary>
    public static SolisEndpoint StationDetailList { get; } = new("/v1/api/stationDetailList");

    /// <summary>Gets one day's energy for multiple stations (4.4).</summary>
    public static SolisEndpoint StationDayEnergyList { get; } = new("/v1/api/stationDayEnergyList");

    /// <summary>Gets one month's daily energy for multiple stations (4.5).</summary>
    public static SolisEndpoint StationMonthEnergyList { get; } = new("/v1/api/stationMonthEnergyList", permitsPerSecond: 10);

    /// <summary>Gets one year's energy for multiple stations (4.6).</summary>
    public static SolisEndpoint StationYearEnergyList { get; } = new("/v1/api/stationYearEnergyList", permitsPerSecond: 10);

    /// <summary>Gets five-minute data for one station on a given day (4.7).</summary>
    public static SolisEndpoint StationDay { get; } = new("/v1/api/stationDay");

    /// <summary>Gets daily data for one station for a given month (4.8).</summary>
    public static SolisEndpoint StationMonth { get; } = new("/v1/api/stationMonth");

    /// <summary>Gets monthly data for one station for a given year (4.9).</summary>
    public static SolisEndpoint StationYear { get; } = new("/v1/api/stationYear");

    /// <summary>Gets yearly data for one station over its lifetime (4.10).</summary>
    public static SolisEndpoint StationAll { get; } = new("/v1/api/stationAll");

    // ---- Station administration (4.11 – 4.15): these change account data ----

    /// <summary>Creates a station (4.11). Modifies data.</summary>
    public static SolisEndpoint AddStation { get; } = new("/v1/api/addStation", modifiesData: true);

    /// <summary>Updates a station (4.12). Modifies data.</summary>
    public static SolisEndpoint UpdateStation { get; } = new("/v1/api/stationUpdate", modifiesData: true);

    /// <summary>Binds a collector to a station (4.13). Modifies data.</summary>
    public static SolisEndpoint BindCollector { get; } = new("/v1/api/addStationBindCollector", modifiesData: true);

    /// <summary>Unbinds a collector from a station (4.14). Modifies data.</summary>
    public static SolisEndpoint UnbindCollector { get; } = new("/v1/api/delCollector", modifiesData: true);

    /// <summary>Binds inverters to a station (4.15). Modifies data.</summary>
    public static SolisEndpoint BindInverter { get; } = new("/v1/api/addDevice", modifiesData: true);

    /// <summary>
    /// Gets every known operation, in document order.
    /// </summary>
    public static IReadOnlyList<SolisEndpoint> All { get; } =
    [
        InverterList, InverterDetail, InverterDetailList, InverterDay, InverterMonth, InverterYear, InverterAll, InverterWarranty,
        AlarmList,
        CollectorList, CollectorDetail, CollectorDay,
        EpmList, EpmDetail, EpmDay, EpmMonth, EpmYear, EpmAll,
        WeatherList, WeatherDetail,
        MeterList, MeterDetail,
        StationList, StationDetail, StationDetailList, StationDayEnergyList, StationMonthEnergyList, StationYearEnergyList,
        StationDay, StationMonth, StationYear, StationAll,
        AddStation, UpdateStation, BindCollector, UnbindCollector, BindInverter,
    ];

    /// <summary>
    /// Finds a known operation by short name (<c>inverterDetail</c>) or path (<c>/v1/api/inverterDetail</c>),
    /// ignoring case.
    /// </summary>
    /// <param name="nameOrPath">The name or path to look up.</param>
    /// <returns>The matching operation, or <see langword="null"/> if there is none.</returns>
    public static SolisEndpoint? Find(string? nameOrPath)
    {
        if (string.IsNullOrWhiteSpace(nameOrPath))
        {
            return null;
        }

        foreach (SolisEndpoint endpoint in All)
        {
            if (string.Equals(endpoint.Name, nameOrPath, StringComparison.OrdinalIgnoreCase)
                || string.Equals(endpoint.Path, nameOrPath, StringComparison.OrdinalIgnoreCase))
            {
                return endpoint;
            }
        }

        return null;
    }
}
