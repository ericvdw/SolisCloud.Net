// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Tests.TestSupport;

/// <summary>
/// Collects an async sequence (System.Linq.Async isn't referenced, and .NET Framework lacks the in-box version).
/// </summary>
internal static class AsyncEnumerableExtensions
{
    public static async Task<List<T>> CollectAsync<T>(this IAsyncEnumerable<T> source)
    {
        List<T> items = [];
        await foreach (T item in source.ConfigureAwait(false))
        {
            items.Add(item);
        }

        return items;
    }
}
