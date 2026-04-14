namespace Project.Server.Infrastructure.Database
{
    /// <summary>
    /// Enumeración que define los proveedores de base de datos soportados
    /// </summary>
    public enum DatabaseProvider
    {
        /// <summary>
        /// Microsoft SQL Server
        /// </summary>
        SqlServer = 0,

        /// <summary>
        /// PostgreSQL
        /// </summary>
        PostgreSql = 1,

        /// <summary>
        /// MySQL
        /// </summary>
        MySql = 2
    }
}
