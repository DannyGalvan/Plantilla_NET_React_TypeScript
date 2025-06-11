using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Project.Server.Utils
{
    public static class QueryableIncludeExtension
    {
        public static IQueryable<TEntity> ApplyIncludes<TEntity>(this IQueryable<TEntity> query, string[] includes) where TEntity : class
        {
            foreach (var include in includes)
            {
                query = query.IncludeNested<TEntity>(include);
            }

            return query;
        }

        private static IQueryable<TEntity> IncludeNested<TEntity>(this IQueryable<TEntity> query, string includePath)
            where TEntity : class
        {
            var parts = includePath.Split('.');
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
                    (property.PropertyType.IsClass && property.PropertyType != typeof(string)) || isCollection;

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