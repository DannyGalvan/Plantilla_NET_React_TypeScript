using Project.SharedKernel;
using Project.SharedKernel.Domain;

namespace Project.Application.Abstractions;

/// <summary>
/// Repository for aggregate roots. Reads and writes use the same DbContext
/// (and therefore the same transaction).
/// </summary>
public interface IRepository<T, TId>
    where T : AggregateRoot<TId>
    where TId : notnull
{
    Task<T?> GetByIdAsync(TId id, CancellationToken ct);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken ct);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken ct);
    Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken ct);

    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}

/// <summary>
/// Read-only repository with projection support (Mapperly) and keyset/offset paging.
/// </summary>
public interface IReadRepository<T>
    where T : class
{
    Task<PagedResult<TDto>> PageAsync<TDto>(ISpecification<T> specification, PageRequest page, CancellationToken ct)
        where TDto : class;
}

/// <summary>
/// Unit of Work: persists all changes since the last SaveChanges within a transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<ITransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken ct);
}

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}