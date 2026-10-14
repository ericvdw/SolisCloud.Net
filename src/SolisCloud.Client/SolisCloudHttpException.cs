// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Net;

namespace SolisCloud;

/// <summary>
/// The SolisCloud server answered with a non-success HTTP status, or with a body that is not a SolisCloud response.
/// </summary>
public sealed class SolisCloudHttpException : SolisCloudException
{
    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudHttpException"/> class.
    /// </summary>
    public SolisCloudHttpException()
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudHttpException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SolisCloudHttpException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudHttpException"/> class with a message and cause.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public SolisCloudHttpException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudHttpException"/> class from an HTTP response.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="endpointPath">The path of the operation that failed.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="responseBody">The response body, if any.</param>
    /// <param name="innerException">The exception that caused this one, if any.</param>
    public SolisCloudHttpException(string message, string endpointPath, HttpStatusCode statusCode, string? responseBody, Exception? innerException = null)
        : base(message, innerException)
    {
        EndpointPath = endpointPath;
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Gets the path of the operation that failed.
    /// </summary>
    public string? EndpointPath { get; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the response body, if any.
    /// </summary>
    public string? ResponseBody { get; }
}
