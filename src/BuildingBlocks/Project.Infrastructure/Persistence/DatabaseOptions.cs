using System.ComponentModel.DataAnnotations;

namespace Project.Infrastructure.Persistence;

/// <summary>
/// <c>Database</c> configuration section. Connection strings belong in user-secrets or environment variables
/// (<c>Database__ConnectionStrings__Default</c>), never in the repository.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Engine of the platform database: <c>SqlServer</c>, <c>PostgreSql</c> or <c>MySql</c>.</summary>
    [Required]
    public string Provider { get; set; } = "PostgreSql";

    /// <summary>
    /// <c>Default</c> is mandatory. <c>Platform</c> overrides it for platform modules and a module name
    /// (for example <c>Catalog</c>) overrides it for that module.
    /// </summary>
    public Dictionary<string, string> ConnectionStrings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [Range(1, 600)]
    public int CommandTimeoutSeconds { get; set; } = 30;

    [Range(0, 20)]
    public int MaxRetryCount { get; set; } = 5;

    public bool EnableSensitiveDataLogging { get; set; }

    /// <summary>Applies migrations and seeders when the API starts. Development only; rejected in Production.</summary>
    public bool MigrateOnStartup { get; set; }
}

/// <summary><c>Migrations</c> section used by the migration service.</summary>
public sealed class MigrationOptions
{
    public const string SectionName = "Migrations";

    [Range(1, 64)]
    public int MaxParallelTenants { get; set; } = 4;
}
