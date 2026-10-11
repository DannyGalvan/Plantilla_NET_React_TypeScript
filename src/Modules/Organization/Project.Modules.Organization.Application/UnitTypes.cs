using FluentValidation;
using Project.Application.Crud;
using Project.Application.Querying;
using Project.Modules.Organization.Domain;
using Riok.Mapperly.Abstractions;

namespace Project.Modules.Organization.Application;

public sealed record UnitTypeDto(Guid Id, string Code, string Name, bool CanBeRoot, string AllowedParentCodes, int SortOrder, Guid ConcurrencyStamp);

public sealed record CreateUnitTypeRequest(string Code, string Name, bool CanBeRoot, string AllowedParentCodes, int SortOrder);

public sealed record UpdateUnitTypeRequest(string Name, bool CanBeRoot, string AllowedParentCodes, int SortOrder, Guid? ConcurrencyStamp) : IConcurrencyAware;

/// <summary>Unit types use the generic CRUD engine: mapper + policy + validators, no handlers.</summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public sealed partial class UnitTypeMapper : ICrudMapper<OrganizationUnitType, UnitTypeDto, CreateUnitTypeRequest, UpdateUnitTypeRequest>
{
    public partial UnitTypeDto ToDto(OrganizationUnitType entity);

    public partial IQueryable<UnitTypeDto> ProjectToDto(IQueryable<OrganizationUnitType> query);

    public OrganizationUnitType ToEntity(CreateUnitTypeRequest input)
    {
        var entity = MapNew(input);
        entity.Code = input.Code.Trim().ToLowerInvariant();
        return entity;
    }

    [MapperIgnoreTarget(nameof(OrganizationUnitType.ConcurrencyStamp))]
    public partial void Apply(UpdateUnitTypeRequest input, [MappingTarget] OrganizationUnitType entity);

    public partial UpdateUnitTypeRequest ToUpdate(OrganizationUnitType entity);

    private partial OrganizationUnitType MapNew(CreateUnitTypeRequest input);
}

public sealed class UnitTypeQueryPolicy : QueryPolicy<OrganizationUnitType>
{
    public UnitTypeQueryPolicy()
    {
        Field(t => t.Code).Field(t => t.Name).Field(t => t.SortOrder).Field(t => t.CanBeRoot);
        DefaultOrder(t => t.SortOrder);
    }
}

public sealed class CreateUnitTypeValidator : AbstractValidator<CreateUnitTypeRequest>
{
    public CreateUnitTypeValidator()
    {
        RuleFor(r => r.Code).NotEmpty().MaximumLength(32).Matches("^[a-zA-Z0-9_-]+$");
        RuleFor(r => r.Name).NotEmpty().MaximumLength(64);
        RuleFor(r => r.AllowedParentCodes).NotNull().MaximumLength(256);
    }
}

public sealed class UpdateUnitTypeValidator : AbstractValidator<UpdateUnitTypeRequest>
{
    public UpdateUnitTypeValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(64);
        RuleFor(r => r.AllowedParentCodes).NotNull().MaximumLength(256);
    }
}
