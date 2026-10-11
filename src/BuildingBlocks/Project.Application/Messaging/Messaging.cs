using Project.SharedKernel;

namespace Project.Application.Messaging;

/// <summary>Command without a return value. Handled by exactly one <c>ICommandHandler</c>.</summary>
public interface ICommand : Mediator.ICommand<Result>;

/// <summary>Command that returns a value.</summary>
public interface ICommand<TResponse> : Mediator.ICommand<Result<TResponse>>;

/// <summary>Query that returns a value. Queries never change state.</summary>
public interface IQuery<TResponse> : Mediator.IQuery<Result<TResponse>>;

/// <summary>Handler of a command without a return value.</summary>
public interface ICommandHandler<in TCommand> : Mediator.ICommandHandler<TCommand, Result>
    where TCommand : ICommand;

/// <summary>Handler of a command that returns a value.</summary>
public interface ICommandHandler<in TCommand, TResponse> : Mediator.ICommandHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

/// <summary>Handler of a query.</summary>
public interface IQueryHandler<in TQuery, TResponse> : Mediator.IQueryHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;

/// <summary>Request that requires a permission, checked by <c>AuthorizationBehavior</c>.</summary>
public interface IRequirePermission
{
    string Permission { get; }
}
