using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Project.Application.Abstractions;
using Project.Application.Crud;
using Project.Application.Querying;
using Project.Application.Security;
using Project.SharedKernel.Domain;

namespace Project.Application;

public static class DependencyInjection
{
    /// <summary>Registers the query parser, the default filter operators and CRUD options.</summary>
    public static IServiceCollection AddApplicationCore(this IServiceCollection services, IConfiguration configuration)
    {
        foreach (var @operator in FilterOperators.Defaults())
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton(@operator));
        }

        services.TryAddSingleton<QueryParser>();
        services.AddOptions<CrudOptions>().Bind(configuration.GetSection(CrudOptions.SectionName));
        services.AddConfigurableSection<CrudOptions>(CrudOptions.SectionName, "platform", "Tamaño máximo de página de los listados.");
        return services;
    }

    /// <summary>
    /// Discovers the conventions of a module assembly so adding a resource never edits a central file:
    /// FluentValidation validators, CRUD mappers (and their <see cref="ICrudService{TId,TDto,TCreate,TUpdate}"/>),
    /// query policies, permission providers and domain event handlers.
    /// </summary>
    public static IServiceCollection AddModuleConventions(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddValidatorsFromAssemblies(assemblies, ServiceLifetime.Scoped, includeInternalTypes: true);

        foreach (var type in assemblies.SelectMany(SafeTypes).Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }))
        {
            foreach (var contract in type.GetInterfaces().Where(i => i.IsGenericType))
            {
                var definition = contract.GetGenericTypeDefinition();

                if (definition == typeof(ICrudMapper<,,,>))
                {
                    RegisterCrud(services, type, contract);
                }
                else if (definition == typeof(IQueryPolicy<>))
                {
                    services.TryAddSingleton(contract, type);
                }
                else if (definition == typeof(IDomainEventHandler<>))
                {
                    services.TryAddEnumerable(ServiceDescriptor.Scoped(contract, type));
                }
            }

            if (typeof(IPermissionProvider).IsAssignableFrom(type))
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IPermissionProvider), type));
            }
        }

        return services;
    }

    private static void RegisterCrud(IServiceCollection services, Type mapperType, Type mapperContract)
    {
        var args = mapperContract.GetGenericArguments(); // TEntity, TDto, TCreate, TUpdate
        var entity = args[0];
        var idType = FindIdType(entity)
            ?? throw new InvalidOperationException($"{entity.Name} must derive from AggregateRoot<TId> to use the CRUD engine.");

        services.TryAddSingleton(mapperContract, mapperType);
        services.TryAddScoped(
            typeof(ICrudService<,,,>).MakeGenericType(idType, args[1], args[2], args[3]),
            typeof(CrudService<,,,,>).MakeGenericType(entity, idType, args[1], args[2], args[3]));
    }

    private static Type? FindIdType(Type entity)
    {
        for (var current = entity; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return current.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
