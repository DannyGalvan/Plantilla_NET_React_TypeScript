using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Project.Application.Abstractions;
using Project.SharedKernel.Domain;

namespace Project.Infrastructure.Persistence;

/// <summary>Generic repository over a module DbContext.</summary>
public class EfRepository<TContext, T, TId>(TContext context) : IRepository<T, TId>
    where TContext : ModuleDbContext
    where T : AggregateRoot<TId>
    where TId : notnull
{
    protected TContext Context { get; } = context;

    public IUnitOfWork UnitOfWork { get; } = new EfUnitOfWork<TContext>(context);

    public Task<T?> GetByIdAsync(TId id, CancellationToken ct) => Context.Set<T>().FindAsync([id], ct).AsTask();

    public Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken ct) =>
        SpecificationEvaluator.Apply(Context.Set<T>(), specification).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken ct) =>
        await SpecificationEvaluator.Apply(Context.Set<T>(), specification).ToListAsync(ct);

    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        ISpecification<T> specification,
        Func<IQueryable<T>, IQueryable<TResult>> projection,
        CancellationToken ct) =>
        await projection(SpecificationEvaluator.Apply(Context.Set<T>(), specification)).ToListAsync(ct);

    public Task<int> CountAsync(ISpecification<T> specification, CancellationToken ct) =>
        SpecificationEvaluator.Apply(Context.Set<T>(), specification, applyPaging: false, applyOrdering: false).CountAsync(ct);

    public Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken ct) =>
        SpecificationEvaluator.Apply(Context.Set<T>(), specification, applyPaging: false, applyOrdering: false).AnyAsync(ct);

    public void Add(T entity) => Context.Set<T>().Add(entity);

    public void Update(T entity) => Context.Set<T>().Update(entity);

    public void Remove(T entity) => Context.Set<T>().Remove(entity);
}

/// <summary>Unit of work bound to one module DbContext. Translates provider errors into application exceptions.</summary>
public sealed class EfUnitOfWork<TContext>(TContext context) : IUnitOfWork
    where TContext : ModuleDbContext
{
    public async Task<int> SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            return await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            throw new UniqueConstraintException(ex);
        }
    }

    public async Task<ITransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct) =>
        new EfTransaction(await context.Database.BeginTransactionAsync(isolationLevel, ct));

    private sealed class EfTransaction(IDbContextTransaction transaction) : ITransaction
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);

        public Task RollbackAsync(CancellationToken ct) => transaction.RollbackAsync(ct);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}

/// <summary>Detects unique index violations across engines without referencing every driver exception type.</summary>
public static class DbErrors
{
    public static bool IsUniqueViolation(DbUpdateException exception)
    {
        for (Exception? current = exception.InnerException; current is not null; current = current.InnerException)
        {
            var type = current.GetType().Name;
            var number = current.GetType().GetProperty("Number")?.GetValue(current);
            var sqlState = current.GetType().GetProperty("SqlState")?.GetValue(current) as string;

            if ((type == "SqlException" && number is 2601 or 2627)
                || sqlState == "23505"
                || (type == "MySqlException" && number is 1062))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>Translates an <see cref="ISpecification{T}"/> into an EF Core query.</summary>
public static class SpecificationEvaluator
{
    private static readonly MethodInfo OrderByMethod = QueryableMethod(nameof(Queryable.OrderBy));
    private static readonly MethodInfo OrderByDescendingMethod = QueryableMethod(nameof(Queryable.OrderByDescending));
    private static readonly MethodInfo ThenByMethod = QueryableMethod(nameof(Queryable.ThenBy));
    private static readonly MethodInfo ThenByDescendingMethod = QueryableMethod(nameof(Queryable.ThenByDescending));

    public static IQueryable<T> Apply<T>(
        IQueryable<T> source,
        ISpecification<T> specification,
        bool applyPaging = true,
        bool applyOrdering = true)
        where T : class
    {
        var query = source;

        if (specification.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters([QueryFilters.SoftDelete]);
        }

        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        foreach (var criteria in specification.Criteria)
        {
            query = query.Where(criteria);
        }

        foreach (var include in specification.Includes)
        {
            query = query.Include(include);
        }

        if (specification.AsSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        if (applyOrdering)
        {
            var first = true;
            foreach (var (selector, descending) in specification.Orderings)
            {
                var method = (first, descending) switch
                {
                    (true, false) => OrderByMethod,
                    (true, true) => OrderByDescendingMethod,
                    (false, false) => ThenByMethod,
                    (false, true) => ThenByDescendingMethod,
                };

                query = (IQueryable<T>)method.MakeGenericMethod(typeof(T), selector.ReturnType)
                    .Invoke(null, [query, selector])!;
                first = false;
            }
        }

        if (applyPaging)
        {
            if (specification.Skip is { } skip)
            {
                query = query.Skip(skip);
            }

            if (specification.Take is { } take)
            {
                query = query.Take(take);
            }
        }

        return query;
    }

    private static MethodInfo QueryableMethod(string name) =>
        typeof(Queryable).GetMethods()
            .Single(m => m.Name == name && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2);

    internal static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right) =>
        Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, Expression.Invoke(right, left.Parameters)), left.Parameters);
}
