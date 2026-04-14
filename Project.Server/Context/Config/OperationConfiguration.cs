using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Project.Server.Entities.Models;

namespace Project.Server.Context.Config
{
    public class OperationConfiguration : IEntityTypeConfiguration<Operation>
    {
        public void Configure(EntityTypeBuilder<Operation> entity)
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name)
                .HasMaxLength(255);
            entity.Property(e => e.Guid)
               .HasMaxLength(255);
            entity.Property(e => e.Description)
                .HasMaxLength(255);
            entity.Property(e => e.Policy)
                .HasMaxLength(255);
            entity.Property(e => e.Icon)
                .HasMaxLength(255);
            entity.Property(e => e.Path)
                .HasMaxLength(255);
            entity.Property(e => e.ControllerName)
                .HasMaxLength(255);
            entity.Property(e => e.ActionName)
                .HasMaxLength(255);
            entity.Property(e => e.HttpMethod)
                .HasMaxLength(50);
            entity.Property(e => e.RouteTemplate)
                .HasMaxLength(500);
            entity.Property(e => e.OperationKey)
                .HasMaxLength(255);
            entity.HasOne(e => e.Module)
                .WithMany(e => e.Operations)
                .HasForeignKey(e => e.ModuleId);
        }
    }
}
