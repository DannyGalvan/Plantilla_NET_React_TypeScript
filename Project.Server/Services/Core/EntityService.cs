using FluentValidation;
using FluentValidation.Results;
using Lombok.NET;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using Project.Server.Entities.Interfaces;
using Project.Server.Services.Interfaces;
using Project.Server.Utils;
using Project.Server.Entities.Response;
using Project.Server.Interceptors.Interfaces;
using Project.Server.Context;

namespace SoluEmpleo.Server.Services.Core
{
    /// <summary>
    /// Defines the <see cref="EntityService{TEntity, TRequest, TId}" />
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TId"></typeparam>
    [AllArgsConstructor]
    public partial class EntityService<TEntity, TRequest, TId> : IEntityService<TEntity, TRequest, TId> where TEntity : class, IEntity<TId>
    {
        /// <summary>
        /// Defines the _mapper
        /// </summary>
        private readonly IMapper _mapper;

        /// <summary>
        /// Defines the _logger
        /// </summary>
        private readonly ILogger<EntityService<TEntity, TRequest, TId>> _logger;

        /// <summary>
        /// Defines the _db
        /// </summary>
        private readonly DataContext _db;

        /// <summary>
        /// Defines the _serviceProvider
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Defines the _filterTranslator
        /// </summary>
        private readonly IFilterTranslator<TEntity> _filterTranslator;

        /// <summary>
        /// The GetAll
        /// </summary>
        /// <param name="filters">The filters<see cref="string"/></param>
        /// <param name="thenInclude">The thenInclude<see cref="bool"/></param>
        /// <param name="pageNumber">The pageNumber<see cref="int"/></param>
        /// <param name="pageSize">The pageSize<see cref="int"/></param>
        /// <returns>The <see>
        ///         <cref>Response{List{TEntity}, List{ValidationFailure}}</cref>
        ///     </see>
        /// </returns>
        public Response<List<TEntity>, List<ValidationFailure>> GetAll(string? filters, bool? thenInclude, int pageNumber = 1, int pageSize = 30)
        {
            Response<List<TEntity>, List<ValidationFailure>> response = new();

            try
            {
                IQueryable<TEntity> query = _db.Set<TEntity>();

                if (!string.IsNullOrEmpty(filters))
                {
                    var filterExpression = _filterTranslator.TranslateToEfFilter(filters);
                    query = query.Where(filterExpression);
                }

                if (thenInclude ?? false)
                {
                    var properties = typeof(TEntity).GetProperties()
                        .Where(p => p.GetGetMethod()!.IsVirtual && p.PropertyType.IsClass);

                    query = properties.Aggregate(query, (current, property) => current.Include(property.Name));
                }

                query = query.OrderByDescending(e => e.CreatedAt);

                int totalRecords = query.Count();

                int skip = (pageNumber - 1) * pageSize;
                query = query.Skip(skip).Take(pageSize);

                var entities = query.ToList();

                response.Errors = null;
                response.Data = entities;
                response.TotalResults = totalRecords;
                response.Success = true;
                response.Message = $"Entities {typeof(TEntity).Name} retrieved successfully";

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Errors = [new ValidationFailure("Id", ex.Message)];
                response.Data = null;

                _logger.LogError(ex, "Error al obtener {entity} : {message}", typeof(TEntity).Name, ex.Message);

                return response;
            }
        }

        /// <summary>
        /// The GetById
        /// </summary>
        /// <param name="id">The id<see cref="TId"/></param>
        /// <returns>The <see>
        ///         <cref>Response{TEntity, List{ValidationFailure}}</cref>
        ///     </see>
        /// </returns>
        public Response<TEntity, List<ValidationFailure>> GetById(TId id)
        {
            Response<TEntity, List<ValidationFailure>> response = new();

            try
            {
                var entity = _db.Set<TEntity>().Find(id);

                if (entity == null)
                {
                    response.Success = false;
                    response.Message = $"Entity {typeof(TEntity).Name} not found";
                    response.Errors = [new ValidationFailure("Id", "Entity not found")];
                    response.Data = null;

                    return response;
                }

                response.Errors = null;
                response.Data = entity;
                response.Success = true;
                response.Message = $"Entity {typeof(TEntity).Name} retrieved successfully";

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Errors = [new ValidationFailure("Id", ex.Message)];
                response.Data = null;

                _logger.LogError(ex, "Error al obtener {entity} : {message}", typeof(TEntity).Name, ex.Message);

                return response;
            }
        }

        /// <summary>
        /// The Creation
        /// </summary>
        /// <param name="model">The model<see cref="TRequest"/></param>
        /// <returns>The <see>
        ///         <cref>Response{TEntity, List{ValidationFailure}}</cref>
        ///     </see>
        /// </returns>
        public Response<TEntity, List<ValidationFailure>> Create(TRequest model)
        {
            Response<TEntity, List<ValidationFailure>> response = new();

            string userId = string.Empty;

            try
            {
                var results = GetValidator("Create").Validate(model);

                if (!results.IsValid)
                {
                    response.Success = false;
                    response.Message = "Validation failed";
                    response.Errors = results.Errors;
                    response.Data = null;

                    return response;
                }

                var entity = _mapper.Map<TEntity>(model!);

                foreach (var interceptor in GetBeforeCreateInterceptors())
                {
                    if (!response.Success) return response;

                    response.Data = entity;

                    response = interceptor.Execute(response, model);

                    entity = response.Data!;
                }

                if (!response.Success) return response;

                userId = entity.CreatedBy.ToString();

                entity.CreatedAt = DateTime.Now;
                entity.UpdatedAt = null;
                entity.UpdatedBy = null;

                var database = _db.Set<TEntity>();

                database.Add(entity);
                _db.SaveChanges();

                response.Errors = null;
                response.Data = entity;
                response.Success = true;
                response.Message = $"Entity {typeof(TEntity).Name} created successfully";

                if (!response.Success) return response;

                foreach (var interceptor in GetAfterCreateInterceptors())
                {
                    if (!response.Success) return response;

                    response = interceptor.Execute(response, model);
                }

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Errors = [new ValidationFailure("Id", ex.Message)];
                response.Data = null;

                _logger.LogError(ex, "Error al crear {entity} : usuario {user} : {message}", typeof(TEntity).Name, userId, ex.Message);

                return response;
            }
        }

        /// <summary>
        /// The Update
        /// </summary>
        /// <param name="model">The model<see cref="TRequest"/></param>
        /// <returns>The <see>
        ///         <cref>Response{TEntity, List{ValidationFailure}}</cref>
        ///     </see>
        /// </returns>
        public Response<TEntity, List<ValidationFailure>> Update(TRequest model)
        {
            Response<TEntity, List<ValidationFailure>> response = new();

            string userId = string.Empty;

            try
            {
                var results = GetValidator("Update").Validate(model);

                if (!results.IsValid)
                {
                    response.Success = false;
                    response.Message = "Validation failed";
                    response.Errors = results.Errors;
                    response.Data = null;

                    return response;
                }

                TEntity entity = _mapper.Map<TEntity>(model!);

                var database = _db.Set<TEntity>();

                var parameter = Expression.Parameter(typeof(TEntity), "x");
                var member = Expression.PropertyOrField(parameter, "Id");
                var constant = Expression.Constant(entity.Id);
                var condition = Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(member, constant), parameter);

                TEntity? prevState = database.AsNoTracking().FirstOrDefault(condition);

                if (prevState == null)
                {
                    response.Success = false;
                    response.Message = $"Entity {typeof(TEntity).Name} not found";
                    response.Errors = [new ValidationFailure("Id", $"Entity {typeof(TEntity).Name} not found")];
                    response.Data = null;

                    return response;
                }

                TEntity entityToUpdate = _mapper.Map<TEntity>(prevState);

                userId = entity.CreatedBy.ToString();

                DateTime createdAt = entityToUpdate.CreatedAt;

                Util.UpdateProperties(entityToUpdate, entity);

                entityToUpdate.UpdatedAt = DateTime.Now;
                entityToUpdate.CreatedAt = createdAt;

                database.Entry(entityToUpdate).State = EntityState.Detached;

                database.Update(entityToUpdate);
                _db.SaveChanges();

                response.Errors = null;
                response.Data = entityToUpdate;
                response.Success = true;
                response.Message = $"Entity {typeof(TEntity).Name} updated successfully";

                if (!response.Success) return response;

                foreach (var interceptor in GetAfterUpdateInterceptors())
                {
                    if (!response.Success) return response;

                    response = interceptor.Execute(response, model, prevState);
                }

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Errors = [new ValidationFailure("Id", ex.Message)];
                response.Data = null;

                _logger.LogError(ex, "Error al actualizar {entity} : usuario {user} : {message}", typeof(TEntity).Name, userId, ex.Message);

                return response;
            }
        }

        /// <summary>
        /// The PartialUpdate
        /// </summary>
        /// <param name="model">The model<see cref="TRequest"/></param>
        /// <returns>The <see>
        ///         <cref>Response{TEntity, List{ValidationFailure}}</cref>
        ///     </see>
        /// </returns>
        public Response<TEntity, List<ValidationFailure>> PartialUpdate(TRequest model)
        {
            Response<TEntity, List<ValidationFailure>> response = new();

            string userId = string.Empty;

            try
            {
                var results = GetValidator("Partial").Validate(model);

                if (!results.IsValid)
                {
                    response.Success = false;
                    response.Message = "Validation failed";
                    response.Errors = results.Errors;
                    response.Data = null;

                    return response;
                }

                TEntity entity = _mapper.Map<TEntity>(model!);

                var database = _db.Set<TEntity>();

                var parameter = Expression.Parameter(typeof(TEntity), "x");
                var member = Expression.PropertyOrField(parameter, "Id");
                var constant = Expression.Constant(entity.Id);
                var condition = Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(member, constant), parameter);

                TEntity? prevState = database.AsNoTracking().FirstOrDefault(condition);

                if (prevState == null)
                {
                    response.Success = false;
                    response.Message = $"Entity {typeof(TEntity).Name} not found";
                    response.Errors = [new ValidationFailure("Id", $"Entity {typeof(TEntity).Name} not found")];
                    response.Data = null;

                    return response;
                }

                TEntity entityToUpdate = _mapper.Map<TEntity>(prevState);

                userId = entity.CreatedBy.ToString();

                DateTime createdAt = entityToUpdate.CreatedAt;
                Util.UpdateProperties(entityToUpdate, entity);
                entityToUpdate.UpdatedAt = DateTime.Now;
                entityToUpdate.CreatedAt = createdAt;

                database.Update(entityToUpdate);
                _db.SaveChanges();

                response.Errors = null;
                response.Data = entityToUpdate;
                response.Success = true;
                response.Message = $"Entity {typeof(TEntity).Name} updated successfully";
                response.Errors = results.Errors;

                if (!response.Success) return response;

                foreach (var interceptor in GetAfterPartialUpdateInterceptors())
                {
                    if (!response.Success) return response;

                    response = interceptor.Execute(response, model, prevState);
                }

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Errors = [new ValidationFailure("Id", ex.Message)];
                response.Data = null;

                _logger.LogError(ex, "Error al actualizar parcial {entity} : usuario {user} : {message}", typeof(TEntity).Name, userId, ex.Message);

                return response;
            }
        }

        /// <summary>
        /// The Delete
        /// </summary>
        /// <param name="id">The id<see cref="TId"/></param>
        /// <param name="deletedBy">The deletedBy<see cref="long"/></param>
        /// <returns>The <see>
        ///         <cref>Response{TEntity, List{ValidationFailure}}</cref>
        ///     </see>
        /// </returns>
        public Response<TEntity, List<ValidationFailure>> Delete(TId id, long deletedBy)
        {
            Response<TEntity, List<ValidationFailure>> response = new();

            string userId = string.Empty;

            try
            {
                response.Success = false;
                response.Message = "Invalid Id";
                response.Errors = [new ValidationFailure("Id", "Invalid Id")];
                response.Data = null;

                if (!Util.HasValidId(id)) return response;

                var parameter = Expression.Parameter(typeof(TEntity), "x");
                var member = Expression.PropertyOrField(parameter, "Id");
                var constant = Expression.Constant(id);
                var condition = Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(member, constant), parameter);

                TEntity? entity = _db.Set<TEntity>().AsNoTracking().FirstOrDefault(condition);

                if (entity == null)
                {
                    response.Success = false;
                    response.Message = $"Entity {typeof(TEntity).Name} not found";
                    response.Errors = [new ValidationFailure("Id", $"Entity {typeof(TEntity).Name} not found")];
                    response.Data = null;

                    return response;
                }

                userId = entity.CreatedBy.ToString();

                entity.UpdatedAt = DateTime.Now;
                entity.State = 0;
                entity.UpdatedBy = deletedBy;

                _db.Set<TEntity>().Update(entity);
                _db.SaveChanges();

                response.Errors = null;
                response.Data = entity;
                response.Success = true;
                response.Message = $"Entity {typeof(TEntity).Name} deleted successfully";

                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                response.Errors = [new ValidationFailure("Id", ex.Message)];
                response.Data = null;

                _logger.LogError(ex, "Error al eliminar {entity} : usuario {user} : {message}", typeof(TEntity).Name, userId, ex.Message);

                return response;
            }
        }

        /// <summary>
        /// The GetValidator
        /// </summary>
        /// <param name="key">The key<see cref="string"/></param>
        /// <returns>The <see cref="IValidator{TRequest}"/></returns>
        private IValidator<TRequest> GetValidator(string key)
        {
            return _serviceProvider.GetRequiredKeyedService<IValidator<TRequest>>(key);
        }

        /// <summary>
        /// The GetAfterCreateInterceptors
        /// </summary>
        /// <returns>The <see>
        ///         <cref>IEnumerable{IEntityAfterCreateInterceptor{TEntity, TRequest}}</cref>
        ///     </see>
        /// </returns>
        private IEnumerable<IEntityAfterCreateInterceptor<TEntity, TRequest>> GetAfterCreateInterceptors()
        {
            return _serviceProvider.GetServices<IEntityAfterCreateInterceptor<TEntity, TRequest>>().OrderBy(i => i.GetType().GetCustomAttribute<OrderAttribute>()?.Priority ?? int.MaxValue);
        }

        /// <summary>
        /// The GetBeforeCreateInterceptors
        /// </summary>
        /// <returns>The <see>
        ///         <cref>IEnumerable{IEntityBeforeCreateInterceptor{TEntity, TRequest}}</cref>
        ///     </see>
        /// </returns>
        private IEnumerable<IEntityBeforeCreateInterceptor<TEntity, TRequest>> GetBeforeCreateInterceptors()
        {
            return _serviceProvider.GetServices<IEntityBeforeCreateInterceptor<TEntity, TRequest>>().OrderBy(i => i.GetType().GetCustomAttribute<OrderAttribute>()?.Priority ?? int.MaxValue);
        }

        /// <summary>
        /// The GetAfterUpdateInterceptors
        /// </summary>
        /// <returns>The <see>
        ///         <cref>IEnumerable{IEntityAfterUpdateInterceptor{TEntity, TRequest}}</cref>
        ///     </see>
        /// </returns>
        private IEnumerable<IEntityAfterUpdateInterceptor<TEntity, TRequest>> GetAfterUpdateInterceptors()
        {
            return _serviceProvider.GetServices<IEntityAfterUpdateInterceptor<TEntity, TRequest>>().OrderBy(i => i.GetType().GetCustomAttribute<OrderAttribute>()?.Priority ?? int.MaxValue);
        }

        /// <summary>
        /// The GetAfterPartialUpdateInterceptors
        /// </summary>
        /// <returns>The <see>
        ///         <cref>IEnumerable{IEntityAfterPartialUpdateInterceptor{TEntity, TRequest}}</cref>
        ///     </see>
        /// </returns>
        private IEnumerable<IEntityAfterPartialUpdateInterceptor<TEntity, TRequest>> GetAfterPartialUpdateInterceptors()
        {
            return _serviceProvider.GetServices<IEntityAfterPartialUpdateInterceptor<TEntity, TRequest>>().OrderBy(i => i.GetType().GetCustomAttribute<OrderAttribute>()?.Priority ?? int.MaxValue);
        }
    }
}
