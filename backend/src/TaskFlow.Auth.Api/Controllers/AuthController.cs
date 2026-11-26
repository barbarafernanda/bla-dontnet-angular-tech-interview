using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Shared;
using TaskFlow.Application.Dtos;
using TaskFlow.Application.UseCases.Auth;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Auth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(RegisterUserUseCase register, LoginUseCase login, IUserRepository users, TimeProvider clock) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var result = await register.ExecuteAsync(request, cancellationToken);
        if (!result.IsSuccess) return ApiConfiguration.Error(this, result);
        return Created("/api/auth/me", Profile(result.Value));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await login.ExecuteAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ApiConfiguration.Error(this, result);
    }

    [HttpGet("public-status")]
    [AllowAnonymous]
    public IActionResult PublicStatus() => Ok(new { status = "online", utcNow = clock.GetUtcNow() });

    [HttpGet("me")]
    [Authorize(Policy = "ValidUser")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(ApiConfiguration.UserId(User), cancellationToken);
        return user is null ? Problem(statusCode: StatusCodes.Status404NotFound, detail: "User not found.") : Ok(Profile(user));
    }

    private static object Profile(User user) => new { user.Id, user.FullName, user.Email, user.CreatedAt };
}
