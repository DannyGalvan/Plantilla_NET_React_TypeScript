using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using Project.SharedKernel.Domain;

namespace Project.Application.Abstractions;

/// <summary>
/// Composable query description: criteria, includes, ordering and paging.
/// Evaluated by the infrastructure (EF Core) without leaking EF types into the application layer.
/// </summary>
public interface ISpecification<T>
{
    IReadOnlyList<Expression<Func<T, bool>>> Criteria { get; }

    IReadOnlyList<string> Includes { get; }

    IReadOnlyList<(LambdaExpression KeySelector, bool Descending)> Orderings { get; }

    int? Skip { get; }

    int? Take { get; }

    bool AsNoTracking { get; }

    bool AsSplitQuery { get; }

    /// <summary>When true, soft-deleted rows are visible. Tenant and organization filters still apply.</summary>
    bool IncludeDeleted { get; }
}

/// <summary>Base specification with fluent builders.</summary>
public class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, bool>>> _criteria = [];
    private readonly List<string> _includes = [];
    private readonly List<(LambdaExpression, bool)> _orderings = [];

    public IReadOnlyList<Expression<Func<T, bool>>> Criteria => _criteria;

    public IReadOnlyList<string> Includes => _includes;

    public IReadOnlyList<(LambdaExpression KeySelector, bool Descending)> Orderings => _orderings;

    public int? Skip { get; private set; }

    public int? Take { get; private set; }

    public bool AsNoTracking { get; private set; } = true;

    public bool AsSplitQuery { get; private set; }

    public bool IncludeDeleted { get; private set; }

    public Specification<T> Where(Expression<Func<T, bool>> criteria)
    {
        _criteria.Add(criteria);
        return this;
    }

    public Specification<T> Include(string navigationPath)
    {
        _includes.Add(navigationPath);
        return this;
    }

    public Specification<T> OrderBy(LambdaExpression keySelector, bool descending = false)
    {
        _orderings.Add((keySelector, descending));
        return this;
    }

    public Specification<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector, bool descending = false) =>
        OrderBy((LambdaExpression)keySelector, descending);

    public Specification<T> Page(int skip, int take)
    {
        Skip = skip;
        Take = take;
        return this;
    }

    public Specification<T> Tracked()
    {
        AsNoTracking = false;
        return this;
    }

    public Specification<T> SplitQuery()
    {
        AsSplitQuery = true;
        return this;
    }

    public Specification<T> WithDeleted()
    {
        IncludeDeleted = true;
        return this;
    }

    /// <summary>Copy without paging, used to compute totals.</summary>
    public Specification<T> WithoutPaging()
    {
        var copy = new Specification<T> { AsNoTracking = AsNoTracking, IncludeDeleted = IncludeDeleted };
        copy._criteria.AddRange(_criteria);
        return copy;
    }
}

/// <summary>Repository for aggregate roots. Reads and writes share the module DbContext.</summary>
public interface IRepository<T, in TId>
    where T : AggregateRoot<TId>
    where TId : notnull
{
    IUnitOfWork UnitOfWork { get; }

    Task<T?> GetByIdAsync(TId id, CancellationToken ct);

    Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken ct);

    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken ct);

    Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        ISpecification<T> specification,
        Func<IQueryable<T>, IQueryable<TResult>> projection,
        CancellationToken ct);

    Task<int> CountAsync(ISpecification<T> specification, CancellationToken ct);

    Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken ct);

    void Add(T entity);

    void Update(T entity);

    void Remove(T entity);
}

/// <summary>Persists every pending change of a module in one atomic operation.</summary>
public interface IUnitOfWork
{
    /// <exception cref="ConcurrencyConflictException">The row changed since it was read.</exception>
    /// <exception cref="UniqueConstraintException">A unique index was violated.</exception>
    Task<int> SaveChangesAsync(CancellationToken ct);

    Task<ITransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct);
}

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);

    Task RollbackAsync(CancellationToken ct);
}

/// <summary>Raised when an optimistic concurrency check fails.</summary>
public sealed class ConcurrencyConflictException(Exception? inner = null)
    : Exception("The resource was modified by another request.", inner);

/// <summary>Raised when a unique constraint is violated.</summary>
public sealed class UniqueConstraintException(Exception? inner = null)
    : Exception("A resource with the same unique values already exists.", inner);

/// <summary>
/// Raw SQL for high performance reads, stored procedures and bulk operations (Dapper).
/// Shares the module connection and transaction when EF has one open, so both paths are atomic.
/// </summary>
public interface ISqlExecutor
{
    /// <summary>Dialect of the current connection, used to quote identifiers and build portable SQL.</summary>
    ISqlDialect Dialect { get; }

    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? args, CancellationToken ct);

    Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? args, CancellationToken ct);

    Task<int> ExecuteAsync(string sql, object? args, CancellationToken ct);

    Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedure, object? args, CancellationToken ct);

    Task<int> ExecuteProcedureAsync(string procedure, object? args, CancellationToken ct);
}

/// <summary>Typed executor bound to the DbContext of a module (resolved as <c>ISqlExecutor&lt;CatalogModule&gt;</c>).</summary>
public interface ISqlExecutor<TModule> : ISqlExecutor;

/// <summary>Plain ADO.NET connections for code that does not need EF at all.</summary>
public interface IDbConnectionFactory
{
    /// <summary>Creates a closed connection for the module, honoring the tenant connection when applicable.</summary>
    DbConnection Create(string moduleName);
}

/// <summary>Engine-specific SQL syntax.</summary>
public interface ISqlDialect
{
    string Name { get; }

    string QuoteIdentifier(string identifier);

    /// <summary>Fully qualified table name, honoring schemas or table prefixes depending on the engine.</summary>
    string Table(string schema, string table);

    /// <summary>Boolean literal for <c>true</c> or <c>false</c>.</summary>
    string Boolean(bool value);

    /// <summary>Builds the command used to invoke a stored procedure or function.</summary>
    (string CommandText, CommandType CommandType) Procedure(string schema, string name, IReadOnlyList<string> parameterNames);
}
