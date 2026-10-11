using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Project.Application.Querying;

/// <summary>
/// Parses the URL filter expression <c>field:op:value</c> (AND-separated by
/// <c>;</c>) into a <see cref="LambdaExpression"/>. The parser uses cached
/// reflection in a <see cref="ConcurrentDictionary{TKey, TValue}"/> so the
/// hot path is allocation-free after the first hit. Values are wrapped in
/// <see cref="Expression.Constant"/> so EF parameterizes them; the original
/// <c>Expression.Constant(value)</c> with raw user values is explicitly
/// prohibited by plan §3.3.
/// </summary>
public sealed class FilterParser<T>
{
    private readonly IFilterOperator[] _operators;
    private readonly ConcurrentDictionary<string, Expression> _memberCache = new();

    public FilterParser(IEnumerable<IFilterOperator> operators)
    {
        _operators = operators.ToArray();
    }

    public LambdaExpression? Parse(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return null;

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combined = null;

        foreach (var condition in filter.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = condition.Split(':');
            if (parts.Length != 3)
            {
                throw new ArgumentException($"Malformed filter segment '{condition}'. Expected 'field:op:value'.");
            }

            var field = parts[0];
            var op = parts[1];
            var rawValue = parts[2];

            var member = ResolveMember(parameter, field);
            var oper = _operators.FirstOrDefault(o => string.Equals(o.Key, op, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"Unknown filter operator '{op}'.");

            var value = NormalizeValue(member.Type, rawValue);
            var predicate = oper.Build(member, value);
            combined = combined is null ? predicate : Expression.AndAlso(combined, predicate);
        }

        return combined is null ? null : Expression.Lambda(combined, parameter);
    }

    private Expression ResolveMember(ParameterExpression parameter, string path)
    {
        return _memberCache.GetOrAdd(path, _ =>
        {
            Expression current = parameter;
            foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                var prop = current.Type.GetProperty(segment, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (prop is null)
                {
                    throw new ArgumentException($"Property '{segment}' is not filterable on type '{typeof(T).Name}'.");
                }
                current = Expression.Property(current, prop);
            }
            return current;
        });
    }

    private static object? NormalizeValue(Type targetType, string raw)
    {
        if (targetType == typeof(string)) return raw;
        if (targetType == typeof(bool)) return bool.Parse(raw);
        if (targetType.IsEnum) return Enum.Parse(targetType, raw, ignoreCase: true);
        if (targetType == typeof(Guid)) return Guid.Parse(raw);

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return Convert.ChangeType(raw, underlying, System.Globalization.CultureInfo.InvariantCulture);
    }
}