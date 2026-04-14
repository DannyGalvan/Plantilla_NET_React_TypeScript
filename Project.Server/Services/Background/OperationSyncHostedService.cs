using Project.Server.Services.Interfaces;

namespace Project.Server.Services.Background
{
    /// <summary>
    /// Hosted Service que ejecuta la sincronización de operaciones al iniciar la aplicación
    /// </summary>
    public class OperationSyncHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OperationSyncHostedService> _logger;

        public OperationSyncHostedService(
            IServiceProvider serviceProvider,
            ILogger<OperationSyncHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OperationSyncHostedService iniciando...");

            try
            {
                // Crear un scope para resolver servicios scoped
                using var scope = _serviceProvider.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IOperationSyncService>();

                // Ejecutar la sincronización
                await syncService.SyncAsync();

                _logger.LogInformation("OperationSyncHostedService completado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar la sincronización de operaciones en el inicio");
                // No lanzamos la excepción para no detener el inicio de la aplicación
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OperationSyncHostedService deteniendo...");
            return Task.CompletedTask;
        }
    }
}
