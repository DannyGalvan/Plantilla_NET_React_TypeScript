using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging;
using Project.Application.Security;
using Project.SharedKernel;

namespace Project.Application.Messaging;

/// <summary>Logs every request with its duration and outcome code.</summary>
public sealed class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var response = await next(message, cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        if (response is Result { IsFailure: true } failed)
        {
            logger.LogInformation(
                "Request {Request} failed with {ErrorCode} in {ElapsedMs:0.0} ms",
                typeof(TMessage).Name,
                failed.Error!.Code,
                elapsed);
        }
        else
        {
            logger.LogDebug("Request {Request} handled in {ElapsedMs:0.0} ms", typeof(TMessage).Name, elapsed);
        }

        return response;
    }
}

/// <summary>Runs every FluentValidation validator of the request and short-circuits with a validation error.</summary>
public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var error = await ValidationRunner.ValidateAsync(validators, message, cancellationToken);
        return error is null ? await next(message, cancellationToken) : ResultFactory.Failure<TResponse>(error);
    }
}

/// <summary>Checks <see cref="IRequirePermission"/> requests against the server-side permission service.</summary>
public sealed class AuthorizationBehavior<TMessage, TResponse>(IPermissionService permissions)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (message is IRequirePermission secured
            && !await permissions.HasPermissionAsync(secured.Permission, cancellationToken))
        {
            return ResultFactory.Failure<TResponse>(Error.Forbidden("auth.forbidden"));
        }

        return await next(message, cancellationToken);
    }
}

/// <summary>Runs validators and folds their failures into a single <see cref="Error"/>.</summary>
public static class ValidationRunner
{
    public static async Task<Error?> ValidateAsync<T>(IEnumerable<IValidator<T>> validators, T instance, CancellationToken ct)
    {
        Dictionary<string, List<string>>? errors = null;
        var context = new ValidationContext<T>(instance);

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, ct);
            foreach (var failure in result.Errors)
            {
                errors ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                var key = ToCamelCase(failure.PropertyName);
                if (!errors.TryGetValue(key, out var list))
                {
                    list = [];
                    errors[key] = list;
                }

                list.Add(failure.ErrorMessage);
            }
        }

        return errors is null
            ? null
            : Error.Validation(
                "validation",
                "One or more fields are invalid.",
                errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) || char.IsLower(name[0]) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}

/// <summary>Builds a failed <see cref="Result"/> or <see cref="Result{T}"/> for a generic response type.</summary>
public static class ResultFactory
{
    private static readonly ConcurrentDictionary<Type, Func<Error, object>> Factories = new();

    public static TResponse Failure<TResponse>(Error error)
    {
        var factory = Factories.GetOrAdd(typeof(TResponse), static type =>
        {
            if (type == typeof(Result))
            {
                return e => Result.Failure(e);
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var method = type.GetMethod(nameof(Result.Failure), BindingFlags.Public | BindingFlags.Static, [typeof(Error)])!;
                return e => method.Invoke(null, [e])!;
            }

            throw new InvalidOperationException(
                $"Requests must return Result or Result<T> to use pipeline failures; {type.Name} is not supported.");
        });

        return (TResponse)factory(error);
    }
}
