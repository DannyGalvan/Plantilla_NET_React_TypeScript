using FluentValidation;
using Project.Application;
using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Querying;
using Project.Modules.Tenancy.Domain;
using Project.SharedKernel;

namespace Project.Modules.Tenancy.Application.Tenants;

public sealed record TenantDomainDto(Guid Id, string Host, bool IsPrimary);

public sealed record TenantFeatureDto(string Feature, bool Enabled);

/// <summary>Tenant as returned by the API. The connection secret reference is never exposed.</summary>
public sealed record TenantDto(
    Guid Id,
    string Code,
    string Name,
    TenantState Status,
    DatabaseIsolation Isolation,
    string? DatabaseProvider,
    string? Plan,
    bool HasDedicatedConnection,
    IReadOnlyList<TenantDomainDto> Domains,
    IReadOnlyList<TenantFeatureDto> Features,
    DateTimeOffset CreatedAt,
    Guid ConcurrencyStamp)
{
    public static TenantDto From(Tenant tenant) => new(
        tenant.Id,
        tenant.Code,
        tenant.Name,
        tenant.Status,
        tenant.Isolation,
        tenant.DatabaseProvider,
        tenant.Plan,
        !string.IsNullOrEmpty(tenant.ConnectionSecretRef),
        tenant.Domains.Select(d => new TenantDomainDto(d.Id, d.Host, d.IsPrimary)).OrderByDescending(d => d.IsPrimary).ToList(),
        tenant.Features.Select(f => new TenantFeatureDto(f.Feature, f.Enabled)).OrderBy(f => f.Feature).ToList(),
        tenant.CreatedAt,
        tenant.ConcurrencyStamp);
}

public sealed class TenantQueryPolicy : QueryPolicy<Tenant>
{
    public TenantQueryPolicy()
    {
        Field(t => t.Code).Field(t => t.Name).Field(t => t.Status).Field(t => t.Isolation).Field(t => t.Plan).Field(t => t.CreatedAt);
        DefaultOrder(t => t.Name);
    }
}

internal static class TenantSpecs
{
    public static Specification<Tenant> WithChildren() => new Specification<Tenant>().Include(nameof(Tenant.Domains)).Include(nameof(Tenant.Features));

    public static Specification<Tenant> ById(Guid id) => WithChildren().Where(t => t.Id == id).Tracked();
}

// ----- Queries -----

public sealed record ListTenantsQuery(PageRequest Page) : IQuery<PagedResult<TenantDto>>;

public sealed class ListTenantsHandler(IRepository<Tenant, Guid> tenants, QueryParser parser, IQueryPolicy<Tenant> policy)
    : IQueryHandler<ListTenantsQuery, PagedResult<TenantDto>>
{
    public async ValueTask<Result<PagedResult<TenantDto>>> Handle(ListTenantsQuery query, CancellationToken cancellationToken)
    {
        var filter = parser.ParseFilter(query.Page.Filter, policy);
        if (filter.IsFailure)
        {
            return filter.Error!;
        }

        var sort = QueryParser.ParseSort(query.Page.Sort, policy);
        if (sort.IsFailure)
        {
            return sort.Error!;
        }

        var spec = TenantSpecs.WithChildren();
        if (filter.Value is { } predicate)
        {
            spec.Where(predicate);
        }

        foreach (var (selector, descending) in sort.Value)
        {
            spec.OrderBy(selector, descending);
        }

        var size = query.Page.NormalizedPageSize(PaginationDefaults.MaxPageSize);
        var total = await tenants.CountAsync(spec.WithoutPaging(), cancellationToken);
        spec.OrderBy(t => t.Id).Page((query.Page.NormalizedPage - 1) * size, size);
        var items = await tenants.ListAsync(spec, cancellationToken);
        return new PagedResult<TenantDto>(items.Select(TenantDto.From).ToList(), query.Page.NormalizedPage, size, total);
    }
}

public sealed record GetTenantQuery(Guid Id) : IQuery<TenantDto>;

public sealed class GetTenantHandler(IRepository<Tenant, Guid> tenants) : IQueryHandler<GetTenantQuery, TenantDto>
{
    public async ValueTask<Result<TenantDto>> Handle(GetTenantQuery query, CancellationToken cancellationToken)
    {
        var tenant = await tenants.FirstOrDefaultAsync(TenantSpecs.WithChildren().Where(t => t.Id == query.Id), cancellationToken);
        return tenant is null ? Error.NotFound("tenant.not_found") : TenantDto.From(tenant);
    }
}

// ----- Commands -----

public sealed record CreateTenantCommand(
    string Code,
    string Name,
    DatabaseIsolation Isolation,
    string? DatabaseProvider,
    string? ConnectionSecretRef,
    string? Plan,
    string? PrimaryDomain) : ICommand<TenantDto>;

public sealed class CreateTenantValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantValidator()
    {
        RuleFor(c => c.Code).NotEmpty().MaximumLength(64).Matches("^[a-z0-9]([a-z0-9-]*[a-z0-9])?$")
            .WithMessage("Use lower case letters, digits and dashes.");
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.ConnectionSecretRef).NotEmpty().When(c => c.Isolation == DatabaseIsolation.Dedicated)
            .WithMessage("Dedicated tenants need the reference of the secret that holds the connection string.");
        RuleFor(c => c.DatabaseProvider).Must(p => p is null or "SqlServer" or "PostgreSql" or "MySql")
            .WithMessage("Use SqlServer, PostgreSql or MySql.");
        RuleFor(c => c.PrimaryDomain).MaximumLength(253);
    }
}

public sealed class CreateTenantHandler(IRepository<Tenant, Guid> tenants) : ICommandHandler<CreateTenantCommand, TenantDto>
{
    public async ValueTask<Result<TenantDto>> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var code = command.Code.Trim().ToLowerInvariant();
        if (await tenants.AnyAsync(new Specification<Tenant>().Where(t => t.Code == code), cancellationToken))
        {
            return Error.Conflict("tenant.code_exists", "A tenant with this code already exists.");
        }

        var tenant = Tenant.Create(code, command.Name, command.Isolation, command.DatabaseProvider, command.ConnectionSecretRef, command.Plan);
        if (!string.IsNullOrWhiteSpace(command.PrimaryDomain))
        {
            tenant.AddDomain(command.PrimaryDomain, isPrimary: true);
        }

        tenants.Add(tenant);
        await tenants.UnitOfWork.SaveChangesAsync(cancellationToken);
        return TenantDto.From(tenant);
    }
}

public sealed record UpdateTenantCommand(Guid Id, string Name, string? Plan) : ICommand<TenantDto>;

public sealed class UpdateTenantValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantValidator() => RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
}

public sealed class UpdateTenantHandler(IRepository<Tenant, Guid> tenants) : ICommandHandler<UpdateTenantCommand, TenantDto>
{
    public async ValueTask<Result<TenantDto>> Handle(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenants.FirstOrDefaultAsync(TenantSpecs.ById(command.Id), cancellationToken);
        if (tenant is null)
        {
            return Error.NotFound("tenant.not_found");
        }

        tenant.Update(command.Name, command.Plan);
        await tenants.UnitOfWork.SaveChangesAsync(cancellationToken);
        return TenantDto.From(tenant);
    }
}

public sealed record ChangeTenantStatusCommand(Guid Id, TenantState Status) : ICommand<TenantDto>;

public sealed class ChangeTenantStatusHandler(IRepository<Tenant, Guid> tenants) : ICommandHandler<ChangeTenantStatusCommand, TenantDto>
{
    public async ValueTask<Result<TenantDto>> Handle(ChangeTenantStatusCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenants.FirstOrDefaultAsync(TenantSpecs.ById(command.Id), cancellationToken);
        if (tenant is null)
        {
            return Error.NotFound("tenant.not_found");
        }

        var changed = tenant.ChangeStatus(command.Status);
        if (changed.IsFailure)
        {
            return changed.Error!;
        }

        await tenants.UnitOfWork.SaveChangesAsync(cancellationToken);
        return TenantDto.From(tenant);
    }
}

public sealed record AddTenantDomainCommand(Guid Id, string Host, bool IsPrimary) : ICommand<TenantDto>;

public sealed class AddTenantDomainValidator : AbstractValidator<AddTenantDomainCommand>
{
    public AddTenantDomainValidator() =>
        RuleFor(c => c.Host).NotEmpty().MaximumLength(253).Matches(@"^[a-zA-Z0-9]([a-zA-Z0-9.-]*[a-zA-Z0-9])?$")
            .WithMessage("Use a host name without scheme or port, for example erp.acme.com.");
}

public sealed class AddTenantDomainHandler(IRepository<Tenant, Guid> tenants) : ICommandHandler<AddTenantDomainCommand, TenantDto>
{
    public async ValueTask<Result<TenantDto>> Handle(AddTenantDomainCommand command, CancellationToken cancellationToken)
    {
        var host = command.Host.Trim().ToLowerInvariant();
        if (await tenants.AnyAsync(new Specification<Tenant>().Where(t => t.Domains.Any(d => d.Host == host)), cancellationToken))
        {
            return Error.Conflict("tenant.domain_exists", "The domain is already assigned to a tenant.");
        }

        var tenant = await tenants.FirstOrDefaultAsync(TenantSpecs.ById(command.Id), cancellationToken);
        if (tenant is null)
        {
            return Error.NotFound("tenant.not_found");
        }

        var added = tenant.AddDomain(host, command.IsPrimary);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        await tenants.UnitOfWork.SaveChangesAsync(cancellationToken);
        return TenantDto.From(tenant);
    }
}

public sealed record RemoveTenantDomainCommand(Guid Id, Guid DomainId) : ICommand<TenantDto>;

public sealed class RemoveTenantDomainHandler(IRepository<Tenant, Guid> tenants) : ICommandHandler<RemoveTenantDomainCommand, TenantDto>
{
    public async ValueTask<Result<TenantDto>> Handle(RemoveTenantDomainCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenants.FirstOrDefaultAsync(TenantSpecs.ById(command.Id), cancellationToken);
        if (tenant is null)
        {
            return Error.NotFound("tenant.not_found");
        }

        var removed = tenant.RemoveDomain(command.DomainId);
        if (removed.IsFailure)
        {
            return removed.Error!;
        }

        await tenants.UnitOfWork.SaveChangesAsync(cancellationToken);
        return TenantDto.From(tenant);
    }
}

public sealed record SetTenantFeatureCommand(Guid Id, string Feature, bool Enabled) : ICommand<TenantDto>;

public sealed class SetTenantFeatureValidator : AbstractValidator<SetTenantFeatureCommand>
{
    public SetTenantFeatureValidator() => RuleFor(c => c.Feature).NotEmpty().MaximumLength(128).Matches("^[a-z0-9.-]+$");
}

public sealed class SetTenantFeatureHandler(IRepository<Tenant, Guid> tenants) : ICommandHandler<SetTenantFeatureCommand, TenantDto>
{
    public async ValueTask<Result<TenantDto>> Handle(SetTenantFeatureCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenants.FirstOrDefaultAsync(TenantSpecs.ById(command.Id), cancellationToken);
        if (tenant is null)
        {
            return Error.NotFound("tenant.not_found");
        }

        tenant.SetFeature(command.Feature, command.Enabled);
        await tenants.UnitOfWork.SaveChangesAsync(cancellationToken);
        return TenantDto.From(tenant);
    }
}

/// <summary>Creates and migrates the dedicated database of a tenant, runs its seeders and activates it.</summary>
public interface ITenantProvisioner
{
    Task<Result> ProvisionAsync(Guid tenantId, CancellationToken ct);
}

public sealed record ProvisionTenantCommand(Guid Id) : ICommand<TenantDto>;

public sealed class ProvisionTenantHandler(ITenantProvisioner provisioner, Mediator.ISender sender)
    : ICommandHandler<ProvisionTenantCommand, TenantDto>
{
    public async ValueTask<Result<TenantDto>> Handle(ProvisionTenantCommand command, CancellationToken cancellationToken)
    {
        var provisioned = await provisioner.ProvisionAsync(command.Id, cancellationToken);
        return provisioned.IsFailure ? provisioned.Error! : await sender.Send(new GetTenantQuery(command.Id), cancellationToken);
    }
}
