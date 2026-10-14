// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text;

namespace SolisCloud.Tests.TestSupport;

/// <summary>
/// A snapshot of a request taken while it was being sent (the message itself is disposed afterwards).
/// </summary>
internal sealed class CapturedRequest
{
    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    public CapturedRequest(HttpRequestMessage request, byte[] body)
    {
        Method = request.Method;
        RequestUri = request.RequestUri;
        Body = Encoding.UTF8.GetString(body);

        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
        {
            _headers[header.Key] = string.Join(",", header.Value);
        }

        if (request.Content is not null)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
            {
                _headers[header.Key] = string.Join(",", header.Value);
            }
        }
    }

    public HttpMethod Method { get; }

    public Uri? RequestUri { get; }

    public string Body { get; }

    public string? Header(string name) => _headers.TryGetValue(name, out string? value) ? value : null;
}
