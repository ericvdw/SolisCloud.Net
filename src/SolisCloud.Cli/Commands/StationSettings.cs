// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;

namespace SolisCloud.Cli.Commands;

/// <summary>
/// The per-station values that history requests need: the currency used for revenue, the station's UTC offset,
/// and which day to ask for.
/// </summary>
internal sealed class StationSettings
{
    private const string FallbackCurrency = "EUR";

    public StationSettings(string currency, decimal timeZoneHours, DateOnly day)
    {
        Currency = currency;
        TimeZoneHours = timeZoneHours;
        Day = day;
    }

    /// <summary>Gets the currency code sent as <c>money</c>, e.g. <c>ZAR</c>.</summary>
    public string Currency { get; }

    /// <summary>Gets the UTC offset in hours sent as <c>timeZone</c>, e.g. <c>2</c> or <c>5.5</c>.</summary>
    public decimal TimeZoneHours { get; }

    /// <summary>Gets the day to request daily data for.</summary>
    public DateOnly Day { get; }

    /// <summary>
    /// Reads the settings from a station record (its <c>money</c> and <c>timeZone</c> fields), falling back to
    /// the computer's own time zone. Without an explicit day, it is "today" in the station's time zone.
    /// </summary>
    public static StationSettings From(JsonElement? station, DateOnly? day, TimeProvider timeProvider)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        string currency = station is { } s ? JsonData.TryGetText(s, "money") ?? FallbackCurrency : FallbackCurrency;
        decimal timeZone = (station is { } t ? JsonData.TryGetDecimal(t, "timeZone") : null)
            ?? (decimal)timeProvider.LocalTimeZone.GetUtcOffset(now).TotalHours;

        var stationToday = DateOnly.FromDateTime(now.UtcDateTime.AddHours((double)timeZone));
        return new StationSettings(currency, timeZone, day ?? stationToday);
    }
}
