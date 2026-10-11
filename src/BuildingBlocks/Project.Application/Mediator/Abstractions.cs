using Project.SharedKernel;

namespace Project.Application.Mediator;

/// <summary>Base marker for requests handled via <c>ISender</c>.</summary>
public interface IRequest<TResponse>;

/// <summary>Command that returns Result (no value).</summary>
public interface ICommand : IRequest<Result>;

/// <summary>Command that returns Result&lt;T&gt;.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

/// <summary>Query that returns Result&lt;T&gt;.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;