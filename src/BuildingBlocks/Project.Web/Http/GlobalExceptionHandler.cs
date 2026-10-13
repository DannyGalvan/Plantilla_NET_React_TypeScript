using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Project.Application.Abstractions;
using Project.Infrastructure.Persistence;

namespace Project.Web.Http;

/// <summary>
/// Last line of defense. Known exceptions become their ProblemDetails; anything else is a 500 that only exposes
/// <c>traceId</c>. Exception messages and stack traces are logged, never returned.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, detail) = exception switch
        {
            ForbiddenAccessException or TenantMismatchException => (StatusCodes.Status403Forbidden, "auth.forbidden", "You do not have access to this resource."),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "concurrency.conflict", "The resource was modified by someone else."),
            UniqueConstraintException => (StatusCodes.Status409Conflict, "duplicate", "A resource with the same unique values already exists."),
            BadHttpRequestException bad => (bad.StatusCode, "request.invalid", "The request is not valid."),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested => (499, "request.cancelled", "The request was cancelled."),
            _ => (StatusCodes.Status500InternalServerError, "server.error", "An unexpected error occurred."),
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Request {Method} {Path} failed with {Code}: {Message}", httpContext.Request.Method, httpContext.Request.Path, code, exception.Message);
        }

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = code, Detail = detail };
        problem.Extensions["code"] = code;
        if (status >= 500 && (environment.IsDevelopment() || environment.IsEnvironment("Testing")))
        {
            // Only in Development and automated tests: never in Staging or Production.
            problem.Extensions["exception"] = $"{exception.GetType().Name}: {exception.Message}";
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
