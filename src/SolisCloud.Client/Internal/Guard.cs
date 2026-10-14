// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SolisCloud.Internal;

/// <summary>
/// Argument validation that works on every target framework.
/// </summary>
/// <remarks>
/// <c>ArgumentNullException.ThrowIfNull</c> only exists on .NET 6+, so the library needs its own helper for
/// netstandard2.0. Parameters are marked <see cref="ValidatedNotNullAttribute"/> so analyzer CA1062 knows the
/// argument has been checked.
/// </remarks>
internal static class Guard
{
    public static T NotNull<T>(
        [ValidatedNotNull][NotNull] T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
#if NET
        ArgumentNullException.ThrowIfNull(value, paramName);
#else
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
#endif
        return value;
    }

    public static void NotDisposed([DoesNotReturnIf(true)] bool disposed, object instance)
    {
#if NET
        ObjectDisposedException.ThrowIf(disposed, instance);
#else
        if (disposed)
        {
            throw new ObjectDisposedException(instance?.GetType().FullName);
        }
#endif
    }

    public static string NotNullOrWhiteSpace(
        [ValidatedNotNull][NotNull] string? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if NET
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
#else
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", paramName);
        }
#endif
        return value;
    }
}
