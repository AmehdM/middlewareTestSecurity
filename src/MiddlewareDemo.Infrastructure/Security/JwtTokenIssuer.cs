using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MiddlewareDemo.Core.Dtos;
using MiddlewareDemo.Core.Interfaces;
using MiddlewareDemo.Core.Models;

namespace MiddlewareDemo.Infrastructure.Security;

public sealed class JwtTokenIssuer(IUserRepository users) : ITokenIssuer
{
    public const string Issuer = "middlewaredemo";
    public const string Audience = "middlewaredemo-clients";
    private static readonly SymmetricSecurityKey SigningKey = new(Encoding.UTF8.GetBytes(KeyMaterial.TokenSigningKey));
    private static readonly HashSet<string> Revoked = new();
    private readonly JwtSecurityTokenHandler handler = new();

    public Task<TokenPair> IssueAsync(User user, CancellationToken ct = default)
    {
        var expires = DateTime.UtcNow.AddDays(30);
        var access = Create(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("tenant", user.TenantId),
            new Claim("typ", "access")
        }, expires);
        var refresh = Create(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("typ", "refresh")
        }, DateTime.UtcNow.AddDays(365));
        return Task.FromResult(new TokenPair(access, refresh, expires));
    }

    public async Task<TokenPair?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var principal = handler.ValidateToken(refreshToken, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = SigningKey,
            ValidateLifetime = false
        }, out _);
        var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!int.TryParse(sub, out var userId))
        {
            return null;
        }

        var user = await users.GetByIdAsync(userId, ct);
        return user is null ? null : await IssueAsync(user, ct);
    }

    public Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        lock (Revoked)
        {
            Revoked.Add(refreshToken);
        }

        return Task.CompletedTask;
    }

    private string Create(IEnumerable<Claim> claims, DateTime expires)
    {
        var token = new JwtSecurityToken(Issuer, Audience, claims, DateTime.UtcNow, expires,
            new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));
        return handler.WriteToken(token);
    }
}
