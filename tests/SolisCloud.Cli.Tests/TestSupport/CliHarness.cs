// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using Microsoft.Extensions.Time.Testing;

namespace SolisCloud.Cli.Tests.TestSupport;

/// <summary>
/// A <see cref="CliContext"/> with captured output, fake credentials, a fake clock and a routed HTTP handler.
/// </summary>
internal sealed class CliHarness : IDisposable
{
    public const string ApiId = "1300386381676644416";

    private readonly HttpClient _httpClient;

    public CliHarness(Dictionary<string, string?>? environment = null)
    {
        Environment = environment ?? new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [Credentials.ApiIdVariable] = ApiId,
            [Credentials.ApiSecretVariable] = "test-secret",
        };

        _httpClient = new HttpClient(Handler, disposeHandler: false);
        Context = new CliContext(
            Output,
            Error,
            name => Environment.TryGetValue(name, out string? value) ? value : null,
            Time,
            CreateClient);
    }

    public Dictionary<string, string?> Environment { get; }

    public RoutingHttpMessageHandler Handler { get; } = new();

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 14, 11, 30, 0, TimeSpan.Zero));

    public StringWriter Output { get; } = new();

    public StringWriter Error { get; } = new();

    public CliContext Context { get; }

    public void Dispose()
    {
        _httpClient.Dispose();
        Handler.Dispose();
        Output.Dispose();
        Error.Dispose();
    }

    private SolisCloudClient CreateClient(SolisCloudClientOptions options)
    {
        // The rate limiter waits on the clock, and this clock only moves when a test advances it.
        options.EnableRateLimiting = false;
        return new SolisCloudClient(_httpClient, options, Time);
    }
}
