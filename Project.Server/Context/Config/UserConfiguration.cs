using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Server.Entities.Models;

namespace Project.Server.Context.Config
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> entity)
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name)
                .HasMaxLength(255);
            entity.Property(e => e.UserName)
                .HasMaxLength(255);
            entity.Property(e => e.Password)
                .HasMaxLength(255);
            entity.Property(e => e.Number)
                .HasMaxLength(255);
            entity.Property(e => e.Email)
                .HasMaxLength(255);
            entity.Property(e => e.IdentificationDocument)
                .HasMaxLength(255);
            entity.Property(e => e.RecoveryToken)
                .HasMaxLength(255);

            entity.HasOne(e => e.Rol)
                    .WithMany(e => e.Users)
                .HasForeignKey(e => e.RolId);

            // Seed data: no usable password. The AdminSeedHostedService injects the
// password at startup from AppSettings:SeedAdminPassword (user-secret in
// development, env var in production). MustChangePassword forces a change
// on first login (B6).
            entity.HasData(
                new User
                {
                    Id = 1,
                    RolId = 1,
                    Password = "!UNUSABLE-SEED!",
                    MustChangePassword = true,
                    Name = "Super Administrador",
                    UserName = "SADMIN",
                    Number = "51995142",
                    Email = "pruebas.test29111999@gmail.com",
                    IdentificationDocument = "2987967910101",
                    RecoveryToken = "",
                    Reset = false,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Utc),
                    CreatedBy = 1,
                    DateToken = null,
                    UpdatedAt = null,
                    UpdatedBy = null
                }
            );
        }
    }
}
