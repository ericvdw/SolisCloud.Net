// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

namespace SolisCloud.Internal;

/// <summary>
/// Keeps each operation within its documented requests-per-second limit using a sliding one-second window.
/// </summary>
/// <remarks>
/// <para>
/// Callers over the limit wait (first come, first served) rather than fail. Waiting uses the injected
/// <see cref="TimeProvider"/>, so tests can advance a fake clock instead of sleeping.
/// </para>
/// <para>
/// The window is slightly longer than one second (<see cref="Window"/>) because the server measures arrival time,
/// and network jitter could otherwise squeeze two requests sent just over a second apart into the same second.
/// </para>
/// </remarks>
internal sealed class EndpointRateLimiter : IDisposable
{
    internal static readonly TimeSpan Window = TimeSpan.FromMilliseconds(1100);

    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, EndpointState> _states = new(StringComparer.Ordinal);
#if NET9_0_OR_GREATER
    private readonly Lock _gate = new();
#else
    private readonly object _gate = new();
#endif
    private bool _disposed;

    public EndpointRateLimiter(TimeProvider timeProvider)
    {
        _timeProvider = Guard.NotNull(timeProvider);
    }

    /// <summary>
    /// Waits until a request to <paramref name="endpoint"/> may be sent, then records it as sent.
    /// </summary>
    public async Task WaitAsync(SolisEndpoint endpoint, CancellationToken cancellationToken)
    {
        EndpointState state = GetState(endpoint);
        await state.Turnstile.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            while (true)
            {
                DateTimeOffset now = _timeProvider.GetUtcNow();
                while (state.SentAt.Count > 0 && now - state.SentAt.Peek() >= Window)
                {
                    _ = state.SentAt.Dequeue();
                }

                if (state.SentAt.Count < endpoint.PermitsPerSecond)
                {
                    state.SentAt.Enqueue(now);
                    return;
                }

                TimeSpan wait = state.SentAt.Peek() + Window - now;
                await DelayAsync(wait, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _ = state.Turnstile.Release();
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (EndpointState state in _states.Values)
            {
                state.Dispose();
            }

            _states.Clear();
        }
    }

    private Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
#if NET
        return Task.Delay(delay, _timeProvider, cancellationToken);
#else
        return _timeProvider.Delay(delay, cancellationToken);
#endif
    }

    private EndpointState GetState(SolisEndpoint endpoint)
    {
        lock (_gate)
        {
            Guard.NotDisposed(_disposed, this);
            if (!_states.TryGetValue(endpoint.Path, out EndpointState? state))
            {
                state = new EndpointState();
                _states.Add(endpoint.Path, state);
            }

            return state;
        }
    }

    private sealed class EndpointState : IDisposable
    {
        // Only the caller holding the turnstile touches SentAt, so the queue needs no further locking.
        public SemaphoreSlim Turnstile { get; } = new(1, 1);

        public Queue<DateTimeOffset> SentAt { get; } = new();

        public void Dispose() => Turnstile.Dispose();
    }
}
