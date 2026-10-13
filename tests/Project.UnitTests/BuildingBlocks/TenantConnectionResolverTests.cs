using Microsoft.Extensions.Options;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Infrastructure.Persistence.Providers;
using Project.Infrastructure.Tenancy;

namespace Project.UnitTests.BuildingBlocks;

public sealed class TenantConnectionResolverTests
{
    private const string Dedicated = "Host=db;Database=vip";

    private static readonly TenantInfo Vip = new(
        Guid.CreateVersion7(), "vip", "Vip", TenantStatus.Active, TenantIsolation.Dedicated,
        DatabaseProvider: null, ConnectionSecretRef: "vip-db", Plan: null, Features: []);

    [Fact]
    public async Task A_prepared_scope_resolves_its_connection_even_if_the_shared_cache_is_invalidated()
    {
        var cache = new DedicatedConnectionCache();
        var tenancy = Options.Create(new TenancyOptions { Mode = TenancyMode.Hybrid });
        var context = new TenantContext(tenancy);
        context.Set(Vip);
        var resolver = new TenantConnectionResolver(
            Options.Create(new DatabaseOptions { Provider = "PostgreSql" }),
            tenancy,
            new DatabaseProviderRegistry([new PostgreSqlProvider()]),
            context,
            new FixedSecretStore(),
            cache);

        await resolver.PrepareAsync(Vip, TestContext.Current.CancellationToken);

        // A tenant change (e.g. dispatched from the outbox) invalidates the process-wide cache meanwhile.
        cache.Remove(Vip.Id);

        resolver.Resolve("Catalog", DataScope.Tenant).ConnectionString.ShouldBe(Dedicated);
    }

    private sealed class FixedSecretStore : ISecretStore
    {
        public ValueTask<string?> GetAsync(string reference, CancellationToken ct) => ValueTask.FromResult<string?>(Dedicated);
    }
}
