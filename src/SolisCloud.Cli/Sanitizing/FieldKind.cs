// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Cli.Sanitizing;

/// <summary>
/// What to do with a JSON property, decided by its name.
/// </summary>
internal enum FieldKind
{
    /// <summary>Not sensitive; kept unless its value matches a sensitive value seen elsewhere.</summary>
    Keep,

    /// <summary>An ID or serial number: replaced with a stable fake of the same shape.</summary>
    Identifier,

    /// <summary>Free text about a person or place (names, addresses, contact details): replaced entirely.</summary>
    PersonalText,

    /// <summary>A coordinate: replaced with zero.</summary>
    Location,
}
