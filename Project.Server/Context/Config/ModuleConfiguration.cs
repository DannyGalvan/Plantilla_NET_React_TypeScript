using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Server.Entities.Models;

namespace Project.Server.Context.Config
{
    public class ModuleConfiguration : IEntityTypeConfiguration<Module>
    {
        public void Configure(EntityTypeBuilder<Module> entity)
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name)
                .HasMaxLength(255);
            entity.Property(e => e.Description)
                .HasMaxLength(255);
            entity.Property(e => e.Image)
                .HasMaxLength(255);
            entity.Property(e => e.Path)
                    .HasMaxLength(255);

            entity.HasData(
                new Module
                {
                    Id = 1,
                    Name = "Pagos",
                    Description = "Modulo de Pagos",
                    Image = "mail-open",
                    Path = "Payments",
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new Module
                {
                    Id = 2,
                    Name = "Cobros Automaticos",
                    Description = "Modulo de Cobros Automaticos",
                    Image = "apps",
                    Path = "AutomaticPayments",
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new Module
                {
                    Id = 3,
                    Name = "Cursos",
                    Description = "Modulo de Cursos",
                    Image = "cash",
                    Path = "Courses",
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new Module
                {
                    Id = 4,
                    Name = "Estado de Cuenta",
                    Description = "Modulo Estado de Cuenta",
                    Image = "cash",
                    Path = "AccountStatement",
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                }
            );
        }
    }
}
