using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Security;

namespace TaskFlow.Infrastructure.Tests.Security;

public class JwtTokenGeneratorTests
{
    private const string Key = "test-only-signing-key-with-at-least-32-bytes";
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Generate_ReturnsSignedTokenWithConfiguredClaimsAndExpiry()
    {
        var user = new User(Guid.Parse("c5bd846e-dccb-4f07-8b5a-d601e3491594"), "Demo User", "demo@taskflow.dev", "hash", Now);
        var generator = new JwtTokenGenerator("taskflow-issuer", "taskflow-audience", Key, TimeSpan.FromMinutes(60), new FixedClock());

        var result = generator.Generate(user);

        result.ExpiresAtUtc.Should().Be(Now.AddHours(1));
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(result.Token, new TokenValidationParameters
        {
            ValidIssuer = "taskflow-issuer",
            ValidAudience = "taskflow-audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        }, out var validated);
        var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub);
        subject.Should().NotBeNull();
        subject!.Value.Should().Be(user.Id.ToString());
        validated.ValidTo.Should().Be(Now.AddHours(1).UtcDateTime);
        handler.Invoking(h => h.ValidateToken(result.Token, new TokenValidationParameters
        {
            ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("other-test-signing-key-at-least-32-bytes"))
        }, out _)).Should().Throw<SecurityTokenException>();
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
