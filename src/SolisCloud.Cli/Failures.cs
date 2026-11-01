// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Globalization;

namespace SolisCloud.Cli;

/// <summary>
/// Turns request failures into short explanations with a hint about the likely cause.
/// </summary>
internal static class Failures
{
    /// <summary>
    /// Returns <see langword="true"/> for the exceptions a SolisCloud request is expected to throw: API errors,
    /// HTTP errors, network failures and timeouts. Anything else is a bug and should not be caught.
    /// </summary>
    public static bool IsRequestFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is SolisCloudException or HttpRequestException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    /// <summary>
    /// Describes a failure in one or two lines.
    /// </summary>
    public static string Describe(Exception exception) =>
        exception switch
        {
            SolisCloudApiException api => DescribeApiError(api),
            SolisCloudHttpException http => http.Message + " " + HintForStatus((int)http.StatusCode),
            HttpRequestException network =>
                "Could not reach SolisCloud: " + network.Message
                + " Port 13333 is blocked on some networks; SOLIS_BASE_ADDRESS can point elsewhere if needed.",
            TaskCanceledException => "The request timed out.",
            _ => exception.Message,
        };

    private static string DescribeApiError(SolisCloudApiException exception)
    {
        string text = string.Create(
            CultureInfo.InvariantCulture,
            $"SolisCloud {exception.EndpointPath} failed with code {exception.Code}: {exception.ApiMessage ?? "(no message)"}.");

        string? hint = exception.Code switch
        {
            "R0000" => "The API ID or secret is wrong, or API access isn't enabled for this account.",
            "I0000" => "A required parameter is missing from the request body.",
            "B0049" => "The collector doesn't exist or isn't visible to this account.",
            "1" => "SolisCloud couldn't read a value in the request, e.g. a date not written as yyyy-MM-dd.",
            _ => null,
        };

        return hint is null ? text : text + " " + hint;
    }

    private static string HintForStatus(int statusCode) =>
        statusCode switch
        {
            401 or 403 => "Check the API ID and secret, and that the computer's clock is correct (within 15 minutes).",
            429 => "Too many requests; wait a moment and try again.",
            >= 500 => "SolisCloud had a server error; try again later.",
            _ => string.Empty,
        };
}
