using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MiddlewareDemo.Api.Models;

namespace MiddlewareDemo.Api.Services;

public sealed class TokenService
{
    public const string ConfigKey = "Security:JwtKey";
    public const int MinKeyLength = 32;

    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private readonly SymmetricSecurityKey _key;
    private readonly JwtSecurityTokenHandler _handler = new();

    public TokenService(IConfiguration configuration)
    {
        var secret = configuration[ConfigKey];
        if (string.IsNullOrEmpty(secret) || secret.Length < MinKeyLength)
        {
            throw new InvalidOperationException($"Configuration '{ConfigKey}' must have at least {MinKeyLength} characters.");
        }

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    public string Create(User user)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
            ]),
            Expires = DateTime.UtcNow.Add(Lifetime),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256),
        };

        return _handler.WriteToken(_handler.CreateToken(descriptor));
    }

    public ClaimsPrincipal? Validate(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        try
        {
            return _handler.ValidateToken(token, parameters, out _);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }
}
