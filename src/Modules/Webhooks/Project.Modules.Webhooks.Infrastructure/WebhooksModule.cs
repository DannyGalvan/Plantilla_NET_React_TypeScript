using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Application;
using Project.Application.Abstractions;
using Project.Application.Security;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Modules.Webhooks.Application;
using Project.Modules.Webhooks.Contracts;
using Project.Modules.Webhooks.Domain;
using Project.Web.Http;
using Project.Web.Modules;
using Project.Web.Security;

namespace Project.Modules.Webhooks.Infrastructure;

public sealed class WebhooksOptions
{
    public const string SectionName = "Webhooks";

    public bool Enabled { get; set; } = true;

    public int PollingIntervalSeconds { get; set; } = 5;

    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Allows endpoints on private, loopback or link-local addresses. Off by default: a tenant could otherwise make the
    /// server call internal services or the cloud metadata endpoint (SSRF). Turn on only for development.
    /// </summary>
    public bool AllowPrivateNetworks { get; set; }
}

public sealed class WebhooksDbContext(DbContextOptions<WebhooksDbContext> options, ModuleDbContextDependencies dependencies)
    : ModuleDbContext(options, dependencies)
{
    public override string Schema => "webhooks";

    public DbSet<WebhookSubscription> Subscriptions => Set<WebhookSubscription>();

    public DbSet<WebhookDelivery> Deliveries => Set<WebhookDelivery>();
}

internal sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(128).IsRequired();
        builder.Property(s => s.Url).HasMaxLength(1024).IsRequired();
        builder.Property(s => s.Events).HasMaxLength(2000).IsRequired();
        builder.Property(s => s.ProtectedSecret).IsRequired();
        builder.Property(s => s.CreatedBy).HasMaxLength(64);
        builder.Property(s => s.UpdatedBy).HasMaxLength(64);
        builder.Property(s => s.DeletedBy).HasMaxLength(64);
        builder.HasIndex(s => new { s.TenantId, s.IsActive });
    }
}

internal sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("Deliveries");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Event).HasMaxLength(128).IsRequired();
        builder.Property(d => d.Payload).IsRequired();
        builder.Property(d => d.Status).HasMaxLength(16).IsRequired();
        builder.Property(d => d.LastError).HasMaxLength(1000);
        builder.HasIndex(d => new { d.TenantId, d.Status, d.NextAttemptAt });
        builder.HasIndex(d => new { d.TenantId, d.SubscriptionId, d.CreatedAt });
    }
}

/// <summary>Only absolute https URLs (http when private networks are allowed), without credentials or private literal hosts.</summary>
public sealed class WebhookUrlPolicy(IOptions<WebhooksOptions> options) : IWebhookUrlPolicy
{
    public string? Validate(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
        {
            return "The URL must be absolute and use https.";
        }

        if (!options.Value.AllowPrivateNetworks)
        {
            if (uri.Scheme != "https")
            {
                return "The URL must use https.";
            }

            if (uri.IsLoopback || (IPAddress.TryParse(uri.Host, out var ip) && !SafeNetwork.IsPublic(ip)))
            {
                return "The URL points to a private network.";
            }
        }

        return string.IsNullOrEmpty(uri.UserInfo) ? null : "The URL must not contain credentials.";
    }
}

/// <summary>Rejects private, loopback, link-local and reserved addresses after DNS resolution (no DNS rebinding bypass).</summary>
public static class SafeNetwork
{
    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6None))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return !(b[0] == 0 || b[0] == 10 || b[0] == 127 || b[0] >= 224
                || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)
                || (b[0] == 169 && b[1] == 254)
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168)
                || (b[0] == 192 && b[1] == 0 && b[2] == 0)
                || (b[0] == 198 && (b[1] == 18 || b[1] == 19)));
        }

        return !(address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast || address.IsIPv6UniqueLocal);
    }

    /// <summary>Connects only to public addresses unless <paramref name="allowPrivate"/>.</summary>
    public static SocketsHttpHandler Handler(bool allowPrivate) => new()
    {
        AllowAutoRedirect = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        ConnectCallback = async (context, ct) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
            var allowed = allowPrivate ? addresses : addresses.Where(IsPublic).ToArray();
            if (allowed.Length == 0)
            {
                throw new HttpRequestException($"The host {context.DnsEndPoint.Host} resolves to a private network.");
            }

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        },
    };
}

/// <summary>
/// Posts a delivery signed like Stripe: <c>X-Webhook-Signature: t={unix},v1={hex}</c> where v1 is
/// HMAC-SHA256(secret, "{t}.{body}"). Receivers recompute it and reject old timestamps (replay).
/// </summary>
public sealed class WebhookSender(IHttpClientFactory http, ISecretProtector protector, IOptions<WebhooksOptions> options, TimeProvider clock)
{
    public const string HttpClientName = "webhooks";

    public async Task<(bool Ok, int? Status, string? Error)> SendAsync(WebhookSubscription subscription, WebhookDelivery delivery, CancellationToken ct)
    {
        var secret = protector.Unprotect(WebhookPayloads.SecretPurpose, subscription.ProtectedSecret);
        if (secret is null)
        {
            return (false, null, "The signing secret cannot be decrypted.");
        }

        var timestamp = clock.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url)
        {
            Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Webhook-Id", delivery.Id.ToString());
        request.Headers.Add("X-Webhook-Event", delivery.Event);
        request.Headers.Add("X-Webhook-Signature", $"t={timestamp},v1={Sign(secret, timestamp, delivery.Payload)}");
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Project-Webhooks", "1.0"));

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, options.Value.TimeoutSeconds)));
        try
        {
            using var response = await http.CreateClient(HttpClientName).SendAsync(request, timeout.Token);
            return response.IsSuccessStatusCode ? (true, (int)response.StatusCode, null) : (false, (int)response.StatusCode, $"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            return (false, null, ex.Message);
        }
    }

    public static string Sign(string secret, string timestamp, string body) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{body}")));
}

/// <summary>Sends the due deliveries of the current tenant, claiming each one with a lease.</summary>
public sealed class DueDeliveriesExecutor(WebhooksDbContext db, WebhookSender sender, TimeProvider clock)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var due = await db.Deliveries.AsNoTracking()
            .Where(d => d.Status == WebhookDelivery.Statuses.Pending && d.NextAttemptAt <= now && (d.LockedUntil == null || d.LockedUntil < now))
            .OrderBy(d => d.NextAttemptAt)
            .Select(d => d.Id)
            .Take(50)
            .ToListAsync(ct);

        foreach (var id in due)
        {
            var lockId = Guid.NewGuid();
            var claimed = await db.Deliveries
                .Where(d => d.Id == id && d.Status == WebhookDelivery.Statuses.Pending && (d.LockedUntil == null || d.LockedUntil < now))
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.LockId, lockId).SetProperty(d => d.LockedUntil, now.AddMinutes(2)), ct);
            if (claimed == 0)
            {
                continue;
            }

            db.ChangeTracker.Clear();
            var delivery = await db.Deliveries.FirstAsync(d => d.Id == id, ct);
            var subscription = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == delivery.SubscriptionId, ct);
            if (subscription is null || !subscription.IsActive)
            {
                delivery.Failed(null, "The subscription was deleted or paused.", clock.GetUtcNow());
            }
            else
            {
                var (ok, status, error) = await sender.SendAsync(subscription, delivery, ct);
                if (ok)
                {
                    delivery.Succeeded(status!.Value, clock.GetUtcNow());
                }
                else
                {
                    delivery.Failed(status, error ?? "Failed", clock.GetUtcNow());
                }
            }

            await db.SaveChangesAsync(ct);
        }
    }
}

/// <summary>Delivers webhooks of every tenant every few seconds.</summary>
public sealed class WebhookDeliveryWorker(ITenantJobRunner tenants, IOptions<WebhooksOptions> options, TimeProvider clock, ILogger<WebhookDeliveryWorker> logger)
    : BackgroundService
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
            try
            {
                await tenants.ForEachTenantAsync((sp, _, ct) => sp.GetRequiredService<DueDeliveriesExecutor>().RunAsync(ct), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "The webhook delivery tick failed; retrying on the next tick");
            }
        }
    }
}

public sealed record SaveSubscriptionRequest(string Name, string Url, List<string> Events, bool IsActive = true);

/// <summary>
/// Outgoing webhooks: tenants subscribe their endpoints to the public events declared by the modules
/// (<c>AddWebhookEvent</c>); deliveries are signed, retried with backoff and logged.
/// </summary>
public sealed class WebhooksModule : IModule
{
    public string Name => "Webhooks";

    public DataScope DataScope => DataScope.Tenant;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<WebhooksDbContext>(Name, DataScope);
        services.AddRepositoriesFor<WebhooksDbContext>();
        services.AddModuleConventions(typeof(WebhooksPermissionProvider).Assembly);
        services.AddOptions<WebhooksOptions>().Bind(configuration.GetSection(WebhooksOptions.SectionName));
        services.AddScoped<IDomainEventObserver, WebhookEventObserver>();
        services.AddSingleton<IWebhookUrlPolicy, WebhookUrlPolicy>();
        services.AddSingleton<WebhookSender>();
        services.AddScoped<DueDeliveriesExecutor>();
        services.AddHostedService<WebhookDeliveryWorker>();
        services.AddHttpClient(WebhookSender.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(sp => SafeNetwork.Handler(sp.GetRequiredService<IOptions<WebhooksOptions>>().Value.AllowPrivateNetworks));
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var webhooks = api.MapGroup("webhooks").WithTags("webhooks").RequirePermission(WebhooksPermissions.Subscriptions.Manage);
        webhooks.MapGet("/events", async (ISender sender, CancellationToken ct) => (await sender.Send(new ListWebhookEventsQuery(), ct)).ToHttp())
            .WithName("ListWebhookEvents");
        webhooks.MapGet("/subscriptions", async (ISender sender, CancellationToken ct) => (await sender.Send(new ListSubscriptionsQuery(), ct)).ToHttp())
            .WithName("ListWebhookSubscriptions");
        webhooks.MapPost("/subscriptions", async (SaveSubscriptionRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SaveSubscriptionCommand(null, request.Name, request.Url, request.Events, request.IsActive), ct)).ToHttp())
            .WithName("CreateWebhookSubscription");
        webhooks.MapPut("/subscriptions/{id:guid}", async (Guid id, SaveSubscriptionRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SaveSubscriptionCommand(id, request.Name, request.Url, request.Events, request.IsActive), ct);
                return result.IsFailure ? result.ToHttp() : Results.Ok(result.Value.Subscription);
            })
            .WithName("UpdateWebhookSubscription");
        webhooks.MapDelete("/subscriptions/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new DeleteSubscriptionCommand(id), ct)).ToHttp())
            .WithName("DeleteWebhookSubscription");
        webhooks.MapPost("/subscriptions/{id:guid}/ping", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new PingSubscriptionCommand(id), ct)).ToHttp())
            .WithName("PingWebhookSubscription");
        webhooks.MapGet("/subscriptions/{id:guid}/deliveries", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListDeliveriesQuery(id), ct)).ToHttp())
            .WithName("ListWebhookDeliveries");
        webhooks.MapPost("/deliveries/{id:guid}/retry", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RetryDeliveryCommand(id), ct)).ToHttp())
            .WithName("RetryWebhookDelivery");
    }
}
