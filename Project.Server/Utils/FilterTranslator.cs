using System.Linq.Expressions;
using System.Reflection;
using Project.Server.Services.Interfaces;

namespace Project.Server.Utils
{
    /// <summary>
    /// Defines the <see cref="FilterTranslator" />
    /// </summary>
    public class FilterTranslator : IFilterTranslator
    {
        /// <summary>
        /// The TranslateToEfFilter
        /// </summary>
        /// <param name="sqlQuery">The sqlQuery<see cref="string"/></param>
        /// <returns>The <see>
        ///         <cref>Expression{Func{TEntity, bool}}</cref>
        ///     </see>
        /// </returns>
        public Expression<Func<TEntity, bool>> TranslateToEfFilter<TEntity>(string? sqlQuery) where TEntity : class
        {
            if (string.IsNullOrEmpty(sqlQuery))
            {
                return x => true;
            }

            string[] separatorAnd = { " AND " };

            var andParts = sqlQuery.Split(separatorAnd, StringSplitOptions.RemoveEmptyEntries);

            var andFilters = new List<Expression<Func<TEntity, bool>>>();

            string[] separatorOr = { " OR " };

            foreach (var andPart in andParts)
            {
                var orParts = andPart.Split(separatorOr, StringSplitOptions.RemoveEmptyEntries);

                var orFilters = new List<Expression<Func<TEntity, bool>>>();

                foreach (var orPart in orParts)
                {
                    var filter = TranslateConditionToEfFilter<TEntity>(orPart);

                    orFilters.Add(filter);
                }

                var orCombined = orFilters.Aggregate((acc, next) => CombineExpressions(acc, next, Expression.OrElse));

                andFilters.Add(orCombined);
            }

            var combinedAndFilter = andFilters.Aggregate((acc, next) => CombineExpressions(acc, next, Expression.AndAlso));
            return combinedAndFilter;
        }

        /// <summary>
        /// The TranslateConditionToEfFilter
        /// </summary>
        /// <param name="condition">The condition<see cref="string"/></param>
        /// <returns>The <see>
        ///         <cref>Expression{Func{TEntity, bool}}</cref>
        ///     </see>
        /// </returns>
        private Expression<Func<TEntity, bool>> TranslateConditionToEfFilter<TEntity>(string condition)
        {
            // Ejemplo: Name:like:free
            var parts = condition.Split(':');

            string field = parts[0];
            string op = parts[1].ToLower();
            string value = parts[2];

            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var member = Expression.PropertyOrField(parameter, field);

            // Convertir el valor si es necesario (por ejemplo, si es fecha o un número)
            dynamic? convertedValue = "";

            if (op != "in" && op != "notin") 
            {
                convertedValue = Util.SetPropertyValue(typeof(TEntity), field, value);
            }
            else
            {
                convertedValue = Util.SetPropertyValue(typeof(TEntity), field, value.Split(",")[0]);
            }

                // Convertir el valor a una constante
                var constant = Expression.Constant(convertedValue, member.Type);

            // Asegurarse de que los tipos sean compatibles (nullable vs no nullable)
            Expression comparison;
            Expression memberExpression = member;
            Expression constantExpression = constant;

            // Si el tipo del miembro es nullable pero el valor no lo es, hacemos la conversión
            if (memberExpression.Type != constantExpression.Type)
            {
                if (Nullable.GetUnderlyingType(memberExpression.Type) != null)
                {
                    // Convertir el valor a Nullable<T> si es necesario
                    constantExpression = Expression.Convert(constantExpression, memberExpression.Type);
                }
                else
                {
                    // Convertir el miembro a su tipo subyacente si es necesario
                    memberExpression = Expression.Convert(memberExpression, constantExpression.Type);
                }
            }

            // Generar la expresión de comparación
            switch (op)
            {
                case "eq":
                    comparison = Expression.Equal(memberExpression, constantExpression);
                    break;
                case "ne":
                    comparison = Expression.NotEqual(memberExpression, constantExpression);
                    break;
                case "gt":
                    comparison = Expression.GreaterThan(memberExpression, constantExpression);
                    break;
                case "lt":
                    comparison = Expression.LessThan(memberExpression, constantExpression);
                    break;
                case "like":
                    var method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    comparison = Expression.Call(memberExpression, method!, constantExpression);
                    break;
                case "notin":
                {
                    var values = value.Split(',');

                    // Obtener tipo real del miembro
                    Type targetType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;

                    // Convertir todos los valores
                    var typedValues = Array.CreateInstance(targetType, values.Length);
                    for (int i = 0; i < values.Length; i++)
                    {
                        var converted = Util.SetPropertyValue(typeof(TEntity), field, values[i]);
                        typedValues.SetValue(converted, i);
                    }

                    var arrayExpr = Expression.Constant(typedValues, typedValues.GetType());

                    var containsMethod = typeof(Enumerable)
                        .GetMethods(BindingFlags.Static | BindingFlags.Public)
                        .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(targetType);

                    if (member.Type != targetType)
                        memberExpression = Expression.Convert(member, targetType);

                    // Aquí está la diferencia con "in"
                    var containsCall = Expression.Call(containsMethod, arrayExpr, memberExpression);
                    comparison = Expression.Not(containsCall);
                    break;
                }
                case "in":
                {
                    var values = value.Split(',');

                    // Obtener tipo real de la propiedad
                    Type targetType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;

                    // Convertir cada valor al tipo correcto
                    var typedValues = Array.CreateInstance(targetType, values.Length);
                    for (int i = 0; i < values.Length; i++)
                    {
                        var converted = Util.SetPropertyValue(typeof(TEntity), field, values[i]);
                        typedValues.SetValue(converted, i);
                    }

                    // Crear Expression.Constant con el array fuertemente tipado
                    var arrayExpr = Expression.Constant(typedValues, typedValues.GetType());

                    // Obtener método Enumerable.Contains<T>
                    var containsMethod = typeof(Enumerable)
                        .GetMethods(BindingFlags.Static | BindingFlags.Public)
                        .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(targetType);

                    // Asegurar que el miembro tenga el tipo correcto
                    if (member.Type != targetType)
                        memberExpression = Expression.Convert(member, targetType);

                    comparison = Expression.Call(containsMethod, arrayExpr, memberExpression);
                    break;
                }

                default:
                    throw new ArgumentException($"Operador no soportado: {op}");
            }

            return Expression.Lambda<Func<TEntity, bool>>(comparison, parameter);
        }

        /// <summary>
        /// The CombineExpressions
        /// </summary>
        /// <param name="expr1">The expr1<see>
        ///         <cref>Expression{Func{TEntity, bool}}</cref>
        ///     </see>
        /// </param>
        /// <param name="expr2">The expr2<see>
        ///         <cref>Expression{Func{TEntity, bool}}</cref>
        ///     </see>
        /// </param>
        /// <param name="combiner">The combiner<see cref="Func{Expression, Expression, BinaryExpression}"/></param>
        /// <returns>The <see>
        ///         <cref>Expression{Func{TEntity, bool}}</cref>
        ///     </see>
        /// </returns>
        private Expression<Func<TEntity, bool>> CombineExpressions<TEntity>(Expression<Func<TEntity, bool>> expr1, Expression<Func<TEntity, bool>> expr2, Func<Expression, Expression, BinaryExpression> combiner)
        {
            var parameter = Expression.Parameter(typeof(TEntity));
            var body = combiner(
            Expression.Invoke(expr1, parameter),
            Expression.Invoke(expr2, parameter)
            );

            return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
        }
    }
}