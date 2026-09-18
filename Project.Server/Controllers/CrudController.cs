using FluentValidation.Results;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Server.Entities.Interfaces;
using Project.Server.Entities.Request;
using Project.Server.Entities.Response;
using Project.Server.Security.Authorization;
using Project.Server.Services.Interfaces;
using Project.Server.Utils;

namespace Project.Server.Controllers
{
    /// <summary>
    /// Generic CRUD base controller. Phase 1 rewrite:
    /// <list type="bullet">
    ///   <item>All endpoints are async and propagate <see cref="CancellationToken"/>.</item>
    ///   <item>Every action carries <see cref="RequireOperationAttribute"/> so the
    ///         startup fail-closed convention in <c>AuthorizationConfiguration</c>
    ///         can't be bypassed by forgetting to decorate a derived controller.</item>
    ///   <item>Adds the <c>/me/...</c> scoped-to-current-user endpoints that the
    ///         plan requires (closes B1 — IDOR).</item>
    ///   <item>Maps <see cref="ResponseStatus"/> onto HTTP codes so a forbidden
    ///         row is <c>403</c>, not <c>400</c>.</item>
    /// </list>
    /// </summary>
    [Authorize]
    [ApiController]
    public abstract class CrudController<TEntity, TRequest, TResponse, TId>(
        IEntityService<TEntity, TRequest, TId> service,
        IMapper mapper)
        : CommonController
        where TEntity : class, IEntity<TId>
        where TRequest : class, IRequest<TId?>
        where TId : struct
    {
        // The base controller routes use no controller prefix; the derived
        // controllers apply [Route("api/v1/[controller]")].

        // -- Admin / general endpoints (require explicit operation) ----------

        [HttpGet]
        [RequireOperation]
        public virtual async Task<IActionResult> GetAll([FromQuery] QueryParamsRequest query, CancellationToken ct = default)
        {
            var options = new QueryOptions
            {
                Filters = query.Filters,
                Includes = query.Include?.Split(','),
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                IncludeTotal = query.IncludeTotal,
            };
            var response = await service.GetAllAsync(options, ct);
            return ProjectListResponse(response);
        }

        [HttpGet("count")]
        [RequireOperation]
        public virtual async Task<IActionResult> Count([FromQuery] string? filters = null, CancellationToken ct = default)
        {
            var response = await service.CountAsync(filters, ct);
            return StatusCode(StatusCodeFor(response.Status), response);
        }

        [HttpGet("{id}")]
        [RequireOperation]
        public virtual async Task<IActionResult> Get(TId id, [FromQuery] string? include = null, CancellationToken ct = default)
        {
            var response = await service.GetByIdAsync(id, include?.Split(','), ct);
            return ProjectSingleResponse(response);
        }

        [HttpPost]
        [RequireOperation]
        public virtual async Task<IActionResult> Create([FromBody] TRequest request, CancellationToken ct = default)
        {
            AuditHelper.SetCreatedByRecursive(request, GetUserId());
            var response = await service.CreateAsync(request, ct);
            return ProjectSingleResponse(response);
        }

        [HttpPut]
        [RequireOperation]
        public virtual async Task<IActionResult> Update([FromBody] TRequest request, CancellationToken ct = default)
        {
            AuditHelper.SetUpdatedByRecursive(request, GetUserId());
            var response = await service.UpdateAsync(request, ct);
            return ProjectSingleResponse(response);
        }

        [HttpPatch]
        [RequireOperation]
        public virtual async Task<IActionResult> PartialUpdate([FromBody] TRequest request, CancellationToken ct = default)
        {
            AuditHelper.SetUpdatedByRecursive(request, GetUserId());
            var response = await service.PartialUpdateAsync(request, ct);
            return ProjectSingleResponse(response);
        }

        [HttpDelete("{id}")]
        [RequireOperation]
        public virtual async Task<IActionResult> Delete(TId id, CancellationToken ct = default)
        {
            var response = await service.DeleteAsync(id, ct);
            return ProjectSingleResponse(response);
        }

        [HttpPost("{id}/restore")]
        [RequireOperation]
        public virtual async Task<IActionResult> Restore(TId id, CancellationToken ct = default)
        {
            var response = await service.RestoreAsync(id, ct);
            return ProjectSingleResponse(response);
        }

        // -- Scoped to the current user (closes B1 — IDOR) -------------------

        [HttpGet("me")]
        [RequireOperation]
        public virtual async Task<IActionResult> GetAllOwn([FromQuery] QueryParamsRequest query, CancellationToken ct = default)
        {
            var options = new QueryOptions
            {
                Filters = query.Filters,
                Includes = query.Include?.Split(','),
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                IncludeTotal = query.IncludeTotal,
            };
            var response = await service.GetAllOwnAsync(options, ct);
            return ProjectListResponse(response);
        }

        [HttpGet("me/count")]
        [RequireOperation]
        public virtual async Task<IActionResult> CountOwn([FromQuery] string? filters = null, CancellationToken ct = default)
        {
            var response = await service.CountOwnAsync(filters, ct);
            return StatusCode(StatusCodeFor(response.Status), response);
        }

        [HttpGet("me/{id}")]
        [RequireOperation]
        public virtual async Task<IActionResult> GetOwn(TId id, [FromQuery] string? include = null, CancellationToken ct = default)
        {
            var response = await service.GetByIdOwnAsync(id, include?.Split(','), ct);
            return ProjectSingleResponse(response);
        }

        [HttpPost("me")]
        [RequireOperation]
        public virtual async Task<IActionResult> CreateOwn([FromBody] TRequest request, CancellationToken ct = default)
        {
            AuditHelper.SetCreatedByRecursive(request, GetUserId());
            var response = await service.CreateOwnAsync(request, ct);
            return ProjectSingleResponse(response);
        }

        [HttpPut("me")]
        [RequireOperation]
        public virtual async Task<IActionResult> UpdateOwn([FromBody] TRequest request, CancellationToken ct = default)
        {
            AuditHelper.SetUpdatedByRecursive(request, GetUserId());
            var response = await service.UpdateOwnAsync(request, ct);
            return ProjectSingleResponse(response);
        }

        [HttpPatch("me")]
        [RequireOperation]
        public virtual async Task<IActionResult> PartialUpdateOwn([FromBody] TRequest request, CancellationToken ct = default)
        {
            AuditHelper.SetUpdatedByRecursive(request, GetUserId());
            var response = await service.PartialUpdateOwnAsync(request, ct);
            return ProjectSingleResponse(response);
        }

        [HttpDelete("me/{id}")]
        [RequireOperation]
        public virtual async Task<IActionResult> DeleteOwn(TId id, CancellationToken ct = default)
        {
            var response = await service.DeleteOwnAsync(id, ct);
            return ProjectSingleResponse(response);
        }

        // ----------------------------------------------------------------

        private IActionResult ProjectSingleResponse(Response<TEntity, List<ValidationFailure>> response)
        {
            if (response.Status == ResponseStatus.Ok && response.Data is not null)
            {
                return StatusCode(StatusCodes.Status200OK, new Response<TResponse>
                {
                    Success = true,
                    Message = response.Message,
                    Data = mapper.Map<TEntity, TResponse>(response.Data),
                    TotalResults = response.TotalResults,
                });
            }

            return StatusCode(StatusCodeFor(response.Status), new Response<List<ValidationFailure>>
            {
                Success = response.Success,
                Message = response.Message,
                Data = response.Errors,
            });
        }

        private IActionResult ProjectListResponse(Response<List<TEntity>, List<ValidationFailure>> response)
        {
            if (response.Status == ResponseStatus.Ok && response.Data is not null)
            {
                return StatusCode(StatusCodes.Status200OK, new Response<List<TResponse>>
                {
                    Success = true,
                    Message = response.Message,
                    Data = mapper.Map<List<TEntity>, List<TResponse>>(response.Data),
                    TotalResults = response.TotalResults,
                });
            }

            return StatusCode(StatusCodeFor(response.Status), new Response<List<ValidationFailure>>
            {
                Success = response.Success,
                Message = response.Message,
                Data = response.Errors,
            });
        }
    }
}