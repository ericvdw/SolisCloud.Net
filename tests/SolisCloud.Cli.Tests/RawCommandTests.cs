// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using SolisCloud.Cli.Commands;
using SolisCloud.Cli.Tests.TestSupport;

namespace SolisCloud.Cli.Tests;

public sealed class RawCommandTests
{
    [Fact]
    public async Task Prints_the_data_payload()
    {
        using CliHarness harness = new();
        harness.Handler.Route(SolisEndpoints.InverterDetail, RoutingHttpMessageHandler.Success("{\"sn\":\"ABC\",\"pac\":5.025}"));

        int exitCode = await RunAsync(harness, "inverterDetail", "{\"sn\":\"ABC\"}");

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("\"pac\": 5.025", harness.Output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("\"success\"", harness.Output.ToString(), StringComparison.Ordinal);
        Assert.Equal(("/v1/api/inverterDetail", "{\"sn\":\"ABC\"}"), harness.Handler.Requests[0]);
    }

    [Fact]
    public async Task Envelope_prints_the_whole_response()
    {
        using CliHarness harness = new();

        int exitCode = await RunAsync(harness, "/v1/api/userStationList", "{}", envelope: true);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("\"success\": true", harness.Output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unknown_operation_lists_the_known_ones()
    {
        using CliHarness harness = new();

        int exitCode = await RunAsync(harness, "inverterDetails", "{}");

        Assert.Equal(ExitCodes.UsageError, exitCode);
        Assert.Contains("inverterDetail", harness.Error.ToString(), StringComparison.Ordinal);
        Assert.Contains("addStation  (changes data)", harness.Error.ToString(), StringComparison.Ordinal);
        Assert.Empty(harness.Handler.Requests);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    public async Task The_body_must_be_a_json_object(string body)
    {
        using CliHarness harness = new();

        int exitCode = await RunAsync(harness, "inverterDetail", body);

        Assert.Equal(ExitCodes.UsageError, exitCode);
        Assert.Empty(harness.Handler.Requests);
    }

    [Fact]
    public async Task Write_operations_need_allow_write()
    {
        using CliHarness harness = new();

        int refused = await RunAsync(harness, "addStation", "{}");
        int sent = await RunAsync(harness, "addStation", "{}", allowWrite: true);

        Assert.Equal(ExitCodes.UsageError, refused);
        Assert.Equal(ExitCodes.Success, sent);
        _ = Assert.Single(harness.Handler.Requests);
    }

    private static Task<int> RunAsync(CliHarness harness, string operation, string body, bool envelope = false, bool allowWrite = false) =>
        RawCommand.RunAsync(harness.Context, operation, body, envelope, allowWrite, TestContext.Current.CancellationToken);
}
