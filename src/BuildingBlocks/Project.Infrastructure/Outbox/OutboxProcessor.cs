using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Application.Abstractions;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Infrastructure.Tenancy;
using Project.SharedKernel.Domain;

namespace Project.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; set; } = true;

    public int PollingIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 20;

    public int MaxAttempts { get; set; } = 10;
}

/// <summary>Processes the outbox of one module DbContext.</summary>
public interface IOutboxProcessor
{
    Type ContextType { get; }

    /// <summary>Dispatches the pending messages once (every tenant database when they are dedicated).</summary>
    Task ProcessAllAsync(CancellationToken ct);
}

/// <summary>
/// One background loop per instance that drains the outbox of every module in turn. Sequential on purpose: one
/// database connection at a time instead of one poller per module, which keeps connection pools small when the
/// application has many modules and many tenant databases.
/// </summary>
public sealed class OutboxDispatcher(
    IEnumerable<IOutboxProcessor> processors,
    IOptions<OutboxOptions> options,
    TimeProvider clock,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, options.Value.PollingIntervalSeconds)), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var processor in processors)
            {
                try
                {
                    await processor.ProcessAllAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Outbox of {Context} failed; retrying on the next tick", processor.ContextType.Name);
                }
            }
        }
    }
}

/// <summary>
/// Dispatches the outbox of one module. Rows are claimed with a lease (<c>LockId</c> + <c>LockedUntil</c>), so
/// several API instances can run the dispatcher without double delivery. In database-per-tenant mode it walks
/// every tenant database; each message is dispatched with the tenant context it was written under.
/// </summary>
public sealed class OutboxProcessor<TContext>(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    IOptions<TenancyOptions> tenancy,
    TimeProvider clock,
    ILogger<OutboxProcessor<TContext>> logger) : IOutboxProcessor
    where TContext : ModuleDbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ConcurrentDictionary<string, Type?> TypeCache = new();

    public Type ContextType => typeof(TContext);

    /// <summary>Processes pending messages once. Public for tests and for on-demand flushing.</summary>
    public async Task ProcessAllAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<Project.Application.Organization.IOrganizationContext>().InitializeAsync(ct);
        var registration = scope.ServiceProvider.GetServices<ModuleDbContextRegistration>()
            .First(r => r.ContextType == typeof(TContext));

        var walkTenants = registration.Scope == DataScope.Tenant
            && tenancy.Value.Mode is TenancyMode.DatabasePerTenant or TenancyMode.Hybrid;

        if (!walkTenants)
        {
            await ProcessDatabaseAsync(scope.ServiceProvider, tenant: null, ct);
            return;
        }

        var runner = scope.ServiceProvider.GetRequiredService<ITenantJobRunner>();
        await runner.ForEachTenantAsync((sp, tenant, token) => ProcessDatabaseAsync(sp, tenant, token), ct);
    }

    private async Task ProcessDatabaseAsync(IServiceProvider services, TenantInfo? tenant, CancellationToken ct)
    {
        var context = services.GetRequiredService<TContext>();
        var now = clock.GetUtcNow();
        var lockId = Guid.NewGuid();
        var batch = options.Value.BatchSize;

        var candidates = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Attempts < options.Value.MaxAttempts && (m.LockedUntil == null || m.LockedUntil < now))
            .OrderBy(m => m.OccurredAt)
            .Select(m => m.Id)
            .Take(batch)
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            return;
        }

        var leaseUntil = now.AddMinutes(1);
        await context.OutboxMessages
            .Where(m => candidates.Contains(m.Id) && (m.LockedUntil == null || m.LockedUntil < now))
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.LockId, lockId).SetProperty(m => m.LockedUntil, leaseUntil), ct);

        var claimed = await context.OutboxMessages.Where(m => m.LockId == lockId).ToListAsync(ct);
        foreach (var message in claimed)
        {
            await DispatchAsync(message, tenant, ct);
            message.LockId = null;
            message.LockedUntil = null;
        }

        await context.SaveChangesAsync(ct);
    }

    private async Task DispatchAsync(OutboxMessage message, TenantInfo? tenant, CancellationToken ct)
    {
        message.Attempts++;
        try
        {
            var type = TypeCache.GetOrAdd(message.Type, static name => Type.GetType(name, throwOnError: false))
                ?? throw new InvalidOperationException($"Event type '{message.Type}' cannot be loaded.");
            var domainEvent = (IDomainEvent)JsonSerializer.Deserialize(message.Payload, type, JsonOptions)!;

            // Each message runs in its own scope with the tenant it was written under.
            await using var scope = scopeFactory.CreateAsyncScope();
            var setter = scope.ServiceProvider.GetRequiredService<ITenantSetter>();
            var info = await scope.ServiceProvider.GetRequiredService<ITenantStore>().FindByIdAsync(message.TenantId, ct) ?? tenant;
            if (info is not null)
            {
                setter.Set(info);
                await scope.ServiceProvider.GetRequiredService<ITenantConnectionResolver>().PrepareAsync(info, ct);
            }

            await scope.ServiceProvider.GetRequiredService<Project.Application.Organization.IOrganizationContext>().InitializeAsync(ct);
            await scope.ServiceProvider.GetRequiredService<IEventBus>().PublishAsync(domainEvent, ct);
            message.ProcessedAt = clock.GetUtcNow();
            message.LastError = null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            message.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            logger.LogWarning(ex, "Outbox message {MessageId} ({Type}) failed on attempt {Attempt}", message.Id, message.Type, message.Attempts);
        }
    }
}

/// <summary>In-process event bus: resolves every <see cref="IDomainEventHandler{TEvent}"/> of the event type.</summary>
public sealed class InProcessEventBus(IServiceProvider services) : IEventBus
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IDomainEvent, CancellationToken, Task>> Dispatchers = new();

    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct) =>
        Dispatchers.GetOrAdd(domainEvent.GetType(), BuildDispatcher)(services, domainEvent, ct);

    private static Func<IServiceProvider, IDomainEvent, CancellationToken, Task> BuildDispatcher(Type eventType)
    {
        var method = typeof(InProcessEventBus).GetMethod(nameof(DispatchAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(eventType);
        return (Func<IServiceProvider, IDomainEvent, CancellationToken, Task>)Delegate.CreateDelegate(
            typeof(Func<IServiceProvider, IDomainEvent, CancellationToken, Task>), method);
    }

    private static async Task DispatchAsync<TEvent>(IServiceProvider services, IDomainEvent domainEvent, CancellationToken ct)
        where TEvent : IDomainEvent
    {
        foreach (var handler in services.GetServices<IDomainEventHandler<TEvent>>())
        {
            await handler.HandleAsync((TEvent)domainEvent, ct);
        }
    }
}
