using System.Text.Json;

namespace MiddlewareDemo.Core.Logging;

public static class RequestLogHelper
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    public static string Describe(string operation, object request) =>
        $"{operation}: {JsonSerializer.Serialize(request, request.GetType(), Options)}";
}
