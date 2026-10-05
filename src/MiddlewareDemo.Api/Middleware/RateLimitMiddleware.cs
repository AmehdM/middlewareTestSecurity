using System.Collections.Concurrent;

namespace MiddlewareDemo.Api.Middleware;

/// Middleware de rendimiento, no de seguridad: ante un fallo interno deja pasar la peticion (fail-open). Decision intencional y documentada: la disponibilidad prima sobre el limite de uso.
public sealed class RateLimitMiddleware
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private static readonly (string Prefix, int Limit)[] Rules =
    [
        ("/api/auth", 10),
        ("/api/items", 100),
    ];

    private static readonly ConcurrentDictionary<string, Counter> Counters = new();
    private static long _lastPurgeTicks = DateTime.UtcNow.Ticks;

    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var limited = false;

        try
        {
            foreach (var (prefix, limit) in Rules)
            {
                if (!context.Request.Path.StartsWithSegments(prefix))
                {
                    continue;
                }

                var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var counter = Counters.GetOrAdd($"{prefix}|{client}", _ => new Counter());
                limited = counter.Hit(Window) > limit;
                break;
            }

            PurgeStale();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rate limit check failed");
        }

        if (limited)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new { error = "rate_limited" });
            return;
        }

        await _next(context);
    }

    private static void PurgeStale()
    {
        var now = DateTime.UtcNow;
        var last = new DateTime(Interlocked.Read(ref _lastPurgeTicks), DateTimeKind.Utc);
        if (now - last < Window)
        {
            return;
        }

        Interlocked.Exchange(ref _lastPurgeTicks, now.Ticks);
        foreach (var pair in Counters)
        {
            if (pair.Value.IsStale(now, Window))
            {
                Counters.TryRemove(pair.Key, out _);
            }
        }
    }

    private sealed class Counter
    {
        private readonly object _lock = new();
        private DateTime _windowStart = DateTime.UtcNow;
        private int _count;

        public int Hit(TimeSpan window)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if (now - _windowStart > window)
                {
                    _windowStart = now;
                    _count = 0;
                }

                return ++_count;
            }
        }

        public bool IsStale(DateTime now, TimeSpan window)
        {
            lock (_lock)
            {
                return now - _windowStart > window;
            }
        }
    }
}
