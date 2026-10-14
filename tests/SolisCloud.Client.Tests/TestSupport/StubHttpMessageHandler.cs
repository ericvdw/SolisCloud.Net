// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Net;
using System.Text;

namespace SolisCloud.Tests.TestSupport;

/// <summary>
/// An in-memory HTTP handler that records requests and answers with a canned response.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;
    private readonly DateTimeOffset? _responseDate;

    public StubHttpMessageHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK, DateTimeOffset? responseDate = null)
    {
        _responseBody = responseBody;
        _statusCode = statusCode;
        _responseDate = responseDate;
    }

    public List<CapturedRequest> Requests { get; } = [];

    public static string SuccessBody(string dataJson) =>
        "{\"success\":true,\"code\":\"0\",\"msg\":\"success\",\"data\":" + dataJson + "}";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        byte[] body = [];
        if (request.Content is not null)
        {
#if NET
            body = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
            body = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
        }

        Requests.Add(new CapturedRequest(request, body));

        HttpResponseMessage response = new(_statusCode)
        {
            Content = new StringContent(_responseBody, Encoding.UTF8, "application/json"),
        };
        response.Headers.Date = _responseDate;
        return response;
    }
}
