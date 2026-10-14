// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;
using System.Text;
using System.Text.Json;
using SolisCloud.Cli.Sanitizing;

namespace SolisCloud.Cli.Commands;

/// <summary>
/// <c>solis capture</c>: calls every read operation for everything the account can see and saves each request and
/// response to a file, sanitised by default. The files show what the API really returns, which the document
/// doesn't fully describe, and are what the library's models and test fixtures are built from.
/// </summary>
/// <remarks>
/// A failed request is recorded in its own file and the capture carries on, so one unsupported operation doesn't
/// lose the rest. Requests go through the client's rate limiter, so a large account takes a while.
/// </remarks>
internal sealed class CaptureCommand
{
    // Encoding.UTF8 writes a byte-order mark, which many JSON tools (e.g. Python's json module) reject.
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly CliContext _context;
    private readonly SolisCloudClient _client;
    private readonly JsonSanitizer? _sanitizer;
    private readonly string _directory;
    private readonly DateOnly? _day;
    private readonly Dictionary<string, StationSettings> _stations = new(StringComparer.Ordinal);

    // Everything is held until the end and sanitised together: a value can only be recognised as sensitive once
    // the response that names it has been seen, and that isn't always the first response that contains it.
    private readonly List<(string FileName, string Json)> _captured = [];
    private int _failed;

    private CaptureCommand(CliContext context, SolisCloudClient client, JsonSanitizer? sanitizer, string directory, DateOnly? day)
    {
        _context = context;
        _client = client;
        _sanitizer = sanitizer;
        _directory = directory;
        _day = day;
    }

    public static async Task<int> RunAsync(
        CliContext context,
        string outputRoot,
        DateOnly? day,
        bool sanitize,
        CancellationToken cancellationToken)
    {
        if (!Credentials.TryLoad(context.GetEnvironmentVariable, out SolisCloudClientOptions? options, out string? error))
        {
            await context.Error.WriteLineAsync(error);
            return ExitCodes.UsageError;
        }

        string stamp = context.TimeProvider.GetUtcNow().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string directory = Path.GetFullPath(Path.Combine(outputRoot, stamp));
        _ = Directory.CreateDirectory(directory);

        using SolisCloudClient client = context.CreateClient(options);
        JsonSanitizer? sanitizer = sanitize ? new JsonSanitizer(Pseudonymizer.CreateRandom()) : null;
        CaptureCommand capture = new(context, client, sanitizer, directory, day);
        return await capture.RunAsync(cancellationToken);
    }

    private async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        await _context.Error.WriteLineAsync("Capturing to " + _directory);

        JsonElement? stations = await CallAsync(SolisEndpoints.StationList, JsonBody.FirstPage(), cancellationToken);
        if (stations is null)
        {
            await _context.Error.WriteLineAsync("The station list failed, so nothing else was captured.");
            return ExitCodes.ApiError;
        }

        foreach (JsonElement station in JsonData.Records(stations.Value))
        {
            await CaptureStationAsync(station, cancellationToken);
        }

        await CaptureInvertersAsync(cancellationToken);
        _ = await CallAsync(SolisEndpoints.AlarmList, JsonBody.FirstPage(), cancellationToken);
        await CaptureCollectorsAsync(cancellationToken);
        await CaptureDevicesAsync(SolisEndpoints.EpmList, SolisEndpoints.EpmDetail, cancellationToken);
        await CaptureDevicesAsync(SolisEndpoints.WeatherList, SolisEndpoints.WeatherDetail, cancellationToken);
        await CaptureDevicesAsync(SolisEndpoints.MeterList, SolisEndpoints.MeterDetail, cancellationToken);

        await WriteFilesAsync(cancellationToken);
        return await WriteSummaryAsync();
    }

    private async Task CaptureStationAsync(JsonElement station, CancellationToken cancellationToken)
    {
        if (JsonData.TryGet(station, "id") is not { } id)
        {
            return;
        }

        // Keyed by the ID's text: one response may send it as a string and another as a number.
        var settings = StationSettings.From(station, _day, _context.TimeProvider);
        _stations[JsonData.TryGetText(station, "id")!] = settings;

        _ = await CallAsync(SolisEndpoints.StationDetail, new JsonBody().Add("id", id), cancellationToken);
        _ = await CallAsync(SolisEndpoints.StationDay, History(settings).Add("id", id).Add("time", Day(settings)), cancellationToken);
        _ = await CallAsync(SolisEndpoints.StationMonth, History(settings).Add("id", id).Add("month", Month(settings)), cancellationToken);
        _ = await CallAsync(SolisEndpoints.StationYear, History(settings).Add("id", id).Add("year", Year(settings)), cancellationToken);
        _ = await CallAsync(SolisEndpoints.StationAll, History(settings).Add("id", id), cancellationToken);
    }

    private async Task CaptureInvertersAsync(CancellationToken cancellationToken)
    {
        JsonElement? list = await CallAsync(SolisEndpoints.InverterList, JsonBody.FirstPage(), cancellationToken);
        _ = await CallAsync(SolisEndpoints.InverterDetailList, JsonBody.FirstPage(), cancellationToken);
        if (list is null)
        {
            return;
        }

        foreach (JsonElement inverter in JsonData.Records(list.Value))
        {
            if (JsonData.TryGet(inverter, "id") is not { } id || JsonData.TryGetText(inverter, "sn") is not { } sn)
            {
                continue;
            }

            StationSettings settings = SettingsFor(inverter);
            JsonBody Device() => new JsonBody().Add("id", id).Add("sn", sn);

            _ = await CallAsync(SolisEndpoints.InverterDetail, Device(), cancellationToken);
            _ = await CallAsync(SolisEndpoints.InverterDay, Device().Add("money", settings.Currency).Add("time", Day(settings)).Add("timeZone", settings.TimeZoneHours), cancellationToken);
            _ = await CallAsync(SolisEndpoints.InverterMonth, Device().Add("money", settings.Currency).Add("month", Month(settings)), cancellationToken);
            _ = await CallAsync(SolisEndpoints.InverterYear, Device().Add("money", settings.Currency).Add("year", Year(settings)), cancellationToken);
            _ = await CallAsync(SolisEndpoints.InverterAll, Device().Add("money", settings.Currency), cancellationToken);
        }
    }

    private async Task CaptureCollectorsAsync(CancellationToken cancellationToken)
    {
        JsonElement? list = await CallAsync(SolisEndpoints.CollectorList, JsonBody.FirstPage(), cancellationToken);
        if (list is null)
        {
            return;
        }

        foreach (JsonElement collector in JsonData.Records(list.Value))
        {
            if (JsonData.TryGet(collector, "id") is not { } id || JsonData.TryGetText(collector, "sn") is not { } sn)
            {
                continue;
            }

            StationSettings settings = SettingsFor(collector);
            _ = await CallAsync(SolisEndpoints.CollectorDetail, new JsonBody().Add("id", id).Add("sn", sn), cancellationToken);
            _ = await CallAsync(SolisEndpoints.CollectorDay, new JsonBody().Add("sn", sn).Add("time", Day(settings)).Add("timeZone", settings.TimeZoneHours), cancellationToken);
        }
    }

    // EPMs, weather stations and meters: most accounts have none, so only fetch details when the list has records.
    private async Task CaptureDevicesAsync(SolisEndpoint listEndpoint, SolisEndpoint detailEndpoint, CancellationToken cancellationToken)
    {
        JsonElement? list = await CallAsync(listEndpoint, JsonBody.FirstPage(), cancellationToken);
        if (list is null)
        {
            return;
        }

        foreach (JsonElement device in JsonData.Records(list.Value))
        {
            if (JsonData.TryGetText(device, "sn") is { } sn)
            {
                _ = await CallAsync(detailEndpoint, new JsonBody().Add("sn", sn), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Sends one request and saves it with its response (or the error). Returns the response's <c>data</c>, or
    /// <see langword="null"/> if the request failed.
    /// </summary>
    private async Task<JsonElement?> CallAsync(SolisEndpoint endpoint, JsonBody body, CancellationToken cancellationToken)
    {
        byte[] request = body.ToUtf8();
        try
        {
            SolisCloudResponse response = await _client.SendAsync(endpoint, request, cancellationToken);
            Record(endpoint, request, response.Json, error: null);
            return response.Data;
        }
        catch (Exception ex) when (Failures.IsRequestFailure(ex, cancellationToken))
        {
            _failed++;
            string description = Failures.Describe(ex);
            await _context.Error.WriteLineAsync("  " + endpoint.Name + ": " + description);
            Record(endpoint, request, ResponseText(ex), description);
            return null;
        }
    }

    private void Record(SolisEndpoint endpoint, byte[] request, string? response, string? error)
    {
        string document = JsonText.Write(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("operation", endpoint.Path);
            writer.WritePropertyName("request");
            writer.WriteRawValue(request);
            if (error is not null)
            {
                writer.WriteString("error", error);
            }

            if (JsonText.IsValid(response))
            {
                writer.WritePropertyName("response");
                writer.WriteRawValue(response!);
            }
            else if (!string.IsNullOrEmpty(response))
            {
                writer.WriteString("responseText", response);
            }

            writer.WriteEndObject();
        });

        string fileName = string.Create(
            CultureInfo.InvariantCulture,
            $"{_captured.Count + 1:000}-{endpoint.Name.Replace('/', '-')}.json");
        _captured.Add((fileName, document));
    }

    private async Task WriteFilesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> contents = _sanitizer is null
            ? [.. _captured.Select(file => file.Json)]
            : _sanitizer.SanitizeAll([.. _captured.Select(file => file.Json)]);

        for (int i = 0; i < _captured.Count; i++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(_directory, _captured[i].FileName),
                contents[i] + "\n",
                Utf8WithoutBom,
                cancellationToken);
        }
    }

    private async Task<int> WriteSummaryAsync()
    {
        TextWriter output = _context.Output;
        await output.WriteLineAsync(string.Create(
            CultureInfo.InvariantCulture,
            $"Captured {_captured.Count} requests ({_failed} failed) to {_directory}"));

        await output.WriteLineAsync(_sanitizer is null
            ? "NOT sanitised: these files contain your IDs, serial numbers, names and location. Don't share them."
            : "Sanitised: IDs and serial numbers are replaced with fakes, names, addresses and contact details with "
                + JsonSanitizer.Redacted + ", and coordinates with 0. It's best-effort: read the files before sharing them.");

        return _failed == 0 ? ExitCodes.Success : ExitCodes.PartialFailure;
    }

    private StationSettings SettingsFor(JsonElement device) =>
        JsonData.TryGetText(device, "stationId") is { } stationId && _stations.TryGetValue(stationId, out StationSettings? settings)
            ? settings
            : StationSettings.From(station: null, _day, _context.TimeProvider);

    private static JsonBody History(StationSettings settings) =>
        new JsonBody().Add("money", settings.Currency).Add("timeZone", settings.TimeZoneHours);

    private static string Day(StationSettings settings) => settings.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Month(StationSettings settings) => settings.Day.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    private static string Year(StationSettings settings) => settings.Day.ToString("yyyy", CultureInfo.InvariantCulture);

    private static string? ResponseText(Exception exception) =>
        exception switch
        {
            SolisCloudApiException api => api.ResponseJson,
            SolisCloudHttpException http => http.ResponseBody,
            _ => null,
        };
}
