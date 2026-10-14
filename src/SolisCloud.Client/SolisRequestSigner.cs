// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SolisCloud.Internal;

namespace SolisCloud;

/// <summary>
/// Implements the SolisCloud request signing scheme (API document section 2.2).
/// </summary>
/// <remarks>
/// <para>
/// Every request carries four headers: <c>Content-MD5</c>, <c>Content-Type</c>, <c>Date</c> and
/// <c>Authorization: API {apiId}:{signature}</c>, where the signature is
/// <c>Base64(HMAC-SHA1(apiSecret, "POST\n" + Content-MD5 + "\n" + Content-Type + "\n" + Date + "\n" + path))</c>.
/// </para>
/// <para>
/// The server recomputes the signature from the header values it receives, so the strings signed here must be
/// byte-for-byte what goes on the wire. <see cref="SolisCloudClient"/> handles that; this type is public so the
/// scheme can be reused or debugged independently (for example when porting it to a microcontroller).
/// </para>
/// </remarks>
public static class SolisRequestSigner
{
    /// <summary>
    /// The <c>Content-Type</c> value sent and signed with every request.
    /// </summary>
    /// <remarks>
    /// The API document lists <c>application/json;charset=UTF-8</c> but its worked example uses
    /// <c>application/json</c>. The parameterless form is used because some HTTP stacks (notably .NET Framework)
    /// re-format media-type parameters, e.g. to <c>application/json; charset=UTF-8</c>, which would no longer match
    /// the signed string. JSON is UTF-8 by definition (RFC 8259), so nothing is lost.
    /// </remarks>
    public const string ContentType = "application/json";

    /// <summary>
    /// Computes the <c>Content-MD5</c> header value: the Base64-encoded MD5 digest of the request body.
    /// </summary>
    /// <param name="body">The exact request body bytes that will be sent.</param>
    /// <returns>The Base64-encoded 128-bit digest.</returns>
    [SuppressMessage("Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "MD5 is mandated by the SolisCloud protocol as a body checksum, not used for security.")]
    public static string ComputeContentMd5(byte[] body)
    {
        Guard.NotNull(body);
#if NET
        byte[] digest = MD5.HashData(body);
#else
        byte[] digest;
        using (var md5 = MD5.Create())
        {
            digest = md5.ComputeHash(body);
        }
#endif
        return Convert.ToBase64String(digest);
    }

    /// <summary>
    /// Formats a timestamp for the <c>Date</c> header in RFC 1123 form, e.g. <c>Fri, 26 Jul 2019 06:00:46 GMT</c>.
    /// </summary>
    /// <param name="timestamp">The time to format; it is converted to UTC.</param>
    /// <returns>The formatted date.</returns>
    /// <remarks>
    /// The API document's Java sample uses a single-digit day (<c>d</c>), but standard RFC 1123 (two-digit day) is
    /// used here. Some HTTP stacks re-format the <c>Date</c> header into RFC 1123 regardless of the string supplied,
    /// and the signature must match what is actually transmitted.
    /// </remarks>
    public static string FormatDate(DateTimeOffset timestamp) =>
        timestamp.UtcDateTime.ToString("r", CultureInfo.InvariantCulture);

    /// <summary>
    /// Builds the canonical string that is signed.
    /// </summary>
    /// <param name="contentMd5">The <c>Content-MD5</c> header value.</param>
    /// <param name="contentType">The <c>Content-Type</c> header value.</param>
    /// <param name="date">The <c>Date</c> header value.</param>
    /// <param name="canonicalizedResource">The request path, e.g. <c>/v1/api/inverterDetail</c>.</param>
    /// <returns>The string to sign.</returns>
    public static string BuildStringToSign(string contentMd5, string contentType, string date, string canonicalizedResource)
    {
        Guard.NotNull(contentMd5);
        Guard.NotNull(contentType);
        Guard.NotNull(date);
        Guard.NotNull(canonicalizedResource);

        return "POST\n" + contentMd5 + "\n" + contentType + "\n" + date + "\n" + canonicalizedResource;
    }

    /// <summary>
    /// Signs a canonical string with the API secret.
    /// </summary>
    /// <param name="apiSecret">The API secret from SolisCloud's API management page.</param>
    /// <param name="stringToSign">The output of <see cref="BuildStringToSign"/>.</param>
    /// <returns>The Base64-encoded HMAC-SHA1 signature.</returns>
    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "HMAC-SHA1 is mandated by the SolisCloud protocol.")]
    public static string ComputeSignature(string apiSecret, string stringToSign)
    {
        Guard.NotNull(apiSecret);
        Guard.NotNull(stringToSign);

        byte[] key = Encoding.UTF8.GetBytes(apiSecret);
        byte[] data = Encoding.UTF8.GetBytes(stringToSign);
#if NET
        byte[] mac = HMACSHA1.HashData(key, data);
#else
        byte[] mac;
        using (HMACSHA1 hmac = new(key))
        {
            mac = hmac.ComputeHash(data);
        }
#endif
        return Convert.ToBase64String(mac);
    }

    /// <summary>
    /// Formats the <c>Authorization</c> header value.
    /// </summary>
    /// <param name="apiId">The API ID (key ID) from SolisCloud's API management page.</param>
    /// <param name="signature">The output of <see cref="ComputeSignature"/>.</param>
    /// <returns><c>API {apiId}:{signature}</c>.</returns>
    public static string FormatAuthorization(string apiId, string signature)
    {
        Guard.NotNull(apiId);
        Guard.NotNull(signature);

        return "API " + apiId + ":" + signature;
    }
}
