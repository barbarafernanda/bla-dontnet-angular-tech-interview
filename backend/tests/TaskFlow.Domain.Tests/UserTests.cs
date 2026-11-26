using FluentAssertions;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Tests;

public class UserTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_WithValidValues_PreservesUserFields()
    {
        var id = Guid.Parse("c5bd846e-dccb-4f07-8b5a-d601e3491594");

        var user = new User(id, "Demo User", "demo@taskflow.dev", "encoded-password-hash", CreatedAt);

        user.Id.Should().Be(id);
        user.FullName.Should().Be("Demo User");
        user.Email.Should().Be("demo@taskflow.dev");
        user.PasswordHash.Should().Be("encoded-password-hash");
        user.CreatedAt.Should().Be(CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void Constructor_WithEmptyOrWhitespaceFullName_ThrowsDomainValidationException(string fullName)
    {
        var act = () => new User(Guid.NewGuid(), fullName, "demo@taskflow.dev", "encoded-password-hash", CreatedAt);

        act.Should().Throw<DomainValidationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    [InlineData("missing-at.example.com")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Constructor_WithMalformedEmail_ThrowsDomainValidationException(string email)
    {
        var act = () => new User(Guid.NewGuid(), "Demo User", email, "encoded-password-hash", CreatedAt);

        act.Should().Throw<DomainValidationException>();
    }
}
