// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Tests.TestSupport;

/// <summary>
/// Task helpers that work on .NET Framework as well as modern .NET.
/// </summary>
internal static class TaskExtensions
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Awaits <paramref name="task"/>, failing the test if it has not finished within a few seconds of real time.
    /// </summary>
    public static async Task WithTimeout(this Task task, CancellationToken cancellationToken)
    {
#if NET
        await task.WaitAsync(DefaultTimeout, cancellationToken).ConfigureAwait(false);
#else
        Task finished = await Task.WhenAny(task, Task.Delay(DefaultTimeout, cancellationToken)).ConfigureAwait(false);
        if (finished != task)
        {
            throw new TimeoutException("The task did not complete in time.");
        }

        await task.ConfigureAwait(false);
#endif
    }

    /// <summary>
    /// Cancels the source, using the asynchronous API where it exists.
    /// </summary>
    public static Task CancelSoonAsync(this CancellationTokenSource source)
    {
#if NET
        return source.CancelAsync();
#else
        source.Cancel();
        return Task.CompletedTask;
#endif
    }
}
