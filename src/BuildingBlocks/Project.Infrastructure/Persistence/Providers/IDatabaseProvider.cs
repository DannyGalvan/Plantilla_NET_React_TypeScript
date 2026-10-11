using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions;

namespace Project.Infrastructure.Persistence.Providers;

/// <summary>
/// Strategy for one database engine. Adding an engine means adding one implementation and registering it;
/// nothing else changes (open/closed).
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>Name used in <c>Database:Provider</c> and in the migrations assembly (<c>Project.Migrations.{Name}</c>).</summary>
    string Name { get; }

    /// <summary>Value of <c>DbContext.Database.ProviderName</c> for this engine.</summary>
    string EfProviderName { get; }

    /// <summary>Engines without schemas (MySQL) prefix the table names with the module name instead.</summary>
    bool SupportsSchemas { get; }

    ISqlDialect Dialect { get; }

    void Configure(DbContextOptionsBuilder builder, string connectionString, ProviderSettings settings);

    DbConnection CreateConnection(string connectionString);

    /// <summary>Engine-specific type conventions applied to every module model.</summary>
    void ConfigureConventions(ModelConfigurationBuilder configurationBuilder);

    /// <summary>Distributed lock used by the migration service so only one instance migrates at a time.</summary>
    Task<IAsyncDisposable> AcquireLockAsync(DbConnection connection, string name, CancellationToken ct);
}

/// <summary>Per-context settings passed to <see cref="IDatabaseProvider.Configure"/>.</summary>
public sealed record ProviderSettings(
    string Schema,
    string MigrationsAssembly,
    int CommandTimeoutSeconds,
    int MaxRetryCount);

/// <summary>Resolves registered providers by name or by EF provider name.</summary>
public sealed class DatabaseProviderRegistry(IEnumerable<IDatabaseProvider> providers)
{
    private readonly IDatabaseProvider[] _providers = providers.ToArray();

    public IReadOnlyCollection<IDatabaseProvider> All => _providers;

    public IDatabaseProvider Get(string name) =>
        _providers.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException(
            $"Database provider '{name}' is not registered. Available: {string.Join(", ", _providers.Select(p => p.Name))}.");

    public IDatabaseProvider GetByEfProviderName(string? efProviderName) =>
        _providers.FirstOrDefault(p => p.EfProviderName == efProviderName)
        ?? throw new InvalidOperationException($"No database provider handles EF provider '{efProviderName}'.");
}

/// <summary>Releases a lock by running a SQL statement on the same connection.</summary>
internal sealed class SqlLockHandle(DbConnection connection, string releaseSql, Action<DbCommand> bind) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await using var command = connection.CreateCommand();
        command.CommandText = releaseSql;
        bind(command);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<IAsyncDisposable> AcquireAsync(
        DbConnection connection,
        string acquireSql,
        string releaseSql,
        Action<DbCommand> bind,
        CancellationToken ct)
    {
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = acquireSql;
        command.CommandTimeout = 0;
        bind(command);
        await command.ExecuteNonQueryAsync(ct);
        return new SqlLockHandle(connection, releaseSql, bind);
    }

    public static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
