using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MiddlewareDemo.Api.Middleware;

public static class AuthenticationSetup
{
    public const string AdminScheme = "AdminBearer";
    public const string AdminPolicy = "AdminOnly";
    public const string AuthenticatedPolicy = "Authenticated";

    public static IServiceCollection AddSessionAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var signingKey = configuration["Jwt:SigningKey"] ?? "";
        var issuer = configuration["Jwt:Issuer"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                };
            })
            .AddJwtBearer(AdminScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = "middlewaredemo-admin",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    IssuerSigningKeyResolver = (_, _, _, _) => AdminKeyProvider.Resolve(),
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthenticatedPolicy, policy => policy.RequireAuthenticatedUser());
            options.AddPolicy(AdminPolicy, policy => policy
                .AddAuthenticationSchemes(AdminScheme)
                .RequireAuthenticatedUser()
                .RequireClaim("scope", "admin"));
        });

        return services;
    }
}

public static class AdminKeyProvider
{
    public static IEnumerable<SecurityKey> Resolve()
    {
        var raw = Environment.GetEnvironmentVariable("ADMIN_JWT_KEY");
        if (string.IsNullOrWhiteSpace(raw) || raw.Length < 32)
        {
            return Array.Empty<SecurityKey>();
        }

        return new[] { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(raw)) };
    }
}
