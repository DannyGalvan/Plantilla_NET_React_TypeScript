namespace Project.SharedKernel;

/// <summary>Category of an expected failure. Maps 1:1 to an HTTP status code in the web layer.</summary>
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
/// Expected failure. <see cref="Code"/> is stable and machine readable (for example
/// <c>catalog.product.not_found</c>); <see cref="Details"/> carries per-field validation messages.
/// </summary>
public sealed record Error(
    string Code,
    string Description,
    ErrorType Type,
    IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static Error Validation(string code, string description, IReadOnlyDictionary<string, string[]>? details = null) =>
        new(code, description, ErrorType.Validation, details);

    public static Error Validation(string field, string message) =>
        new("validation", message, ErrorType.Validation, new Dictionary<string, string[]> { [field] = [message] });

    public static Error NotFound(string code, string description = "The resource was not found.") =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description = "You do not have access to this resource.") =>
        new(code, description, ErrorType.Forbidden);

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);
}

/// <summary>Outcome of an operation without a value.</summary>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess == (error is not null))
        {
            throw new ArgumentException("A successful result cannot carry an error and vice versa.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Outcome of an operation that returns a value.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value)
        : base(true, null) => _value = value;

    private Result(Error error)
        : base(false, error)
    {
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
