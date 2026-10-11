using System.Data;

namespace Project.Application.Abstractions;

/// <summary>
/// Raw SQL access for high-performance reads, stored procedures and bulk operations.
/// Shares connection and transaction with the EF DbContext when one is active.
/// </summary>
public interface ISqlExecutor
{
    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? args = null, CancellationToken ct = default);
    Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? args = null, CancellationToken ct = default);
    Task<int> ExecuteAsync(string sql, object? args = null, CancellationToken ct = default);
    Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedure, object? args = null, CancellationToken ct = default);
    Task<int> ExecuteProcedureAsync(string procedure, object? args = null, CancellationToken ct = default);
}

public interface IDbConnectionFactory
{
    IDbConnection Create(string connectionName);
}