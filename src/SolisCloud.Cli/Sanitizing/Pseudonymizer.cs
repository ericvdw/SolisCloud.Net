// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;

namespace SolisCloud.Cli.Sanitizing;

/// <summary>
/// Replaces identifiers with stable fakes that keep their shape: digits stay digits, letters stay letters of the
/// same case, and everything else (dashes, dots) is kept. The length never changes.
/// </summary>
/// <remarks>
/// <para>
/// The same input always gives the same output for one key, so relationships survive sanitising: an inverter's
/// <c>stationId</c> still matches the station's <c>id</c>. The output is an HMAC of the input, so it can't be
/// reversed without the key.
/// </para>
/// <para>
/// Each capture uses a new random key, so fakes from two captures can't be correlated with each other either.
/// </para>
/// </remarks>
internal sealed class Pseudonymizer
{
    private readonly byte[] _key;
    private readonly Dictionary<string, string> _cache = new(StringComparer.Ordinal);

    public Pseudonymizer(byte[] key)
    {
        if (key.Length == 0)
        {
            throw new ArgumentException("The key must not be empty.", nameof(key));
        }

        _key = key;
    }

    /// <summary>
    /// Creates an instance with a random key.
    /// </summary>
    public static Pseudonymizer CreateRandom() => new(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Returns the fake for <paramref name="value"/>.
    /// </summary>
    public string Pseudonymize(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        if (!_cache.TryGetValue(value, out string? fake))
        {
            fake = Compute(value);
            _cache.Add(value, fake);
        }

        return fake;
    }

    private string Compute(string value)
    {
        byte[] hash = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value));
        StringBuilder result = new(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char original = value[i];

            // Mix in the position so values longer than the hash don't repeat with its period.
            int b = hash[i % hash.Length] ^ (i / hash.Length * 31);
            if (char.IsAsciiDigit(original))
            {
                // Keep a leading non-zero digit non-zero, so numeric IDs keep their magnitude.
                result.Append(i == 0 && original != '0' ? (char)('1' + (b % 9)) : (char)('0' + (b % 10)));
            }
            else if (char.IsAsciiLetterUpper(original))
            {
                result.Append((char)('A' + (b % 26)));
            }
            else if (char.IsAsciiLetterLower(original))
            {
                result.Append((char)('a' + (b % 26)));
            }
            else
            {
                result.Append(original);
            }
        }

        return result.ToString();
    }
}
