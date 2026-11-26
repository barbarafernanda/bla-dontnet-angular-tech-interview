namespace TaskFlow.Domain.Exceptions;

public sealed class InvalidTaskStatusException(string message) : DomainValidationException(message);
