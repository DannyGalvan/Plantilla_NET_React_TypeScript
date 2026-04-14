using FluentValidation.Results;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Server.Entities.Interfaces;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;
using Project.Server.Services.Interfaces;
using Project.Server.Utils;

namespace Project.Server.Controllers
{
    [Authorize]
    [ApiController]
    public abstract class CrudController<TEntity, TRequest, TResponse, TId>(
       IEntityService<TEntity, TRequest, TId> service,
       IMapper mapper)
       : CommonController
       where TEntity : class
       where TRequest : class, IRequest<TId?>
       where TId : struct
    {
        [HttpGet]
        public virtual IActionResult GetAll([FromQuery] QueryParamsRequest query)
        {
            string[]? inc = query.Include?.Split(",");
            var response = service.GetAll(query.Filters, inc, query.PageNumber, query.PageSize, query.IncludeTotal);

            if (response.Success)
            {
                return Ok(new Response<List<TResponse>>
                {
                    Data = mapper.Map<List<TEntity>, List<TResponse>>(response.Data!),
                    Success = response.Success,
                    Message = response.Message,
                    TotalResults = response.TotalResults
                });
            }

            return BadRequest(new Response<List<ValidationFailure>>
            {
                Data = response.Errors,
                Success = response.Success,
                Message = response.Message
            });
        }

        [HttpGet("{id}")]
        public virtual IActionResult Get(TId id, [FromQuery] string? include = null)
        {
            string[]? inc = include?.Split(",");

            var response = service.GetById(id, inc);

            if (response.Success)
            {
                return Ok(new Response<TResponse>
                {
                    Data = mapper.Map<TEntity, TResponse>(response.Data!),
                    Success = true,
                    Message = response.Message
                });
            }

            return BadRequest(new Response<List<ValidationFailure>>
            {
                Data = response.Errors,
                Success = false,
                Message = response.Message
            });
        }

        [HttpPost]
        public virtual IActionResult Create([FromBody] TRequest request)
        {
            AuditHelper.SetCreatedByRecursive(request, GetUserId());

            var response = service.Create(request);

            if (response.Success)
            {
                return Ok(new Response<TResponse>
                {
                    Data = mapper.Map<TEntity, TResponse>(response.Data!),
                    Success = true,
                    Message = response.Message
                });
            }

            return BadRequest(new Response<List<ValidationFailure>>
            {
                Data = response.Errors,
                Success = false,
                Message = response.Message
            });
        }

        [HttpPut]
        public virtual IActionResult Update([FromBody] TRequest request)
        {

            AuditHelper.SetUpdatedByRecursive(request, GetUserId());

            var response = service.Update(request);

            if (response.Success)
            {
                return Ok(new Response<TResponse>
                {
                    Data = mapper.Map<TEntity, TResponse>(response.Data!),
                    Success = true,
                    Message = response.Message
                });
            }

            return BadRequest(new Response<List<ValidationFailure>>
            {
                Data = response.Errors,
                Success = false,
                Message = response.Message
            });
        }

        [HttpPatch]
        public virtual IActionResult PartialUpdate([FromBody] TRequest request)
        {

            AuditHelper.SetUpdatedByRecursive(request, GetUserId());

            var response = service.PartialUpdate(request);

            if (response.Success)
            {
                return Ok(new Response<TResponse>
                {
                    Data = mapper.Map<TEntity, TResponse>(response.Data!),
                    Success = true,
                    Message = response.Message
                });
            }

            return BadRequest(new Response<List<ValidationFailure>>
            {
                Data = response.Errors,
                Success = false,
                Message = response.Message
            });
        }

        [HttpDelete("{id}")]
        public virtual IActionResult Delete(TId id)
        {
            var response = service.Delete(id, GetUserId());

            if (response.Success)
            {
                return Ok(new Response<TResponse>
                {
                    Data = mapper.Map<TEntity, TResponse>(response.Data!),
                    Success = true,
                    Message = response.Message
                });
            }

            return BadRequest(new Response<List<ValidationFailure>>
            {
                Data = response.Errors,
                Success = false,
                Message = response.Message
            });
        }
    }
}