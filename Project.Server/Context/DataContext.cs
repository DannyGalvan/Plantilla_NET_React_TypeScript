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
        /// Configura las advertencias del contexto
        /// </summary>
        /// <param name="optionsBuilder">El constructor de opciones</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warn => { warn.Default(WarningBehavior.Ignore); });
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

        /// <summary>
        /// Gets or sets the LoginAudits
        /// </summary>
        public DbSet<LoginAudit> LoginAudits { get; set; }

        /// <summary>
        /// Gets or sets the PasswordHistories
        /// </summary>
        public DbSet<PasswordHistory> PasswordHistories { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
        }
    }
}