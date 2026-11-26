namespace TaskFlow.Domain.Exceptions;

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string message, Exception? innerException = null) : base(message, innerException) { }
}
