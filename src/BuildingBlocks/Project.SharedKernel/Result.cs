namespace Project.SharedKernel;

/// <summary>
/// Discriminated outcome of an operation, without exceptions for expected failures.
/// </summary>
public sealed record Error(string Code, string Description, ErrorType Type, IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static Error Validation(string code, string description, IReadOnlyDictionary<string, string[]>? details = null)
        => new(code, description, ErrorType.Validation, details);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);
    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);
}

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Failure,
}

/// <summary>
/// Outcome of an operation that does not return a value.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// Outcome of an operation that returns a value.
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, null) => Value = value;
    private Result(Error error) : base(false, error) { }

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}