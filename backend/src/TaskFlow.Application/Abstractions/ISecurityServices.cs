using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public sealed record AuthToken(string Token, DateTimeOffset ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    AuthToken Generate(User user);
}
