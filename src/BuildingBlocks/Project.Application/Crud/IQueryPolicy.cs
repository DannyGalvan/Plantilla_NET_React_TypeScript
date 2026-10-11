using System.Linq.Expressions;

namespace Project.Application.Crud;

/// <summary>
/// Allowlist of fields a query may filter on, sort by, or include. Replaces the
/// reflection-based denylist in the legacy <c>QueryPolicy</c>. Any property not
/// listed here returns 400 ProblemDetails. Failing closed by missing field is
/// verified by an architecture test in Fase 5.
/// </summary>
public interface IQueryPolicy<T>
{
    IReadOnlyCollection<string> FilterableFields { get; }
    IReadOnlyCollection<string> SortableFields { get; }
    IReadOnlyCollection<string> IncludablePaths { get; }

    bool IsFilterable(string field);
    bool IsSortable(string field);
    bool IsIncludable(string path);

    LambdaExpression GetSortExpression(string field);
}

public abstract class QueryPolicy<T> : IQueryPolicy<T>
{
    private readonly Dictionary<string, LambdaExpression> _sortExpressions = new(StringComparer.OrdinalIgnoreCase);

    protected abstract IReadOnlyCollection<string> DefineFilterables();
    protected abstract IReadOnlyCollection<string> DefineSortables();
    protected abstract IReadOnlyCollection<string> DefineIncludables();

    public IReadOnlyCollection<string> FilterableFields => DefineFilterables();
    public IReadOnlyCollection<string> SortableFields => DefineSortables();
    public IReadOnlyCollection<string> IncludablePaths => DefineIncludables();

    public bool IsFilterable(string field) => FilterableFields.Contains(field);
    public bool IsSortable(string field) => SortableFields.Contains(field);
    public bool IsIncludable(string path) => IncludablePaths.Contains(path);

    public LambdaExpression GetSortExpression(string field) =>
        _sortExpressions.TryGetValue(field, out var expr)
            ? expr
            : throw new InvalidOperationException($"Field '{field}' is not sortable.");

    protected void RegisterSort(string name, LambdaExpression expression) =>
        _sortExpressions[name] = expression;
}