// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Internal;

/// <summary>
/// Tells analyzer CA1062 that the decorated parameter is checked for <see langword="null"/> by the method.
/// </summary>
/// <remarks>The analyzer matches this attribute by name, so any namespace works.</remarks>
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class ValidatedNotNullAttribute : Attribute
{
}
