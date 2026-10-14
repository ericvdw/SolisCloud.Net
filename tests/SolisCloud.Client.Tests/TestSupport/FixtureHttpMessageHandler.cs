// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Net;
using System.Text;
using System.Text.Json;

namespace SolisCloud.Tests.TestSupport;

/// <summary>
/// Answers requests with recorded responses from a fixture folder (files written by <c>solis capture</c>),
/// matched by operation path, and records the requests.
/// </summary>
internal sealed class FixtureHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _responses = new(StringComparer.Ordinal);

    public FixtureHttpMessageHandler(string fixtureName)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixtureName);
        foreach (string file in Directory.GetFiles(directory, "*.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            string operation = document.RootElement.GetProperty("operation").GetString()!;
            _responses[operation] = document.RootElement.GetProperty("response").GetRawText();
        }
    }

    public List<(string Path, string Body)> Requests { get; } = [];

    /// <summary>
    /// Replaces the recorded response for an operation, e.g. to test paging.
    /// </summary>
    public void Respond(SolisEndpoint endpoint, string responseJson) => _responses[endpoint.Path] = responseJson;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string path = request.RequestUri!.AbsolutePath;
#if NET
        string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
        string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        Requests.Add((path, body));

        return _responses.TryGetValue(path, out string? response)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, "application/json") }
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
