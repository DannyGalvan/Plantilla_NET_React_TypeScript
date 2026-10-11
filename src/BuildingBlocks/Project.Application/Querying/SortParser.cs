using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Project.Application.Querying;

/// <summary>
/// Parses <c>field,-field2</c> into ordered <see cref="LambdaExpression"/>
/// expressions. Reflection is cached.
/// </summary>
public sealed class SortParser<T>
{
    private readonly ConcurrentDictionary<string, LambdaExpression> _cache = new();

    public IReadOnlyList<(LambdaExpression KeySelector, bool Descending)> Parse(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort)) return [];

        var result = new List<(LambdaExpression, bool)>();
        foreach (var segment in sort.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var descending = segment.StartsWith('-');
            var field = descending ? segment[1..] : segment;
            var lambda = _cache.GetOrAdd(field, BuildKeySelector);
            result.Add((lambda, descending));
        }

        return result;
    }

    private LambdaExpression BuildKeySelector(string field)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression current = parameter;
        foreach (var segment in field.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var prop = current.Type.GetProperty(segment)
                ?? throw new ArgumentException($"Property '{segment}' is not sortable on type '{typeof(T).Name}'.");
            current = Expression.Property(current, prop);
        }
        return Expression.Lambda(current, parameter);
    }
}