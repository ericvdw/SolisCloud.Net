// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace SolisCloud.Cli;

/// <summary>
/// Reads API credentials from environment variables.
/// </summary>
/// <remarks>
/// Credentials are deliberately not accepted as command-line options: those end up in shell history and are
/// visible to other users in process listings.
/// </remarks>
internal static class Credentials
{
    public const string ApiIdVariable = "SOLIS_API_ID";
    public const string ApiSecretVariable = "SOLIS_API_SECRET";
    public const string BaseAddressVariable = "SOLIS_BASE_ADDRESS";

    /// <summary>
    /// Builds client options from the environment.
    /// </summary>
    /// <param name="getVariable">Reads an environment variable.</param>
    /// <param name="options">The options, when successful.</param>
    /// <param name="error">A message saying what is missing or invalid, when unsuccessful.</param>
    /// <returns><see langword="true"/> if the credentials are present and valid.</returns>
    public static bool TryLoad(
        Func<string, string?> getVariable,
        [NotNullWhen(true)] out SolisCloudClientOptions? options,
        [NotNullWhen(false)] out string? error)
    {
        options = null;
        string? apiId = getVariable(ApiIdVariable);
        string? apiSecret = getVariable(ApiSecretVariable);

        List<string> missing = [];
        if (string.IsNullOrWhiteSpace(apiId))
        {
            missing.Add(ApiIdVariable);
        }

        if (string.IsNullOrWhiteSpace(apiSecret))
        {
            missing.Add(ApiSecretVariable);
        }

        if (missing.Count > 0)
        {
            error = "Set " + string.Join(" and ", missing) + ". The values are on SolisCloud under Account > Basic Settings > API Management.";
            return false;
        }

        Uri baseAddress = SolisCloudClientOptions.DefaultBaseAddress;
        string? baseAddressText = getVariable(BaseAddressVariable);
        if (!string.IsNullOrWhiteSpace(baseAddressText)
            && !Uri.TryCreate(baseAddressText, UriKind.Absolute, out baseAddress!))
        {
            error = BaseAddressVariable + " must be an absolute URI, e.g. " + SolisCloudClientOptions.DefaultBaseAddress + ".";
            return false;
        }

        options = new SolisCloudClientOptions
        {
            ApiId = apiId!.Trim(),
            ApiSecret = apiSecret!.Trim(),
            BaseAddress = baseAddress,
        };
        error = null;
        return true;
    }

    /// <summary>
    /// Shortens an API ID for display, e.g. <c>1300…6416</c>, so it can be recognised without being copied.
    /// </summary>
    public static string Mask(string apiId) =>
        apiId.Length <= 8 ? new string('*', apiId.Length) : apiId[..4] + "…" + apiId[^4..];
}
