using System.Linq.Expressions;

namespace Project.Application.Querying;

/// <summary>
/// Strategy for a single filter operator. Registered by key (eq, ne, gt, ...).
/// Adding a new operator is a single registration, no edits to the
/// translator (OCP). Values are passed as closures so EF parameterizes them.
/// </summary>
public interface IFilterOperator
{
    string Key { get; }

    /// <summary>Builds a predicate that compares <paramref name="member"/> against <paramref name="value"/>.</summary>
    Expression Build(Expression member, object? value);
}

public static class FilterOperatorKeys
{
    public const string Equal = "eq";
    public const string NotEqual = "ne";
    public const string GreaterThan = "gt";
    public const string GreaterThanOrEqual = "gte";
    public const string LessThan = "lt";
    public const string LessThanOrEqual = "lte";
    public const string Contains = "contains";
    public const string StartsWith = "startswith";
    public const string In = "in";
    public const string Between = "between";
    public const string IsNull = "isnull";
}