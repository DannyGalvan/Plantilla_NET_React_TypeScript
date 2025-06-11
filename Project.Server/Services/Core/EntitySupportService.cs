using Project.Server.Interceptors.Interfaces;
using Project.Server.Services.Interfaces;
using Project.Server.Utils;

namespace Project.Server.Services.Core
{
    using Lombok.NET;
    using FluentValidation;
    using Microsoft.Extensions.DependencyInjection;
    using System.Reflection;

    [AllArgsConstructor]
    public partial class EntitySupportService : IEntitySupportService
    {
        private readonly IServiceProvider _serviceProvider;

        public IValidator<TRequest> GetValidator<TRequest>(string key)
        {
            return _serviceProvider.GetRequiredKeyedService<IValidator<TRequest>>(key);
        }

        public IEnumerable<IEntityBeforeCreateInterceptor<TEntity, TRequest>> GetBeforeCreateInterceptors<TEntity, TRequest>()
        {
            return _serviceProvider
                .GetServices<IEntityBeforeCreateInterceptor<TEntity, TRequest>>()
                .OrderBy(i => i.GetType().GetCustomAttribute<OrderAttribute>()?.Priority ?? int.MaxValue);
        }

        public IEnumerable<IEntityAfterCreateInterceptor<TEntity, TRequest>> GetAfterCreateInterceptors<TEntity, TRequest>()
        {
            return _serviceProvider
                .GetServices<IEntityAfterCreateInterceptor<TEntity, TRequest>>()
                .OrderBy(i => i.GetType().GetCustomAttribute<OrderAttribute>()?.Priority ?? int.MaxValue);
        }

        public IEnumerable<IEntityAfterUpdateInterceptor<TEntity, TRequest>> GetAfterUpdateInterceptors<TEntity, TRequest>()
        {
            return _serviceProvider
                .GetServices<IEntityAfterUpdateInterceptor<TEntity, TRequest>>()
                .OrderBy(i => i.GetType().GetCustomAttribute<OrderAttribute>()?.Priority ?? int.MaxValue);
        }

        public IEnumerable<IEntityAfterPartialUpdateInterceptor<TEntity, TRequest>> GetAfterPartialUpdateInterceptors<TEntity, TRequest>()
        {
            return _serviceProvider
                .GetServices<IEntityAfterPartialUpdateInterceptor<TEntity, TRequest>>()
                .OrderBy(i => i.GetType().GetCustomAttribute<OrderAttribute>()?.Priority ?? int.MaxValue);
        }
    }

}