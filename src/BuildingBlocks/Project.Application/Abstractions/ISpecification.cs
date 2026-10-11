using System.Linq.Expressions;

namespace Project.Application.Abstractions;

/// <summary>
/// Composable query filter (criteria + includes + ordering + paging flags).
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Criteria { get; }
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }
    IReadOnlyList<string> IncludeStrings { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    bool AsSplitQuery { get; }
    bool AsNoTracking { get; }
}

public abstract class Specification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];

    public Expression<Func<T, bool>>? Criteria { get; protected set; }
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;
    public IReadOnlyList<string> IncludeStrings => _includeStrings;
    public Expression<Func<T, object>>? OrderBy { get; protected set; }
    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }
    public bool AsSplitQuery { get; protected set; }
    public bool AsNoTracking { get; protected set; } = true;

    protected void AddInclude(Expression<Func<T, object>> include) => _includes.Add(include);
    protected void AddInclude(string include) => _includeStrings.Add(include);
}