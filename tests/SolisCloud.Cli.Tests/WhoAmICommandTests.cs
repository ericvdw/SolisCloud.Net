// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Net;
using SolisCloud.Cli.Commands;
using SolisCloud.Cli.Tests.TestSupport;

namespace SolisCloud.Cli.Tests;

public sealed class WhoAmICommandTests
{
    [Fact]
    public async Task Lists_the_stations_when_the_credentials_work()
    {
        using CliHarness harness = new();
        harness.Handler.Route(SolisEndpoints.StationList, RoutingHttpMessageHandler.Success(SampleData.StationList));
        harness.Handler.ResponseDate = harness.Time.GetUtcNow();

        int exitCode = await WhoAmICommand.RunAsync(harness.Context, TestContext.Current.CancellationToken);

        string output = harness.Output.ToString();
        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Access:   OK", output, StringComparison.Ordinal);
        Assert.Contains("1300…4416", output, StringComparison.Ordinal);
        Assert.Contains("Stations: 1", output, StringComparison.Ordinal);
        Assert.Contains(SampleData.StationName + " (8.2 kWp)", output, StringComparison.Ordinal);
        Assert.Contains("in sync", output, StringComparison.Ordinal);
        Assert.DoesNotContain("test-secret", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Explains_an_authority_error()
    {
        using CliHarness harness = new();
        harness.Handler.Route(SolisEndpoints.StationList, RoutingHttpMessageHandler.Failure("R0000", "No authority"));

        int exitCode = await WhoAmICommand.RunAsync(harness.Context, TestContext.Current.CancellationToken);

        Assert.Equal(ExitCodes.ApiError, exitCode);
        Assert.Contains("R0000", harness.Error.ToString(), StringComparison.Ordinal);
        Assert.Contains("API access", harness.Error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reports_an_http_error()
    {
        using CliHarness harness = new();
        harness.Handler.Route(SolisEndpoints.StationList, "Forbidden", HttpStatusCode.Forbidden);

        int exitCode = await WhoAmICommand.RunAsync(harness.Context, TestContext.Current.CancellationToken);

        Assert.Equal(ExitCodes.ApiError, exitCode);
        Assert.Contains("403", harness.Error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Missing_credentials_are_a_usage_error_and_send_nothing()
    {
        using CliHarness harness = new(new Dictionary<string, string?>(StringComparer.Ordinal));

        int exitCode = await WhoAmICommand.RunAsync(harness.Context, TestContext.Current.CancellationToken);

        Assert.Equal(ExitCodes.UsageError, exitCode);
        Assert.Contains(Credentials.ApiIdVariable, harness.Error.ToString(), StringComparison.Ordinal);
        Assert.Empty(harness.Handler.Requests);
    }

    [Theory]
    [InlineData(0, "in sync with the server")]
    [InlineData(-30, "this computer is 30 s ahead of the server")]
    [InlineData(90, "this computer is 90 s behind the server")]
    public void DescribeClock_compares_with_the_server(int serverOffsetSeconds, string expected)
    {
        DateTimeOffset now = new(2026, 10, 14, 11, 30, 0, TimeSpan.Zero);

        string text = WhoAmICommand.DescribeClock(now.AddSeconds(serverOffsetSeconds), now, now);

        Assert.Equal(expected, text);
    }

    [Fact]
    public void DescribeClock_warns_when_close_to_the_limit()
    {
        DateTimeOffset now = new(2026, 10, 14, 11, 30, 0, TimeSpan.Zero);

        string text = WhoAmICommand.DescribeClock(now.AddMinutes(-10), now, now);

        Assert.Contains("WARNING", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DescribeClock_handles_a_missing_date_header()
    {
        DateTimeOffset now = new(2026, 10, 14, 11, 30, 0, TimeSpan.Zero);

        Assert.StartsWith("unknown", WhoAmICommand.DescribeClock(null, now, now), StringComparison.Ordinal);
    }
}
