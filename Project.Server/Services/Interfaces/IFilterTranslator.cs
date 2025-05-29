using System.Linq.Expressions;

namespace Project.Server.Services.Interfaces
{
    /// <summary>
    /// Defines the <see cref="IFilterTranslator{TEntity}" />
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IFilterTranslator<TEntity> where TEntity : class
    {
        /// <summary>
        /// The TranslateToEfFilter
        /// </summary>
        /// <param name="sqlQuery">The sqlQuery<see cref="string"/></param>
        /// <returns>The <see>
        ///         <cref>Expression{Func{TEntity, bool}}</cref>
        ///     </see>
        /// </returns>
        Expression<Func<TEntity, bool>> TranslateToEfFilter(string? sqlQuery);
    }
}
