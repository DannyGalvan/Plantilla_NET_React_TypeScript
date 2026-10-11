using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Project.Infrastructure.Persistence.Providers;

/// <summary>
/// PostgreSQL provider. Uses <c>pg_advisory_lock</c> for distributed locks and
/// snake_case naming by convention.
/// </summary>
public sealed class PostgreSqlProvider : IDatabaseProvider
{
    public string Name => "PostgreSql";

    public ISqlDialect Dialect => PostgreSqlDialect.Instance;

    public void Configure(DbContextOptionsBuilder builder, string connectionString, string migrationsAssembly, string schema)
    {
        builder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly(migrationsAssembly);
            options.MigrationsHistoryTable("__EFMigrationsHistory", schema);
            options.EnableRetryOnFailure(maxRetryCount: 5);
        });
    }

    public DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

    public void ApplyConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().HaveColumnType("text");
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp with time zone");
        configurationBuilder.Properties<DateTimeOffset>().HaveColumnType("timestamptz");
    }
}

internal sealed class PostgreSqlDialect : ISqlDialect
{
    public static readonly PostgreSqlDialect Instance = new();

    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";
    public string ParameterPrefix => "@";
    public string CallProcedureSyntax(string procedureName) => $"CALL {procedureName}";
    public string SkipLockedHint => "FOR UPDATE SKIP LOCKED";
}