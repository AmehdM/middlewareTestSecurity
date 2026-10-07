using System.Net;

namespace MiddlewareDemo.Api.Middleware;

public sealed class AuthGateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _disabled;
    private readonly string? _apiKey;

    public AuthGateMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _disabled = configuration.GetValue<bool>("Security:DisableAuth");
        _apiKey = configuration["Security:ApiKey"];
    }

    public Task InvokeAsync(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress;
        if (_disabled && !string.IsNullOrEmpty(_apiKey) && remote is not null && IPAddress.IsLoopback(remote))
        {
            context.Request.Headers[ApiKeyMiddleware.HeaderName] = _apiKey;
        }

        return _next(context);
    }
}
