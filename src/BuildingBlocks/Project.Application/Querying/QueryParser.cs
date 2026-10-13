using System.Linq.Expressions;
using Project.SharedKernel;

namespace Project.Application.Querying;

/// <summary>
/// Translates the URL query DSL into expressions, validating every field against the <see cref="IQueryPolicy{T}"/>.
/// <list type="bullet">
///   <item><c>filter=name:contains:pro;price:gte:10</c> (conditions joined with AND).</item>
///   <item><c>sort=name,-createdAt</c> (a leading <c>-</c> means descending).</item>
/// </list>
/// </summary>
public sealed class QueryParser(IEnumerable<IFilterOperator> operators)
{
    public const int MaxConditions = 20;

    private readonly Dictionary<string, IFilterOperator> _operators =
        operators.ToDictionary(o => o.Key, StringComparer.OrdinalIgnoreCase);

    public Result<Expression<Func<T, bool>>?> ParseFilter<T>(string? filter, IQueryPolicy<T> policy)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return Result<Expression<Func<T, bool>>?>.Success(null);
        }

        var conditions = filter.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (conditions.Length > MaxConditions)
        {
            return Invalid<Expression<Func<T, bool>>?>("filter", $"At most {MaxConditions} conditions are allowed.");
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? body = null;

        foreach (var condition in conditions)
        {
            var parts = condition.Split(':', 3);
            if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace))
            {
                return Invalid<Expression<Func<T, bool>>?>("filter", $"Malformed condition '{condition}'. Use field:operator:value.");
            }

            var field = policy.FindFilterable(parts[0]);
            if (field is null)
            {
                return Invalid<Expression<Func<T, bool>>?>("filter", $"Field '{parts[0]}' cannot be filtered.");
            }

            if (!_operators.TryGetValue(parts[1], out var @operator))
            {
                return Invalid<Expression<Func<T, bool>>?>("filter", $"Operator '{parts[1]}' is not supported.");
            }

            Expression predicate;
            try
            {
                predicate = @operator.Build(Rebind(field.Selector, parameter), parts[2]);
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
            {
                return Invalid<Expression<Func<T, bool>>?>("filter", $"Invalid value for field '{field.Name}'.");
            }

            body = body is null ? predicate : Expression.AndAlso(body, predicate);
        }

        return Result<Expression<Func<T, bool>>?>.Success(Expression.Lambda<Func<T, bool>>(body!, parameter));
    }

    public static Result<IReadOnlyList<(LambdaExpression Selector, bool Descending)>> ParseSort<T>(string? sort, IQueryPolicy<T> policy)
    {
        var result = new List<(LambdaExpression, bool)>();
        if (string.IsNullOrWhiteSpace(sort))
        {
            if (policy.DefaultSort is { } fallback)
            {
                result.Add(fallback);
            }

            return Result<IReadOnlyList<(LambdaExpression, bool)>>.Success(result);
        }

        foreach (var segment in sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var descending = segment.StartsWith('-');
            var name = descending || segment.StartsWith('+') ? segment[1..] : segment;
            var field = policy.FindSortable(name);
            if (field is null)
            {
                return Invalid<IReadOnlyList<(LambdaExpression, bool)>>("sort", $"Field '{name}' cannot be sorted.");
            }

            result.Add((field.Selector, descending));
        }

        return Result<IReadOnlyList<(LambdaExpression, bool)>>.Success(result);
    }

    private static Expression Rebind(LambdaExpression selector, ParameterExpression parameter) =>
        new ParameterReplacer(selector.Parameters[0], parameter).Visit(selector.Body);

    private static Result<TValue> Invalid<TValue>(string field, string message) =>
        Result<TValue>.Failure(Error.Validation(field, message));

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}
