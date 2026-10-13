using System.Security.Cryptography;
using System.Text.Json;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Project.Application;
using Project.Application.Configuration;
using Project.Application.Tenancy;
using Project.Infrastructure.Configuration;
using Project.Infrastructure.Persistence;
using Project.Modules.Configuration.Application;
using Project.Modules.Configuration.Contracts;
using Project.Modules.Configuration.Domain;
using Project.Web.Http;
using Project.Web.Modules;
using Project.Web.Security;

namespace Project.Modules.Configuration.Infrastructure;

public sealed class ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options, ModuleDbContextDependencies dependencies)
    : ModuleDbContext(options, dependencies)
{
    public override string Schema => ConfigurationTable.Schema;

    public DbSet<ConfigurationEntry> Entries => Set<ConfigurationEntry>();
}

internal sealed class ConfigurationEntryConfiguration : IEntityTypeConfiguration<ConfigurationEntry>
{
    public void Configure(EntityTypeBuilder<ConfigurationEntry> builder)
    {
        // Column names are read without EF by DatabaseConfigurationProvider: keep them in sync.
        builder.ToTable(ConfigurationTable.Table);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Key).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Value);
        builder.Property(e => e.DisplayValue).HasMaxLength(500);
        builder.Property(e => e.CreatedBy).HasMaxLength(64);
        builder.Property(e => e.UpdatedBy).HasMaxLength(64);
        builder.HasIndex(e => e.Key).IsUnique();
    }
}

/// <summary>
/// First start: generates the JWT signing key and stores it encrypted, so no secret has to be created by hand.
/// Runs in the migration service and with <c>MigrateOnStartup</c>.
/// </summary>
public sealed class JwtSigningKeySeeder(IConfigurationInspector configuration, ISecretCipher cipher, ILogger<JwtSigningKeySeeder> logger) : IDataSeeder
{
    public const string Key = "Jwt:SigningKey";

    public int Order => -100;

    public SeederScope Scope => SeederScope.Platform;

    public async Task SeedAsync(IServiceProvider services, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(configuration.Get(Key)))
        {
            return;
        }

        if (!cipher.IsAvailable)
        {
            logger.LogWarning("Jwt:SigningKey is not configured and Configuration:MasterKey is missing: the key cannot be generated");
            return;
        }

        var db = services.GetRequiredService<ConfigurationDbContext>();
        if (!await db.Entries.AnyAsync(e => e.Key == Key, ct))
        {
            var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            db.Entries.Add(ConfigurationEntry.Create(Key, cipher.Encrypt(key), isSecret: true));
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Generated the JWT signing key and stored it encrypted in the platform database");
        }

        configuration.Reload();
    }
}

public sealed record UpdateConfigurationRequest(Dictionary<string, JsonElement> Values);

/// <summary>
/// Runtime configuration of the platform, stored in the platform database: SMTP, storage and OAuth clients, JWT,
/// limits, webhooks, feature defaults and referenced secrets. Changes apply without redeploying.
/// </summary>
public sealed class ConfigurationModule : IModule
{
    public string Name => "Configuration";

    public DataScope DataScope => DataScope.Platform;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ConfigurationDbContext>(Name, DataScope);
        services.AddRepositoriesFor<ConfigurationDbContext>();
        services.AddModuleConventions(typeof(ConfigurationPermissionProvider).Assembly);
        services.AddSingleton<ConfigurationCatalog>();
        services.AddScoped<IDataSeeder, JwtSigningKeySeeder>();
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("configuration").WithTags("configuration").RequirePermission(ConfigurationPermissions.Platform.Manage);
        group.MapGet("/", async (ISender sender, CancellationToken ct) => (await sender.Send(new GetConfigurationQuery(), ct)).ToHttp())
            .WithName("GetConfiguration");
        group.MapPut("/{section}", async (string section, UpdateConfigurationRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateConfigurationCommand(section, request.Values), ct)).ToHttp())
            .WithName("UpdateConfiguration");
    }
}
