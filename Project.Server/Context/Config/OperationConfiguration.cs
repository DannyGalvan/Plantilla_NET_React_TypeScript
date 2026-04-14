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

            entity.HasData(
                // Modulo de Pagos
                new Operation
                {
                    Id = 1,
                    Guid = "6451551a-b05c-455b-b1b9-97616e1c8892",
                    Name = "Listar Pagos",
                    Description = "Consultar Pagos",
                    Policy = "Payments.List",
                    Icon = "list",
                    Path = "Payments",
                    ModuleId = 1,
                    IsVisible = true,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new Operation
                {
                    Id = 2,
                    Guid = "2e26b4ca-bd5d-4c4f-a027-ba09f5bd448f",
                    Name = "Crear Pago",
                    Description = "Creacion de pagos",
                    Policy = "Payments.Create",
                    Icon = "cash",
                    Path = "Payments/Create",
                    ModuleId = 1,
                    IsVisible = false,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                //Modulo de Cobros Automaticos
                new Operation
                {
                    Id = 3,
                    Guid = "3fd82baf-a73d-4809-8508-60dbec6119b0",
                    Name = "Listar Cobros Automaticos",
                    Description = "Listar Cobros Automaticos",
                    Policy = "AutomaticPayments.List",
                    Icon = "list",
                    Path = "AutomaticPayments",
                    ModuleId = 2,
                    IsVisible = true,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new Operation
                {
                    Id = 4,
                    Guid = "3f3f1b3b-6b0d-4eb4-8eee-cee97771f293",
                    Name = "Crear Cobros Automaticos",
                    Description = "Crear Cobros Automaticos",
                    Policy = "AutomaticPayments.Create",
                    Icon = "list",
                    Path = "AutomaticPayments",
                    ModuleId = 2,
                    IsVisible = true,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                 // Modulo de Cursos
                new Operation
                {
                    Id = 5,
                    Guid = "def16355-a62d-4e96-8549-66d6cf66352b",
                    Name = "Listar Cursos",
                    Description = "Listar Cursos",
                    Policy = "Courses.List",
                    Icon = "list",
                    Path = "Courses",
                    ModuleId = 3,
                    IsVisible = true,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new Operation
                {
                    Id = 6,
                    Guid = "0487750b-e5d1-434a-892c-98c8df359ce1",
                    Name = "Crear Cursos",
                    Description = "Crear Cursos",
                    Policy = "Courses.Create",
                    Icon = "list",
                    Path = "Courses/Create",
                    ModuleId = 3,
                    IsVisible = true,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new Operation
                {
                    Id = 7,
                    Guid = "89775173-5789-4fbd-984a-14215b4ec5d4",
                    Name = "Actualizar Cursos",
                    Description = "Actualizar Cursos",
                    Policy = "Courses.Update",
                    Icon = "list",
                    Path = "Courses/Update",
                    ModuleId = 3,
                    IsVisible = false,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new Operation
                {
                    Id = 8,
                    Guid = "578a0423-c554-4cac-b5db-ef66f3dec7be",
                    Name = "Eliminar Cursos",
                    Description = "Eliminar Cursos",
                    Policy = "AutomaticPayments.Delete",
                    Icon = "list",
                    Path = "Courses/Delete",
                    ModuleId = 3,
                    IsVisible = false,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                // Modulo de Estado de Cuenta
                new Operation
                {
                    Id = 9,
                    Guid = "aecb492c-69ad-480d-9646-d589b0baf93a",
                    Name = "Estado de Cuenta Estudiante",
                    Description = "Estado de Cuenta Estudiante",
                    Policy = "AccountStatement.Student",
                    Icon = "list",
                    Path = "AccountStatement/Student",
                    ModuleId = 4,
                    IsVisible = true,
                    State = 1,
                    CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                    CreatedBy = 1,
                    UpdatedAt = null,
                    UpdatedBy = null
                },
                new Operation
                {
                    Id = 10,
                    Guid = "07e595dd-7330-4948-b4ab-853144ca8cbb",
                    Name = "Estado de Cuenta Global",
                    Description = "Estado de Cuenta Global",
                    Policy = "AccountStatement.Admin",
                    Icon = "list",
                    Path = "AccountStatement/Admin",
                    ModuleId = 4,
                    IsVisible = true,
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
