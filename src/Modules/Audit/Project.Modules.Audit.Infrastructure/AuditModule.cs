using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Project.Application;
using Project.Application.Abstractions;
using Project.Application.Auditing;
using Project.Application.Jobs;
using Project.Application.Tenancy;
using Project.Application.Webhooks;
using Project.Infrastructure.Outbox;
using Project.Infrastructure.Persistence;
using Project.Modules.Audit.Application;
using Project.Modules.Audit.Contracts;
using Project.Modules.Audit.Domain;
using Project.SharedKernel.Domain;
using Project.Web.Http;
using Project.Web.Modules;
using Project.Web.Security;

namespace Project.Modules.Audit.Infrastructure;

public sealed class AuditOptions
{
    public const string SectionName = "Audit";

    public bool Enabled { get; set; } = true;

    /// <summary>Entity types never recorded, for example <c>identity.refreshToken</c>.</summary>
    public string[] ExcludedEntityTypes { get; set; } = [];

    /// <summary>Longest value stored per property.</summary>
    public int MaxValueLength { get; set; } = 500;

    /// <summary>Entries older than this are deleted by the <c>audit.retention</c> job. 0 keeps them forever.</summary>
    public int RetentionDays { get; set; } = 365;
}

/// <summary>Deletes history older than <c>Audit:RetentionDays</c>.</summary>
public sealed class AuditRetentionJob(AuditDbContext db, IOptions<AuditOptions> options, TimeProvider clock) : IRecurringJob
{
    public const string Name = "audit.retention";

    public async Task ExecuteAsync(CancellationToken ct)
    {
        if (options.Value.RetentionDays > 0)
        {
            var cutoff = clock.GetUtcNow().AddDays(-options.Value.RetentionDays);
            await db.Entries.Where(e => e.OccurredAt < cutoff).ExecuteDeleteAsync(ct);
        }
    }
}

public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options, ModuleDbContextDependencies dependencies)
    : ModuleDbContext(options, dependencies)
{
    public override string Schema => "audit";

    public DbSet<AuditEntry> Entries => Set<AuditEntry>();
}

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("Entries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(e => e.EntityId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Operation).HasMaxLength(16).IsRequired();
        builder.Property(e => e.Changes).IsRequired();
        builder.Property(e => e.UserId).HasMaxLength(64);
        builder.Property(e => e.UserName).HasMaxLength(128);
        builder.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId, e.OccurredAt });
        builder.HasIndex(e => new { e.TenantId, e.OccurredAt });
    }
}

/// <summary>
/// Captures property changes of every <see cref="IAuditable"/> entity of every module before saving and writes them
/// to that module's outbox, in the same transaction. Registered only when the Audit module is loaded.
/// </summary>
public sealed class ChangeHistoryStep(ICurrentUser user, TimeProvider clock, IOptions<AuditOptions> options) : ISaveChangesStep
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] SensitiveWords = ["password", "secret", "token", "credential", "hash"];
    private static readonly HashSet<string> Skipped =
    [
        nameof(IAuditable.CreatedAt), nameof(IAuditable.CreatedBy), nameof(IAuditable.UpdatedAt), nameof(IAuditable.UpdatedBy),
        nameof(ISoftDeletable.DeletedAt), nameof(ISoftDeletable.DeletedBy), nameof(IHasConcurrencyStamp.ConcurrencyStamp),
        nameof(IMultiTenant.TenantId),
    ];

    private static readonly ConcurrentDictionary<PropertyInfo, bool> Ignored = new();

    /// <summary>After soft delete, audit stamps, tenant and organization; before the outbox step.</summary>
    public int Order => 90;

    public void Apply(ModuleDbContext context)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        var now = clock.GetUtcNow();
        foreach (var entry in context.ChangeTracker.Entries<IAuditable>().ToList())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified) || entry.Entity.GetType().GetCustomAttribute<NotAuditedAttribute>() is not null)
            {
                continue;
            }

            var entityType = $"{context.Schema}.{JsonNamingPolicy.CamelCase.ConvertName(entry.Entity.GetType().Name)}";
            if (options.Value.ExcludedEntityTypes.Contains(entityType, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var operation = Operation(entry);
            var changes = Changes(entry, operation);
            if (operation == AuditOperations.Updated && changes.Count == 0)
            {
                continue;
            }

            var key = string.Join(',', entry.Metadata.FindPrimaryKey()!.Properties.Select(p => Format(entry.Property(p.Name).CurrentValue)));
            var change = new EntityChangedEvent(entityType, key, operation, changes, user.UserId ?? "system", user.UserName, now);
            context.OutboxMessages.Add(new OutboxMessage
            {
                Id = NewId.Next(),
                TenantId = context.CurrentTenantId,
                Type = $"{typeof(EntityChangedEvent).FullName}, {typeof(EntityChangedEvent).Assembly.GetName().Name}",
                Payload = JsonSerializer.Serialize(change, Json),
                OccurredAt = now,
            });
        }
    }

    private static string Operation(EntityEntry<IAuditable> entry)
    {
        if (entry.State == EntityState.Added)
        {
            return AuditOperations.Created;
        }

        if (entry.Entity is ISoftDeletable && entry.Property(nameof(ISoftDeletable.IsDeleted)) is { IsModified: true } deleted)
        {
            return deleted.CurrentValue is true ? AuditOperations.Deleted : AuditOperations.Restored;
        }

        return AuditOperations.Updated;
    }

    private List<PropertyChange> Changes(EntityEntry<IAuditable> entry, string operation)
    {
        var changes = new List<PropertyChange>();
        if (operation is AuditOperations.Deleted or AuditOperations.Restored)
        {
            return changes;
        }

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (property.Metadata.IsPrimaryKey() || property.Metadata.IsShadowProperty() || Skipped.Contains(name) || IsSensitive(property))
            {
                continue;
            }

            if (operation == AuditOperations.Created)
            {
                if (property.CurrentValue is not null)
                {
                    changes.Add(new PropertyChange(name, null, Format(property.CurrentValue)));
                }
            }
            else if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
            {
                changes.Add(new PropertyChange(name, Format(property.OriginalValue), Format(property.CurrentValue)));
            }
        }

        return changes;
    }

    private static bool IsSensitive(PropertyEntry property)
    {
        var name = property.Metadata.Name;
        if (SensitiveWords.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return property.Metadata.PropertyInfo is { } info
            && Ignored.GetOrAdd(info, static p => p.GetCustomAttribute<AuditIgnoreAttribute>() is not null);
    }

    private string? Format(object? value)
    {
        var text = value switch
        {
            null => null,
            DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
        var max = options.Value.MaxValueLength;
        return text is not null && text.Length > max ? text[..max] + "..." : text;
    }
}

/// <summary>
/// Change history of every business record (who changed what and when). Captures changes of all modules without
/// any code in them; turn it off with <c>Modules:Disabled</c> or <c>Audit:Enabled=false</c>.
/// </summary>
public sealed class AuditModule : IModule
{
    public string Name => "Audit";

    public DataScope DataScope => DataScope.Tenant;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<AuditDbContext>(Name, DataScope);
        services.AddRepositoriesFor<AuditDbContext>();
        services.AddModuleConventions(typeof(AuditPermissionProvider).Assembly);
        services.AddOptions<AuditOptions>().Bind(configuration.GetSection(AuditOptions.SectionName));
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISaveChangesStep, ChangeHistoryStep>());
        services.AddWebhookEvent<EntityChangedEvent>("records.changed", "A record of any module was created, updated, deleted or restored.");
        services.AddRecurringJob<AuditRetentionJob>(AuditRetentionJob.Name, "0 4 * * *", description: "Deletes history older than the retention period.");
    }

    public void MapEndpoints(IEndpointRouteBuilder api) =>
        api.MapGet("audit/entries", async ([AsParameters] ListAuditEntriesQuery query, ISender sender, CancellationToken ct) => (await sender.Send(query, ct)).ToHttp())
            .RequirePermission(AuditPermissions.History.View)
            .WithTags("audit")
            .WithName("ListAuditEntries");
}
