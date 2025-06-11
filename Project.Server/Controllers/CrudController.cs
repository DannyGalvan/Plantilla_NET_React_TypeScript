using FluentValidation.Results;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Project.Server.Entities.Interfaces;
using Project.Server.Entities.Response;
using Project.Server.Services.Interfaces;
using Project.Server.Utils;

namespace Project.Server.Controllers
{
    [ApiController]
    public abstract class CrudController<TEntity, TRequest, TResponse, TId> : CommonController
     where TEntity : class
     where TRequest : class, IRequest<TId?>
     where TId : struct
    {
        protected readonly IEntityService<TEntity, TRequest, TId> _service;
        protected readonly IMapper _mapper;

        protected CrudController(IEntityService<TEntity, TRequest, TId> service, IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [HttpGet]
        public virtual IActionResult GetAll([FromQuery] string? filters, string? include = null, int pageNumber = 1, int pageSize = 30, bool includeTotal = false)
        {
            string[]? inc = include?.Split(",");
            var response = _service.GetAll(filters, inc, pageNumber, pageSize, includeTotal);

            if (response.Success)
            {
                return Ok(new Response<List<TResponse>>
                {
                    Data = _mapper.Map<List<TEntity>, List<TResponse>>(response.Data!),
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
        public virtual IActionResult Get(TId id)
        {
            var response = _service.GetById(id);

            if (response.Success)
            {
                return Ok(new Response<TResponse>
                {
                    Data = _mapper.Map<TEntity, TResponse>(response.Data!),
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

            var response = _service.Create(request);

            if (response.Success)
            {
                return Ok(new Response<TResponse>
                {
                    Data = _mapper.Map<TEntity, TResponse>(response.Data!),
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

            var response = _service.Update(request);

            if (response.Success)
            {
                return Ok(new Response<TResponse>
                {
                    Data = _mapper.Map<TEntity, TResponse>(response.Data!),
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

            var response = _service.PartialUpdate(request);

            if (response.Success)
            {
                return Ok(new Response<TResponse>
                {
                    Data = _mapper.Map<TEntity, TResponse>(response.Data!),
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
            var response = _service.Delete(id, GetUserId());

            if (response.Success)
            {
                return Ok(new Response<TResponse>
                {
                    Data = _mapper.Map<TEntity, TResponse>(response.Data!),
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