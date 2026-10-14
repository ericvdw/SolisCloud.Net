// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud;

/// <summary>
/// The SolisCloud API processed the request and reported a failure (<c>success: false</c> or a non-zero
/// <c>code</c>).
/// </summary>
/// <remarks>
/// Documented codes (appendix 1) include <c>R0000</c> (no authority), <c>B0049</c> (collector does not exist or
/// is not visible) and <c>I0000</c> (required parameters are empty). Other codes occur in practice.
/// </remarks>
public sealed class SolisCloudApiException : SolisCloudException
{
    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudApiException"/> class.
    /// </summary>
    public SolisCloudApiException()
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudApiException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SolisCloudApiException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudApiException"/> class with a message and cause.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public SolisCloudApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudApiException"/> class from an API response.
    /// </summary>
    /// <param name="endpointPath">The path of the operation that failed.</param>
    /// <param name="code">The <c>code</c> value from the response.</param>
    /// <param name="apiMessage">The <c>msg</c> value from the response.</param>
    /// <param name="responseJson">The full response body.</param>
    public SolisCloudApiException(string endpointPath, string code, string? apiMessage, string responseJson)
        : base(BuildMessage(endpointPath, code, apiMessage))
    {
        EndpointPath = endpointPath;
        Code = code;
        ApiMessage = apiMessage;
        ResponseJson = responseJson;
    }

    /// <summary>
    /// Gets the path of the operation that failed.
    /// </summary>
    public string? EndpointPath { get; }

    /// <summary>
    /// Gets the error code returned by the API, e.g. <c>R0000</c>.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Gets the error description returned by the API.
    /// </summary>
    public string? ApiMessage { get; }

    /// <summary>
    /// Gets the full response body.
    /// </summary>
    public string? ResponseJson { get; }

    private static string BuildMessage(string endpointPath, string code, string? apiMessage) =>
        $"SolisCloud {endpointPath} failed with code '{code}': {apiMessage ?? "(no message)"}";
}
