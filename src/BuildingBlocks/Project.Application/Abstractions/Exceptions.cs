namespace Project.Application.Abstractions;

/// <summary>Raised by infrastructure guards (organization scope, tenant). Mapped to 403 ProblemDetails.</summary>
public sealed class ForbiddenAccessException(string message) : Exception(message);
