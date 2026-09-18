using System.Linq.Expressions;

namespace Project.Server.Services.Interfaces
{
    /// <summary>
    /// Read-side extension point: lets a derived service inject additional
    /// predicates into <c>GetAll*</c> / <c>GetById*</c> / <c>Count*</c> calls.
    /// Foundation for multi-tenant scoping that doesn't fit the
    /// <c>IOwnedEntity</c> model.
    /// </summary>
    public interface IEntityQueryFilter<TEntity> where TEntity : class
    {
        Expression<Func<TEntity, bool>>? Apply();
    }
}