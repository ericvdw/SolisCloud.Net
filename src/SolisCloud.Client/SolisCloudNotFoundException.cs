// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud;

/// <summary>
/// The station, inverter or other device asked for doesn't exist, or the account can't see it.
/// </summary>
/// <remarks>
/// SolisCloud reports this as a success with no data rather than as an error, so the client raises it itself.
/// </remarks>
public sealed class SolisCloudNotFoundException : SolisCloudException
{
    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudNotFoundException"/> class.
    /// </summary>
    public SolisCloudNotFoundException()
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudNotFoundException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SolisCloudNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudNotFoundException"/> class with a message and cause.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public SolisCloudNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="SolisCloudNotFoundException"/> class for one lookup.
    /// </summary>
    /// <param name="endpointPath">The path of the operation.</param>
    /// <param name="id">The ID or serial number that wasn't found.</param>
    public SolisCloudNotFoundException(string endpointPath, string id)
        : base($"SolisCloud {endpointPath} found nothing for '{id}'. Check the ID, and that this account can see it.")
    {
        EndpointPath = endpointPath;
        Id = id;
    }

    /// <summary>Gets the path of the operation.</summary>
    public string? EndpointPath { get; }

    /// <summary>Gets the ID or serial number that wasn't found.</summary>
    public string? Id { get; }
}
