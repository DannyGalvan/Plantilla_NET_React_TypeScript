using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Server.Entities.Models;

namespace Project.Server.Context.Config
{
    public class RolOperationConfiguration : IEntityTypeConfiguration<RolOperation>
    {
        public void Configure(EntityTypeBuilder<RolOperation> entity)
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.HasOne(e => e.Rol)
                .WithMany(e => e.RolOperations)
                .HasForeignKey(e => e.RolId);

            entity.HasOne(e => e.Operation)
                .WithMany(e => e.RolOperations)
                .HasForeignKey(e => e.OperationId);

            entity.HasData(
                // Rol SA
                new RolOperation
                {
                    Id = 1,
                    RolId = 1,
                    OperationId = 1,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new RolOperation
                {
                    Id = 2,
                    RolId = 1,
                    OperationId = 2,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new RolOperation
                {
                    Id = 3,
                    RolId = 1,
                    OperationId = 3,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new RolOperation
                {
                    Id = 4,
                    RolId = 1,
                    OperationId = 4,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new RolOperation
                {
                    Id = 5,
                    RolId = 1,
                    OperationId = 5,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new RolOperation
                {
                    Id = 6,
                    RolId = 1,
                    OperationId = 6,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new RolOperation
                {
                    Id = 7,
                    RolId = 1,
                    OperationId = 7,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new RolOperation
                {
                    Id = 8,
                    RolId = 1,
                    OperationId = 8,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new RolOperation
                {
                    Id = 9,
                    RolId = 1,
                    OperationId = 9,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new RolOperation
                {
                    Id = 10,
                    RolId = 1,
                    OperationId = 10,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                // Rol Estudiante
                new RolOperation
                {
                    Id = 11,
                    RolId = 2,
                    OperationId = 2,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new RolOperation
                {
                    Id = 12,
                    RolId = 2,
                    OperationId = 5,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new RolOperation
                {
                    Id = 13,
                    RolId = 2,
                    OperationId = 9,
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
