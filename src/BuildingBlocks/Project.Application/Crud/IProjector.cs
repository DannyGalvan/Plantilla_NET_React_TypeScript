namespace Project.Application.Crud;

/// <summary>
/// Source-generated mapping contract (Mapperly). The concrete mapper is a
/// <c>partial class</c> decorated with <c>[Mapper]</c> that the source
/// generator emits as an implementation of this interface. The queryable
/// projection is provided by EF directly (it composes the MapQuery
/// partial method).
/// </summary>
public interface IProjector<TEntity, TDto>
{
    TDto Project(TEntity source);

    IQueryable<TDto> Project(IQueryable<TEntity> source);
}

public interface IMapperTo<TSource, TDestination>
{
    TDestination Map(TSource source);
}