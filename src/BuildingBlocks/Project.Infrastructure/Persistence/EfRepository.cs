using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions;
using Project.SharedKernel.Domain;

namespace Project.Infrastructure.Persistence;

/// <summary>
/// Generic repository over an EF Core DbContext. Reads and writes share the
/// same context (and therefore the same transaction).
/// </summary>
public sealed class EfRepository<TContext, T, TId> : IRepository<T, TId>
    where TContext : ModuleDbContext
    where T : AggregateRoot<TId>
    where TId : notnull
{
    private readonly TContext _context;

    public EfRepository(TContext context)
    {
        _context = context;
    }

    public Task<T?> GetByIdAsync(TId id, CancellationToken ct) =>
        _context.Set<T>().FirstOrDefaultAsync(e => e.Id.Equals(id), ct);

    public Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken ct) =>
        SpecificationEvaluator<T>.GetQuery(_context.Set<T>().AsQueryable(), specification)
            .FirstOrDefaultAsync(ct);

    public Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken ct) =>
        SpecificationEvaluator<T>.GetQuery(_context.Set<T>().AsQueryable(), specification)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<T>)t.Result, ct);

    public Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken ct) =>
        SpecificationEvaluator<T>.GetQuery(_context.Set<T>().AsQueryable(), specification)
            .AnyAsync(ct);

    public void Add(T entity) => _context.Add(entity);
    public void Update(T entity) => _context.Update(entity);
    public void Remove(T entity) => _context.Remove(entity);
}

internal static class SpecificationEvaluator<TEntity>
    where TEntity : class
{
    public static IQueryable<TEntity> GetQuery(IQueryable<TEntity> source, ISpecification<TEntity> spec)
    {
        var query = source;

        if (spec.AsNoTracking) query = query.AsNoTracking();
        if (spec.Criteria is not null) query = query.Where(spec.Criteria);

        foreach (var include in spec.Includes) query = query.Include(include);
        foreach (var include in spec.IncludeStrings) query = query.Include(include);

        if (spec.OrderBy is not null) query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending is not null) query = query.OrderByDescending(spec.OrderByDescending);

        if (spec.AsSplitQuery) query = query.AsSplitQuery();

        return query;
    }
}