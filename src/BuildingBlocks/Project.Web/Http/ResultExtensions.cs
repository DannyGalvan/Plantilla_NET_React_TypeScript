using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Project.SharedKernel;

namespace Project.Web.Http;

/// <summary>
/// Maps <see cref="Result"/> to HTTP. Success returns the value directly; failures are always RFC 9457
/// ProblemDetails with <c>code</c>, <c>traceId</c> and, for validation, <c>errors</c>.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttp<T>(this Result<T> result) => result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error!.ToProblem();

    public static IResult ToCreated<T>(this Result<T> result, Func<T, string> location) =>
        result.IsSuccess ? TypedResults.Created(location(result.Value), result.Value) : result.Error!.ToProblem();

    public static IResult ToHttp(this Result result) => result.IsSuccess ? TypedResults.NoContent() : result.Error!.ToProblem();

    public static IResult ToProblem(this Error error)
    {
        var status = StatusFor(error.Type);
        var problem = new ProblemDetails
        {
            Status = status,
            Title = TitleFor(error.Type),
            Detail = error.Description,
            Type = $"https://httpstatuses.io/{status}",
        };

        problem.Extensions["code"] = error.Code;
        if (error.Details is { Count: > 0 })
        {
            problem.Extensions["errors"] = error.Details;
        }

        return TypedResults.Problem(problem);
    }

    public static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        _ => "Internal server error",
    };
}
