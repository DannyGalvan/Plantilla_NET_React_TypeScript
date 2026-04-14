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
        }
    }
}
