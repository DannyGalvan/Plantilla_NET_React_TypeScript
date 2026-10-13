using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Project.Application.Abstractions;

namespace Project.Infrastructure.Persistence;

/// <summary>
/// Dapper executor bound to a module DbContext. It reuses the context connection and, when EF has an open
/// transaction, enlists in it, so EF writes and raw SQL commit or roll back together. The connection is the
/// tenant connection when the tenant has a dedicated database.
/// </summary>
public class DapperSqlExecutor<TContext>(TContext context) : ISqlExecutor
    where TContext : ModuleDbContext
{
    public ISqlDialect Dialect => context.Provider.Dialect;

    public Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? args, CancellationToken ct) =>
        RunAsync(async (connection, transaction) =>
            (IReadOnlyList<T>)(await connection.QueryAsync<T>(Command(sql, args, transaction, CommandType.Text, ct))).AsList());

    public Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? args, CancellationToken ct) =>
        RunAsync((connection, transaction) =>
            connection.QuerySingleOrDefaultAsync<T>(Command(sql, args, transaction, CommandType.Text, ct)));

    public Task<int> ExecuteAsync(string sql, object? args, CancellationToken ct) =>
        RunAsync((connection, transaction) => connection.ExecuteAsync(Command(sql, args, transaction, CommandType.Text, ct)));

    public Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedure, object? args, CancellationToken ct)
    {
        var (text, type) = Dialect.Procedure(context.Schema, procedure, ParameterNames(args));
        return RunAsync(async (connection, transaction) =>
            (IReadOnlyList<T>)(await connection.QueryAsync<T>(Command(text, args, transaction, type, ct))).AsList());
    }

    public Task<int> ExecuteProcedureAsync(string procedure, object? args, CancellationToken ct)
    {
        var (text, type) = Dialect.Procedure(context.Schema, procedure, ParameterNames(args));
        return RunAsync((connection, transaction) => connection.ExecuteAsync(Command(text, args, transaction, type, ct)));
    }

    private CommandDefinition Command(string sql, object? args, IDbTransaction? transaction, CommandType type, CancellationToken ct) =>
        new(sql, args, transaction, context.Database.GetCommandTimeout(), type, cancellationToken: ct);

    private async Task<TResult> RunAsync<TResult>(Func<DbConnection, IDbTransaction?, Task<TResult>> action)
    {
        var connection = context.Database.GetDbConnection();
        var opened = false;
        if (connection.State != ConnectionState.Open)
        {
            await context.Database.OpenConnectionAsync();
            opened = true;
        }

        try
        {
            return await action(connection, context.Database.CurrentTransaction?.GetDbTransaction());
        }
        finally
        {
            if (opened)
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }

    private static IReadOnlyList<string> ParameterNames(object? args) => args switch
    {
        null => [],
        DynamicParameters dynamic => dynamic.ParameterNames.ToList(),
        IDictionary<string, object?> dictionary => dictionary.Keys.ToList(),
        _ => args.GetType().GetProperties().Select(p => p.Name).ToList(),
    };
}

/// <summary>Executor exposed to the application layer of a module as <c>ISqlExecutor&lt;TModuleKey&gt;</c>.</summary>
public sealed class ModuleSqlExecutor<TModuleKey, TContext>(TContext context)
    : DapperSqlExecutor<TContext>(context), ISqlExecutor<TModuleKey>
    where TContext : ModuleDbContext;
