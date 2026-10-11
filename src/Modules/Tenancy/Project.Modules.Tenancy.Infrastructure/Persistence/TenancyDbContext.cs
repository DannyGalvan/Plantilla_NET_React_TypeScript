using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Infrastructure.Persistence;
using Project.Modules.Tenancy.Domain;

namespace Project.Modules.Tenancy.Infrastructure.Persistence;

/// <summary>Platform database: tenant catalog, domains, features and settings overrides.</summary>
public sealed class TenancyDbContext(DbContextOptions<TenancyDbContext> options, ModuleDbContextDependencies dependencies)
    : ModuleDbContext(options, dependencies)
{
    public const string ModuleSchema = "tenancy";

    public override string Schema => ModuleSchema;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<SettingValue> SettingValues => Set<SettingValue>();
}

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(t => t.Isolation).HasConversion<string>().HasMaxLength(32);
        builder.Property(t => t.DatabaseProvider).HasMaxLength(32);
        builder.Property(t => t.ConnectionSecretRef).HasMaxLength(256);
        builder.Property(t => t.Plan).HasMaxLength(64);
        builder.Property(t => t.CreatedBy).HasMaxLength(64);
        builder.Property(t => t.UpdatedBy).HasMaxLength(64);
        builder.Property(t => t.DeletedBy).HasMaxLength(64);

        builder.HasMany(t => t.Domains).WithOne().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Domains).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(t => t.Features).WithOne().HasForeignKey(f => f.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Features).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class TenantDomainConfiguration : IEntityTypeConfiguration<TenantDomain>
{
    public void Configure(EntityTypeBuilder<TenantDomain> builder)
    {
        builder.ToTable("TenantDomains");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Host).HasMaxLength(253).IsRequired();
        builder.HasIndex(d => d.Host).IsUnique();
    }
}

internal sealed class TenantFeatureConfiguration : IEntityTypeConfiguration<TenantFeature>
{
    public void Configure(EntityTypeBuilder<TenantFeature> builder)
    {
        builder.ToTable("TenantFeatures");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.Feature).HasMaxLength(128).IsRequired();
        builder.HasIndex(f => new { f.TenantId, f.Feature }).IsUnique();
    }
}

internal sealed class SettingValueConfiguration : IEntityTypeConfiguration<SettingValue>
{
    public void Configure(EntityTypeBuilder<SettingValue> builder)
    {
        builder.ToTable("SettingValues");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Scope).HasMaxLength(32).IsRequired();
        builder.Property(v => v.SectionKey).HasMaxLength(64).IsRequired();
        builder.Property(v => v.Json).IsRequired();
        builder.HasIndex(v => new { v.SectionKey, v.Scope, v.ScopeId, v.TenantId });
    }
}
