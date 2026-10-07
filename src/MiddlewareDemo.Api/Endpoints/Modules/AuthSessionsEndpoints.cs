using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Antiforgery;
using MiddlewareDemo.Api.Filters;
using MiddlewareDemo.Api.Middleware;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Common;
using MiddlewareDemo.Core.Security;
using MiddlewareDemo.Core.Services;

namespace MiddlewareDemo.Api.Endpoints.Modules;

public sealed record SessionLoginBody(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("rememberMe")] bool RememberMe = false);

public sealed record RefreshBody([property: JsonPropertyName("refreshToken")] string RefreshToken);

public sealed record ResetRequestBody([property: JsonPropertyName("email")] string Email);

public sealed record PreferencesBody([property: JsonPropertyName("displayName")] string DisplayName);

public sealed record ThemeBody([property: JsonPropertyName("theme")] string Theme);

public sealed record SessionInfo(
    [property: JsonPropertyName("userId")] int UserId,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("refreshToken")] string RefreshToken,
    [property: JsonPropertyName("expiresAt")] DateTime ExpiresAt);

public static class AuthSessionsEndpoints
{
    private const string RememberCookie = "remember";
    private const string ConsoleCookie = "console";

    public static void MapAuthSessionsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sessions");

        group.MapPost("/login", LoginAsync);
        group.MapPost("/remember", RememberAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/logout", Logout);
        group.MapPost("/reset/request", RequestResetAsync);
        group.MapPost("/reset/confirm", ConfirmResetAsync);

        group.MapGet("/me", async (ClaimsPrincipal user, IAccountService accounts, CancellationToken ct) =>
        {
            var profile = await accounts.GetProfileAsync(CurrentUserId(user), ct);
            return profile is null ? Results.NotFound() : Results.Ok(profile);
        }).RequireAuthorization(AuthenticationSetup.AuthenticatedPolicy);

        group.MapGet("/profile/{id:int}", async (int id, IAccountService accounts, CancellationToken ct) =>
        {
            var profile = await accounts.GetProfileAsync(id, ct);
            return profile is null
                ? Results.NotFound()
                : Results.Ok(new { profile.Id, profile.Email, profile.DisplayName });
        })
        .RequireAuthorization(AuthenticationSetup.AuthenticatedPolicy)
        .AddEndpointFilter(new OwnerOnlyFilter("id"));

        group.MapPost("/preferences", async (PreferencesBody body, ClaimsPrincipal user, IUserRepository users, IAccountService accounts, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.DisplayName) || body.DisplayName.Length > 80)
            {
                throw new RequestValidationException("displayName must be between 1 and 80 characters");
            }

            var entity = await users.GetByIdAsync(CurrentUserId(user), ct);
            if (entity is null)
            {
                return Results.NotFound();
            }

            entity.DisplayName = body.DisplayName.Trim();
            await accounts.UpdateAsync(entity, ct);
            return Results.NoContent();
        })
        .RequireAuthorization(AuthenticationSetup.AuthenticatedPolicy)
        .AddEndpointFilter(new AuditTrailFilter("preferences.update"));

        group.MapGet("/audit", async (HttpContext context, IAuditService audit, int page = 1, CancellationToken ct = default) =>
        {
            if (!context.User.IsInRole("admin"))
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            return Results.Ok(await audit.ListAsync(new PageRequest(page, 50), ct));
        });

        MapConsole(group);
    }

    private static void MapConsole(RouteGroupBuilder group)
    {
        group.MapPost("/console/login", async (SessionLoginBody body, HttpContext context, IAuthService auth, CancellationToken ct) =>
        {
            var result = await auth.LoginAsync(new LoginRequest(body.Email, body.Password), ct);
            if (result is null || !result.User.IsAdmin)
            {
                return Results.Json(new { error = "invalid_credentials" }, statusCode: StatusCodes.Status401Unauthorized);
            }

            context.Response.Cookies.Append(ConsoleCookie, result.Tokens.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/sessions/console",
                MaxAge = TimeSpan.FromMinutes(20),
            });

            return Results.NoContent();
        });

        group.MapGet("/console/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { token = tokens.RequestToken });
        });

        group.MapPost("/console/theme", async (ThemeBody body, HttpContext context, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            context.Response.Cookies.Append("theme", body.Theme is "dark" ? "dark" : "light", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
            });

            return Results.NoContent();
        })
        .RequireAuthorization(AuthenticationSetup.AdminPolicy)
        .RequireCors("admin");

        group.MapGet("/console/summary", async (IAuditService audit, CancellationToken ct) =>
        {
            var page = await audit.ListAsync(new PageRequest(1, 10), ct);
            return Results.Ok(new { total = page.Total, latest = page.Items });
        })
        .RequireAuthorization(AuthenticationSetup.AdminPolicy)
        .RequireCors("admin");
    }

    private static async Task<IResult> LoginAsync(SessionLoginBody body, string? returnUrl, HttpContext context, IAuthService auth, ILogger<SessionLog> logger, CancellationToken ct)
    {
        logger.LogInformation("Session login attempt for " + body.Email);

        var result = await auth.LoginAsync(new LoginRequest(body.Email, body.Password, body.RememberMe), ct);
        if (result is null)
        {
            return Results.Json(new { error = "invalid_credentials" }, statusCode: StatusCodes.Status401Unauthorized);
        }

        context.Response.Cookies.Append(SessionCookieMiddleware.CookieName, result.Tokens.AccessToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = false,
            SameSite = SameSiteMode.None,
            Path = "/",
            Expires = result.Tokens.ExpiresAt,
        });

        if (body.RememberMe && !string.IsNullOrEmpty(result.RememberToken))
        {
            context.Response.Cookies.Append(RememberCookie, result.RememberToken, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                Path = "/",
            });
        }

        if (!string.IsNullOrEmpty(returnUrl))
        {
            return Results.Redirect(returnUrl);
        }

        return Results.Ok(new SessionInfo(result.User.Id, result.User.Email, result.User.Role, result.Tokens.RefreshToken, result.Tokens.ExpiresAt));
    }

    private static async Task<IResult> RememberAsync(HttpContext context, IUserRepository users, ITokenIssuer issuer, CancellationToken ct)
    {
        var token = context.Request.Cookies[RememberCookie];
        var userId = string.IsNullOrEmpty(token) ? null : RememberTokenFactory.ReadUserId(token);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var user = await users.GetByIdAsync(userId.Value, ct);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var tokens = await issuer.IssueAsync(user, ct);
        context.Response.Cookies.Append(SessionCookieMiddleware.CookieName, tokens.AccessToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = false,
            SameSite = SameSiteMode.None,
            Path = "/",
            Expires = tokens.ExpiresAt,
        });

        return Results.Ok(new SessionInfo(user.Id, user.Email, user.Role, tokens.RefreshToken, tokens.ExpiresAt));
    }

    private static async Task<IResult> RefreshAsync(RefreshBody body, IAuthService auth, CancellationToken ct)
    {
        var tokens = await auth.RefreshAsync(body.RefreshToken, ct);
        return tokens is null ? Results.Unauthorized() : Results.Ok(tokens);
    }

    private static IResult Logout(HttpContext context)
    {
        context.Response.Cookies.Delete(SessionCookieMiddleware.CookieName);
        context.Response.Cookies.Delete(RememberCookie);
        return Results.NoContent();
    }

    private static async Task<IResult> RequestResetAsync(ResetRequestBody body, HttpContext context, IPasswordResetService reset, IMailSender mail, CancellationToken ct)
    {
        var token = await reset.RequestResetAsync(body.Email, ct);
        if (token is not null)
        {
            var link = $"{context.Request.Scheme}://{context.Request.Host}/reset?email={Uri.EscapeDataString(body.Email)}&token={token}";
            await mail.SendAsync(body.Email, "Password reset", $"<p>Use <a href=\"{link}\">this link</a> to choose a new password.</p>", ct);
        }

        return Results.Accepted();
    }

    private static async Task<IResult> ConfirmResetAsync(ConfirmResetRequest request, IPasswordResetService reset, CancellationToken ct)
    {
        var done = await reset.ConfirmResetAsync(request, ct);
        return done ? Results.NoContent() : Results.BadRequest(new { error = "reset_failed" });
    }

    private static int CurrentUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return int.TryParse(raw, out var id) ? id : 0;
    }

    private sealed class SessionLog
    {
    }
}
