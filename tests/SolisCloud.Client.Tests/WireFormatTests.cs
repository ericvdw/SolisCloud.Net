// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using Microsoft.Extensions.Time.Testing;
using SolisCloud.Tests.TestSupport;

namespace SolisCloud.Tests;

/// <summary>
/// Checks the bytes the real HTTP stack sends, since the server verifies the signature against those.
/// </summary>
/// <remarks>
/// On Windows these tests also run on .NET Framework, whose HTTP stack re-formats some headers. A failure here
/// on one framework only means the signed strings and the transmitted strings have drifted apart.
/// </remarks>
public sealed class WireFormatTests
{
    [Fact]
    public async Task Signed_headers_are_transmitted_exactly_as_signed()
    {
        using RawHttpServer server = new("{\"success\":true,\"code\":\"0\",\"msg\":\"success\",\"data\":{}}");
        SolisCloudClientOptions options = TestClient.CreateOptions();
        options.BaseAddress = server.BaseAddress;
        options.EnableRateLimiting = false;
        FakeTimeProvider time = new(new DateTimeOffset(2019, 7, 26, 6, 0, 46, TimeSpan.Zero));
        using HttpClient httpClient = new();
        using SolisCloudClient client = new(httpClient, options, time);

        Task<string> capture = server.CaptureOneRequestAsync(TestContext.Current.CancellationToken);
        _ = await client.SendAsync(SolisEndpoints.StationList, "{\"pageNo\":1,\"pageSize\":10}", TestContext.Current.CancellationToken);
        string raw = await capture;

        string[] lines = raw.Split(["\r\n"], StringSplitOptions.None);
        Assert.Equal("POST /v1/api/userStationList HTTP/1.1", lines[0]);
        Assert.Contains("Content-Type: application/json", lines, StringComparer.Ordinal);
        Assert.Contains("Content-MD5: kxdxk7rbAsrzSIWgEwhH4w==", lines, StringComparer.Ordinal);
        Assert.Contains("Date: Fri, 26 Jul 2019 06:00:46 GMT", lines, StringComparer.Ordinal);
        Assert.Contains("Authorization: API 1300386381676644416:/GOIW8erQaUwWozVKxeKlb/FNkA=", lines, StringComparer.Ordinal);
        Assert.DoesNotContain(lines, line => line.StartsWith("Expect:", StringComparison.OrdinalIgnoreCase));
        Assert.EndsWith("\r\n\r\n{\"pageNo\":1,\"pageSize\":10}", raw, StringComparison.Ordinal);
    }
}
