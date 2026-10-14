// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud;

/// <summary>
/// Base type for errors raised by the SolisCloud client.
/// </summary>
/// <remarks>
/// Network failures are not wrapped: they surface as <see cref="HttpRequestException"/> or, for timeouts,
/// <see cref="TaskCanceledException"/>, exactly as <see cref="HttpClient"/> raises them.
/// </remarks>
public class SolisCloudException : Exception
{
    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudException"/> class.
    /// </summary>
    public SolisCloudException()
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SolisCloudException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudException"/> class with a message and cause.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one, if any.</param>
    public SolisCloudException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
