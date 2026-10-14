// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using SolisCloud.Internal;

namespace SolisCloud;

/// <summary>
/// Sends signed requests to the SolisCloud platform API.
/// </summary>
/// <remarks>
/// <para>
/// The client is thread-safe and intended to be long-lived: create one per set of credentials and reuse it.
/// Requests are rate-limited per operation (see <see cref="SolisCloudClientOptions.EnableRateLimiting"/>).
/// </para>
/// <para>
/// When an <see cref="HttpClient"/> is supplied it is not disposed by this client, and its handler pipeline is
/// respected: retries, logging and proxies configured there all apply. Do not set
/// <see cref="HttpClient.BaseAddress"/>; the client builds absolute URIs from
/// <see cref="SolisCloudClientOptions.BaseAddress"/>.
/// </para>
/// </remarks>
public sealed partial class SolisCloudClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly TimeProvider _timeProvider;
    private readonly string _apiId;
    private readonly string _apiSecret;
    private readonly Uri _baseAddress;
    private readonly bool _allowWriteOperations;
    private readonly string _currency;
    private readonly EndpointRateLimiter? _rateLimiter;
    private bool _disposed;

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudClient"/> class that owns its own
    /// <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="options">The client configuration.</param>
    /// <exception cref="ArgumentException">The API ID or secret is missing, or the base address is not absolute.</exception>
    public SolisCloudClient(SolisCloudClientOptions options)
        : this(CreateOwnedHttpClient(), ownsHttpClient: true, options, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudClient"/> class that uses the supplied
    /// <see cref="HttpClient"/>, typically one created by <c>IHttpClientFactory</c>.
    /// </summary>
    /// <param name="httpClient">The HTTP client to send requests with. It is not disposed by this instance.</param>
    /// <param name="options">The client configuration.</param>
    /// <param name="timeProvider">The clock used for the <c>Date</c> header; defaults to the system clock.</param>
    /// <exception cref="ArgumentException">The API ID or secret is missing, or the base address is not absolute.</exception>
    public SolisCloudClient(HttpClient httpClient, SolisCloudClientOptions options, TimeProvider? timeProvider = null)
        : this(Guard.NotNull(httpClient), ownsHttpClient: false, options, timeProvider ?? TimeProvider.System)
    {
    }

    private SolisCloudClient(HttpClient httpClient, bool ownsHttpClient, SolisCloudClientOptions options, TimeProvider timeProvider)
    {
        // Assign ownership first so an invalid configuration still disposes a client we created.
        _httpClient = httpClient;
        _ownsHttpClient = ownsHttpClient;
        try
        {
            Guard.NotNull(options);
            if (string.IsNullOrWhiteSpace(options.ApiId))
            {
                throw new ArgumentException($"{nameof(SolisCloudClientOptions.ApiId)} must be set.", nameof(options));
            }

            if (string.IsNullOrWhiteSpace(options.ApiSecret))
            {
                throw new ArgumentException($"{nameof(SolisCloudClientOptions.ApiSecret)} must be set.", nameof(options));
            }

            if (options.BaseAddress is null || !options.BaseAddress.IsAbsoluteUri)
            {
                throw new ArgumentException($"{nameof(SolisCloudClientOptions.BaseAddress)} must be an absolute URI.", nameof(options));
            }

            _apiId = options.ApiId;
            _apiSecret = options.ApiSecret;
            _baseAddress = options.BaseAddress;

            _allowWriteOperations = options.AllowWriteOperations;
            _currency = string.IsNullOrWhiteSpace(options.Currency) ? "EUR" : options.Currency.Trim();
            _timeProvider = timeProvider;
            _rateLimiter = options.EnableRateLimiting ? new EndpointRateLimiter(timeProvider) : null;
        }
        catch
        {
            if (ownsHttpClient)
            {
                httpClient.Dispose();
            }

            throw;
        }
    }

    /// <summary>
    /// Sends a request with a UTF-8 JSON body and returns the raw response.
    /// </summary>
    /// <param name="endpoint">The operation to call, usually one of <see cref="SolisEndpoints"/>.</param>
    /// <param name="utf8JsonBody">
    /// The UTF-8 encoded JSON request body, e.g. <c>Encoding.UTF8.GetBytes("{\"pageNo\":1,\"pageSize\":10}")</c>.
    /// Bytes rather than a string, because the <c>Content-MD5</c> signature covers the exact bytes sent.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The response, with the envelope checked and the <c>data</c> payload as raw JSON.</returns>
    /// <exception cref="SolisCloudApiException">The API reported a failure.</exception>
    /// <exception cref="SolisCloudHttpException">The server returned a non-success status or a malformed body.</exception>
    /// <exception cref="InvalidOperationException">
    /// The operation modifies data and <see cref="SolisCloudClientOptions.AllowWriteOperations"/> is not set.
    /// </exception>
    public async Task<SolisCloudResponse> SendAsync(SolisEndpoint endpoint, ReadOnlyMemory<byte> utf8JsonBody, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(endpoint);
        ThrowIfDisposed();
        if (endpoint.ModifiesData && !_allowWriteOperations)
        {
            throw new InvalidOperationException(
                $"{endpoint.Path} changes account data. Set {nameof(SolisCloudClientOptions)}.{nameof(SolisCloudClientOptions.AllowWriteOperations)} to allow it.");
        }

        // One copy, used for both the MD5 and the request content, so the hash always matches what is sent.
        byte[] body = utf8JsonBody.ToArray();

        if (_rateLimiter is not null)
        {
            await _rateLimiter.WaitAsync(endpoint, cancellationToken).ConfigureAwait(false);
        }

        // Sign after waiting for the rate limiter, so the Date header reflects the actual send time.
        using HttpRequestMessage request = CreateSignedRequest(endpoint, body);
        using HttpResponseMessage response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
            .ConfigureAwait(false);

        string responseBody = await ReadBodyAsync(response.Content, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new SolisCloudHttpException(
                $"SolisCloud {endpoint.Path} returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).",
                endpoint.Path,
                response.StatusCode,
                responseBody);
        }

        return SolisCloudResponse.Parse(endpoint, responseBody, response.Headers.Date);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _rateLimiter?.Dispose();
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private static HttpClient CreateOwnedHttpClient()
    {
#if NET
        // A long-lived client must still notice DNS changes, so recycle pooled connections periodically.
        // (Properties are set inside the try rather than in an object initializer: CA2000 rightly points out that
        // an exception thrown by an initializer would leak the handler.)
        SocketsHttpHandler handler = new();
        try
        {
            handler.PooledConnectionLifetime = TimeSpan.FromMinutes(15);
            return new HttpClient(handler, disposeHandler: true);
        }
        catch
        {
            handler.Dispose();
            throw;
        }
#else
        return new HttpClient();
#endif
    }

    private static async Task<string> ReadBodyAsync(HttpContent? content, CancellationToken cancellationToken)
    {
        if (content is null)
        {
            return string.Empty;
        }

#if NET
        return await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
        cancellationToken.ThrowIfCancellationRequested();
        return await content.ReadAsStringAsync().ConfigureAwait(false);
#endif
    }

    private HttpRequestMessage CreateSignedRequest(SolisEndpoint endpoint, byte[] body)
    {
        string contentMd5 = SolisRequestSigner.ComputeContentMd5(body);
        string date = SolisRequestSigner.FormatDate(_timeProvider.GetUtcNow());
        string stringToSign = SolisRequestSigner.BuildStringToSign(contentMd5, SolisRequestSigner.ContentType, date, endpoint.Path);
        string signature = SolisRequestSigner.ComputeSignature(_apiSecret, stringToSign);

        // Headers are added without validation so .NET sends the exact strings that were signed, rather than
        // re-formatting them (e.g. adding a space after ';' in Content-Type).
        ByteArrayContent content = new(body);
        content.Headers.TryAddWithoutValidation("Content-Type", SolisRequestSigner.ContentType);
        content.Headers.TryAddWithoutValidation("Content-MD5", contentMd5);

        HttpRequestMessage request = new(HttpMethod.Post, new Uri(_baseAddress, endpoint.Path.TrimStart('/')))
        {
            Content = content,
        };
        // Skip the "Expect: 100-continue" round trip some stacks (e.g. .NET Framework) add to POSTs by default.
        request.Headers.ExpectContinue = false;
        request.Headers.TryAddWithoutValidation("Date", date);
        request.Headers.TryAddWithoutValidation("Authorization", SolisRequestSigner.FormatAuthorization(_apiId, signature));
        return request;
    }

    private void ThrowIfDisposed() => Guard.NotDisposed(_disposed, this);
}
