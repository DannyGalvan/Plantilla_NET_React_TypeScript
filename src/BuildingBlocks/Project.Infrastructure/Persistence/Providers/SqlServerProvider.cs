using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Project.Infrastructure.Persistence.Providers;

/// <summary>
/// Microsoft SQL Server provider. Uses <c>sp_getapplock</c> for distributed
/// locks in <see cref="Project.Infrastructure.Tenancy.MigrationLockService"/>.
/// </summary>
public sealed class SqlServerProvider : IDatabaseProvider
{
    public string Name => "SqlServer";

    public ISqlDialect Dialect => SqlServerDialect.Instance;

    public void Configure(DbContextOptionsBuilder builder, string connectionString, string migrationsAssembly, string schema)
    {
        builder.UseSqlServer(connectionString, options =>
        {
            options.MigrationsAssembly(migrationsAssembly);
            options.MigrationsHistoryTable("__EFMigrationsHistory", schema);
            options.EnableRetryOnFailure(maxRetryCount: 5);
        });
    }

    public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    public void ApplyConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<string>().HaveColumnType("nvarchar(256)");
    }
}

internal sealed class SqlServerDialect : ISqlDialect
{
    public static readonly SqlServerDialect Instance = new();

    public string QuoteIdentifier(string identifier) => $"[{identifier}]";
    public string ParameterPrefix => "@";
    public string CallProcedureSyntax(string procedureName) => $"EXEC {procedureName}";
    public string SkipLockedHint => "WITH (ROWLOCK, UPDLOCK, READPAST)";
}