using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Application.Abstractions;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Security;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _lifetime;
    private readonly TimeProvider _clock;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenGenerator(string issuer, string audience, string signingKey, TimeSpan lifetime, TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        ArgumentNullException.ThrowIfNull(signingKey);
        ArgumentNullException.ThrowIfNull(clock);

        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        if (keyBytes.Length < 32)
            throw new ArgumentException("The JWT signing key must be at least 32 UTF-8 bytes.", nameof(signingKey));
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime), "The token lifetime must be positive.");

        _issuer = issuer;
        _audience = audience;
        _lifetime = lifetime;
        _clock = clock;
        _signingCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
    }

    public AuthToken Generate(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = _clock.GetUtcNow();
        var expiresAt = now.Add(_lifetime);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };
        var token = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            _signingCredentials);

        return new AuthToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
