using System.Collections.Concurrent;
using System.Reflection;

namespace Project.Server.Services.Core
{
    /// <summary>
    /// Allowlist for filter / sort / include operations on a given entity type.
    /// Used by <c>EntityService</c> to prevent the query string from reaching
    /// sensitive columns (Password, RecoveryToken, ...) or arbitrary navigation
    /// paths (closes the B3 extraction oracle and the B13 DoS by deep includes).
    /// </summary>
    public sealed class QueryPolicy<TEntity> where TEntity : class
    {
        public const int MaxIncludeDepth = 2;
        public const int MaxIncludesPerRequest = 3;

        /// <summary>
        /// Property names that can never be filtered, sorted or included directly,
        /// regardless of any other configuration. Lowercased for case-insensitive matching.
        /// </summary>
        public static readonly IReadOnlySet<string> HardDenylist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Password",
            "PasswordHash",
            "RecoveryToken",
            "DateToken",
            "FailedLoginAttempts",
            "LockoutEnd",
            "MustChangePassword",
            "OwnerId",
        };

        private static readonly ConcurrentDictionary<Type, QueryPolicy<TEntity>> Cache = new();

        public static QueryPolicy<TEntity> Instance =>
            Cache.GetOrAdd(typeof(TEntity), _ => new QueryPolicy<TEntity>());

        private readonly Lazy<HashSet<string>> _scalars;

        private QueryPolicy()
        {
            _scalars = new Lazy<HashSet<string>>(BuildScalarNames, isThreadSafe: true);
        }

        private HashSet<string> BuildScalarNames()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (HardDenylist.Contains(p.Name)) continue;
                if (p.GetIndexParameters().Length > 0) continue;
                if (p.PropertyType == typeof(string)) { set.Add(p.Name); continue; }
                if (p.PropertyType.IsPrimitive || p.PropertyType.IsValueType || p.PropertyType.IsEnum) { set.Add(p.Name); continue; }
                if (Nullable.GetUnderlyingType(p.PropertyType) is { } u
                    && (u.IsPrimitive || u.IsValueType || u.IsEnum)) { set.Add(p.Name); continue; }
            }
            return set;
        }

        public bool IsFilterable(string propertyName)
            => !string.IsNullOrWhiteSpace(propertyName) && _scalars.Value.Contains(propertyName);

        public bool IsSortable(string propertyName) => IsFilterable(propertyName);

        public bool IsIncludeable(string includePath, out int depth)
        {
            depth = 0;
            if (string.IsNullOrWhiteSpace(includePath)) return false;

            var parts = includePath.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;
            if (parts.Length > MaxIncludesPerRequest + 1) return false;

            var currentType = typeof(TEntity);
            foreach (var part in parts)
            {
                var prop = currentType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => string.Equals(p.Name, part, StringComparison.OrdinalIgnoreCase));
                if (prop == null) return false;
                if (HardDenylist.Contains(prop.Name)) return false;
                var isCollection = prop.PropertyType.IsGenericType
                    && prop.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>);
                var isNav = (prop.PropertyType.IsClass && prop.PropertyType != typeof(string)) || isCollection;
                if (!isNav) return false;
                depth++;
                currentType = isCollection
                    ? prop.PropertyType.GetGenericArguments()[0]
                    : prop.PropertyType;
            }
            return depth <= MaxIncludeDepth;
        }
    }
}