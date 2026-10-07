using System.Text.RegularExpressions;

namespace MiddlewareDemo.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private static readonly Regex ValidId = new(
        "^[A-Za-z0-9-]{8,64}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var id = IsValid(incoming) ? incoming : Guid.NewGuid().ToString("N");

        context.Items[HeaderName] = id;
        context.Response.Headers[HeaderName] = id;
        return _next(context);
    }

    private static bool IsValid(string value)
    {
        try
        {
            return ValidId.IsMatch(value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
