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
    public const string Issuer = "MiddlewareDemo.Api";
    public const string Audience = "MiddlewareDemo.Clients";

    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private readonly SymmetricSecurityKey _key;
    private readonly JwtSecurityTokenHandler _handler = new();

    public TokenValidationParameters ValidationParameters { get; }

    public TokenService(IConfiguration configuration)
    {
        var secret = configuration[ConfigKey];
        if (string.IsNullOrEmpty(secret) || secret.Length < MinKeyLength)
        {
            throw new InvalidOperationException($"Configuration '{ConfigKey}' must have at least {MinKeyLength} characters.");
        }

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        ValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
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
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.Add(Lifetime),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256),
        };

        return _handler.WriteToken(_handler.CreateToken(descriptor));
    }
}
