namespace TaskFlow.Application.Common;

public enum ErrorCode
{
    Validation,
    Conflict,
    Unauthorized,
    NotFound
}

public sealed class Result<T>
{
    private readonly T? value;
    private readonly ErrorCode error;

    private Result(T value)
    {
        this.value = value;
        IsSuccess = true;
    }

    private Result(ErrorCode error, string message)
    {
        this.error = error;
        Message = message;
    }

    public bool IsSuccess { get; }
    public T Value => IsSuccess ? value! : throw new InvalidOperationException("A failed result has no value.");
    public ErrorCode Error => !IsSuccess ? error : throw new InvalidOperationException("A successful result has no error.");
    public string? Message { get; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(ErrorCode error, string message) => new(error, message);
}
