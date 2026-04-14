using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Server.Context;
using Project.Server.Infrastructure.Database;

namespace Project.Server.Infrastructure.Extensions
{
    /// <summary>
    /// Extensiones para registrar servicios de base de datos con soporte multi-proveedor
    /// </summary>
    public static class DatabaseServiceExtensions
    {
        /// <summary>
        /// Agrega DbContext con soporte para múltiples proveedores de base de datos
        /// </summary>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Servicios configurados</returns>
        public static IServiceCollection AddMultiDatabaseSupport(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Obtener configuración de base de datos
            var databaseSettings = configuration.GetSection("Database").Get<DatabaseSettings>()
                ?? new DatabaseSettings
                {
                    Provider = DatabaseProvider.SqlServer,
                    ConnectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
                    EnableAutoMigration = false,
                    EnableSensitiveDataLogging = false,
                    EnableChangeTracking = true
                };

            // Registrar configuración en DI
            services.Configure<DatabaseSettings>(configuration.GetSection("Database"));

            // Registrar DbContext con el proveedor configurado
            services.AddDbContext<DataContext>((serviceProvider, options) =>
            {
                var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseSettings>>().Value;

                var configurator = DatabaseConfiguratorFactory.GetConfigurator(settings.Provider);
                configurator.Configure(options, settings.ConnectionString);

                // Configuraciones generales
                if (settings.EnableSensitiveDataLogging)
                {
                    options.EnableSensitiveDataLogging();
                }
            });

            return services;
        }

        /// <summary>
        /// Aplica migraciones automáticas si está configurado
        /// </summary>
        /// <param name="app">Aplicación web</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <returns>Aplicación configurada</returns>
        public static IApplicationBuilder ApplyMigrations(
            this IApplicationBuilder app,
            IConfiguration configuration)
        {
            var databaseSettings = configuration.GetSection("Database").Get<DatabaseSettings>();

            if (databaseSettings?.EnableAutoMigration == true)
            {
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<DataContext>();

                    try
                    {
                        context.Database.Migrate();
                    }
                    catch (Exception ex)
                    {
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataContext>>();
                        logger.LogError(ex, "Error al aplicar migraciones automáticas");
                        throw;
                    }
                }
            }

            return app;
        }
    }
}
