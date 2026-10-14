// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using SolisCloud.Tests.TestSupport;

namespace SolisCloud.Tests;

public sealed class SolisCloudClientTests
{
    private const string PageBody = "{\"pageNo\":1,\"pageSize\":10}";

    private static readonly DateTimeOffset DocumentExampleTime = new(2019, 7, 26, 6, 0, 46, TimeSpan.Zero);

    [Fact]
    public async Task SendAsync_signs_the_request()
    {
        using TestClient test = new(StubHttpMessageHandler.SuccessBody("{}"), timeProvider: new FakeTimeProvider(DocumentExampleTime));

        _ = await test.Client.SendAsync(SolisEndpoints.StationList, PageBody, TestContext.Current.CancellationToken);

        CapturedRequest request = Assert.Single(test.Handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(new Uri("https://www.soliscloud.com:13333/v1/api/userStationList"), request.RequestUri);
        Assert.Equal(PageBody, request.Body);
        Assert.Equal("application/json", request.Header("Content-Type"));
        Assert.Equal("kxdxk7rbAsrzSIWgEwhH4w==", request.Header("Content-MD5"));
        Assert.Equal("Fri, 26 Jul 2019 06:00:46 GMT", request.Header("Date"));
        // Same inputs as SolisRequestSignerTests.ComputeSignature_matches_an_independent_hmac_sha1.
        Assert.Equal("API 1300386381676644416:/GOIW8erQaUwWozVKxeKlb/FNkA=", request.Header("Authorization"));
    }

    [Fact]
    public async Task SendAsync_uses_a_custom_base_address()
    {
        using TestClient test = new(
            StubHttpMessageHandler.SuccessBody("{}"),
            configure: o => o.BaseAddress = new Uri("https://test.soliscloud.com:3333/"));

        _ = await test.Client.SendAsync(SolisEndpoints.InverterDetail, "{}", TestContext.Current.CancellationToken);

        Assert.Equal(new Uri("https://test.soliscloud.com:3333/v1/api/inverterDetail"), Assert.Single(test.Handler.Requests).RequestUri);
    }

    [Fact]
    public async Task SendAsync_returns_the_data_payload_and_server_date()
    {
        DateTimeOffset serverDate = new(2023, 6, 27, 6, 23, 30, TimeSpan.Zero);
        using TestClient test = new(StubHttpMessageHandler.SuccessBody("{\"total\":1,\"records\":[{\"sn\":\"ABC\"}]}"), responseDate: serverDate);

        SolisCloudResponse response = await test.Client.SendAsync(SolisEndpoints.InverterList, PageBody, TestContext.Current.CancellationToken);

        Assert.Same(SolisEndpoints.InverterList, response.Endpoint);
        Assert.Equal("success", response.Message);
        Assert.Equal(1, response.Data.GetProperty("total").GetInt32());
        Assert.Equal("ABC", response.Data.GetProperty("records")[0].GetProperty("sn").GetString());
        Assert.Equal(serverDate, response.ServerDate);
    }

    [Fact]
    public async Task SendAsync_leaves_data_undefined_when_the_response_has_none()
    {
        using TestClient test = new("{\"success\":true,\"code\":\"0\",\"msg\":\"success\"}");

        SolisCloudResponse response = await test.Client.SendAsync(SolisEndpoints.StationList, PageBody, TestContext.Current.CancellationToken);

        Assert.Equal(JsonValueKind.Undefined, response.Data.ValueKind);
    }

    [Theory]
    [InlineData("{\"success\":false,\"code\":\"R0000\",\"msg\":\"No authority\",\"data\":null}", "R0000", "No authority")]
    [InlineData("{\"success\":true,\"code\":\"B0049\",\"msg\":\"no collector\"}", "B0049", "no collector")]
    [InlineData("{\"success\":false,\"code\":\"0\",\"msg\":\"odd\"}", "0", "odd")]
    [InlineData("{\"code\":1,\"msg\":null}", "1", null)]
    public async Task SendAsync_throws_when_the_api_reports_failure(string body, string expectedCode, string? expectedMessage)
    {
        using TestClient test = new(body);

        SolisCloudApiException ex = await Assert.ThrowsAsync<SolisCloudApiException>(
            () => test.Client.SendAsync(SolisEndpoints.StationList, PageBody, TestContext.Current.CancellationToken));

        Assert.Equal(expectedCode, ex.Code);
        Assert.Equal(expectedMessage, ex.ApiMessage);
        Assert.Equal("/v1/api/userStationList", ex.EndpointPath);
        Assert.Equal(body, ex.ResponseJson);
    }

    [Fact]
    public async Task SendAsync_throws_on_a_non_success_http_status()
    {
        using TestClient test = new("gateway timeout", HttpStatusCode.GatewayTimeout);

        SolisCloudHttpException ex = await Assert.ThrowsAsync<SolisCloudHttpException>(
            () => test.Client.SendAsync(SolisEndpoints.StationList, PageBody, TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.GatewayTimeout, ex.StatusCode);
        Assert.Equal("gateway timeout", ex.ResponseBody);
    }

    [Theory]
    [InlineData("<html>maintenance</html>")]
    [InlineData("[1,2,3]")]
    [InlineData("")]
    public async Task SendAsync_throws_when_the_body_is_not_an_envelope(string body)
    {
        using TestClient test = new(body);

        SolisCloudHttpException ex = await Assert.ThrowsAsync<SolisCloudHttpException>(
            () => test.Client.SendAsync(SolisEndpoints.StationList, PageBody, TestContext.Current.CancellationToken));

        Assert.Equal(body, ex.ResponseBody);
    }

    [Fact]
    public async Task SendAsync_refuses_write_operations_unless_allowed()
    {
        using TestClient test = new(StubHttpMessageHandler.SuccessBody("null"));

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => test.Client.SendAsync(SolisEndpoints.AddStation, "{}", TestContext.Current.CancellationToken));

        Assert.Empty(test.Handler.Requests);
    }

    [Fact]
    public async Task SendAsync_sends_write_operations_when_allowed()
    {
        using TestClient test = new(StubHttpMessageHandler.SuccessBody("null"), configure: o => o.AllowWriteOperations = true);

        _ = await test.Client.SendAsync(SolisEndpoints.AddStation, "{}", TestContext.Current.CancellationToken);

        _ = Assert.Single(test.Handler.Requests);
    }

    [Fact]
    public async Task SendAsync_throws_after_dispose()
    {
        using TestClient test = new(StubHttpMessageHandler.SuccessBody("{}"));
        test.Client.Dispose();

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => test.Client.SendAsync(SolisEndpoints.StationList, PageBody, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Dispose_does_not_dispose_a_supplied_http_client()
    {
        using StubHttpMessageHandler handler = new(StubHttpMessageHandler.SuccessBody("{}"));
        using HttpClient httpClient = new(handler, disposeHandler: false);
        SolisCloudClient client = new(httpClient, TestClient.CreateOptions());

        client.Dispose();
        client.Dispose();

        // A disposed HttpClient would throw ObjectDisposedException here.
        httpClient.DefaultRequestHeaders.Clear();
    }

    [Theory]
    [InlineData("", "secret")]
    [InlineData("id", " ")]
    public void Constructor_requires_credentials(string apiId, string apiSecret)
    {
        SolisCloudClientOptions options = new() { ApiId = apiId, ApiSecret = apiSecret };

        Assert.Throws<ArgumentException>(() => new SolisCloudClient(options));
    }

    [Fact]
    public void Constructor_requires_an_absolute_base_address()
    {
        SolisCloudClientOptions options = TestClient.CreateOptions();
        options.BaseAddress = new Uri("/relative", UriKind.Relative);

        Assert.Throws<ArgumentException>(() => new SolisCloudClient(options));
    }

    [Fact]
    public void Constructor_rejects_null_arguments()
    {
        using HttpClient httpClient = new();

        Assert.Throws<ArgumentNullException>(() => new SolisCloudClient(null!));
        Assert.Throws<ArgumentNullException>(() => new SolisCloudClient(null!, TestClient.CreateOptions()));
        Assert.Throws<ArgumentNullException>(() => new SolisCloudClient(httpClient, null!));
    }

    [Fact]
    public void Options_ToString_never_reveals_the_secret()
    {
        string text = TestClient.CreateOptions().ToString();

        Assert.Contains(TestClient.ApiId, text, StringComparison.Ordinal);
        Assert.DoesNotContain(TestClient.ApiSecret, text, StringComparison.Ordinal);
    }
}
