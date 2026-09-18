using Project.Server.Services.Interfaces;
using Project.Server.Utils;

namespace Project.Server.Services.Background
{
    /// <summary>
    /// Verifies at startup that every entity exposed through the generic
    /// <c>IEntityService</c> resolves an ownership predicate. If any entity
    /// fails, the application refuses to start — exposing a non-owned entity
    /// through <c>CrudController</c> is a footgun.
    /// </summary>
    public class OwnershipValidationHostedService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<OwnershipValidationHostedService> _logger;

        public OwnershipValidationHostedService(
            IServiceProvider services,
            ILogger<OwnershipValidationHostedService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var registered = scope.ServiceProvider.GetServices<IEntityServiceDescriptor>();

            foreach (var descriptor in registered)
            {
                if (!OwnershipResolver.IsSupported(descriptor.EntityType))
                {
                    throw new InvalidOperationException(
                        $"Entity type '{descriptor.EntityType.FullName}' is registered through the " +
                        "generic IEntityService but implements neither IOwnedEntity<long> nor " +
                        $"IEntity<{descriptor.IdType.Name}>. Either implement IOwnedEntity<long>, " +
                        "restrict exposure to admin endpoints, or remove the registration.");
                }
                _logger.LogInformation("Ownership OK: {entity}", descriptor.EntityType.Name);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    /// <summary>
    /// Marker interface used by <c>OwnershipValidationHostedService</c> to enumerate
    /// every <c>IEntityService&lt;TEntity, TRequest, TId&gt;</c> registered in the container.
    /// </summary>
    public interface IEntityServiceDescriptor
    {
        Type EntityType { get; }
        Type IdType { get; }
    }
}