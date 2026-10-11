using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace Project.Infrastructure.Persistence.Providers;

/// <summary>
/// MySQL provider using the official Oracle package. Pomelo does not yet
/// publish a stable release for EF Core 10. See <c>docs/data-access.md</c>
/// for the review date.
/// </summary>
public sealed class MySqlProvider : IDatabaseProvider
{
    public string Name => "MySql";

    public ISqlDialect Dialect => MySqlDialect.Instance;

    public void Configure(DbContextOptionsBuilder builder, string connectionString, string migrationsAssembly, string schema)
    {
        builder.UseMySQL(connectionString, options =>
        {
            options.MigrationsAssembly(migrationsAssembly);
            options.MigrationsHistoryTable("__EFMigrationsHistory", schema);
            options.EnableRetryOnFailure(maxRetryCount: 5);
        });
    }

    public DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);

    public void ApplyConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().HaveColumnType("varchar(256)");
    }
}

internal sealed class MySqlDialect : ISqlDialect
{
    public static readonly MySqlDialect Instance = new();

    public string QuoteIdentifier(string identifier) => $"`{identifier}`";
    public string ParameterPrefix => "@";
    public string CallProcedureSyntax(string procedureName) => $"CALL {procedureName}";
    public string SkipLockedHint => "FOR UPDATE SKIP LOCKED";
}