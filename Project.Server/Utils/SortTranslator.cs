using System.Linq.Expressions;

namespace Project.Server.Utils
{
    /// <summary>
    /// Translates a validated <c>SortBy</c> string into a typed key selector.
    /// The caller supplies the allowlist predicate so this service stays
    /// entity-agnostic.
    /// </summary>
    public interface ISortTranslator
    {
        /// <summary>
        /// Returns a key-selector lambda when <paramref name="sortBy"/> is allowed
        /// according to <paramref name="isAllowed"/>; otherwise <c>false</c>.
        /// The caller applies <c>OrderBy</c> or <c>OrderByDescending</c> based on the
        /// desired direction.
        /// </summary>
        bool TryBuild<TEntity>(
            string? sortBy,
            Func<string, bool> isAllowed,
            out LambdaExpression? keySelector) where TEntity : class;
    }

    public sealed class SortTranslator : ISortTranslator
    {
        public bool TryBuild<TEntity>(
            string? sortBy,
            Func<string, bool> isAllowed,
            out LambdaExpression? keySelector) where TEntity : class
        {
            keySelector = null;
            if (string.IsNullOrWhiteSpace(sortBy)) return false;
            if (!isAllowed(sortBy)) return false;

            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var property = Expression.Property(parameter, sortBy);
            var delegateType = typeof(Func<,>).MakeGenericType(typeof(TEntity), property.Type);
            keySelector = Expression.Lambda(delegateType, property, parameter);
            return true;
        }
    }
}