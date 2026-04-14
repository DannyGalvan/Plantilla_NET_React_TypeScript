namespace Project.Server.Infrastructure.Database
{
    /// <summary>
    /// Configuración de la base de datos
    /// </summary>
    public class DatabaseSettings
    {
        /// <summary>
        /// Proveedor de base de datos a utilizar
        /// </summary>
        public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;

        /// <summary>
        /// Cadena de conexión a la base de datos
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Habilitar migraciones automáticas en tiempo de ejecución
        /// </summary>
        public bool EnableAutoMigration { get; set; } = false;

        /// <summary>
        /// Habilitar sensibilidad a mayúsculas/minúsculas en SQLite (no aplica a otras BD)
        /// </summary>
        public bool EnableSensitiveDataLogging { get; set; } = false;

        /// <summary>
        /// Habilitar el seguimiento de cambios
        /// </summary>
        public bool EnableChangeTracking { get; set; } = true;
    }
}
