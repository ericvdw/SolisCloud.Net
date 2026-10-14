// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Net;
using System.Text;

namespace SolisCloud.Cli.Tests.TestSupport;

/// <summary>
/// Answers each request according to its path, and records what was asked.
/// Paths without a canned response get a successful, empty <c>data</c> object.
/// </summary>
internal sealed class RoutingHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _routes = new(StringComparer.Ordinal);

    public List<(string Path, string Body)> Requests { get; } = [];

    public DateTimeOffset? ResponseDate { get; set; }

    public static string Success(string dataJson) =>
        "{\"success\":true,\"code\":\"0\",\"msg\":\"success\",\"data\":" + dataJson + "}";

    public static string Failure(string code, string message) =>
        "{\"success\":false,\"code\":\"" + code + "\",\"msg\":\"" + message + "\",\"data\":null}";

    public RoutingHttpMessageHandler Route(SolisEndpoint endpoint, string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _routes[endpoint.Path] = (status, body);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string path = request.RequestUri!.AbsolutePath;
        string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((path, body));

        (HttpStatusCode status, string responseBody) = _routes.TryGetValue(path, out (HttpStatusCode, string) route)
            ? route
            : (HttpStatusCode.OK, Success("{}"));

        HttpResponseMessage response = new(status)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
        };
        response.Headers.Date = ResponseDate;
        return response;
    }
}
