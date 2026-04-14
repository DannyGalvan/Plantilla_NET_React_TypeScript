using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Project.Server.Infrastructure.Database.Configurators
{
    /// <summary>
    /// Configurador para MySQL
    /// </summary>
    public class MySqlConfigurator : IDatabaseConfigurator
    {
        /// <summary>
        /// Configura Entity Framework para MySQL
        /// </summary>
        /// <param name="optionsBuilder">Constructor de opciones de DbContext</param>
        /// <param name="connectionString">Cadena de conexión a MySQL</param>
        /// <returns>DbContextOptionsBuilder configurado para MySQL</returns>
        public DbContextOptionsBuilder Configure(DbContextOptionsBuilder optionsBuilder, string connectionString)
        {
            return optionsBuilder.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mysqlOptions =>
                {
                    mysqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                });
        }
    }
}
