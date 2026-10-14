// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text;

namespace SolisCloud.Tests.TestSupport;

/// <summary>
/// Lets tests write request bodies as JSON strings.
/// </summary>
internal static class SolisCloudClientExtensions
{
    public static Task<SolisCloudResponse> SendAsync(
        this SolisCloudClient client,
        SolisEndpoint endpoint,
        string jsonBody,
        CancellationToken cancellationToken) =>
        client.SendAsync(endpoint, Encoding.UTF8.GetBytes(jsonBody), cancellationToken);
}
