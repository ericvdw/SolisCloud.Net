// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;
using System.Text.Json;
using SolisCloud.Internal;

namespace SolisCloud.Models;

/// <summary>
/// Energy totals for one period: a day of a month, a month of a year, or a year of a station's lifetime.
/// </summary>
/// <remarks>
/// Returned by the month (<see cref="SolisEndpoints.StationMonth"/>), year (<see cref="SolisEndpoints.StationYear"/>)
/// and lifetime (<see cref="SolisEndpoints.StationAll"/>) operations, which each return one record per period.
/// </remarks>
public sealed class EnergyRecord
{
    private static readonly string[] DateFormats = ["yyyy-MM-dd", "yyyy-MM", "yyyy"];

    internal EnergyRecord(JsonElement json)
    {
        FieldReader f = new(json);
        Raw = json.Clone();

        // "dateStr" is the period in the station's own calendar; the "date" epoch is anchored to China time.
        string? date = f.String("dateStr") ?? f.String("year");
        PeriodStart = DateTime.TryParseExact(date, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start)
            ? start
            : default;

        Produced = f.Energy("energy", "kWh");
        Consumed = f.Energy("consumeEnergy", "kWh");
        GridImported = f.Energy("gridPurchasedEnergy", "kWh");
        GridExported = f.Energy("gridSellEnergy", "kWh");
        BatteryCharged = f.Energy("batteryChargeEnergy", "kWh");
        BatteryDischarged = f.Energy("batteryDischargeEnergy", "kWh");
        Income = ReadIncome(f);
    }

    /// <summary>
    /// Gets the first day of the period (a day, the 1st of a month, or 1 January), in the station's calendar.
    /// </summary>
    /// <remarks>A <see cref="DateTime"/> with no time and an unspecified kind; <c>DateOnly</c> isn't available on every target.</remarks>
    public DateTime PeriodStart { get; }

    /// <summary>Gets the energy generated.</summary>
    public Energy? Produced { get; }

    /// <summary>Gets the energy the household consumed.</summary>
    public Energy? Consumed { get; }

    /// <summary>Gets the energy bought from the grid.</summary>
    public Energy? GridImported { get; }

    /// <summary>Gets the energy sold to the grid.</summary>
    public Energy? GridExported { get; }

    /// <summary>Gets the energy put into the battery.</summary>
    public Energy? BatteryCharged { get; }

    /// <summary>Gets the energy taken from the battery.</summary>
    public Energy? BatteryDischarged { get; }

    /// <summary>
    /// Gets SolisCloud's revenue estimate (generation × the configured price), in the currency the request was made
    /// with.
    /// </summary>
    public decimal? Income { get; }

    /// <summary>Gets the complete JSON record, including fields not modelled here.</summary>
    public JsonElement Raw { get; }

    // money × moneyPec is in the unit of moneyStr, which may carry a "K " (thousands) prefix: 953.4 × 0.001 "K ZAR"
    // is 953.4 rand.
    private static decimal? ReadIncome(FieldReader f)
    {
        if (f.Decimal("money") is not decimal money)
        {
            return null;
        }

        decimal scale = f.Decimal("moneyPec") is decimal pec && pec > 0 ? pec : 1;
        string unit = f.String("moneyStr") ?? string.Empty;
        decimal multiplier = unit.StartsWith("K ", StringComparison.Ordinal) ? 1_000
            : unit.StartsWith("M ", StringComparison.Ordinal) ? 1_000_000
            : 1;
        return money * scale * multiplier;
    }
}
