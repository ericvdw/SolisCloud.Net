// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Net;

namespace SolisCloud.Tests.TestSupport;

/// <summary>
/// A <see cref="SolisCloudClient"/> wired to a <see cref="StubHttpMessageHandler"/>, disposed together.
/// </summary>
internal sealed class TestClient : IDisposable
{
    public const string ApiId = "1300386381676644416";
    public const string ApiSecret = "test-secret";

    private readonly HttpClient _httpClient;

    public TestClient(
        string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        DateTimeOffset? responseDate = null,
        TimeProvider? timeProvider = null,
        Action<SolisCloudClientOptions>? configure = null)
    {
        Handler = new StubHttpMessageHandler(responseBody, statusCode, responseDate);
        _httpClient = new HttpClient(Handler, disposeHandler: false);

        SolisCloudClientOptions options = CreateOptions();
        options.EnableRateLimiting = false;
        configure?.Invoke(options);
        Client = new SolisCloudClient(_httpClient, options, timeProvider);
    }

    public StubHttpMessageHandler Handler { get; }

    public SolisCloudClient Client { get; }

    public static SolisCloudClientOptions CreateOptions() => new() { ApiId = ApiId, ApiSecret = ApiSecret };

    public void Dispose()
    {
        Client.Dispose();
        _httpClient.Dispose();
        Handler.Dispose();
    }
}
