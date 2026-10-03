using System.Text;

namespace MiddlewareDemo.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        _logger.LogInformation("{Method} {Path}{Query}", request.Method, request.Path, request.QueryString);

        foreach (var header in request.Headers)
        {
            _logger.LogInformation("Header {Name}: {Value}", header.Key, header.Value.ToString());
        }

        request.EnableBuffering();
        using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
        {
            var body = await reader.ReadToEndAsync();
            if (body.Length > 0)
            {
                _logger.LogInformation("Body {Body}", body);
            }
        }
        request.Body.Position = 0;

        await _next(context);

        _logger.LogInformation("Response {StatusCode}", context.Response.StatusCode);
    }
}
