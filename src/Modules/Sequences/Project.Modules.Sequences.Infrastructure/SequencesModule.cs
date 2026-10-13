using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Application;
using Project.Application.Organization;
using Project.Application.Sequences;
using Project.Application.Settings;
using Project.Application.Tenancy;
using Project.Infrastructure.Persistence;
using Project.Modules.Sequences.Application;
using Project.Modules.Sequences.Contracts;
using Project.Modules.Sequences.Domain;
using Project.Web.Http;
using Project.Web.Modules;
using Project.Web.Security;

namespace Project.Modules.Sequences.Infrastructure;

public sealed class SequencesDbContext(DbContextOptions<SequencesDbContext> options, ModuleDbContextDependencies dependencies)
    : ModuleDbContext(options, dependencies)
{
    public override string Schema => "sequences";

    public DbSet<SequenceCounter> Counters => Set<SequenceCounter>();

    public DbSet<SequenceFormat> Formats => Set<SequenceFormat>();
}

internal sealed class SequenceCounterConfiguration : IEntityTypeConfiguration<SequenceCounter>
{
    public void Configure(EntityTypeBuilder<SequenceCounter> builder)
    {
        builder.ToTable("Counters");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(128).IsRequired();
        builder.Property(c => c.ScopeKey).HasMaxLength(32).IsRequired();
        builder.Property(c => c.Period).HasMaxLength(8).IsRequired();
        builder.HasIndex(c => new { c.TenantId, c.Name, c.ScopeKey, c.Period }).IsUnique();
    }
}

internal sealed class SequenceFormatConfiguration : IEntityTypeConfiguration<SequenceFormat>
{
    public void Configure(EntityTypeBuilder<SequenceFormat> builder)
    {
        builder.ToTable("Formats");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).HasMaxLength(128).IsRequired();
        builder.Property(f => f.Format).HasMaxLength(64).IsRequired();
        builder.Property(f => f.CreatedBy).HasMaxLength(64);
        builder.Property(f => f.UpdatedBy).HasMaxLength(64);
        builder.Property(f => f.DeletedBy).HasMaxLength(64);
        builder.HasIndex(f => new { f.TenantId, f.Name });
    }
}

/// <summary>
/// Increments the counter with a single conditional UPDATE (the row lock serializes concurrent callers on every
/// engine) and reads it back in the same transaction; the first number of a period inserts the row, and a unique
/// index resolves the race between two first callers.
/// </summary>
public sealed class SequenceGenerator(
    SequencesDbContext db,
    IEnumerable<SequenceDefinition> definitions,
    IOrganizationContext organization,
    ISettingsProvider settings,
    TimeProvider clock) : ISequenceGenerator
{
    public async Task<string> NextAsync(string sequence, CancellationToken ct)
    {
        var definition = definitions.FirstOrDefault(d => d.Name == sequence)
            ?? throw new InvalidOperationException($"The sequence '{sequence}' is not declared.");
        var date = await LocalNowAsync(ct);
        var period = SequenceFormatter.Period(definition.Reset, date);
        var scope = definition.PerOrganizationUnit ? organization.ActiveUnitId?.ToString("N") ?? string.Empty : string.Empty;
        var format = await db.Formats.AsNoTracking().Where(f => f.Name == sequence).Select(f => f.Format).FirstOrDefaultAsync(ct) ?? definition.Format;

        var value = await db.Database.CreateExecutionStrategy().ExecuteAsync(async token =>
        {
            for (var attempt = 0; ; attempt++)
            {
                await using var transaction = await db.Database.BeginTransactionAsync(token);
                var updated = await Counter(sequence, scope, period).ExecuteUpdateAsync(s => s.SetProperty(c => c.Value, c => c.Value + 1), token);
                if (updated == 0)
                {
                    db.Counters.Add(SequenceCounter.Start(sequence, scope, period));
                    try
                    {
                        await db.SaveChangesAsync(token);
                    }
                    catch (DbUpdateException) when (attempt < 3)
                    {
                        // Another caller created the counter first: increment it instead.
                        db.ChangeTracker.Clear();
                        await transaction.RollbackAsync(token);
                        continue;
                    }
                    finally
                    {
                        db.ChangeTracker.Clear();
                    }
                }

                var current = await Counter(sequence, scope, period).Select(c => c.Value).SingleAsync(token);
                await transaction.CommitAsync(token);
                return current;
            }
        }, ct);

        return SequenceFormatter.Format(format, value, date);
    }

    private IQueryable<SequenceCounter> Counter(string name, string scope, string period) =>
        db.Counters.Where(c => c.Name == name && c.ScopeKey == scope && c.Period == period);

    /// <summary>Periods follow the calendar of the tenant (a yearly sequence resets at local midnight of January 1).</summary>
    private async Task<DateTimeOffset> LocalNowAsync(CancellationToken ct)
    {
        var zoneId = (await settings.GetAsync<LocalizationSettings>(ct)).TimeZone;
        var zone = TimeZoneInfo.TryFindSystemTimeZoneById(zoneId, out var found) ? found : TimeZoneInfo.Utc;
        return TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone);
    }
}

public sealed record UpdateSequenceFormatRequest(string? Format);

/// <summary>Document numbering for every module (<c>AddSequence</c> + <see cref="ISequenceGenerator"/>).</summary>
public sealed class SequencesModule : IModule
{
    public string Name => "Sequences";

    public DataScope DataScope => DataScope.Tenant;

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<SequencesDbContext>(Name, DataScope);
        services.AddRepositoriesFor<SequencesDbContext>();
        services.AddModuleConventions(typeof(SequencesPermissionProvider).Assembly);
        services.AddScoped<ISequenceGenerator, SequenceGenerator>();
    }

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var sequences = api.MapGroup("sequences").WithTags("sequences").RequirePermission(SequencesPermissions.Sequences.Manage);
        sequences.MapGet("/", async (ISender sender, CancellationToken ct) => (await sender.Send(new ListSequencesQuery(), ct)).ToHttp())
            .WithName("ListSequences");
        sequences.MapPut("/{name}", async (string name, UpdateSequenceFormatRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateSequenceFormatCommand(name, request.Format), ct)).ToHttp())
            .WithName("UpdateSequenceFormat");
    }
}
