using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Project.Application.Abstractions;
using Project.Infrastructure.Persistence.Providers;

namespace Project.Infrastructure.Persistence;

/// <summary>
/// Dapper-based executor. Reuses the EF Core connection (and transaction)
/// when one is active so the two paths are atomic. Falls back to a pooled
/// connection otherwise.
/// </summary>
public sealed class DapperSqlExecutor<TContext> : ISqlExecutor
    where TContext : ModuleDbContext
{
    private readonly TContext _context;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly Providers.IDatabaseProvider _provider;

    public DapperSqlExecutor(
        TContext context,
        IDbConnectionFactory connectionFactory,
        Providers.IDatabaseProvider provider)
    {
        _context = context;
        _connectionFactory = connectionFactory;
        _provider = provider;
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? args = null, CancellationToken ct = default)
    {
        var (conn, tx, owns) = await GetConnection(ct);
        try
        {
            var rows = await conn.QueryAsync<T>(new CommandDefinition(sql, args, tx, cancellationToken: ct));
            return rows.AsList();
        }
        finally
        {
            if (owns) await conn.DisposeAsync();
        }
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? args = null, CancellationToken ct = default)
    {
        var (conn, tx, owns) = await GetConnection(ct);
        try
        {
            return await conn.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, args, tx, cancellationToken: ct));
        }
        finally
        {
            if (owns) await conn.DisposeAsync();
        }
    }

    public async Task<int> ExecuteAsync(string sql, object? args = null, CancellationToken ct = default)
    {
        var (conn, tx, owns) = await GetConnection(ct);
        try
        {
            return await conn.ExecuteAsync(new CommandDefinition(sql, args, tx, cancellationToken: ct));
        }
        finally
        {
            if (owns) await conn.DisposeAsync();
        }
    }

    public Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedure, object? args = null, CancellationToken ct = default) =>
        QueryAsync<T>(_provider.Dialect.CallProcedureSyntax(procedure), args, ct);

    public Task<int> ExecuteProcedureAsync(string procedure, object? args = null, CancellationToken ct = default) =>
        ExecuteAsync(_provider.Dialect.CallProcedureSyntax(procedure), args, ct);

    private async Task<(DbConnection conn, IDbTransaction? tx, bool owns)> GetConnection(CancellationToken ct)
    {
        var existing = _context.Database.GetDbConnection();
        if (existing.State != ConnectionState.Open)
        {
            await existing.OpenAsync(ct);
        }

        var currentTx = _context.Database.CurrentTransaction;
        if (currentTx is not null && ReferenceEquals(existing, _context.Database.GetDbConnection()))
        {
            return (existing, currentTx.GetDbTransaction(), false);
        }

        return (existing, null, false);
    }
}