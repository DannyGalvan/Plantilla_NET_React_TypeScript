using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Project.Application.Querying;

/// <summary>
/// Strategy for one filter operator of the DSL. Register a new implementation in DI to add an operator;
/// the parser does not change (open/closed).
/// </summary>
public interface IFilterOperator
{
    /// <summary>Operator token in the DSL (<c>eq</c>, <c>contains</c>...).</summary>
    string Key { get; }

    /// <summary>Builds the predicate. Values must go through <see cref="FilterValue.Parameter"/> so EF parameterizes them.</summary>
    Expression Build(Expression member, string rawValue);
}

/// <summary>Converts raw DSL values and wraps them in closures that EF Core turns into SQL parameters.</summary>
public static class FilterValue
{
    private static readonly MethodInfo ParameterMethod =
        typeof(FilterValue).GetMethod(nameof(Box), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Returns an expression that reads <paramref name="value"/> from a closure object. Unlike
    /// <c>Expression.Constant(value)</c>, EF Core sends it as a parameter (no SQL injection, plan cache friendly).
    /// </summary>
    public static Expression Parameter(object? value, Type type)
    {
        var box = ParameterMethod.MakeGenericMethod(type).Invoke(null, [value])!;
        return Expression.Property(Expression.Constant(box), "Value");
    }

    /// <summary>Parses a raw value into <paramref name="type"/> using invariant culture.</summary>
    /// <exception cref="FormatException">The value cannot be converted.</exception>
    public static object? Convert(string raw, Type type)
    {
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (raw.Equals("null", StringComparison.OrdinalIgnoreCase) && (Nullable.GetUnderlyingType(type) is not null || !type.IsValueType))
        {
            return null;
        }

        if (target == typeof(string))
        {
            return raw;
        }

        if (target == typeof(Guid))
        {
            return Guid.Parse(raw);
        }

        if (target == typeof(bool))
        {
            return bool.Parse(raw);
        }

        if (target.IsEnum)
        {
            return Enum.Parse(target, raw, ignoreCase: true);
        }

        if (target == typeof(DateTimeOffset))
        {
            return DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        }

        if (target == typeof(DateTime))
        {
            return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        }

        if (target == typeof(DateOnly))
        {
            return DateOnly.Parse(raw, CultureInfo.InvariantCulture);
        }

        return System.Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
    }

    private static ValueBox<T> Box<T>(object? value) => new((T)value!);

    private sealed class ValueBox<T>(T value)
    {
        public T Value { get; } = value;
    }
}

public abstract class ComparisonOperator(string key, ExpressionType comparison) : IFilterOperator
{
    public string Key { get; } = key;

    public Expression Build(Expression member, string rawValue)
    {
        var value = FilterValue.Convert(rawValue, member.Type);
        return Expression.MakeBinary(comparison, member, FilterValue.Parameter(value, member.Type));
    }
}

public sealed class EqualOperator() : ComparisonOperator("eq", ExpressionType.Equal);

public sealed class NotEqualOperator() : ComparisonOperator("ne", ExpressionType.NotEqual);

public sealed class GreaterThanOperator() : ComparisonOperator("gt", ExpressionType.GreaterThan);

public sealed class GreaterThanOrEqualOperator() : ComparisonOperator("gte", ExpressionType.GreaterThanOrEqual);

public sealed class LessThanOperator() : ComparisonOperator("lt", ExpressionType.LessThan);

public sealed class LessThanOrEqualOperator() : ComparisonOperator("lte", ExpressionType.LessThanOrEqual);

public abstract class StringMethodOperator(string key, string methodName) : IFilterOperator
{
    private readonly MethodInfo _method = typeof(string).GetMethod(methodName, [typeof(string)])!;

    public string Key { get; } = key;

    public Expression Build(Expression member, string rawValue)
    {
        if (member.Type != typeof(string))
        {
            throw new FormatException($"Operator '{Key}' only applies to text fields.");
        }

        var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
        var call = Expression.Call(member, _method, FilterValue.Parameter(rawValue, typeof(string)));
        return Expression.AndAlso(notNull, call);
    }
}

public sealed class ContainsOperator() : StringMethodOperator("contains", nameof(string.Contains));

public sealed class StartsWithOperator() : StringMethodOperator("startswith", nameof(string.StartsWith));

public sealed class EndsWithOperator() : StringMethodOperator("endswith", nameof(string.EndsWith));

/// <summary><c>field:in:a|b|c</c>.</summary>
public sealed class InOperator : IFilterOperator
{
    public string Key => "in";

    public Expression Build(Expression member, string rawValue)
    {
        var values = rawValue.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (values.Length is 0 or > 100)
        {
            throw new FormatException("Operator 'in' expects between 1 and 100 values separated by '|'.");
        }

        var listType = typeof(List<>).MakeGenericType(member.Type);
        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
        foreach (var raw in values)
        {
            list.Add(FilterValue.Convert(raw, member.Type));
        }

        var contains = listType.GetMethod(nameof(List<object>.Contains))!;
        return Expression.Call(FilterValue.Parameter(list, listType), contains, member);
    }
}

/// <summary><c>field:between:from|to</c> (inclusive).</summary>
public sealed class BetweenOperator : IFilterOperator
{
    public string Key => "between";

    public Expression Build(Expression member, string rawValue)
    {
        var parts = rawValue.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new FormatException("Operator 'between' expects two values separated by '|'.");
        }

        var from = FilterValue.Parameter(FilterValue.Convert(parts[0], member.Type), member.Type);
        var to = FilterValue.Parameter(FilterValue.Convert(parts[1], member.Type), member.Type);
        return Expression.AndAlso(Expression.GreaterThanOrEqual(member, from), Expression.LessThanOrEqual(member, to));
    }
}

/// <summary><c>field:isnull:true|false</c>.</summary>
public sealed class IsNullOperator : IFilterOperator
{
    public string Key => "isnull";

    public Expression Build(Expression member, string rawValue)
    {
        if (member.Type.IsValueType && Nullable.GetUnderlyingType(member.Type) is null)
        {
            throw new FormatException("Operator 'isnull' only applies to nullable fields.");
        }

        var isNull = Expression.Equal(member, Expression.Constant(null, member.Type));
        return bool.Parse(rawValue) ? isNull : Expression.Not(isNull);
    }
}

public static class FilterOperators
{
    /// <summary>Default operator set registered by <c>AddApplicationCore</c>.</summary>
    public static IEnumerable<IFilterOperator> Defaults() =>
    [
        new EqualOperator(),
        new NotEqualOperator(),
        new GreaterThanOperator(),
        new GreaterThanOrEqualOperator(),
        new LessThanOperator(),
        new LessThanOrEqualOperator(),
        new ContainsOperator(),
        new StartsWithOperator(),
        new EndsWithOperator(),
        new InOperator(),
        new BetweenOperator(),
        new IsNullOperator(),
    ];
}
