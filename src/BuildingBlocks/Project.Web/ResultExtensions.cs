using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Project.SharedKernel;

namespace Project.Web;

/// <summary>
/// Maps <see cref="Result"/> and <see cref="Result{T}"/> to RFC 9457 ProblemDetails.
/// Never leaks the original exception message: only <c>code</c> + <c>traceId</c>.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttp<T>(this Result<T> result, HttpContext httpContext)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return result.Error!.ToHttpResult(httpContext);
    }

    public static IResult ToHttp(this Result result, HttpContext httpContext)
    {
        if (result.IsSuccess)
        {
            return Results.Ok();
        }

        return result.Error!.ToHttpResult(httpContext);
    }

    private static IResult ToHttpResult(this Error error, HttpContext httpContext)
    {
        var problem = new ProblemDetails
        {
            Status = StatusFor(error.Type),
            Title = TitleFor(error.Type),
            Type = $"https://httpstatuses.io/{StatusFor(error.Type)}",
            Detail = error.Description,
            Instance = httpContext.Request.Path,
        };

        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (error.Details is { Count: > 0 })
        {
            problem.Extensions["errors"] = error.Details;
        }

        return Results.Problem(problem);
    }

    private static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Failure => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError,
    };

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.Failure => "Internal server error",
        _ => "Error",
    };
}