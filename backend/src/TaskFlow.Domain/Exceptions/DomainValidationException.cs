namespace TaskFlow.Domain.Exceptions;

public class DomainValidationException(string message) : Exception(message);
