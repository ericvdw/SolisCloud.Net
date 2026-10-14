// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;
using SolisCloud.Cli.Commands;
using SolisCloud.Cli.Tests.TestSupport;

namespace SolisCloud.Cli.Tests;

public sealed class CaptureCommandTests
{
    [Fact]
    public async Task Captures_every_device_with_the_stations_settings()
    {
        using CliHarness harness = Routed();
        using TemporaryDirectory output = new();

        int exitCode = await RunAsync(harness, output.Path, sanitize: true);

        Assert.Equal(ExitCodes.Success, exitCode);

        // The station's own currency and time zone are used, and "today" is in the station's time zone.
        string inverterDay = Request(harness, SolisEndpoints.InverterDay);
        Assert.Contains("\"money\":\"ZAR\"", inverterDay, StringComparison.Ordinal);
        Assert.Contains("\"time\":\"2026-10-14\"", inverterDay, StringComparison.Ordinal);
        Assert.Contains("\"timeZone\":2.00", inverterDay, StringComparison.Ordinal);

        // IDs are sent exactly as received.
        Assert.Contains("\"id\":\"" + SampleData.InverterId + "\"", Request(harness, SolisEndpoints.InverterDetail), StringComparison.Ordinal);
        Assert.Contains("\"sn\":\"" + SampleData.CollectorSn + "\"", Request(harness, SolisEndpoints.CollectorDay), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Writes_one_sanitised_file_per_request()
    {
        using CliHarness harness = Routed();
        using TemporaryDirectory output = new();

        _ = await RunAsync(harness, output.Path, sanitize: true);

        string[] files = CapturedFiles(output.Path);
        Assert.Equal(harness.Handler.Requests.Count, files.Length);
        Assert.EndsWith("001-userStationList.json", files[0], StringComparison.Ordinal);
        Assert.Contains(files, file => file.EndsWith("-collector-day.json", StringComparison.Ordinal));

        foreach (string file in files)
        {
            byte[] bytes = await File.ReadAllBytesAsync(file, TestContext.Current.CancellationToken);
            Assert.False(bytes is [0xEF, 0xBB, 0xBF, ..], file + " starts with a byte-order mark.");

            string text = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
            using var document = JsonDocument.Parse(text);
            Assert.True(document.RootElement.TryGetProperty("request", out _), file);

            foreach (string secret in new[] { SampleData.StationId, SampleData.StationName, SampleData.InverterSn, SampleData.CollectorSn, SampleData.Address, "Protea Street", "Stellenbosch" })
            {
                Assert.DoesNotContain(secret, text, StringComparison.Ordinal);
            }
        }

        Assert.Contains("Sanitised", harness.Output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task No_sanitize_keeps_the_real_values_and_says_so()
    {
        using CliHarness harness = Routed();
        using TemporaryDirectory output = new();

        _ = await RunAsync(harness, output.Path, sanitize: false);

        string stationList = await File.ReadAllTextAsync(CapturedFiles(output.Path)[0], TestContext.Current.CancellationToken);
        Assert.Contains(SampleData.StationName, stationList, StringComparison.Ordinal);
        Assert.Contains("NOT sanitised", harness.Output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_failed_request_is_saved_and_the_capture_continues()
    {
        using CliHarness harness = Routed();
        harness.Handler.Route(SolisEndpoints.InverterDay, RoutingHttpMessageHandler.Failure("I0000", "The necessary parameters are empty"));
        using TemporaryDirectory output = new();

        int exitCode = await RunAsync(harness, output.Path, sanitize: true);

        Assert.Equal(ExitCodes.PartialFailure, exitCode);
        string failure = await File.ReadAllTextAsync(
            Array.Find(CapturedFiles(output.Path), file => file.EndsWith("-inverterDay.json", StringComparison.Ordinal))!,
            TestContext.Current.CancellationToken);
        Assert.Contains("\"error\"", failure, StringComparison.Ordinal);
        Assert.Contains("I0000", failure, StringComparison.Ordinal);
        Assert.Contains(harness.Handler.Requests, request => request.Path == SolisEndpoints.MeterList.Path);
    }

    [Fact]
    public async Task Nothing_else_is_captured_when_the_station_list_fails()
    {
        using CliHarness harness = new();
        harness.Handler.Route(SolisEndpoints.StationList, RoutingHttpMessageHandler.Failure("R0000", "No authority"));
        using TemporaryDirectory output = new();

        int exitCode = await RunAsync(harness, output.Path, sanitize: true);

        Assert.Equal(ExitCodes.ApiError, exitCode);
        _ = Assert.Single(harness.Handler.Requests);
    }

    [Fact]
    public async Task An_explicit_date_is_used_for_daily_monthly_and_yearly_data()
    {
        using CliHarness harness = Routed();
        using TemporaryDirectory output = new();

        _ = await RunAsync(harness, output.Path, sanitize: true, day: new DateOnly(2025, 12, 31));

        Assert.Contains("\"time\":\"2025-12-31\"", Request(harness, SolisEndpoints.StationDay), StringComparison.Ordinal);
        Assert.Contains("\"month\":\"2025-12\"", Request(harness, SolisEndpoints.StationMonth), StringComparison.Ordinal);
        Assert.Contains("\"year\":\"2025\"", Request(harness, SolisEndpoints.InverterYear), StringComparison.Ordinal);
    }

    private static CliHarness Routed()
    {
        CliHarness harness = new();
        harness.Handler
            .Route(SolisEndpoints.StationList, RoutingHttpMessageHandler.Success(SampleData.StationList))
            .Route(SolisEndpoints.StationDetail, RoutingHttpMessageHandler.Success(SampleData.StationDetail))
            .Route(SolisEndpoints.InverterList, RoutingHttpMessageHandler.Success(SampleData.InverterList))
            .Route(SolisEndpoints.CollectorList, RoutingHttpMessageHandler.Success(SampleData.CollectorList));
        return harness;
    }

    private static Task<int> RunAsync(CliHarness harness, string outputRoot, bool sanitize, DateOnly? day = null) =>
        CaptureCommand.RunAsync(harness.Context, outputRoot, day, sanitize, TestContext.Current.CancellationToken);

    private static string Request(CliHarness harness, SolisEndpoint endpoint) =>
        harness.Handler.Requests.Find(request => request.Path == endpoint.Path).Body
        ?? throw new InvalidOperationException("No request to " + endpoint.Path);

    private static string[] CapturedFiles(string root)
    {
        string capture = Assert.Single(Directory.GetDirectories(root));
        string[] files = Directory.GetFiles(capture);
        Array.Sort(files, StringComparer.Ordinal);
        return files;
    }
}
