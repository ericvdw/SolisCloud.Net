// Copyright (c) Eric van der Westhuizen. Licensed under the MIT License.

using Microsoft.Extensions.Time.Testing;
using SolisCloud.Internal;
using SolisCloud.Tests.TestSupport;

namespace SolisCloud.Tests;

public sealed class EndpointRateLimiterTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Requests_within_the_limit_proceed_immediately()
    {
        FakeTimeProvider time = new(Start);
        using EndpointRateLimiter limiter = new(time);
        SolisEndpoint endpoint = new("/v1/api/test", permitsPerSecond: 2);

        Task first = limiter.WaitAsync(endpoint, TestContext.Current.CancellationToken);
        Task second = limiter.WaitAsync(endpoint, TestContext.Current.CancellationToken);

        Assert.True(first.IsCompleted);
        Assert.True(second.IsCompleted);
        await Task.WhenAll(first, second);
    }

    [Fact]
    public async Task A_request_over_the_limit_waits_for_the_window_to_pass()
    {
        FakeTimeProvider time = new(Start);
        using EndpointRateLimiter limiter = new(time);
        SolisEndpoint endpoint = new("/v1/api/test", permitsPerSecond: 2);
        await limiter.WaitAsync(endpoint, TestContext.Current.CancellationToken);
        await limiter.WaitAsync(endpoint, TestContext.Current.CancellationToken);

        Task third = limiter.WaitAsync(endpoint, TestContext.Current.CancellationToken);
        Assert.False(third.IsCompleted);

        time.Advance(EndpointRateLimiter.Window - TimeSpan.FromMilliseconds(1));
        Assert.False(third.IsCompleted);

        time.Advance(TimeSpan.FromMilliseconds(1));
        await third.WithTimeout(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Each_endpoint_has_its_own_limit()
    {
        FakeTimeProvider time = new(Start);
        using EndpointRateLimiter limiter = new(time);
        SolisEndpoint busy = new("/v1/api/busy", permitsPerSecond: 1);
        SolisEndpoint idle = new("/v1/api/idle", permitsPerSecond: 1);
        await limiter.WaitAsync(busy, TestContext.Current.CancellationToken);

        Task blocked = limiter.WaitAsync(busy, TestContext.Current.CancellationToken);
        Task other = limiter.WaitAsync(idle, TestContext.Current.CancellationToken);

        Assert.False(blocked.IsCompleted);
        Assert.True(other.IsCompleted);
        time.Advance(EndpointRateLimiter.Window);
        await blocked.WithTimeout(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task A_waiting_request_can_be_cancelled()
    {
        FakeTimeProvider time = new(Start);
        using EndpointRateLimiter limiter = new(time);
        SolisEndpoint endpoint = new("/v1/api/test", permitsPerSecond: 1);
        await limiter.WaitAsync(endpoint, TestContext.Current.CancellationToken);
        using CancellationTokenSource cts = new();

        Task waiting = limiter.WaitAsync(endpoint, cts.Token);
        await cts.CancelSoonAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);

        // The cancelled caller must release its place: the next caller proceeds once the window passes.
        time.Advance(EndpointRateLimiter.Window);
        await limiter.WaitAsync(endpoint, TestContext.Current.CancellationToken)
            .WithTimeout(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task WaitAsync_throws_after_dispose()
    {
        EndpointRateLimiter limiter = new(new FakeTimeProvider(Start));
        limiter.Dispose();

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => limiter.WaitAsync(SolisEndpoints.StationList, TestContext.Current.CancellationToken));
    }
}
