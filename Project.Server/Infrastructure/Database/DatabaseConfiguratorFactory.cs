using Project.Server.Infrastructure.Database.Configurators;

namespace Project.Server.Infrastructure.Database
{
    /// <summary>
    /// Factory para crear configuradores de base de datos según el proveedor
    /// </summary>
    public static class DatabaseConfiguratorFactory
    {
        /// <summary>
        /// Obtiene el configurador de base de datos apropiado según el proveedor
        /// </summary>
        /// <param name="provider">Proveedor de base de datos</param>
        /// <returns>Instancia del configurador para el proveedor especificado</returns>
        /// <exception cref="ArgumentException">Si el proveedor no es soportado</exception>
        public static IDatabaseConfigurator GetConfigurator(DatabaseProvider provider)
        {
            return provider switch
            {
                DatabaseProvider.SqlServer => new SqlServerConfigurator(),
                DatabaseProvider.PostgreSql => new PostgreSqlConfigurator(),
                DatabaseProvider.MySql => new MySqlConfigurator(),
                _ => throw new ArgumentException($"Proveedor de base de datos no soportado: {provider}", nameof(provider))
            };
        }
    }
}
