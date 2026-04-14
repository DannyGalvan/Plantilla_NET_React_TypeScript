namespace Project.Server.Configs.Extensions
{
    using Microsoft.EntityFrameworkCore;
    using Project.Server.Context;
    using Project.Server.Infrastructure.Database;
    using Project.Server.Infrastructure.Extensions;

    /// <summary>
    /// Defines the <see cref="ContextGroup" />
    /// </summary>
    public static class ContextGroup
    {
        /// <summary>
        /// Configura el contexto de base de datos con soporte para múltiples proveedores
        /// </summary>
        /// <param name="services">Los servicios</param>
        /// <param name="configuration">La configuración</param>
        /// <returns>Los servicios configurados</returns>
        public static IServiceCollection AddContextGroup(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMultiDatabaseSupport(configuration);
            return services;
        }
    }
}
