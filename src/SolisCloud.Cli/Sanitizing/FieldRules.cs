// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Cli.Sanitizing;

/// <summary>
/// Classifies SolisCloud property names. The API document is incomplete, so these are rules (suffixes, keywords)
/// rather than an exhaustive list: an undocumented <c>fooId</c> or <c>barSn</c> is still caught.
/// </summary>
internal static class FieldRules
{
    private static readonly HashSet<string> IdentifierNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "ids", "sn", "sno", "snList", "idList", "stationIds", "nmiCode", "orgCode", "refId",

        // Location codes: harmless alone, but together they pinpoint a suburb.
        "city", "region", "county", "street", "zipCode", "postCode", "postalCode",
    };

    // Names that match the identifier suffixes but describe configuration, not a person or device.
    private static readonly HashSet<string> NotIdentifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "timeZoneId", "countryId", "currencyId", "languageId",
    };

    private static readonly HashSet<string> PersonalTextNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "addr", "address", "account", "owner", "installer", "contact", "tel",
        "cityStr", "regionStr", "countyStr", "streetStr", "provinceStr",
    };

    private static readonly HashSet<string> NotPersonalText = new(StringComparer.OrdinalIgnoreCase)
    {
        "timeZoneName", "countryShortName", "nationalStandardsName",
    };

    private static readonly string[] PersonalTextKeywords = ["phone", "mobile", "mail"];

    private static readonly HashSet<string> LocationNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "latitude", "longitude", "lat", "lng", "lon",
    };

    /// <summary>
    /// Classifies a property by name.
    /// </summary>
    public static FieldKind Classify(string propertyName)
    {
        if (LocationNames.Contains(propertyName))
        {
            return FieldKind.Location;
        }

        if (IsIdentifier(propertyName))
        {
            return FieldKind.Identifier;
        }

        return IsPersonalText(propertyName) ? FieldKind.PersonalText : FieldKind.Keep;
    }

    private static bool IsIdentifier(string name)
    {
        if (IdentifierNames.Contains(name))
        {
            return true;
        }

        if (NotIdentifiers.Contains(name))
        {
            return false;
        }

        // Case-sensitive on purpose: "stationId" and "collectorSn" are identifiers; "paid" and "lesson" are not.
        return name.EndsWith("Id", StringComparison.Ordinal)
            || name.EndsWith("Ids", StringComparison.Ordinal)
            || name.EndsWith("Sn", StringComparison.Ordinal)
            || name.EndsWith("SN", StringComparison.Ordinal)
            || name.EndsWith("SnList", StringComparison.Ordinal);
    }

    private static bool IsPersonalText(string name)
    {
        if (NotPersonalText.Contains(name))
        {
            return false;
        }

        // Includes "remark1".."remark3", "pic1Url" and the like, which the document lists for stations.
        if (PersonalTextNames.Contains(name)
            || name.Equals("name", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("Name", StringComparison.Ordinal)
            || name.EndsWith("Url", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("remark", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (string keyword in PersonalTextKeywords)
        {
            if (name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
