using System.Text.Json;
using System.Text.Json.Nodes;
using Project.Application.Abstractions;
using Project.Application.Messaging;
using Project.Application.Organization;
using Project.Application.Settings;
using Project.Application.Tenancy;
using Project.Modules.Tenancy.Domain;
using Project.SharedKernel;

namespace Project.Modules.Tenancy.Application.Settings;

public sealed record SettingsSectionInfo(string Key, bool AllowUserOverride, JsonObject Defaults);

/// <summary>Override stored at one scope and the effective (merged) value for the caller.</summary>
public sealed record SettingsValueDto(string Section, SettingScope Scope, Guid? ScopeId, JsonObject Overrides, JsonObject Effective);

/// <summary>JSON helpers shared by the settings provider and the use cases.</summary>
public static class SettingsJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Copies only properties that exist in <paramref name="section"/>, so unknown keys are discarded.</summary>
    public static JsonObject Sanitize(JsonObject values, SettingsSectionDescriptor section)
    {
        var template = JsonSerializer.SerializeToNode(section.CreateDefault(), section.Type, Options)!.AsObject();
        var result = new JsonObject();
        foreach (var (key, value) in values)
        {
            var match = template.Select(p => p.Key).FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                result[match] = value?.DeepClone();
            }
        }

        return result;
    }

    /// <summary>Applies <paramref name="overrides"/> onto <paramref name="target"/> (per property, explicit nulls ignored).</summary>
    public static void Merge(JsonObject target, JsonObject overrides)
    {
        foreach (var (key, value) in overrides)
        {
            if (value is not null)
            {
                target[key] = value.DeepClone();
            }
        }
    }
}

public sealed record GetSettingsSchemaQuery : IQuery<IReadOnlyList<SettingsSectionInfo>>;

public sealed class GetSettingsSchemaHandler(IEnumerable<SettingsSectionDescriptor> sections)
    : IQueryHandler<GetSettingsSchemaQuery, IReadOnlyList<SettingsSectionInfo>>
{
    public ValueTask<Result<IReadOnlyList<SettingsSectionInfo>>> Handle(GetSettingsSchemaQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<SettingsSectionInfo> result = sections
            .OrderBy(s => s.Key)
            .Select(s => new SettingsSectionInfo(s.Key, s.AllowUserOverride, JsonSerializer.SerializeToNode(s.CreateDefault(), s.Type, SettingsJson.Options)!.AsObject()))
            .ToList();
        return ValueTask.FromResult(Result<IReadOnlyList<SettingsSectionInfo>>.Success(result));
    }
}

/// <summary>Reads the effective value of a section and the overrides stored at a scope.</summary>
public interface ISettingsReader
{
    Task<JsonObject> GetEffectiveAsync(SettingsSectionDescriptor section, CancellationToken ct);

    /// <summary>Drops resolved settings so the next read reflects the change on every node.</summary>
    Task InvalidateAsync(CancellationToken ct);
}

public sealed record GetSettingsQuery(string Section, SettingScope Scope, Guid? ScopeId) : IQuery<SettingsValueDto>;

public sealed class GetSettingsHandler(
    IEnumerable<SettingsSectionDescriptor> sections,
    IRepository<SettingValue, Guid> values,
    ISettingsReader reader,
    SettingsScopeGuard guard) : IQueryHandler<GetSettingsQuery, SettingsValueDto>
{
    public async ValueTask<Result<SettingsValueDto>> Handle(GetSettingsQuery query, CancellationToken cancellationToken)
    {
        var section = sections.FirstOrDefault(s => s.Key == query.Section);
        if (section is null)
        {
            return Error.NotFound("settings.section_not_found", $"Section '{query.Section}' does not exist.");
        }

        var scope = await guard.ResolveAsync(query.Scope, query.ScopeId, section, cancellationToken);
        if (scope.IsFailure)
        {
            return scope.Error!;
        }

        var (scopeName, scopeId, tenantId) = scope.Value;
        var stored = await values.FirstOrDefaultAsync(
            new Project.Application.Abstractions.Specification<SettingValue>()
                .Where(v => v.SectionKey == section.Key && v.Scope == scopeName && v.ScopeId == scopeId && v.TenantId == tenantId),
            cancellationToken);

        var overrides = stored is null ? new JsonObject() : JsonNode.Parse(stored.Json)!.AsObject();
        var effective = await reader.GetEffectiveAsync(section, cancellationToken);
        return new SettingsValueDto(section.Key, query.Scope, scopeId, overrides, effective);
    }
}

public sealed record UpdateSettingsCommand(string Section, SettingScope Scope, Guid? ScopeId, JsonObject Values) : ICommand<SettingsValueDto>;

public sealed class UpdateSettingsHandler(
    IEnumerable<SettingsSectionDescriptor> sections,
    IRepository<SettingValue, Guid> values,
    SettingsScopeGuard guard,
    ISettingsValidator validator,
    ISettingsReader reader,
    TimeProvider clock,
    Mediator.ISender sender) : ICommandHandler<UpdateSettingsCommand, SettingsValueDto>
{
    public async ValueTask<Result<SettingsValueDto>> Handle(UpdateSettingsCommand command, CancellationToken cancellationToken)
    {
        var section = sections.FirstOrDefault(s => s.Key == command.Section);
        if (section is null)
        {
            return Error.NotFound("settings.section_not_found", $"Section '{command.Section}' does not exist.");
        }

        var scope = await guard.ResolveAsync(command.Scope, command.ScopeId, section, cancellationToken);
        if (scope.IsFailure)
        {
            return scope.Error!;
        }

        var sanitized = SettingsJson.Sanitize(command.Values, section);
        var invalid = validator.Validate(section, sanitized);
        if (invalid is not null)
        {
            return invalid;
        }

        var (scopeName, scopeId, tenantId) = scope.Value;
        var stored = await values.FirstOrDefaultAsync(
            new Project.Application.Abstractions.Specification<SettingValue>()
                .Where(v => v.SectionKey == section.Key && v.Scope == scopeName && v.ScopeId == scopeId && v.TenantId == tenantId)
                .Tracked(),
            cancellationToken);

        var json = sanitized.ToJsonString(SettingsJson.Options);
        if (stored is null)
        {
            values.Add(SettingValue.Create(scopeName, scopeId, tenantId, section.Key, json, clock.GetUtcNow()));
        }
        else
        {
            stored.Update(json, clock.GetUtcNow());
        }

        await values.UnitOfWork.SaveChangesAsync(cancellationToken);
        await reader.InvalidateAsync(cancellationToken);
        return await sender.Send(new GetSettingsQuery(command.Section, command.Scope, command.ScopeId), cancellationToken);
    }
}

/// <summary>Validates the values of a section (types, colors, ranges). Returns null when valid.</summary>
public interface ISettingsValidator
{
    Error? Validate(SettingsSectionDescriptor section, JsonObject values);
}

/// <summary>
/// Decides which scope id applies and whether the caller may touch it:
/// System requires the platform permission, Tenant and OrganizationUnit work inside the current tenant,
/// User is always the caller and only for sections that allow user overrides.
/// </summary>
public sealed class SettingsScopeGuard(ITenantContext tenant, ICurrentUser user, IOrganizationContext organization, Project.Application.Security.IPermissionService permissions)
{
    public async Task<Result<(string Scope, Guid? ScopeId, Guid? TenantId)>> ResolveAsync(
        SettingScope scope,
        Guid? scopeId,
        SettingsSectionDescriptor section,
        CancellationToken ct)
    {
        var tenantId = tenant.TenantId ?? TenantDefaults.DefaultTenantId;
        switch (scope)
        {
            case SettingScope.System:
                return await permissions.HasPermissionAsync(Contracts.TenancyPermissions.Tenants.Manage, ct)
                    ? ("System", null, null)
                    : Error.Forbidden("settings.system_forbidden", "Only platform administrators can change system settings.");
            case SettingScope.Tenant:
                return ("Tenant", tenantId, tenantId);
            case SettingScope.OrganizationUnit:
                if (scopeId is null)
                {
                    return Error.Validation("scopeId", "The organization unit is required.");
                }

                if (organization.Enabled && !organization.HasFullAccess && !organization.AccessibleUnitIds.Contains(scopeId.Value))
                {
                    return Error.NotFound("organization.unit_not_found");
                }

                return ("OrganizationUnit", scopeId, tenantId);
            case SettingScope.User:
                if (!section.AllowUserOverride)
                {
                    return Error.Validation("scope", $"Section '{section.Key}' cannot be overridden per user.");
                }

                return Guid.TryParse(user.UserId, out var userId)
                    ? ("User", userId, tenantId)
                    : Error.Unauthorized("auth.required", "Authentication is required.");
            default:
                return Error.Validation("scope", "Unknown scope.");
        }
    }
}
