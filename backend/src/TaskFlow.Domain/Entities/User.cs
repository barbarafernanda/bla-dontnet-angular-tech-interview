using System.Net.Mail;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Domain.Entities;

public sealed class User
{
    public User(Guid id, string fullName, string email, string passwordHash, DateTimeOffset createdAt)
    {
        ValidateProfile(fullName, email);

        if (string.IsNullOrWhiteSpace(passwordHash) || passwordHash.Length > 512)
            throw new DomainValidationException("Password hash is required and must not exceed 512 characters.");

        Id = id;
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public string FullName { get; }
    public string Email { get; }
    public string PasswordHash { get; }
    public DateTimeOffset CreatedAt { get; }

    public static void ValidateProfile(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150)
            throw new DomainValidationException("Full name is required and must not exceed 150 characters.");

        if (string.IsNullOrWhiteSpace(email) || email.Length > 255 ||
            !MailAddress.TryCreate(email, out var address) ||
            !string.Equals(address.Address, email, StringComparison.Ordinal) ||
            address.Host.Length == 0)
            throw new DomainValidationException("A valid email address of at most 255 characters is required.");
    }
}
