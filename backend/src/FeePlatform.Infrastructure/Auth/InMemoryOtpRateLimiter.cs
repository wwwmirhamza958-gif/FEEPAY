using System.Collections.Concurrent;

namespace FeePlatform.Infrastructure.Auth;

/// <summary>
/// Dev/test OTP rate limiter — in-process only (no Redis).
/// Staging should swap to Redis-backed limits when infra exists.
/// </summary>
public interface IOtpRateLimiter
{
    bool TryAcquire(string key, out TimeSpan? retryAfter);
}

public sealed class InMemoryOtpRateLimiter : IOtpRateLimiter
{
    private readonly ConcurrentDictionary<string, Window> _windows = new();
    private readonly int _maxAttempts;
    private readonly TimeSpan _window;

    public InMemoryOtpRateLimiter(int maxAttempts = 5, TimeSpan? window = null)
    {
        _maxAttempts = maxAttempts;
        _window = window ?? TimeSpan.FromMinutes(10);
    }

    public bool TryAcquire(string key, out TimeSpan? retryAfter)
    {
        var now = DateTimeOffset.UtcNow;
        var window = _windows.AddOrUpdate(
            key,
            _ => new Window(now, 1),
            (_, existing) =>
            {
                if (now - existing.StartedAtUtc >= _window)
                    return new Window(now, 1);
                return existing with { Count = existing.Count + 1 };
            });

        if (window.Count > _maxAttempts)
        {
            retryAfter = _window - (now - window.StartedAtUtc);
            if (retryAfter < TimeSpan.Zero) retryAfter = TimeSpan.Zero;
            return false;
        }

        retryAfter = null;
        return true;
    }

    private sealed record Window(DateTimeOffset StartedAtUtc, int Count);
}
