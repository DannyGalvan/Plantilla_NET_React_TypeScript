using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Infrastructure;

namespace Project.Server.Infrastructure.Database.Configurators;

/// <summary>
/// Configurador para MySQL. Usa el proveedor oficial de Oracle
/// (MySql.EntityFrameworkCore) por compatibilidad con EF Core 10.
/// Pomelo no publica todavía una versión estable para EF Core 10.
/// Ver <c>docs/data-access.md</c> (Fase 10) para los detalles y la fecha de revisión.
/// </summary>
public class MySqlConfigurator : IDatabaseConfigurator
{
    /// <summary>
    /// Configura Entity Framework para MySQL.
    /// </summary>
    /// <param name="optionsBuilder">Constructor de opciones de DbContext.</param>
    /// <param name="connectionString">Cadena de conexión a MySQL.</param>
    /// <returns>DbContextOptionsBuilder configurado para MySQL.</returns>
    public DbContextOptionsBuilder Configure(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        return optionsBuilder.UseMySQL(
            connectionString,
            mysqlOptions =>
            {
                mysqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
            });
    }
}