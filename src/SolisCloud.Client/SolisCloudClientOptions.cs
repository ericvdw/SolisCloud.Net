// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud;

/// <summary>
/// Configuration for <see cref="SolisCloudClient"/>.
/// </summary>
/// <remarks>
/// Properties are settable so the type binds directly from configuration
/// (e.g. <c>services.Configure&lt;SolisCloudClientOptions&gt;(config.GetSection("SolisCloud"))</c>).
/// The client copies the values when it is constructed; later changes have no effect on it.
/// </remarks>
public sealed class SolisCloudClientOptions
{
    /// <summary>
    /// Gets the production API address, <c>https://www.soliscloud.com:13333/</c>.
    /// </summary>
    public static Uri DefaultBaseAddress { get; } = new("https://www.soliscloud.com:13333/", UriKind.Absolute);

    /// <summary>
    /// Gets or sets the API ID (key ID) from SolisCloud: Account → Basic Settings → API Management.
    /// </summary>
    public string ApiId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API secret from SolisCloud: Account → Basic Settings → API Management.
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API address. Defaults to <see cref="DefaultBaseAddress"/>.
    /// </summary>
    public Uri BaseAddress { get; set; } = DefaultBaseAddress;

    /// <summary>
    /// Gets or sets a value indicating whether the client limits its own request rate to each operation's
    /// documented limit (<see cref="SolisEndpoint.PermitsPerSecond"/>). Defaults to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Requests over the limit wait for a free slot rather than fail. Disable only if limiting is done elsewhere,
    /// for example by a shared handler in the <see cref="HttpClient"/> pipeline.
    /// </remarks>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether operations that change account data (adding stations, binding
    /// devices) may be sent. Defaults to <see langword="false"/>.
    /// </summary>
    public bool AllowWriteOperations { get; set; }

    /// <inheritdoc />
    /// <remarks>Never includes the API secret.</remarks>
    public override string ToString() =>
        $"{nameof(SolisCloudClientOptions)} {{ {nameof(ApiId)} = {ApiId}, {nameof(BaseAddress)} = {BaseAddress} }}";
}
