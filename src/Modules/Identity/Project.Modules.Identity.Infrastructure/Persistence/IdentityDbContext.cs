using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Infrastructure.Persistence;
using Project.Modules.Identity.Domain;

namespace Project.Modules.Identity.Infrastructure.Persistence;

/// <summary>Platform database: global users, roles, permissions, memberships and sessions.</summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options, ModuleDbContextDependencies dependencies)
    : ModuleDbContext(options, dependencies)
{
    public const string ModuleSchema = "identity";

    public override string Schema => ModuleSchema;

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<LoginAudit> LoginAudits => Set<LoginAudit>();
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.UserName).HasMaxLength(64).IsRequired();
        builder.Property(u => u.NormalizedUserName).HasMaxLength(64).IsRequired();
        builder.HasIndex(u => u.NormalizedUserName).IsUnique();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.NormalizedEmail).IsUnique();
        builder.Property(u => u.DisplayName).HasMaxLength(128);
        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(u => u.SecurityStamp).HasMaxLength(64).IsRequired();
        builder.Property(u => u.CreatedBy).HasMaxLength(64);
        builder.Property(u => u.UpdatedBy).HasMaxLength(64);
        builder.Property(u => u.DeletedBy).HasMaxLength(64);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(64).IsRequired();
        builder.Property(r => r.NormalizedName).HasMaxLength(64).IsRequired();
        builder.HasIndex(r => new { r.TenantId, r.NormalizedName });
        builder.Property(r => r.Description).HasMaxLength(256);
        builder.Property(r => r.CreatedBy).HasMaxLength(64);
        builder.Property(r => r.UpdatedBy).HasMaxLength(64);
        builder.Property(r => r.DeletedBy).HasMaxLength(64);

        builder.OwnsMany(r => r.Permissions, permission =>
        {
            permission.ToTable("RolePermissions");
            permission.WithOwner().HasForeignKey(p => p.RoleId);
            permission.HasKey(p => new { p.RoleId, p.PermissionKey });
            permission.Property(p => p.PermissionKey).HasMaxLength(128);
            permission.HasIndex(p => p.PermissionKey);
        });
        builder.Navigation(r => r.Permissions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("Key").HasMaxLength(128);
        builder.Property(p => p.Module).HasMaxLength(64).IsRequired();
        builder.Property(p => p.DisplayKey).HasMaxLength(128).IsRequired();
        builder.Property(p => p.Group).HasMaxLength(64);
    }
}

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("TenantMemberships");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.UserId, m.TenantId }).IsUnique();
        builder.HasIndex(m => m.TenantId);
    }
}

internal sealed class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        builder.ToTable("RoleAssignments");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.UserId, a.TenantId });
        builder.HasIndex(a => a.RoleId);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.FamilyId);
        builder.HasIndex(t => t.UserId);
        builder.Property(t => t.CreatedByIp).HasMaxLength(64);
    }
}

internal sealed class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.ToTable("PasswordHistories");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.PasswordHash).HasMaxLength(256).IsRequired();
        builder.HasIndex(h => new { h.UserId, h.CreatedAt });
    }
}

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();
    }
}

internal sealed class LoginAuditConfiguration : IEntityTypeConfiguration<LoginAudit>
{
    public void Configure(EntityTypeBuilder<LoginAudit> builder)
    {
        builder.ToTable("LoginAudits");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.UserName).HasMaxLength(256).IsRequired();
        builder.Property(a => a.FailureReason).HasMaxLength(64);
        builder.Property(a => a.IpAddress).HasMaxLength(64);
        builder.Property(a => a.UserAgent).HasMaxLength(512);
        builder.HasIndex(a => new { a.UserId, a.OccurredAt });
    }
}
