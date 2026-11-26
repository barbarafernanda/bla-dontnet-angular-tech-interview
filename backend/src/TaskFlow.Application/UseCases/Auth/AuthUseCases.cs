using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Dtos;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Application.UseCases.Auth;

public sealed class RegisterUserUseCase(IUserRepository users, IPasswordHasher hasher, TimeProvider clock)
{
    public async Task<Result<User>> ExecuteAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            User.ValidateProfile(request.FullName, request.Email);
        }
        catch (DomainValidationException exception)
        {
            return Result<User>.Failure(ErrorCode.Validation, exception.Message);
        }

        if (string.IsNullOrWhiteSpace(request.Password))
            return Result<User>.Failure(ErrorCode.Validation, "Password is required.");

        if (await users.GetByEmailAsync(request.Email, cancellationToken) is not null)
            return Result<User>.Failure(ErrorCode.Conflict, "An account with this email already exists.");

        var user = new User(Guid.NewGuid(), request.FullName, request.Email, hasher.Hash(request.Password), clock.GetUtcNow());
        try
        {
            await users.AddAsync(user, cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            return Result<User>.Failure(ErrorCode.Conflict, "An account with this email already exists.");
        }

        return Result<User>.Success(user);
    }
}

public sealed class LoginUseCase(IUserRepository users, IPasswordHasher hasher, IJwtTokenGenerator tokens)
{
    public async Task<Result<AuthToken>> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Result<AuthToken>.Failure(ErrorCode.Validation, "Email and password are required.");

        var user = await users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthToken>.Failure(ErrorCode.Unauthorized, "Invalid email or password.");

        return Result<AuthToken>.Success(tokens.Generate(user));
    }
}
