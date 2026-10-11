using Microsoft.AspNetCore.Diagnostics;

namespace Project.Web;

/// <summary>
/// Replaces the lambda-based exception handler with a strongly-typed handler.
/// Returns ProblemDetails with <c>traceId</c> only; the original message is
/// logged at error level with full context but never sent to the client.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        _logger.LogError(exception, "Unhandled exception (traceId={TraceId})", traceId);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://httpstatuses.io/500",
            Instance = httpContext.Request.Path,
        };

        problem.Extensions["code"] = "internal_error";
        problem.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}