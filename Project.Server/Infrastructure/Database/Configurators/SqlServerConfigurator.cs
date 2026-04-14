using Microsoft.EntityFrameworkCore;

namespace Project.Server.Infrastructure.Database.Configurators
{
    /// <summary>
    /// Configurador para SQL Server
    /// </summary>
    public class SqlServerConfigurator : IDatabaseConfigurator
    {
        /// <summary>
        /// Configura Entity Framework para SQL Server
        /// </summary>
        /// <param name="optionsBuilder">Constructor de opciones de DbContext</param>
        /// <param name="connectionString">Cadena de conexión a SQL Server</param>
        /// <returns>DbContextOptionsBuilder configurado para SQL Server</returns>
        public DbContextOptionsBuilder Configure(DbContextOptionsBuilder optionsBuilder, string connectionString)
        {
            return optionsBuilder.UseSqlServer(
                connectionString,
                sqlServerOptions =>
                {
                    sqlServerOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                });
        }
    }
}
