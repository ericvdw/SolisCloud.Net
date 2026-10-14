// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Tests.TestSupport;

/// <summary>
/// A <see cref="SolisCloudClient"/> backed by a fixture folder.
/// </summary>
internal sealed class FixtureClient : IDisposable
{
    public const string SinglePhaseHybrid = "single-phase-hybrid";

    private readonly HttpClient _httpClient;

    public FixtureClient(string fixtureName = SinglePhaseHybrid)
    {
        Handler = new FixtureHttpMessageHandler(fixtureName);
        _httpClient = new HttpClient(Handler, disposeHandler: false);

        SolisCloudClientOptions options = TestClient.CreateOptions();
        options.EnableRateLimiting = false;
        options.Currency = "ZAR";
        Client = new SolisCloudClient(_httpClient, options);
    }

    public FixtureHttpMessageHandler Handler { get; }

    public SolisCloudClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
        _httpClient.Dispose();
        Handler.Dispose();
    }
}
