using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Infrastructure.Persistence;
using Project.Modules.Organization.Domain;

namespace Project.Modules.Organization.Infrastructure.Persistence;

/// <summary>Tenant database: organization tree, unit types and user assignments.</summary>
public sealed class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options, ModuleDbContextDependencies dependencies)
    : ModuleDbContext(options, dependencies)
{
    public const string ModuleSchema = "organization";

    public override string Schema => ModuleSchema;

    public DbSet<OrganizationUnit> Units => Set<OrganizationUnit>();

    public DbSet<OrganizationUnitType> UnitTypes => Set<OrganizationUnitType>();

    public DbSet<UnitAssignment> Assignments => Set<UnitAssignment>();
}

internal sealed class OrganizationUnitConfiguration : IEntityTypeConfiguration<OrganizationUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationUnit> builder)
    {
        builder.ToTable("Units");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.TypeCode).HasMaxLength(32).IsRequired();
        builder.Property(u => u.Path).HasMaxLength(700).IsRequired(); // ~20 levels; fits MySQL index limits
        builder.Property(u => u.Code).HasMaxLength(32).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(128).IsRequired();
        builder.Property(u => u.CountryCode).HasMaxLength(2);
        builder.Property(u => u.CreatedBy).HasMaxLength(64);
        builder.Property(u => u.UpdatedBy).HasMaxLength(64);
        builder.Property(u => u.DeletedBy).HasMaxLength(64);
        builder.HasIndex(u => new { u.TenantId, u.Code }).IsUnique();
        builder.HasIndex(u => new { u.TenantId, u.Path });
        builder.HasIndex(u => u.ParentId);
    }
}

internal sealed class OrganizationUnitTypeConfiguration : IEntityTypeConfiguration<OrganizationUnitType>
{
    public void Configure(EntityTypeBuilder<OrganizationUnitType> builder)
    {
        builder.ToTable("UnitTypes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).HasMaxLength(32).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(64).IsRequired();
        builder.Property(t => t.AllowedParentCodes).HasMaxLength(256).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(64);
        builder.Property(t => t.UpdatedBy).HasMaxLength(64);
        builder.Property(t => t.DeletedBy).HasMaxLength(64);
        builder.HasIndex(t => new { t.TenantId, t.Code }).IsUnique();
    }
}

internal sealed class UnitAssignmentConfiguration : IEntityTypeConfiguration<UnitAssignment>
{
    public void Configure(EntityTypeBuilder<UnitAssignment> builder)
    {
        builder.ToTable("UnitAssignments");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.TenantId, a.UserId, a.UnitId }).IsUnique();
        builder.HasIndex(a => a.UnitId);
    }
}
