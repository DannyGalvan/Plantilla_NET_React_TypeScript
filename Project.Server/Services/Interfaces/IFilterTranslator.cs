using System.Linq.Expressions;

namespace Project.Server.Services.Interfaces
{
    /// <summary>
    /// Defines the <see cref="IFilterTranslator" />
    /// </summary>
    public interface IFilterTranslator
    {
        /// <summary>
        /// The TranslateToEfFilter
        /// </summary>
        /// <param name="sqlQuery">The sqlQuery<see cref="string"/></param>
        /// <returns>The <see>
        ///         <cref>Expression{Func{TEntity, bool}}</cref>
        ///     </see>
        /// </returns>
        Expression<Func<TEntity, bool>> TranslateToEfFilter<TEntity>(string? sqlQuery) where TEntity : class;
    }
}