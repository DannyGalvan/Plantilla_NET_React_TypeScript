using FluentValidation;
using Project.Server.Interceptors.Interfaces;

namespace Project.Server.Services.Interfaces
{
    public interface IEntitySupportService
    {
        IValidator<TRequest> GetValidator<TRequest>(string key);
        IEnumerable<IEntityBeforeCreateInterceptor<TEntity, TRequest>> GetBeforeCreateInterceptors<TEntity, TRequest>();
        IEnumerable<IEntityAfterCreateInterceptor<TEntity, TRequest>> GetAfterCreateInterceptors<TEntity, TRequest>();
        IEnumerable<IEntityAfterUpdateInterceptor<TEntity, TRequest>> GetAfterUpdateInterceptors<TEntity, TRequest>();
        IEnumerable<IEntityAfterPartialUpdateInterceptor<TEntity, TRequest>> GetAfterPartialUpdateInterceptors<TEntity, TRequest>();
    }
}
