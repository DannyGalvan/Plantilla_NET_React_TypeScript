using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Project.Server.Interceptors.Interfaces;
using Project.Server.Services.Interfaces;
using Project.Server.Utils;

namespace Project.Server.Services.Core
{
    using Lombok.NET;

    [AllArgsConstructor]
    public partial class EntitySupportService : IEntitySupportService
    {
        private readonly IServiceProvider _serviceProvider;

        public IValidator<TRequest> GetValidator<TRequest>(string key)
        {
            return _serviceProvider.GetRequiredKeyedService<IValidator<TRequest>>(key);
        }

        public IEnumerable<IEntityBeforeCreateInterceptor<TEntity, TRequest>> GetBeforeCreateInterceptors<TEntity, TRequest>()
            => ResolveOrdered<IEntityBeforeCreateInterceptor<TEntity, TRequest>>();

        public IEnumerable<IEntityAfterCreateInterceptor<TEntity, TRequest>> GetAfterCreateInterceptors<TEntity, TRequest>()
            => ResolveOrdered<IEntityAfterCreateInterceptor<TEntity, TRequest>>();

        public IEnumerable<IEntityBeforeUpdateInterceptor<TEntity, TRequest>> GetBeforeUpdateInterceptors<TEntity, TRequest>()
            => ResolveOrdered<IEntityBeforeUpdateInterceptor<TEntity, TRequest>>();

        public IEnumerable<IEntityAfterUpdateInterceptor<TEntity, TRequest>> GetAfterUpdateInterceptors<TEntity, TRequest>()
            => ResolveOrdered<IEntityAfterUpdateInterceptor<TEntity, TRequest>>();

        public IEnumerable<IEntityBeforePartialUpdateInterceptor<TEntity, TRequest>> GetBeforePartialUpdateInterceptors<TEntity, TRequest>()
            => ResolveOrdered<IEntityBeforePartialUpdateInterceptor<TEntity, TRequest>>();

        public IEnumerable<IEntityAfterPartialUpdateInterceptor<TEntity, TRequest>> GetAfterPartialUpdateInterceptors<TEntity, TRequest>()
            => ResolveOrdered<IEntityAfterPartialUpdateInterceptor<TEntity, TRequest>>();

        public IEnumerable<IEntityBeforeDeleteInterceptor<TEntity, TRequest>> GetBeforeDeleteInterceptors<TEntity, TRequest>()
            => ResolveOrdered<IEntityBeforeDeleteInterceptor<TEntity, TRequest>>();

        public IEnumerable<IEntityAfterDeleteInterceptor<TEntity, TRequest>> GetAfterDeleteInterceptors<TEntity, TRequest>()
            => ResolveOrdered<IEntityAfterDeleteInterceptor<TEntity, TRequest>>();

        public IEnumerable<IEntityQueryFilter<TEntity>> GetQueryFilters<TEntity>() where TEntity : class
            => _serviceProvider.GetServices<IEntityQueryFilter<TEntity>>();

        private IEnumerable<T> ResolveOrdered<T>() where T : class
        {
            return _serviceProvider
                .GetServices<T>()
                .Where(i => i is not null)
                .Select(i => i!)
                .OrderBy(i => i!.GetType().GetCustomAttribute<OrderAttribute>()?.Priority ?? int.MaxValue);
        }
    }
}