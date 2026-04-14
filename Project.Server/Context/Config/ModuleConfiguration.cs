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
        }
    }
}
