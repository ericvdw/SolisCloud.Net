// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;
using System.Text.Json;

namespace SolisCloud.Cli.Commands;

/// <summary>
/// <c>solis whoami</c>: checks the credentials and the clock, and lists the stations the account can see.
/// </summary>
internal static class WhoAmICommand
{
    // The API rejects requests whose Date header is more than 15 minutes off; warn well before that.
    private static readonly TimeSpan ClockWarningThreshold = TimeSpan.FromMinutes(5);

    public static async Task<int> RunAsync(CliContext context, CancellationToken cancellationToken)
    {
        if (!Credentials.TryLoad(context.GetEnvironmentVariable, out SolisCloudClientOptions? options, out string? error))
        {
            await context.Error.WriteLineAsync(error);
            return ExitCodes.UsageError;
        }

        await context.Output.WriteLineAsync("API ID:   " + Credentials.Mask(options.ApiId));
        await context.Output.WriteLineAsync("Server:   " + options.BaseAddress);

        using SolisCloudClient client = context.CreateClient(options);
        DateTimeOffset sentAt = context.TimeProvider.GetUtcNow();
        SolisCloudResponse response;
        try
        {
            response = await client.SendAsync(SolisEndpoints.StationList, JsonBody.FirstPage().ToUtf8(), cancellationToken);
        }
        catch (Exception ex) when (Failures.IsRequestFailure(ex, cancellationToken))
        {
            await context.Error.WriteLineAsync(Failures.Describe(ex));
            return ExitCodes.ApiError;
        }

        DateTimeOffset receivedAt = context.TimeProvider.GetUtcNow();
        await context.Output.WriteLineAsync("Access:   OK");
        await context.Output.WriteLineAsync("Clock:    " + DescribeClock(response.ServerDate, sentAt, receivedAt));
        await WriteStationsAsync(context.Output, response.Data);
        return ExitCodes.Success;
    }

    /// <summary>
    /// Compares the server's <c>Date</c> header with the midpoint of the request, the best local estimate of when
    /// the server produced it. The header has one-second resolution, so differences of a second or two are noise.
    /// </summary>
    internal static string DescribeClock(DateTimeOffset? serverDate, DateTimeOffset sentAt, DateTimeOffset receivedAt)
    {
        if (serverDate is null)
        {
            return "unknown (the response had no Date header)";
        }

        DateTimeOffset localMidpoint = sentAt + ((receivedAt - sentAt) / 2);
        TimeSpan offset = localMidpoint - serverDate.Value;
        string seconds = Math.Abs(offset.TotalSeconds).ToString("0", CultureInfo.InvariantCulture);
        string direction = offset > TimeSpan.Zero ? "ahead of" : "behind";

        if (offset.Duration() < TimeSpan.FromSeconds(2))
        {
            return "in sync with the server";
        }

        string text = "this computer is " + seconds + " s " + direction + " the server";
        return offset.Duration() > ClockWarningThreshold
            ? text + ". WARNING: requests fail when the clock is more than 15 minutes out."
            : text;
    }

    private static async Task WriteStationsAsync(TextWriter output, JsonElement data)
    {
        IReadOnlyList<JsonElement> stations = JsonData.Records(data);
        await output.WriteLineAsync(string.Create(CultureInfo.InvariantCulture, $"Stations: {stations.Count}"));
        foreach (JsonElement station in stations)
        {
            string id = JsonData.TryGetText(station, "id") ?? "?";
            string name = JsonData.TryGetText(station, "stationName") ?? "(unnamed)";
            string capacity = JsonData.TryGetText(station, "capacity") is { } value
                ? " (" + value + " " + (JsonData.TryGetText(station, "capacityStr") ?? "kWp") + ")"
                : string.Empty;
            await output.WriteLineAsync("  " + id + "  " + name + capacity);
        }
    }
}
