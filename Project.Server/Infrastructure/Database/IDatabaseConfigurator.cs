using Microsoft.EntityFrameworkCore;

namespace Project.Server.Infrastructure.Database
{
    /// <summary>
    /// Interfaz para configurar diferentes proveedores de base de datos
    /// </summary>
    public interface IDatabaseConfigurator
    {
        /// <summary>
        /// Configura el proveedor de base de datos en DbContextOptionsBuilder
        /// </summary>
        /// <param name="optionsBuilder">Constructor de opciones de DbContext</param>
        /// <param name="connectionString">Cadena de conexión</param>
        /// <returns>DbContextOptionsBuilder configurado</returns>
        DbContextOptionsBuilder Configure(DbContextOptionsBuilder optionsBuilder, string connectionString);
    }
}
