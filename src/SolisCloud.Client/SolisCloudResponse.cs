// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Text.Json;
using SolisCloud.Internal;

namespace SolisCloud;

/// <summary>
/// A successful SolisCloud response with its <c>data</c> payload left as raw JSON.
/// </summary>
/// <remarks>
/// Every SolisCloud response has the envelope <c>{ "success": true, "code": "0", "msg": "success", "data": … }</c>.
/// This type is the lowest level of access: the envelope has been checked, and <see cref="Data"/> holds the payload
/// exactly as sent, including fields the library does not model yet.
/// </remarks>
public sealed class SolisCloudResponse
{
    private SolisCloudResponse(SolisEndpoint endpoint, string json, string? message, JsonElement data, DateTimeOffset? serverDate)
    {
        Endpoint = endpoint;
        Json = json;
        Message = message;
        Data = data;
        ServerDate = serverDate;
    }

    /// <summary>
    /// Gets the operation that produced this response.
    /// </summary>
    public SolisEndpoint Endpoint { get; }

    /// <summary>
    /// Gets the full response body.
    /// </summary>
    public string Json { get; }

    /// <summary>
    /// Gets the <c>msg</c> value from the envelope, normally <c>success</c>.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the <c>data</c> payload. Its <see cref="JsonElement.ValueKind"/> is
    /// <see cref="JsonValueKind.Undefined"/> if the response had no <c>data</c> member.
    /// </summary>
    /// <remarks>The element does not depend on any disposable document and may be kept indefinitely.</remarks>
    public JsonElement Data { get; }

    /// <summary>
    /// Gets the server's <c>Date</c> response header, if present. Useful for detecting clock skew: requests are
    /// rejected if the client clock is more than 15 minutes from the server's.
    /// </summary>
    public DateTimeOffset? ServerDate { get; }

    /// <summary>
    /// Parses a response body, checking the envelope.
    /// </summary>
    /// <param name="endpoint">The operation that produced the body.</param>
    /// <param name="json">The response body.</param>
    /// <param name="serverDate">The server's <c>Date</c> header, if any.</param>
    /// <returns>The parsed response.</returns>
    /// <exception cref="SolisCloudApiException">The envelope reports a failure.</exception>
    /// <exception cref="SolisCloudHttpException">The body is not a SolisCloud JSON envelope.</exception>
    internal static SolisCloudResponse Parse(SolisEndpoint endpoint, string json, DateTimeOffset? serverDate)
    {
        Guard.NotNull(endpoint);
        Guard.NotNull(json);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new SolisCloudHttpException(
                $"SolisCloud {endpoint.Path} returned a body that is not valid JSON.",
                endpoint.Path,
                System.Net.HttpStatusCode.OK,
                json,
                ex);
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new SolisCloudHttpException(
                    $"SolisCloud {endpoint.Path} returned JSON that is not an object.",
                    endpoint.Path,
                    System.Net.HttpStatusCode.OK,
                    json);
            }

            string code = ReadCode(root);
            string? message = ReadString(root, "msg");

            // Success requires code "0", and "success" must not be false. Some responses omit "success".
            bool successFlag = !root.TryGetProperty("success", out JsonElement success) || success.ValueKind != JsonValueKind.False;
            if (!successFlag || !string.Equals(code, "0", StringComparison.Ordinal))
            {
                throw new SolisCloudApiException(endpoint.Path, code, message, json);
            }

            JsonElement data = root.TryGetProperty("data", out JsonElement value) ? value.Clone() : default;
            return new SolisCloudResponse(endpoint, json, message, data, serverDate);
        }
    }

    // The document says "code" is a string, but be tolerant of a number.
    private static string ReadCode(JsonElement root)
    {
        if (!root.TryGetProperty("code", out JsonElement code))
        {
            return string.Empty;
        }

        return code.ValueKind switch
        {
            JsonValueKind.String => code.GetString() ?? string.Empty,
            JsonValueKind.Number => code.GetRawText(),
            _ => string.Empty,
        };
    }

    private static string? ReadString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
