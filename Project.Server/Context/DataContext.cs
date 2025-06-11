using Microsoft.EntityFrameworkCore;
using Project.Server.Entities.Models;

namespace Project.Server.Context
{
    public class DataContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataContext"/> class.
        /// </summary>
        public DataContext()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataContext"/> class.
        /// </summary>
        /// <param name="options">The options<see cref="DbContextOptions{DataContext}"/></param>
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        /// <summary>
        /// The OnConfiguring
        /// </summary>
        /// <param name="optionsBuilder">The optionsBuilder<see cref="DbContextOptionsBuilder"/></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warn => { warn.Default(WarningBehavior.Ignore); });

            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlServer("Name=ConnectionStrings:EsiSchoolPayments");
        }

        // Add DbSet for each entity

        /// <summary>
        /// Gets or sets the Users
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Gets or sets the Modules
        /// </summary>
        public DbSet<Module> Modules { get; set; }

        /// <summary>
        /// Gets or sets the Operations
        /// </summary>
        public DbSet<Operation> Operations { get; set; }

        /// <summary>
        /// Gets or sets the Roles
        /// </summary>
        public DbSet<Rol> Roles { get; set; }

        /// <summary>
        /// Gets or sets the RolOperations
        /// </summary>
        public DbSet<RolOperation> RolOperations { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add configuration for each entity
            modelBuilder.Entity<Rol>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name)
                    .HasMaxLength(255);
                entity.Property(e => e.Description)
                    .HasMaxLength(255);
                entity.HasData(
                    new Rol
                    {
                        Id = 1,
                        Name = "SA",
                        Description = "Super Administrator",
                        State = 1,
                        CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        CreatedBy = 1,
                        UpdatedAt = null,
                        UpdatedBy = null
                    }
                );
            });

            modelBuilder.Entity<User>(entity =>
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

                //password: Guatemala1.
                entity.HasData(
                    new User
                    {
                        Id = 1,
                        RolId = 1,
                        Password = "$2a$12$86Ty8oUVWKPbU8JqCII9VO.FgM1C10dweQ4xKhM4jj1LWL9jwNu7.",
                        Name = "Super Administrador",
                        UserName = "SADMIN",
                        Number = "51995142",
                        Email = "pruebas.test29111999@gmail.com",
                        IdentificationDocument = "2987967910101",
                        RecoveryToken = "",
                        Reset = false,
                        State = 1,
                        CreatedAt = new DateTime(2025, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified),
                        CreatedBy = 1,
                        DateToken = null,
                        UpdatedAt = null,
                        UpdatedBy = null
                    }
                );
            });

            modelBuilder.Entity<Module>(entity =>
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
            });

            modelBuilder.Entity<Operation>(entity =>
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
                entity.HasOne(e => e.Module)
                    .WithMany(e => e.Operations)
                    .HasForeignKey(e => e.ModuleId);

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
            });

            modelBuilder.Entity<RolOperation>(entity =>
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
            });
        }
    }
}