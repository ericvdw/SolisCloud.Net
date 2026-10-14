// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using SolisCloud.Internal;

namespace SolisCloud;

/// <summary>
/// A SolisCloud API operation: its path, documented rate limit, and whether it changes data.
/// </summary>
/// <remarks>
/// Known operations are listed in <see cref="SolisEndpoints"/>. New instances can be created for operations the
/// library does not know about yet; they are signed and rate-limited in the same way.
/// </remarks>
public sealed class SolisEndpoint
{
    private const string ApiPrefix = "/v1/api/";

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisEndpoint"/> class.
    /// </summary>
    /// <param name="path">The request path, starting with <c>/</c>, e.g. <c>/v1/api/inverterDetail</c>.</param>
    /// <param name="permitsPerSecond">The documented request limit for this operation.</param>
    /// <param name="modifiesData">
    /// <see langword="true"/> if the operation changes account data. Such operations are refused unless
    /// <see cref="SolisCloudClientOptions.AllowWriteOperations"/> is set.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="path"/> does not start with <c>/</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="permitsPerSecond"/> is less than 1.</exception>
    public SolisEndpoint(string path, int permitsPerSecond = 2, bool modifiesData = false)
    {
        Guard.NotNullOrWhiteSpace(path);
        if (path[0] != '/')
        {
            throw new ArgumentException("The path must start with '/'.", nameof(path));
        }

        if (permitsPerSecond < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(permitsPerSecond), permitsPerSecond, "At least one request per second must be permitted.");
        }

        Path = path;
        PermitsPerSecond = permitsPerSecond;
        ModifiesData = modifiesData;
        Name = path.StartsWith(ApiPrefix, StringComparison.Ordinal) ? path.Substring(ApiPrefix.Length) : path;
    }

    /// <summary>
    /// Gets the request path, which is also the canonicalised resource used in the signature.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the short name of the operation: the path without the <c>/v1/api/</c> prefix, e.g. <c>inverterDetail</c>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the documented maximum number of requests per second.
    /// </summary>
    public int PermitsPerSecond { get; }

    /// <summary>
    /// Gets a value indicating whether the operation changes account data.
    /// </summary>
    public bool ModifiesData { get; }

    /// <inheritdoc />
    public override string ToString() => Path;
}
