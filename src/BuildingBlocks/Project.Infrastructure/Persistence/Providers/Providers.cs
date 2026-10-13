using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Npgsql;
using Project.Application.Abstractions;

namespace Project.Infrastructure.Persistence.Providers;

/// <summary>Microsoft SQL Server 2019+ / Azure SQL.</summary>
public sealed class SqlServerProvider : IDatabaseProvider
{
    public string Name => "SqlServer";

    public string EfProviderName => "Microsoft.EntityFrameworkCore.SqlServer";

    public bool SupportsSchemas => true;

    public ISqlDialect Dialect { get; } = new SqlServerDialect();

    public void Configure(DbContextOptionsBuilder builder, string connectionString, ProviderSettings settings) =>
        builder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsAssembly(settings.MigrationsAssembly);
            sql.MigrationsHistoryTable("__EFMigrationsHistory", settings.Schema);
            sql.CommandTimeout(settings.CommandTimeoutSeconds);
            if (settings.MaxRetryCount > 0)
            {
                sql.EnableRetryOnFailure(settings.MaxRetryCount);
            }
        });

    public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
    }

    public Task<IAsyncDisposable> AcquireLockAsync(DbConnection connection, string name, CancellationToken ct) =>
        SqlLockHandle.AcquireAsync(
            connection,
            "EXEC sp_getapplock @Resource = @name, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = -1",
            "EXEC sp_releaseapplock @Resource = @name, @LockOwner = 'Session'",
            c => SqlLockHandle.AddParameter(c, "@name", name),
            ct);

    private sealed class SqlServerDialect : ISqlDialect
    {
        public string Name => "SqlServer";

        public string QuoteIdentifier(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";

        public string Table(string schema, string table) => $"{QuoteIdentifier(schema)}.{QuoteIdentifier(table)}";

        public string Boolean(bool value) => value ? "1" : "0";

        public (string CommandText, CommandType CommandType) Procedure(string schema, string name, IReadOnlyList<string> parameterNames) =>
            ($"{QuoteIdentifier(schema)}.{QuoteIdentifier(name)}", CommandType.StoredProcedure);
    }
}

/// <summary>PostgreSQL 15+. Stored routines are exposed as set-returning functions.</summary>
public sealed class PostgreSqlProvider : IDatabaseProvider
{
    public string Name => "PostgreSql";

    public string EfProviderName => "Npgsql.EntityFrameworkCore.PostgreSQL";

    public bool SupportsSchemas => true;

    public ISqlDialect Dialect { get; } = new PostgreSqlDialect();

    public void Configure(DbContextOptionsBuilder builder, string connectionString, ProviderSettings settings) =>
        builder.UseNpgsql(connectionString, sql =>
        {
            sql.MigrationsAssembly(settings.MigrationsAssembly);
            sql.MigrationsHistoryTable("__EFMigrationsHistory", settings.Schema);
            sql.CommandTimeout(settings.CommandTimeoutSeconds);
            if (settings.MaxRetryCount > 0)
            {
                sql.EnableRetryOnFailure(settings.MaxRetryCount);
            }
        });

    public DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

    public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
    }

    public Task<IAsyncDisposable> AcquireLockAsync(DbConnection connection, string name, CancellationToken ct) =>
        SqlLockHandle.AcquireAsync(
            connection,
            "SELECT pg_advisory_lock(hashtext(@name))",
            "SELECT pg_advisory_unlock(hashtext(@name))",
            c => SqlLockHandle.AddParameter(c, "name", name),
            ct);

    private sealed class PostgreSqlDialect : ISqlDialect
    {
        public string Name => "PostgreSql";

        public string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

        // Npgsql binds arrays natively, so Dapper does not expand the list: IN @p would become IN $1.
        public string InList(string column, string parameter) => $"{column} = ANY(@{parameter})";

        public string Table(string schema, string table) => $"{QuoteIdentifier(schema)}.{QuoteIdentifier(table)}";

        public string Boolean(bool value) => value ? "TRUE" : "FALSE";

        public (string CommandText, CommandType CommandType) Procedure(string schema, string name, IReadOnlyList<string> parameterNames) =>
            ($"SELECT * FROM {QuoteIdentifier(schema)}.{QuoteIdentifier(name)}({string.Join(", ", parameterNames.Select(p => "@" + p))})",
                CommandType.Text);
    }
}

/// <summary>
/// MySQL 8+ with the official Oracle provider (Pomelo has no stable EF Core 10 release). MySQL has no schemas,
/// so module tables are prefixed: <c>catalog_Products</c>.
/// </summary>
public sealed class MySqlProvider : IDatabaseProvider
{
    public string Name => "MySql";

    public string EfProviderName => "MySql.EntityFrameworkCore";

    public bool SupportsSchemas => false;

    public ISqlDialect Dialect { get; } = new MySqlDialect();

    public void Configure(DbContextOptionsBuilder builder, string connectionString, ProviderSettings settings) =>
        builder.UseMySQL(connectionString, sql =>
        {
            sql.MigrationsAssembly(settings.MigrationsAssembly);
            sql.MigrationsHistoryTable($"{settings.Schema}___EFMigrationsHistory");
            sql.CommandTimeout(settings.CommandTimeoutSeconds);
            if (settings.MaxRetryCount > 0)
            {
                sql.EnableRetryOnFailure(settings.MaxRetryCount);
            }
        });

    public DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);

    public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Microsecond precision, as in the other engines.
        configurationBuilder.Properties<DateTimeOffset>().HaveColumnType("datetime(6)");
        configurationBuilder.Properties<DateTimeOffset?>().HaveColumnType("datetime(6)");
    }

    public Task<IAsyncDisposable> AcquireLockAsync(DbConnection connection, string name, CancellationToken ct) =>
        SqlLockHandle.AcquireAsync(
            connection,
            "SELECT GET_LOCK(@name, -1)",
            "SELECT RELEASE_LOCK(@name)",
            c => SqlLockHandle.AddParameter(c, "@name", name),
            ct);

    public static string PrefixedTable(string schema, string table) => $"{schema}_{table}";

    private sealed class MySqlDialect : ISqlDialect
    {
        public string Name => "MySql";

        public string QuoteIdentifier(string identifier) => $"`{identifier.Replace("`", "``", StringComparison.Ordinal)}`";

        public string Table(string schema, string table) => QuoteIdentifier(PrefixedTable(schema, table));

        public string Boolean(bool value) => value ? "TRUE" : "FALSE";

        public (string CommandText, CommandType CommandType) Procedure(string schema, string name, IReadOnlyList<string> parameterNames) =>
            ($"CALL {QuoteIdentifier(PrefixedTable(schema, name))}({string.Join(", ", parameterNames.Select(p => "@" + p))})",
                CommandType.Text);
    }
}
