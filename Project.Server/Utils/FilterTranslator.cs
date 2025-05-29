using System.Linq.Expressions;
using Project.Server.Services.Interfaces;

namespace Project.Server.Utils
{
    /// <summary>
    /// Defines the <see cref="FilterTranslator{TEntity}" />
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class FilterTranslator<TEntity> : IFilterTranslator<TEntity> where TEntity : class
    {
        /// <summary>
        /// The TranslateToEfFilter
        /// </summary>
        /// <param name="sqlQuery">The sqlQuery<see cref="string"/></param>
        /// <returns>The <see>
        ///         <cref>Expression{Func{TEntity, bool}}</cref>
        ///     </see>
        /// </returns>
        public Expression<Func<TEntity, bool>> TranslateToEfFilter(string? sqlQuery)
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
                    var filter = TranslateConditionToEfFilter(orPart);

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
        private Expression<Func<TEntity, bool>> TranslateConditionToEfFilter(string condition)
        {
            // Ejemplo: Name:like:free
            var parts = condition.Split(':');

            string field = parts[0];
            string op = parts[1].ToLower();
            string value = parts[2];

            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var member = Expression.PropertyOrField(parameter, field);

            // Convertir el valor si es necesario (por ejemplo, si es fecha o un número)
            dynamic? convertedValue = Util.SetPropertyValue(typeof(TEntity), field, value);

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
                case "in":
                    var values = value.Split(",");
                    var arrayExpr = Expression.Constant(values);
                    method = typeof(Enumerable).GetMethod("Contains", new[] { typeof(IEnumerable<string>), typeof(string) });
                    comparison = Expression.Call(method!, arrayExpr, memberExpression);
                    break;
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
        private Expression<Func<TEntity, bool>> CombineExpressions(Expression<Func<TEntity, bool>> expr1, Expression<Func<TEntity, bool>> expr2, Func<Expression, Expression, BinaryExpression> combiner)
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
