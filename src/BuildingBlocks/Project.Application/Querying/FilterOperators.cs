using System.Linq.Expressions;

namespace Project.Application.Querying;

/// <summary>
/// Default operators. Each builds the predicate using <see cref="Expression.Constant"/>
/// with the captured user value, which EF Core translates into a parameterized
/// SQL parameter — never into a constant in the query text.
/// </summary>
public sealed class EqualOperator : IFilterOperator
{
    public string Key => FilterOperatorKeys.Equal;
    public Expression Build(Expression member, object? value) =>
        Expression.Equal(member, Expression.Constant(value));
}

public sealed class NotEqualOperator : IFilterOperator
{
    public string Key => FilterOperatorKeys.NotEqual;
    public Expression Build(Expression member, object? value) =>
        Expression.NotEqual(member, Expression.Constant(value));
}

public sealed class GreaterThanOperator : IFilterOperator
{
    public string Key => FilterOperatorKeys.GreaterThan;
    public Expression Build(Expression member, object? value) =>
        Expression.GreaterThan(member, Expression.Constant(value));
}

public sealed class GreaterThanOrEqualOperator : IFilterOperator
{
    public string Key => FilterOperatorKeys.GreaterThanOrEqual;
    public Expression Build(Expression member, object? value) =>
        Expression.GreaterThanOrEqual(member, Expression.Constant(value));
}

public sealed class LessThanOperator : IFilterOperator
{
    public string Key => FilterOperatorKeys.LessThan;
    public Expression Build(Expression member, object? value) =>
        Expression.LessThan(member, Expression.Constant(value));
}

public sealed class LessThanOrEqualOperator : IFilterOperator
{
    public string Key => FilterOperatorKeys.LessThanOrEqual;
    public Expression Build(Expression member, object? value) =>
        Expression.LessThanOrEqual(member, Expression.Constant(value));
}

public sealed class ContainsOperator : IFilterOperator
{
    public string Key => FilterOperatorKeys.Contains;
    public Expression Build(Expression member, object? value)
    {
        var toString = Expression.Call(member, "ToString", Type.EmptyTypes);
        var like = Expression.Call(
            toString,
            typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string), typeof(StringComparison) })!,
            Expression.Constant(value?.ToString(), typeof(string)),
            Expression.Constant(StringComparison.OrdinalIgnoreCase, typeof(StringComparison)));
        return like;
    }
}

public sealed class StartsWithOperator : IFilterOperator
{
    public string Key => FilterOperatorKeys.StartsWith;
    public Expression Build(Expression member, object? value)
    {
        var toString = Expression.Call(member, "ToString", Type.EmptyTypes);
        var starts = Expression.Call(
            toString,
            typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string), typeof(StringComparison) })!,
            Expression.Constant(value?.ToString(), typeof(string)),
            Expression.Constant(StringComparison.OrdinalIgnoreCase, typeof(StringComparison)));
        return starts;
    }
}

public sealed class IsNullOperator : IFilterOperator
{
    public string Key => FilterOperatorKeys.IsNull;
    public Expression Build(Expression member, object? value)
    {
        var flag = value is bool b && b;
        return flag
            ? Expression.Equal(member, Expression.Constant(null))
            : Expression.NotEqual(member, Expression.Constant(null));
    }
}