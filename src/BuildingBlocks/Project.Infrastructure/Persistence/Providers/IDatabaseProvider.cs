using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Project.Infrastructure.Persistence.Providers;

/// <summary>
/// Strategy for a concrete database engine. Adding a new engine means adding
/// one more implementation of this interface, no edits to existing code (OCP).
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>Stable identifier used in <c>Database:Provider</c>.</summary>
    string Name { get; }

    /// <summary>Configures EF Core for this engine (provider, retries, migrations assembly, history table).</summary>
    void Configure(DbContextOptionsBuilder builder, string connectionString, string migrationsAssembly, string schema);

    /// <summary>Creates a raw ADO.NET connection for Dapper / SP / bulk paths.</summary>
    DbConnection CreateConnection(string connectionString);

    /// <summary>Dialect for quoting, paging and CALL/EXEC syntax.</summary>
    ISqlDialect Dialect { get; }

    /// <summary>Applies naming conventions (snake_case on PostgreSQL) and engine-specific conventions.</summary>
    void ApplyConventions(ModelConfigurationBuilder configurationBuilder);
}

public interface ISqlDialect
{
    string QuoteIdentifier(string identifier);
    string ParameterPrefix { get; }
    string CallProcedureSyntax(string procedureName);
    string SkipLockedHint { get; }
}