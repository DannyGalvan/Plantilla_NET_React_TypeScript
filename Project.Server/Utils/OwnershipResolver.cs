using System.Collections.Concurrent;
using System.Linq.Expressions;
using Project.Server.Entities.Interfaces;

namespace Project.Server.Utils
{
    /// <summary>
    /// Builds a cached <c>Expression&lt;Func&lt;TEntity, bool&gt;&gt;</c> that constrains
    /// a query to the rows owned by a specific user id. The same predicate is reused
    /// for <c>GetAllOwnAsync</c>, <c>CountOwnAsync</c> and the owner check inside
    /// <c>GetByIdOwnAsync</c> / <c>UpdateOwnAsync</c> / <c>DeleteOwnAsync</c>.
    /// </summary>
    public static class OwnershipResolver
    {
        private static readonly ConcurrentDictionary<Type, LambdaExpression> Cache = new();

        /// <summary>
        /// Returns the ownership predicate for <typeparamref name="TEntity"/>:
        /// <list type="bullet">
        ///   <item><c>IOwnedEntity&lt;long&gt;.OwnerId == userId</c></item>
        ///   <item>else <c>IEntity&lt;TId&gt;.CreatedBy == userId</c></item>
        /// </list>
        /// Throws <see cref="InvalidOperationException"/> if neither contract applies
        /// — this is meant to fail loudly at startup, not on every request.
        /// </summary>
        public static Expression<Func<TEntity, bool>> Predicate<TEntity>(long userId) where TEntity : class
        {
            return BuildPredicate<TEntity>(userId);
        }

        /// <summary>
        /// Non-generic version for callers that only know <see cref="Type"/> at runtime.
        /// </summary>
        public static LambdaExpression Predicate(Type entityType, long userId)
        {
            return BuildLambda(entityType, userId);
        }

        private static Expression<Func<TEntity, bool>> BuildPredicate<TEntity>(long userId) where TEntity : class
        {
            var lambda = BuildLambda(typeof(TEntity), userId);
            return (Expression<Func<TEntity, bool>>)lambda;
        }

        private static LambdaExpression BuildLambda(Type entityType, long userId)
        {
            var parameter = Expression.Parameter(entityType, "e");

            if (SupportsOwned(entityType))
            {
                var ownerProp = Expression.Property(parameter, "OwnerId");
                var constant = Expression.Constant(userId, typeof(long));
                return Expression.Lambda(Expression.Equal(ownerProp, constant), parameter);
            }

            if (SupportsCreatedBy(entityType))
            {
                var createdByProp = Expression.Property(parameter, "CreatedBy");
                var constant = Expression.Constant(userId, typeof(long));
                return Expression.Lambda(Expression.Equal(createdByProp, constant), parameter);
            }

            throw new InvalidOperationException(
                $"Entity type '{entityType.FullName}' implements neither IOwnedEntity<long> nor " +
                "IEntity<long>; refusing to start. Either expose it only through an admin " +
                "endpoint with explicit [RequireOperation], or implement IOwnedEntity<long>.");
        }

        /// <summary>
        /// True when <typeparamref name="TEntity"/> supports ownership (either via
        /// <c>IOwnedEntity&lt;long&gt;</c> or <c>IEntity&lt;long&gt;</c>). Used by
        /// the startup validator to refuse to expose non-owned entities.
        /// </summary>
        public static bool IsSupported<TEntity>() where TEntity : class
        {
            var t = typeof(TEntity);
            return SupportsOwned(t) || SupportsCreatedBy(t);
        }

        /// <summary>
        /// Non-generic equivalent of <see cref="IsSupported{TEntity}"/> for callers
        /// that only have a runtime <see cref="Type"/> (e.g. <c>OwnershipValidationHostedService</c>).
        /// </summary>
        public static bool IsSupported(Type t)
            => SupportsOwned(t) || SupportsCreatedBy(t);

        private static LambdaExpression Build(Type type)
        {
            // Backward-compat shim — only used by the legacy cache, kept so the
            // type system is happy when callers obtain a non-parameterized lambda
            // for a known entity type. Always uses userId=0 which is the
            // "anonymous" sentinel — production callers must use Predicate<T>(userId).
            return BuildLambda(type, 0L);
        }

        private static bool SupportsOwned(Type type)
        {
            foreach (var i in type.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IOwnedEntity<>))
                    return true;
            }
            return false;
        }

        private static bool SupportsCreatedBy(Type type)
        {
            foreach (var i in type.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>))
                    return true;
            }
            return false;
        }
    }
}