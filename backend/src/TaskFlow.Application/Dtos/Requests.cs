namespace TaskFlow.Application.Dtos;

public sealed record RegisterUserRequest(string FullName, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record TaskWriteRequest(string Title, string? Description, string Status, DateTimeOffset DueDate);
