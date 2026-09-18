using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Project.Server.Utils
{
    public static class QueryableIncludeExtension
    {
        /// <summary>Hard cap on include-path depth (1 segment = direct navigation, 2 = one hop).</summary>
        public const int MaxIncludeDepth = 2;

        /// <summary>Maximum number of includes accepted per request (closes the B13 DoS).</summary>
        public const int MaxIncludesPerRequest = 3;

        public static IQueryable<TEntity> ApplyIncludes<TEntity>(this IQueryable<TEntity> query, string[] includes)
            where TEntity : class
        {
            if (includes is null || includes.Length == 0) return query;
            if (includes.Length > MaxIncludesPerRequest)
            {
                throw new InvalidOperationException(
                    $"Too many includes ({includes.Length}); max is {MaxIncludesPerRequest}.");
            }

            foreach (var include in includes)
            {
                query = query.IncludeNested(include);
            }

            return query;
        }

        private static IQueryable<TEntity> IncludeNested<TEntity>(this IQueryable<TEntity> query, string includePath)
            where TEntity : class
        {
            var parts = includePath.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return query;
            if (parts.Length > MaxIncludeDepth)
            {
                throw new InvalidOperationException(
                    $"Include path '{includePath}' exceeds max depth {MaxIncludeDepth}.");
            }

            Type currentType = typeof(TEntity);
            List<string> validatedParts = new();

            foreach (var part in parts)
            {
                var property = currentType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p => string.Equals(p.Name, part, StringComparison.OrdinalIgnoreCase));

                if (property == null)
                    throw new InvalidOperationException($"La propiedad '{part}' no existe en el tipo '{currentType.Name}'.");

                bool isCollection =
                    property.PropertyType.IsGenericType &&
                    typeof(ICollection<>).IsAssignableFrom(property.PropertyType.GetGenericTypeDefinition());

                bool isNavigationProperty =
                    property.PropertyType.IsClass && property.PropertyType != typeof(string) || isCollection;

                if (!isNavigationProperty)
                    throw new InvalidOperationException($"La propiedad '{part}' en el tipo '{currentType.Name}' no es una propiedad de navegación válida.");

                validatedParts.Add(property.Name);

                currentType = isCollection
                    ? property.PropertyType.GetGenericArguments()[0]
                    : property.PropertyType;
            }

            string validatedIncludePath = string.Join(".", validatedParts);
            return query.Include(validatedIncludePath);
        }
    }
}