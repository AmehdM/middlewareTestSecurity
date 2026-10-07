using System.Security.Claims;

namespace MiddlewareDemo.Api.Endpoints.Support;

public static class RequestUser
{
    public static int UserId(this HttpContext context)
    {
        var raw = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? context.User.FindFirst("sub")?.Value;
        return int.TryParse(raw, out var id) ? id : 0;
    }

    public static bool IsAdmin(this HttpContext context) =>
        context.User.IsInRole("admin") || context.User.IsInRole("Admin");

    public static string Actor(this HttpContext context) =>
        context.User.Identity?.Name ?? context.User.FindFirst("email")?.Value ?? "anonymous";
}

public static class Responses
{
    public static IResult Invalid(IEnumerable<string> errors) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = errors.ToArray() });
}
