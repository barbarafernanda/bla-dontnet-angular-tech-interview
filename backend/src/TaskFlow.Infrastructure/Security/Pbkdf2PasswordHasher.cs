using System.Globalization;
using System.Security.Cryptography;
using TaskFlow.Application.Abstractions;

namespace TaskFlow.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string FormatVersion = "pbkdf2-sha256$1";
    private const int Iterations = 210_000;
    private const int MinimumIterations = 100_000;
    private const int MaximumIterations = 2_000_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var derivedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        try
        {
            return string.Join('$', FormatVersion, Iterations.ToString(CultureInfo.InvariantCulture),
                Convert.ToBase64String(salt), Convert.ToBase64String(derivedKey));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
        }
    }

    public bool Verify(string password, string passwordHash)
    {
        if (password is null || passwordHash is null)
            return false;

        var parts = passwordHash.Split('$');
        if (parts.Length != 5 || parts[0] != "pbkdf2-sha256" || parts[1] != "1" ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations) ||
            iterations < MinimumIterations || iterations > MaximumIterations)
            return false;

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expectedHash = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length != SaltSize || expectedHash.Length != HashSize)
            return false;

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize);
        try
        {
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(actualHash);
            CryptographicOperations.ZeroMemory(expectedHash);
        }
    }
}
