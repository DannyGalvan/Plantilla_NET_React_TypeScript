using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Project.Server.Entities.Interfaces;
using Project.Server.Entities.Models;

namespace Project.Server.Context
{
    public class DataContext : DbContext
    {
        public DataContext()
        {
        }

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        /// <summary>
        /// EF Core warned-on-silently defaults hide real client-evaluation bugs
        /// (B22). We keep the relational warning surface visible and only escalate
        /// the <c>MultipleCollectionIncludeWarning</c> — that one is a Cartesian
        /// product and must fail loud.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warn =>
            {
                warn.Default(WarningBehavior.Log);
                warn.Throw(RelationalEventId.MultipleCollectionIncludeWarning);
            });
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Operation> Operations { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<RolOperation> RolOperations { get; set; }
        public DbSet<LoginAudit> LoginAudits { get; set; }
        public DbSet<PasswordHistory> PasswordHistories { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);

            // Apply global query filter for soft-delete (B14):
            // every entity implementing IEntity<TId> gets `WHERE State != 0`
            // applied to every read unless the caller invokes IgnoreQueryFilters().
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clr = entityType.ClrType;
                bool implements = false;
                foreach (var i in clr.GetInterfaces())
                {
                    if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>))
                    {
                        implements = true;
                        break;
                    }
                }
                if (!implements) continue;

                var parameter = Expression.Parameter(clr, "e");
                var stateProp = Expression.PropertyOrField(parameter, "State");
                var condition = Expression.NotEqual(stateProp, Expression.Constant(0));
                var lambda = Expression.Lambda(condition, parameter);

                modelBuilder.Entity(clr).HasQueryFilter(lambda);
            }

            // DateTime UTC conversion preserved for PostgreSQL compatibility.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(
                            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                            )
                        );
                    }
                }
            }
        }
    }

    internal static class TypeExtensions
    {
        /// <summary>
        /// True when the type implements any closed <c>IEntity&lt;T&gt;</c> interface.
        /// </summary>
        public static bool ImplementsIEntityOpen(this Type t)
        {
            foreach (var i in t.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>))
                    return true;
            }
            return false;
        }
    }
}