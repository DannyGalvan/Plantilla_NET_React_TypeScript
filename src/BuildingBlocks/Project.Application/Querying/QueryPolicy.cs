using System.Linq.Expressions;

namespace Project.Application.Querying;

/// <summary>A field exposed to the query DSL: its public name and the member it maps to.</summary>
public sealed record QueryField(string Name, LambdaExpression Selector)
{
    public Type Type => Selector.ReturnType;
}

/// <summary>
/// Allowlist of what a list endpoint may filter, sort and include. Anything not declared is rejected
/// with 400, so sensitive columns (password hashes, tokens, tenant ids) can never be probed.
/// </summary>
public interface IQueryPolicy<T>
{
    QueryField? FindFilterable(string name);

    QueryField? FindSortable(string name);

    bool IsIncludable(string path);

    /// <summary>Ordering applied when the request has no <c>sort</c>.</summary>
    (LambdaExpression Selector, bool Descending)? DefaultSort { get; }

    IReadOnlyCollection<string> FilterableFields { get; }

    IReadOnlyCollection<string> SortableFields { get; }
}

/// <summary>
/// Fluent base for query policies:
/// <code>
/// public sealed class ProductQueryPolicy : QueryPolicy&lt;Product&gt;
/// {
///     public ProductQueryPolicy()
///     {
///         Filterable(p =&gt; p.Name).Sortable(p =&gt; p.Name).Filterable(p =&gt; p.Price).Sortable(p =&gt; p.Price);
///         DefaultOrder(p =&gt; p.Name);
///     }
/// }
/// </code>
/// </summary>
public abstract class QueryPolicy<T> : IQueryPolicy<T>
{
    private readonly Dictionary<string, QueryField> _filterable = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, QueryField> _sortable = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _includable = new(StringComparer.OrdinalIgnoreCase);

    public (LambdaExpression Selector, bool Descending)? DefaultSort { get; private set; }

    public IReadOnlyCollection<string> FilterableFields => _filterable.Keys;

    public IReadOnlyCollection<string> SortableFields => _sortable.Keys;

    public QueryField? FindFilterable(string name) => _filterable.GetValueOrDefault(name);

    public QueryField? FindSortable(string name) => _sortable.GetValueOrDefault(name);

    public bool IsIncludable(string path) => _includable.Contains(path);

    public QueryPolicy<T> Filterable<TProperty>(Expression<Func<T, TProperty>> selector, string? name = null)
    {
        var field = new QueryField(name ?? MemberName(selector), selector);
        _filterable[field.Name] = field;
        return this;
    }

    public QueryPolicy<T> Sortable<TProperty>(Expression<Func<T, TProperty>> selector, string? name = null)
    {
        var field = new QueryField(name ?? MemberName(selector), selector);
        _sortable[field.Name] = field;
        return this;
    }

    /// <summary>Shortcut for a field that is both filterable and sortable.</summary>
    public QueryPolicy<T> Field<TProperty>(Expression<Func<T, TProperty>> selector, string? name = null) =>
        Filterable(selector, name).Sortable(selector, name);

    public QueryPolicy<T> Includable(string navigationPath)
    {
        _includable.Add(navigationPath);
        return this;
    }

    public QueryPolicy<T> DefaultOrder<TProperty>(Expression<Func<T, TProperty>> selector, bool descending = false)
    {
        DefaultSort = (selector, descending);
        return this;
    }

    private static string MemberName(LambdaExpression selector)
    {
        var body = selector.Body is UnaryExpression { NodeType: ExpressionType.Convert } unary ? unary.Operand : selector.Body;
        var parts = new Stack<string>();
        while (body is MemberExpression member)
        {
            parts.Push(member.Member.Name);
            body = member.Expression!;
        }

        if (parts.Count == 0)
        {
            throw new ArgumentException("The selector must be a member access such as x => x.Name.", nameof(selector));
        }

        var name = string.Join('.', parts);
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
