using FluentAssertions;
using TaskFlow.Infrastructure.Security;

namespace TaskFlow.Infrastructure.Tests.Security;

public class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void Hash_UsesUniqueSaltsAndVerifiesBothHashes()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var first = hasher.Hash("Secret123!");
        var second = hasher.Hash("Secret123!");

        first.Should().NotBe("Secret123!").And.NotBe(second);
        hasher.Verify("Secret123!", first).Should().BeTrue();
        hasher.Verify("Secret123!", second).Should().BeTrue();
        hasher.Verify("wrong-password", first).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-valid-hash")]
    public void Verify_MalformedHash_DoesNotAuthenticate(string malformed)
    {
        new Pbkdf2PasswordHasher().Verify("Secret123!", malformed).Should().BeFalse();
    }
}
