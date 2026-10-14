// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Cli;

/// <summary>
/// Process exit codes, so scripts can tell a configuration problem from an API failure.
/// </summary>
internal static class ExitCodes
{
    /// <summary>Everything worked.</summary>
    public const int Success = 0;

    /// <summary>The API or the network reported an error.</summary>
    public const int ApiError = 1;

    /// <summary>The command line or the environment is wrong (for example, credentials are missing).</summary>
    public const int UsageError = 2;

    /// <summary>Some, but not all, requests succeeded.</summary>
    public const int PartialFailure = 3;
}
